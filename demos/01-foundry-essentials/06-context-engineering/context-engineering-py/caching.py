import uuid
from session import model, openai
from history import forecast

run = uuid.uuid4().hex[:8]
handbook = f"Travel desk handbook {run}\n" + "\n".join(f"Day {day}\n{forecast(day)}" for day in range(1, 5))
questions = ["Which day has the most rain?", "Which day is the windiest?", "Which day is the warmest?"]


def ask(label, instructions, question):
    response = openai.responses.create(
        model=model,
        instructions=instructions,
        input=question,
        max_output_tokens=2000,
        prompt_cache_key=f"m1-handbook-{run}",
        prompt_cache_retention="24h",
        store=False,
    )
    usage = response.usage
    print(f"{label:<22}: {usage.input_tokens:>5} input, {usage.input_tokens_details.cached_tokens:>5} cached")


for number, question in enumerate(questions, start=1):
    ask(f"stable prefix, call {number}", f"{handbook}\nAnswer in one short sentence.", question)

ask("variable text first", f"Question: {questions[0]}\n{handbook}\nAnswer in one short sentence.", questions[0])
