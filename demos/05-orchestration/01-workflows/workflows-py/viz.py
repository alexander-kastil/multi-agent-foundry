from pathlib import Path

from agent_framework import WorkflowViz

from routing import build_workflow

viz = WorkflowViz(build_workflow())
mermaid = viz.to_mermaid()
print(mermaid)
Path("output").mkdir(exist_ok=True)
Path("output/invoice-routing.mmd").write_text(mermaid, encoding="utf-8")
