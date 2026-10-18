from datetime import datetime
from typing import Annotated, Any, Callable, Set
from pathlib import Path
from pydantic import Field

# Global variable to store outcome directory
OUTCOME_DIRECTORY = "data/outcome"

def append_to_log_file(filepath: str, content: str) -> None:
    """Helper function to append content to a progress log file in the outcome directory."""
    # Write to progress log file in outcome directory to track actions as they happen
    filename = Path(filepath).name
    progress_log_path = Path(OUTCOME_DIRECTORY) / filename.replace(".log", "-progress.log")
    
    # Ensure outcome directory exists
    Path(OUTCOME_DIRECTORY).mkdir(parents=True, exist_ok=True)
    
    with open(progress_log_path, 'a', encoding='utf-8') as file:
        file.write('\n' + content.strip())

def restart_service(
    service_name: Annotated[str, Field(description="The name of the service to restart")],
    logfile: Annotated[str, Field(description="The path to the log file")]
) -> str:
    """A function that restarts the named service."""
    log_entries = [
        f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] ALERT  DevopsAssistant: Multiple failures detected in {service_name}. Restarting service.",
        f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] INFO  {service_name}: Restart initiated.",
        f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] INFO  {service_name}: Service restarted successfully.",
    ]

    log_message = "\n".join(log_entries)
    append_to_log_file(logfile, log_message)
    
    return f"Service {service_name} restarted successfully."

def rollback_transaction(
    logfile: Annotated[str, Field(description="The path to the log file")]
) -> str:
    """A function that rollsback the transaction."""
    log_entries = [
        f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] ALERT  DevopsAssistant: Transaction failure detected. Rolling back transaction batch.",
        f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] INFO   TransactionProcessor: Rolling back transaction batch.",
        f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] INFO   Transaction rollback completed successfully.",
    ]

    log_message = "\n".join(log_entries)
    append_to_log_file(logfile, log_message)
    
    return "Transaction rolled back successfully."

def redeploy_resource(
    resource_name: Annotated[str, Field(description="The name of the resource to redeploy")],
    logfile: Annotated[str, Field(description="The path to the log file")]
) -> str:
    """A function that redeploys the named resource."""
    log_entries = [
        f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] ALERT  DevopsAssistant: Resource deployment failure detected in '{resource_name}'. Redeploying resource.",
        f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] INFO   DeploymentManager: Redeployment request submitted.",
        f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] INFO   DeploymentManager: Service successfully redeployed, resource '{resource_name}' created successfully.",
    ]

    log_message = "\n".join(log_entries)
    append_to_log_file(logfile, log_message)
    
    return f"Resource '{resource_name}' redeployed successfully."

def increase_quota(
    logfile: Annotated[str, Field(description="The path to the log file")]
) -> str:
    """A function that increases the quota."""
    log_entries = [
        f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] ALERT  DevopsAssistant: High request volume detected. Increasing quota.",
        f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] INFO   APIManager: Quota increase request submitted.",
        f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] INFO   APIManager: Quota successfully increased to 150% of previous limit.",
    ]

    log_message = "\n".join(log_entries)
    append_to_log_file(logfile, log_message)

    return "Successfully increased quota."

def escalate_issue(
    logfile: Annotated[str, Field(description="The path to the log file")]
) -> str:
    """A function that escalates the issue."""
    log_entries = [
        f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] ALERT  DevopsAssistant: Cannot resolve issue.",
        f"[{datetime.now().strftime('%Y-%m-%d %H:%M:%S')}] ALERT  DevopsAssistant: Requesting escalation.",
    ]
    
    log_message = "\n".join(log_entries)
    append_to_log_file(logfile, log_message)
    
    return "Submitted escalation request."

# Define the set of devops functions
devops_functions: Set[Callable[..., Any]] = {
    restart_service,
    rollback_transaction,
    redeploy_resource,
    increase_quota,
    escalate_issue
}