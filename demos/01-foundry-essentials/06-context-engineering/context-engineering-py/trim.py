from session import model, openai
from history import QUESTION, build_turns, flatten

turns = build_turns(20)

for name, kept in (("all 20 turns", turns), ("last 4 turns", turns[-4:])):
    response = openai.responses.create(
        model=model,
        instructions="Answer in one short sentence. If you do not know, say so.",
        input=[*flatten(kept), {"role": "user", "content": QUESTION}],
        store=False,
        max_output_tokens=2000,
    )
    print(f"{name:<13}: {response.usage.input_tokens:>6} input tokens")
    print(f"{'':<13}  {response.output_text}")
