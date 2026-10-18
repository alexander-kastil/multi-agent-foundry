import os
import logging
from datetime import datetime


class MermaidDiagramGenerator:
    """Generates Mermaid sequence diagrams for incident resolution workflows."""
    
    def __init__(self, ticket_folder_path: str = "./tickets"):
        """
        Initialize the diagram generator.
        
        Args:
            ticket_folder_path: Directory where diagram files will be saved
        """
        self.ticket_folder_path = ticket_folder_path
        
    def generate_diagram(self, verbose: bool = False) -> str:
        """
        Generate a Mermaid sequence diagram showing the incident resolution workflow.
        
        Args:
            verbose: If True, include detailed notes and explanations
            
        Returns:
            Mermaid diagram as a string
        """
        if verbose:
            # Verbose diagram with detailed notes, instructions, and context
            diagram = """sequenceDiagram
    participant User
    participant Orchestrator as Orchestrator Agent<br/>(Main Coordinator)
    participant IncidentMgr as Incident Manager<br/>(Log Analysis)
    participant DevOps as DevOps Assistant<br/>(Action Execution)
    participant LogFile as Log File
    
    User->>Orchestrator: Process log file
    Note over Orchestrator: Receive log file path<br/>Initialize incident resolution
    
    Orchestrator->>LogFile: Read log file
    LogFile-->>Orchestrator: Log content
    Note over Orchestrator: Analyze log content<br/>Identify issues and patterns
    
    Orchestrator->>IncidentMgr: Request analysis & recommendation
    Note over IncidentMgr: Analyze logs for issues:<br/>• Service failures<br/>• Transaction errors<br/>• Resource problems<br/>• Quota issues
    IncidentMgr-->>Orchestrator: Return: Recommended action
    
    Orchestrator->>DevOps: Execute recommended action
    Note over DevOps: Perform corrective action:<br/>• Restart service<br/>• Rollback transaction<br/>• Redeploy resource<br/>• Increase quota<br/>• Escalate if needed
    DevOps-->>Orchestrator: Return: Action result
    
    Orchestrator->>LogFile: Verify issue resolution
    LogFile-->>Orchestrator: Updated log status
    
    alt Issue Resolved
        Note over Orchestrator: Incident resolved successfully
        Orchestrator-->>User: "No action needed"
    else Issue Persists
        Note over Orchestrator: Try alternative action<br/>(up to 5 iterations)
        Orchestrator->>DevOps: Execute alternative action
    else Unresolvable
        Note over Orchestrator: Max iterations reached
        Orchestrator-->>User: "Escalate issue"
    end
"""
        else:
            # Simple diagram without detailed notes
            diagram = """sequenceDiagram
    participant User
    participant Orchestrator
    participant IncidentMgr
    participant DevOps
    participant LogFile
    
    User->>Orchestrator: Process log file
    Orchestrator->>LogFile: Read log
    LogFile-->>Orchestrator: Log content
    Orchestrator->>IncidentMgr: Analyze & recommend
    IncidentMgr-->>Orchestrator: Recommended action
    Orchestrator->>DevOps: Execute action
    DevOps-->>Orchestrator: Action result
    Orchestrator->>LogFile: Verify resolution
    LogFile-->>Orchestrator: Status
    
    alt Resolved
        Orchestrator-->>User: "No action needed"
    else Not Resolved
        Orchestrator->>DevOps: Try alternative
    else Max Iterations
        Orchestrator-->>User: "Escalate issue"
    end
"""
        return diagram
    
    def save_diagram_file(
        self,
        log_filename: str,
        resolution: str = "",
        iterations: int = 0,
        token_usage_in: int = 0,
        token_usage_out: int = 0,
        original_issue: str | None = None,
        resolution_summary: str | None = None,
        final_response: str | None = None,
    ) -> str:
        """
        Save a Mermaid diagram file for the given incident resolution.
        
        Args:
            log_filename: The log file that was processed
            resolution: The final resolution status
            iterations: Number of iterations performed
            token_usage_in: Number of input tokens used
            token_usage_out: Number of output tokens used
            
        Returns:
            Path to the saved diagram file
        """
        # Generate incident number based on timestamp
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        incident_id = timestamp
        
        # Generate both diagrams
        simple_diagram = self.generate_diagram(verbose=False)
        verbose_diagram = self.generate_diagram(verbose=True)
        
        # Calculate total tokens
        token_usage_total = token_usage_in + token_usage_out
        
        # Prepare additional details
        original_issue_text = original_issue or "(not available)"
        resolution_summary_text = resolution_summary or "(not available)"
        final_response_text = final_response or "(not available)"

        # Create the file content with both diagrams and extra incident details
        file_content = (
            f"# Incident {incident_id}\n\n"
            f"## Incident Details\n"
            f"- **Log File**: {log_filename}\n"
            f"- **Original Issue**: {original_issue_text}\n"
            f"- **Incident Resolution Summary**: {resolution_summary_text}\n"
            f"- **Final Response**: {final_response_text}\n"
            f"- **Resolution**: {resolution if resolution else 'In Progress'}\n"
            f"- **Iterations**: {iterations}\n"
            f"- **Token Usage**: In: {token_usage_in}, Out: {token_usage_out}, Total: {token_usage_total}\n\n"
            f"## Diagram\n````mermaid\n{simple_diagram}````\n\n"
            f"## Verbose Diagram\n````mermaid\n{verbose_diagram}````\n"
        )
        
        # Ensure the ticket folder exists
        os.makedirs(self.ticket_folder_path, exist_ok=True)
        
        # Save to file with UTF-8 encoding to support Unicode characters
        filename = os.path.join(self.ticket_folder_path, f"incident-{incident_id}.md")
        with open(filename, "w", encoding="utf-8") as f:
            f.write(file_content)
        
        logging.info(f"Diagram file saved: {filename}")
        return filename
