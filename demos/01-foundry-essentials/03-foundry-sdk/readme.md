# Foundry SDK & Microsoft.Extensions.AI

Code that talks to a model has two jobs: reach your Foundry project with the right identity, and speak a request format the model understands. The Foundry SDK handles the first job and the OpenAI-compatible API handles the second. In .NET, Microsoft.Extensions.AI adds a provider-neutral interface on top so the rest of your application never depends on a vendor client.

## One project endpoint, two clients

The Foundry SDK is a thin client over every Foundry project API, reached through the single project endpoint. It gives you two kinds of client, and most applications use both.

| Client | Use it for |
| --- | --- |
| Project client (`AIProjectClient`) | Foundry-native operations with no OpenAI equivalent: deployments, connections, project properties, tracing |
| OpenAI-compatible client | Anything that follows OpenAI concepts: the Responses API, conversations, agents, evaluations, model calls |

You get the second client from the first, so authentication and the endpoint are configured once.

```python
import os
from azure.ai.projects import AIProjectClient
from azure.identity import DefaultAzureCredential

project = AIProjectClient(
    endpoint=os.environ["PROJECT_ENDPOINT"],
    credential=DefaultAzureCredential(),
)
openai = project.get_openai_client()

response = openai.responses.create(
    model="gpt-5.4",
    input="What is the size of France in square miles?",
)
print(response.output_text)
```

Other SDKs exist for specific jobs. The OpenAI SDK pointed at the `/openai/v1` endpoint of the resource gives the lowest latency and the full OpenAI surface, including embeddings. The Anthropic SDK targets Claude models deployed in Foundry, and the Agent Framework builds agents on top of the Foundry SDK.

This class uses the project client and its OpenAI-compatible client because they reach models, agents and tools through one endpoint.

## Python packages and versions

The Foundry (new) experience uses the 2.x line of `azure-ai-projects`. The 1.x line belongs to Foundry (classic) and its API shapes differ.

| SDK version | Portal | Package |
| --- | --- | --- |
| 2.x | Foundry (new) | `azure-ai-projects` |
| 1.x | Foundry (classic) | `azure-ai-projects==1.0.0` |

The 2.x samples need Python 3.10 or later and `openai` 3.0.0 or later. Inference goes through `project.get_openai_client()` and the `openai` package. Pin both in `requirements.txt` so a demo that works today works next month.

```text
azure-ai-projects==2.9.0
azure-identity==1.26.0
openai==3.35.1
python-dotenv==1.2.4
```

## Responses and Chat Completions

The Responses API is the primary surface: it carries instructions separately from input, returns a typed list of output items, and is the surface that agents and conversations are built on. Chat Completions stays available for code that already speaks the messages format, and model router also answers it.

```python
completion = openai.chat.completions.create(
    model="gpt-5.4",
    messages=[
        {"role": "system", "content": "You are a weather chatbot."},
        {"role": "user", "content": "Is it sunny in Vienna?"},
    ],
)
print(completion.choices[0].message.content)
```

Tool calling follows the same pattern in both. With Responses the model returns a `function_call` item, your code runs the function, and you send back a `function_call_output` item with the same `call_id`.

```python
input_items += response.output
input_items.append(
    {"type": "function_call_output", "call_id": item.call_id, "output": json.dumps(result)}
)
final = openai.responses.create(model="gpt-5.4", input=input_items, tools=tools)
```

## The .NET packages

The .NET SDK for Foundry (new) is `Azure.AI.Projects` 2.0.x, which brings `Azure.AI.Projects.Agents` and `Azure.AI.Extensions.OpenAI` with it. `Azure.AI.Extensions.OpenAI` is the generally available package that provides the OpenAI-compatible clients. Never install the preview `Azure.AI.Projects.OpenAI` next to it: both define the same types in different namespaces and the compiler reports ambiguous references.

```xml
<ItemGroup>
  <PackageReference Include="Azure.AI.Projects" Version="2.0.1" />
  <PackageReference Include="Azure.AI.Extensions.OpenAI" Version="2.0.0" />
  <PackageReference Include="Azure.Identity" Version="1.21.0" />
  <PackageReference Include="Microsoft.Extensions.AI" Version="10.4.1" />
  <PackageReference Include="Microsoft.Extensions.AI.OpenAI" Version="10.4.1" />
</ItemGroup>
```

The `10.4.1` pair is deliberate. `Azure.AI.Extensions.OpenAI` 2.0.0 is built against the `OpenAI` package 2.9.1, and `Microsoft.Extensions.AI.OpenAI` 10.4.1 depends on that same version.

Newer adapter releases pull a newer `OpenAI` package, and the project then fails at run time with a `MissingMethodException` when the Responses client is created. When you move to a newer Foundry package, move the adapter with it.

## Microsoft.Extensions.AI

Microsoft.Extensions.AI defines `IChatClient`, one interface for chat that works with any provider. You write application code against `IChatClient` and decide at the composition root which model client sits behind it. The adapter package turns the Foundry Responses client into an `IChatClient`.

```csharp
AIProjectClient project = new(new Uri(endpoint), new DefaultAzureCredential());

IChatClient client = project
    .GetProjectOpenAIClient()
    .GetProjectResponsesClient()
    .AsIChatClient("gpt-5.4");

ChatResponse response = await client.GetResponseAsync("What is the capital of Austria?");
Console.WriteLine(response.Text);
```

Behavior such as function calling is added as middleware with `ChatClientBuilder`. `AIFunctionFactory.Create` turns a C# method into a tool, and `UseFunctionInvocation` runs the call loop for you.

```csharp
IChatClient client = new ChatClientBuilder(inner)
    .UseFunctionInvocation()
    .Build();

ChatOptions options = new() { Tools = [AIFunctionFactory.Create(LookupWeather)] };
ChatResponse response = await client.GetResponseAsync(messages, options);
```

Because the agent frameworks in later modules also consume `IChatClient`, everything you build here carries over.

## Hands-On Demo

[Call Models with the Foundry SDK and IChatClient](./demo-foundry-sdk.md) sends the same request through the Responses API and Chat Completions in Python, adds function calling, and then reproduces chat, streaming and tool use in C# through `IChatClient` in `foundry-sdk-cs/`. `extensions-ai-cs/` holds the same chat call in a single file.

## Key Topics

- [Microsoft Foundry SDKs and endpoints](https://learn.microsoft.com/en-us/azure/foundry/how-to/develop/sdk-overview)
- [Prepare your development environment](https://learn.microsoft.com/en-us/azure/foundry/how-to/develop/install-cli-sdk)
- [Azure AI Projects client library for Python](https://learn.microsoft.com/en-us/python/api/overview/azure/ai-projects-readme)
- [Azure AI Projects client library for .NET](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/ai.projects-readme)
- [Microsoft.Extensions.AI libraries](https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai)
- [Quickstart: Get started with Microsoft Foundry](https://learn.microsoft.com/en-us/azure/foundry/quickstarts/get-started-code)
