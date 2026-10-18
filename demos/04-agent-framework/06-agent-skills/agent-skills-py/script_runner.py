import subprocess
import sys
from pathlib import Path

from agent_framework import FileSkill, FileSkillScript


def run_script(skill: FileSkill, script: FileSkillScript, args: dict | list[str] | None = None) -> str:
    script_path = Path(script.full_path)
    command = [sys.executable, str(script_path)]
    if isinstance(args, list):
        command.extend(args)
    completed = subprocess.run(
        command, capture_output=True, text=True, timeout=30, cwd=str(script_path.parent)
    )
    return completed.stdout.strip()
