import time

from azure.search.documents.knowledgebases import KnowledgeBaseRetrievalClient
from azure.search.documents.knowledgebases.models import (
    KnowledgeBaseMessage,
    KnowledgeBaseMessageTextContent,
    KnowledgeBaseRetrievalRequest,
    KnowledgeRetrievalLowReasoningEffort,
    KnowledgeRetrievalMediumReasoningEffort,
    KnowledgeRetrievalMinimalReasoningEffort,
    KnowledgeRetrievalOutputMode,
    KnowledgeRetrievalSemanticIntent,
)

from common import API_VERSION, KNOWLEDGE_BASE, SEARCH_ENDPOINT, credential

QUESTION = (
    "I am choosing between the Summit 4S and the Trailhead 2 for a January trip. "
    "Which one can take winter storms, and if it arrives damaged what must I do and how long do I have?"
)

client = KnowledgeBaseRetrievalClient(
    endpoint=SEARCH_ENDPOINT, knowledge_base_name=KNOWLEDGE_BASE, credential=credential, api_version=API_VERSION
)


def retrieve(effort, **request):
    started = time.time()
    response = client.retrieve(
        KnowledgeBaseRetrievalRequest(retrieval_reasoning_effort=effort, include_activity=True, **request)
    )
    return response, time.time() - started


print("== Step 3: compare reasoning effort")
runs = {
    "minimal": retrieve(
        KnowledgeRetrievalMinimalReasoningEffort(),
        intents=[KnowledgeRetrievalSemanticIntent(search=QUESTION)],
        output_mode=KnowledgeRetrievalOutputMode.EXTRACTIVE_DATA,
    ),
}
messages = [KnowledgeBaseMessage(role="user", content=[KnowledgeBaseMessageTextContent(text=QUESTION)])]
for name, effort in (("low", KnowledgeRetrievalLowReasoningEffort()), ("medium", KnowledgeRetrievalMediumReasoningEffort())):
    runs[name] = retrieve(effort, messages=messages, output_mode=KnowledgeRetrievalOutputMode.ANSWER_SYNTHESIS)

for name, (response, seconds) in runs.items():
    activity = [a.as_dict() for a in response.activity or []]
    searches = [a for a in activity if a["type"] == "searchIndex"]
    planning = next((a for a in activity if a["type"] == "modelQueryPlanning"), None)
    synthesis = next((a for a in activity if a["type"] == "modelAnswerSynthesis"), None)
    print(f"\n-- {name}: {seconds:.1f}s, {len(searches)} index searches, {len(response.references or [])} references")
    print(f"   planning tokens: {planning['inputTokens'] + planning['outputTokens'] if planning else 0}, synthesis tokens: {synthesis['inputTokens'] + synthesis['outputTokens'] if synthesis else 0}")
    print("  ", response.response[0].content[0].text[:400].replace("\n", " "))
