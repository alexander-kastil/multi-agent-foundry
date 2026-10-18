import json
from azure.ai.projects.models import PromptAgentDefinition, FunctionTool
from openai.types.responses.response_input_param import FunctionCallOutput
from foundry import project, openai_client, MODEL
from tenant_context import (
    PartitionedStore,
    CodeReviewRepository,
    current_tenant,
    extract_tenant_from_token,
    issue_token,
    new_signing_key,
)

signing_key = new_signing_key()
other_key = new_signing_key()

store = PartitionedStore()
repository = CodeReviewRepository(store)

for tenant, reviews in {
    "tenant-a": [("rev-a-001", "billing.py", ["hardcoded api key"]), ("rev-a-002", "auth.py", [])],
    "tenant-b": [("rev-b-001", "payroll.py", ["sql injection in salary export"])],
}.items():
    current_tenant.set(tenant)
    for review_id, code_file, findings in reviews:
        repository.save_review(review_id, code_file, findings)

forged = issue_token(other_key, "tenant-b")
try:
    extract_tenant_from_token(f"Bearer {forged}", signing_key.public_key())
except Exception as e:
    print(f"Forged token for tenant-b rejected: {type(e).__name__}")

token = issue_token(signing_key, "tenant-a")
tenant_id = extract_tenant_from_token(f"Bearer {token}", signing_key.public_key())
current_tenant.set(tenant_id)
print(f"Request context established for {tenant_id}\n")

no_params = {"type": "object", "properties": {}, "required": [], "additionalProperties": False}
tools = [
    FunctionTool(
        name="list_reviews",
        description="List the code reviews of the current tenant.",
        parameters=no_params,
        strict=True,
    ),
    FunctionTool(
        name="get_review",
        description="Fetch one code review by id from the current tenant.",
        parameters={
            "type": "object",
            "properties": {"review_id": {"type": "string", "description": "Review id, e.g. rev-a-001."}},
            "required": ["review_id"],
            "additionalProperties": False,
        },
        strict=True,
    ),
]

agent = project.agents.create_version(
    agent_name="m8-zt-review-assistant",
    definition=PromptAgentDefinition(
        model=MODEL,
        instructions="You answer questions about code reviews using only the tools. Report exactly what the tools return.",
        tools=tools,
    ),
)
reference = {"name": agent.name, "type": "agent_reference"}
conversation = openai_client.conversations.create()

prompt = "List my reviews, then fetch review rev-b-001 and tell me what it contains."
print(f"USER: {prompt}")
response = openai_client.responses.create(
    input=prompt,
    conversation=conversation.id,
    extra_body={"agent_reference": reference},
)

while True:
    outputs = []
    for item in response.output:
        if item.type == "function_call":
            args = json.loads(item.arguments)
            result = repository.list_reviews() if item.name == "list_reviews" else repository.get_review(**args)
            print(f"  [tool call] {item.name}({args}) -> {json.dumps(result)}")
            outputs.append(FunctionCallOutput(type="function_call_output", call_id=item.call_id, output=json.dumps(result)))
    if not outputs:
        break
    response = openai_client.responses.create(
        input=outputs,
        conversation=conversation.id,
        extra_body={"agent_reference": reference},
    )

print(f"\nAGENT: {response.output_text}")

openai_client.conversations.delete(conversation_id=conversation.id)
project.agents.delete_version(agent_name=agent.name, agent_version=agent.version)
