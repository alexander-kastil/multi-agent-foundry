from session import model, openai
from history import QUESTION, build_turns, flatten, turn

THRESHOLD = 6000

conversation = flatten(build_turns(8))

for day in range(9, 15):
    conversation += turn(day)[:3]
    if day == 14:
        conversation.append({"role": "user", "content": QUESTION})
    response = openai.responses.create(
        model=model,
        instructions="Answer in one short sentence.",
        input=conversation,
        store=False,
        max_output_tokens=2000,
        context_management=[{"type": "compaction", "compact_threshold": THRESHOLD}],
    )
    conversation += [item.model_dump(exclude_none=True) for item in response.output]
    compactions = [k for k, item in enumerate(conversation) if item.get("type") == "compaction"]
    if compactions:
        conversation = conversation[compactions[-1]:]
    kinds = [item.type for item in response.output]
    print(f"day {day}: {response.usage.input_tokens:>6} input tokens, output {kinds}, window now {len(conversation)} items")

print(response.output_text)
