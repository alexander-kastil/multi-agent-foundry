# Tune Hybrid Search, Reranking and Routing

A retrieval step that works on your first test question often fails on the second: the exact code is missed, or the synonym is. In this demo you measure keyword, vector and hybrid search on a small clinical knowledge set instead of trusting one example, add a reranker, route questions across three sources and see how chunking changes what the model receives. You finish by running hybrid search with the semantic ranker on a real Azure AI Search index.

The knowledge set is fictional Northwind Health data: a drug formulary, clinical guidelines and lab references, held as JSON in `data/`.

## Prerequisites

- A Microsoft Foundry project with `gpt-5.4` and `text-embedding-3-small` deployments, and `az login` completed.
- For Step 6 only: an Azure AI Search service with role-based access, a managed identity with the Cognitive Services OpenAI User role on the Foundry resource, and your user in the Search Service Contributor and Search Index Data Contributor roles. The `provision.azcli` from the previous topic sets this up.
- Python 3.12 or later, or the .NET 10 SDK for the C# project.

## Run it

```bash
cd demos/03-agent-service/05-advanced-rag/advanced-rag-py
python -m venv .venv
.venv/Scripts/pip install -r requirements.txt
cp .env.example .env
.venv/Scripts/python hybrid_demo.py
.venv/Scripts/python routing_demo.py
.venv/Scripts/python chunking_demo.py
.venv/Scripts/python azure_search_hybrid.py
```

The C# project in `advanced-rag-cs/` has one command per script:

```bash
cd demos/03-agent-service/05-advanced-rag/advanced-rag-cs
cp .env.example .env
dotnet run -- hybrid
dotnet run -- routing
dotnet run -- chunking
dotnet run -- search
```

Set the five values in `.env` first. Steps 1 to 5 use a small local index (`local_index.py`) that implements BM25, cosine search over Foundry embeddings and RRF, so you can see each mechanism. Embeddings are cached in `.cache/` after the first run.

## Step 1: Compare keyword, vector and hybrid search

The first scene runs four queries in all three modes. Two are exact identifiers and two are synonyms.

```python
for source, query in SCENE_QUERIES:
    for mode in ("keyword", "vector", "hybrid"):
        hits = indexes[source].search(query, mode=mode, vector_weight=SOURCE_WEIGHTS[source], top=3)
        print(f"  {mode:<8}", " | ".join(h["title"] for h in hits) or "no results")
```

```csharp
foreach (var (source, query) in SceneQueries)
    foreach (string mode in new[] { "keyword", "vector", "hybrid" })
    {
        List<Hit> hits = await indexes[source].Search(query, mode, SourceWeights[source], top: 3);
        Console.WriteLine($"  {mode,-8} {(hits.Count > 0 ? string.Join(" | ", hits.Select(hit => hit.Title)) : "no results")}");
    }
```

The `LocalIndex` class in `LocalIndex.cs` is the C# twin of `local_index.py`: the same tokenizer, BM25, cosine search and RRF with `k = 60`. Embeddings come from an `OpenAIClient` that targets the Foundry resource's `/openai/v1/` route, because the project route returns 404 for embeddings.

Expected output (trimmed):

```text
=== 1. Keyword vs vector vs hybrid (hybrid uses the per-source weights tuned in scene 2) ===

[clinical_guidelines] heart attack
  keyword  no results
  vector   Acute coronary syndrome pathway | Atrial fibrillation management | Chest pain with shortness of breath assessment
  hybrid   Acute coronary syndrome pathway | Atrial fibrillation management | Chest pain with shortness of breath assessment

[drug_formulary] NDC 00071-0156-23
  keyword  Atorvastatin 40 mg tablets
  vector   Salbutamol inhaler 100 mcg | Insulin glargine 100 units/mL | Metformin 500 mg tablets
  hybrid   Atorvastatin 40 mg tablets | Salbutamol inhaler 100 mcg | Insulin glargine 100 units/mL
```

Keyword search finds the exact code and finds nothing for "heart attack". Vector search finds the heart pathway and loses the code. Hybrid keeps both wins.

## Step 2: Tune the vector weight per source

One weight does not fit every source. The script scores each mode by MRR over six labeled queries per source and sweeps the vector weight.

```python
for w in WEIGHTS:
    row.append(sum(reciprocal_rank(indexes[source], q, e, mode="hybrid", vector_weight=w) for q, e in pairs) / len(pairs))
```

```csharp
foreach (double w in Weights)
    row += $"{await MeanReciprocalRank(pairs, indexes[source], "hybrid", w),-8:0.00}";
```

Expected output:

```text
=== 2. Tune the vector weight per knowledge source (MRR over labeled queries) ===
source                w=0.3   w=0.7   w=1.0   w=1.5   keyword  vector
drug_formulary        1.00    1.00    1.00    1.00    1.00     0.72
clinical_guidelines   0.67    0.67    0.67    0.75    0.42     1.00
```

The formulary is full of codes and brand names, so keyword is already perfect there and vector alone scores 0.72. The guidelines are written in clinical prose, so vector alone scores 1.00 and keyword 0.42. In this small set, pushing the vector weight up helps the guidelines the most, and the weight that wins is a property of the source.

## Step 3: Rerank with an LLM

The last scene takes the hybrid candidates from both sources for one question and asks the model to score each from 1 to 10.

```python
response = openai_client.responses.create(
    model=MODEL,
    input=RERANK_PROMPT.format(query=RERANK_QUERY, title=candidate["title"], content=candidate["content"]),
)
candidate["rerank_score"] = float(response.output_text.strip())
```

```csharp
string prompt = string.Format(RerankPrompt, Labeled.RerankQuery, candidate.Title, candidate.Content);
string answer = (await Models.Responses.CreateResponseAsync(prompt)).Value.GetOutputText();
rated.Add((candidate, double.Parse(answer.Trim(), CultureInfo.InvariantCulture)));
```

Expected output (scores vary between runs):

```text
=== 3. LLM-as-reranker on the hybrid candidates ===
Query: What should I monitor after starting an ACE inhibitor in a patient with diabetes?
hybrid order : Lisinopril 10 mg tablets | Hypertension in chronic kidney disease | Metformin 500 mg tablets | Type 2 diabetes treatment algorithm | Atrial fibrillation management
reranked top3: Hypertension in chronic kidney disease (8) | Lisinopril 10 mg tablets (7) | Metformin 500 mg tablets (1)
```

The reranker promotes the guideline about monitoring kidney function above the drug label, which fits the question. It also shows the limit: it can only reorder the five candidates it received.

## Step 4: Route questions across three sources

`routing_demo.py` asks the model for a probability per source, searches the sources above 0.7 in parallel, normalizes scores and merges. Five questions cover a clear match, a two-source question and a question that fits no source.

```python
def choose_sources(probabilities, unavailable):
    ranked = sorted(probabilities, key=probabilities.get, reverse=True)
    chosen = [s for s in ranked if probabilities[s] > HIGH]
    if not chosen:
        chosen = [s for s in ranked if probabilities[s] > LOW] if max(probabilities.values()) > LOW else list(ranked)
    available = [s for s in chosen if s not in unavailable]
    return available or [s for s in SOURCES if s not in unavailable]
```

```csharp
Dictionary<string, double> probabilities = await Classify(query);
List<string> chosen = ChooseSources(probabilities, unavailable);
List<Hit>[] results = await Task.WhenAll(chosen.Select(source => indexes[source].Search(query, "hybrid", SourceWeights[source], top: 3)));
```

`Task.WhenAll` replaces the Python thread pool for the parallel searches.

Expected output (trimmed):

```text
Query: What TSH level indicates hypothyroidism?
  confidence: {'drug_formulary': 0.0, 'clinical_guidelines': 0.44, 'lab_references': 0.96}
  searched  : ['lab_references']

Query: How do I manage a patient with diabetic ketoacidosis who is hypokalaemic?
  confidence: {'drug_formulary': 0.34, 'clinical_guidelines': 0.97, 'lab_references': 0.82}
  searched  : ['clinical_guidelines', 'lab_references']

Query: Tell me about the Northwind cafeteria opening hours.
  confidence: {'drug_formulary': 0.0, 'clinical_guidelines': 0.0, 'lab_references': 0.0}
  searched  : ['drug_formulary', 'clinical_guidelines', 'lab_references']

average sources searched per query: 1.6 of 3

Query with drug_formulary unavailable: What are side effects of metformin?
  searched  : ['clinical_guidelines', 'lab_references'] (fallback because the classified source is down)
```

The TSH question needs one source and the ketoacidosis question needs two. The cafeteria question has no confident source, so the router widens to all three, and the unavailable-source case shows the fallback. The confidence values are model output and change between runs.

## Step 5: Compare chunking strategies

The document is a 1,299 character ketoacidosis protocol. The script embeds it as 300-character fixed chunks and as structural chunks, one per section, and finds the best chunk for two questions.

```python
def structural_chunks(text):
    sections = re.split(r"\n(?=## )", text)[1:]
    return [{"heading": s.splitlines()[0].lstrip("# "), "content": "\n".join(s.splitlines()[1:]).strip()} for s in sections]
```

```csharp
static List<(string Heading, string Content)> StructuralChunks(string text) =>
    Regex.Split(text, @"
(?=## )").Skip(1).Select(section =>
    {
        string[] lines = section.Split('
');
        return (lines[0].TrimStart('#', ' '), string.Join("
", lines.Skip(1)).Trim());
    }).ToList();
```

Expected output (trimmed):

```text
Document: Management of Diabetic Ketoacidosis (1299 characters)
fixed-size chunks: 5 of 300 characters; structural chunks: 5 sections

Query: potassium is below 3.3, do I hold insulin

  fixed-size  (score 0.672), cuts across section boundaries:
    mol/L give none and recheck in two hours. If it is 3.3 to 5.3 mmol/L add 40 mmol per litre of fluid. If it is below 3.3 mmol/L hold insulin and give potassium first.  ## Complication management Watch for cerebral oedema ...

  structural  (score 0.745), one complete section:
    Check serum potassium before starting insulin. If potassium is above 5.3 mmol/L give none and recheck in two hours. If it is 3.3 to 5.3 mmol/L add 40 mmol per litre of fluid. If it is below 3.3 mmol/L hold insulin and give potassium first.
```

The fixed-size chunk starts mid-sentence, has lost the heading and drags in the next section. The structural chunk carries the complete rule and scores higher. Parent-child chunking goes one step further and returns the section overview with the matched chunk.

## Step 6: Run hybrid search with the semantic ranker on Azure AI Search

The same techniques now run on a service. The script creates an index with a vector field, an Azure OpenAI vectorizer and a semantic configuration, uploads all 30 documents and scores four modes by MRR.

```python
def run(query, mode, source=None, top=10):
    vector = VectorizableTextQuery(text=query, k_nearest_neighbors=50, fields="content_vector", weight=VECTOR_WEIGHT)
    if mode == "keyword":
        return list(search.search(search_text=query, **arguments))
    if mode == "vector":
        return list(search.search(search_text=None, vector_queries=[vector], **arguments))
    if mode == "hybrid":
        return list(search.search(search_text=query, vector_queries=[vector], **arguments))
    return list(search.search(search_text=query, vector_queries=[vector], query_type="semantic", semantic_configuration_name="default", **arguments))
```

The C# project uses `Azure.Search.Documents` 12.0.0. The index definition, the vectorizer and the semantic configuration are the same objects as in Python; the query switches to the semantic ranker with `SearchQueryType.Semantic` and a `SemanticSearchOptions`.

```csharp
options.VectorSearch = new VectorSearchOptions { Queries = { new VectorizableTextQuery(query) { KNearestNeighborsCount = 50, Fields = { "content_vector" } } } };
options.QueryType = SearchQueryType.Semantic;
options.SemanticSearch = new SemanticSearchOptions { SemanticConfigurationName = "default" };
```

Expected output:

```text
== Load the documents into Azure AI Search
m3-health: 30 documents

== Mean reciprocal rank over the labeled queries (higher is better) ==
source                keyword             vector              hybrid              hybrid + semantic   
drug_formulary        0.83                0.72                0.78                1.00                
clinical_guidelines   0.45                1.00                0.58                0.92                

== Semantic ranker on: What should I monitor after starting an ACE inhibitor in a patient with diabetes?
hybrid order  : Hypertension in chronic kidney disease | Lisinopril 10 mg tablets | Type 2 diabetes treatment algorithm | Hemoglobin A1c | Hypothyroidism diagnosis and treatment
semantic order: Lisinopril 10 mg tablets (2.48) | Serum potassium (2.13) | Estimated glomerular filtration rate (2.02) | Hypertension in chronic kidney disease (1.95) | Diabetic ketoacidosis protocol (1.92)

Deleted index m3-health
```

The semantic ranker lifts the formulary to a perfect 1.00 and the guidelines from 0.58 to 0.92, and its reranker score (0 to 4) reorders the monitoring question toward the drug label, potassium and kidney function. Plain hybrid is not always better than one mode on a small set: guidelines are still best with vector alone at 1.00. That is the reason to measure on your own queries before choosing a configuration.

The C# commands print the same sections. Trimmed output of `dotnet run -- search` and `dotnet run -- routing`:

```text
== Mean reciprocal rank over the labeled queries (higher is better) ==
source                keyword             vector              hybrid              hybrid + semantic   
drug_formulary        0.67                0.73                0.78                1.00                
clinical_guidelines   0.45                1.00                0.58                0.92                

semantic order: Lisinopril 10 mg tablets (2.48) | Serum potassium (2.13) | Estimated glomerular filtration rate (2.02) | Hypertension in chronic kidney disease (1.95) | Diabetic ketoacidosis protocol (1.92)

Query: What TSH level indicates hypothyroidism?
  confidence: {'drug_formulary': 0, 'clinical_guidelines': 0.56, 'lab_references': 0.97}
  searched  : ['lab_references']
average sources searched per query: 1.8 of 3
```

## Verify

Steps 1 and 2 show keyword and vector failing on different queries and the MRR table agreeing, Step 4 searches fewer than all sources on average (1.6 of 3), and Step 6 shows the semantic ranker returning a perfect formulary score on the service.

## Cleanup

The scripts delete the index they create and keep nothing else on the server. Remove the local embedding cache if you want a cold run:

```bash
rm -rf .cache
```
