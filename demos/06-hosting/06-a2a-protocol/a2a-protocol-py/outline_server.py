import os

from a2a.types import AgentSkill

from a2a_host import create_agent, serve

agent = create_agent(
    "m6h2-outline-agent",
    "You write a concise outline with 4 to 6 sections of 5 to 10 words each for the title or topic you are given.",
)

skill = AgentSkill(
    id="generate_outline",
    name="Generate outline",
    description="Writes an outline for a blog post.",
    tags=["outline", "blog"],
    examples=["Create an outline for: Hosted agents without the plumbing."],
)

serve(agent, "Writes blog post outlines.", skill, int(os.getenv("OUTLINE_PORT", "9102")))
