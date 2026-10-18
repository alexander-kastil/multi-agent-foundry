from session import model, openai
from history import QUESTION, build_turns, flatten

for days in (2, 10, 20):
    response = openai.responses.create(
        model=model,
        instructions="Answer in one short sentence.",
        input=[*flatten(build_turns(days)), {"role": "user", "content": QUESTION}],
        store=False,
        max_output_tokens=2000,
    )
    usage = response.usage
    print(f"{days:>3} days : {usage.input_tokens:>6} input tokens, {usage.output_tokens:>3} output tokens")
    print(f"         {response.output_text}")
