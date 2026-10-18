import json

from agent_framework import InlineSkill, SkillFrontmatter

altitude_skill = InlineSkill(
    frontmatter=SkillFrontmatter(
        name="altitude",
        description="Convert mountain altitudes between meters and feet. Use when a customer gives a summit or hut height in one unit and needs the other.",
    ),
    instructions="Use the convert script. Pass the value and the factor (3.28084 for meters to feet, 0.3048 for feet to meters).",
)


@altitude_skill.script(name="convert", description="Convert a value: result = value x factor")
def convert(value: float, factor: float) -> str:
    return json.dumps({"value": value, "factor": factor, "result": round(value * factor, 1)})
