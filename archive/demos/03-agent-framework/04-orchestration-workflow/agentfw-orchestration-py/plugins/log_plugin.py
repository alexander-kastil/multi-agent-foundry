import os
from typing import Annotated, Any, Callable, Set
from pathlib import Path
from pydantic import Field

# Global variable to store outcome directory
OUTCOME_DIRECTORY = "data/outcome"

def read_log_file(filepath: Annotated[str, Field(description="The path to the log file to read")]) -> str:
    """Accesses the given file path string and returns the file contents as a string.
    Includes both the original log and any progress log entries from actions taken."""
    # Read the original log file
    with open(filepath, 'r', encoding='utf-8') as file:
        original_log = file.read()
    
    # Look for progress log in outcome directory
    filename = Path(filepath).name
    progress_log_path = Path(OUTCOME_DIRECTORY) / filename.replace(".log", "-progress.log")
    
    # Check if progress log exists and append it
    if progress_log_path.exists():
        with open(progress_log_path, 'r', encoding='utf-8') as file:
            progress_log = file.read()
        return f"{original_log}\n\n--- ACTIONS IN PROGRESS ---\n{progress_log}"
    
    return original_log

def print_log_summary(filepath: str) -> None:
    """Print a summary of log severities (errors, warnings, alerts, critical)."""
    try:
        with open(filepath, 'r', encoding='utf-8') as file:
            lines = file.readlines()
        
        error_count = sum(1 for line in lines if " ERROR " in line)
        warning_count = sum(1 for line in lines if " WARNING " in line)
        alert_count = sum(1 for line in lines if " ALERT " in line)
        critical_count = sum(1 for line in lines if " CRITICAL " in line)
        
        print(f"\033[93mSummary: errors={error_count}, warnings={warning_count}, alerts={alert_count}, critical={critical_count}\033[0m")
    except Exception as e:
        print(f"\033[93mSummary unavailable: {e}\033[0m")

def write_outcome(original_log_path: str, outcome_text: str) -> None:
    """Write the final outcome to an outcome log file."""
    filename = Path(original_log_path).name
    Path(OUTCOME_DIRECTORY).mkdir(parents=True, exist_ok=True)
    outcome_log_path = Path(OUTCOME_DIRECTORY) / filename.replace(".log", "-outcome.log")
    
    with open(outcome_log_path, 'w', encoding='utf-8') as file:
        file.write(outcome_text)

# Define the set of log file functions
log_functions: Set[Callable[..., Any]] = {
    read_log_file
}
