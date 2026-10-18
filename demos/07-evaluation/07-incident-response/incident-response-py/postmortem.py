POSTMORTEM_TEMPLATE = """# Postmortem: {title}

## Incident summary
{summary}

## Timeline
{timeline}

## Root cause
{root_cause}

## Contributing factors
{contributing_factors}

## What went well
{went_well}

## What did not go well
{went_badly}

## Action items
{actions}
"""


def bullet(items):
    return "\n".join(f"- {item}" for item in items)


def write_postmortem(path, incident):
    content = POSTMORTEM_TEMPLATE.format(
        title=incident["title"],
        summary=incident["summary"],
        timeline=bullet(incident["timeline"]),
        root_cause=incident["root_cause"],
        contributing_factors=bullet(incident["contributing_factors"]),
        went_well=bullet(incident["went_well"]),
        went_badly=bullet(incident["went_badly"]),
        actions=bullet(f"{action['item']} (owner: {action['owner']}, due: {action['due']})" for action in incident["actions"]),
    )
    with open(path, "w") as file:
        file.write(content)
    return content
