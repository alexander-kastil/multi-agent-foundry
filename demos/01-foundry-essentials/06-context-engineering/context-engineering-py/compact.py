from collections import Counter
from session import model, openai
from history import QUESTION, build_turns, flatten

history = flatten(build_turns(20))

compacted = openai.responses.compact(model=model, input=history)

print(f"items before : {dict(Counter(item.get('type', 'message') for item in history))}")
print(f"items after  : {dict(Counter(item.type for item in compacted.output))}")

window = [item.model_dump(exclude_none=True, warnings=False) for item in compacted.output]

response = openai.responses.create(
    model=model,
    instructions="Answer in one short sentence. If you do not know, say so.",
    input=[*window, {"role": "user", "content": QUESTION}],
    store=False,
    max_output_tokens=2000,
)
print(f"compact call : {compacted.usage.input_tokens} in, {compacted.usage.output_tokens} out")
print(f"next turn    : {response.usage.input_tokens} input tokens")
print(f"answer       : {response.output_text}")
