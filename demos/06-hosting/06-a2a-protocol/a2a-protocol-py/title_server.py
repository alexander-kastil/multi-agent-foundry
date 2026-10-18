import os

from a2a.types import AgentSkill

from a2a_host import create_agent, serve

agent = create_agent(
    "m6h2-title-agent",
    "You write one catchy blog post title for the topic you are given. Reply with the title only.",
)

skill = AgentSkill(
    id="generate_title",
    name="Generate title",
    description="Writes a blog post title for a topic.",
    tags=["title", "blog"],
    examples=["Give me a title for a post about hosted agents."],
)

serve(agent, "Writes blog post titles.", skill, int(os.getenv("TITLE_PORT", "9101")))
