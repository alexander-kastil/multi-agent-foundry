import os
import logging
from datetime import datetime


class MermaidDiagramGenerator:
    """Generates Mermaid sequence diagrams for connected agent workflows."""
    
    def __init__(self, ticket_folder_path: str = "./tickets"):
        """
        Initialize the diagram generator.
        
        Args:
            ticket_folder_path: Directory where diagram files will be saved
        """
        self.ticket_folder_path = ticket_folder_path
        
    def generate_diagram(self, verbose: bool = False) -> str:
        """
        Generate a Mermaid sequence diagram showing the connected agents workflow.
        
        Args:
            verbose: If True, include detailed notes and explanations
            
        Returns:
            Mermaid diagram as a string
        """
        if verbose:
            # Verbose diagram with detailed notes, instructions, and context
            diagram = """sequenceDiagram
    participant User
    participant TriageAgent as Triage Agent<br/>(Main Orchestrator)
    participant PriorityAgent as Priority Agent<br/>(Urgency Assessment)
    participant TeamAgent as Team Agent<br/>(Team Assignment)
    participant EffortAgent as Effort Agent<br/>(Complexity Estimation)
    
    User->>TriageAgent: Submit ticket description
    Note over TriageAgent: Parse ticket content<br/>Identify key requirements<br/>Plan assessment strategy
    
    TriageAgent->>PriorityAgent: Request priority assessment
    Note over PriorityAgent: Analyze ticket urgency:<br/>• User-facing/blocking → High<br/>• Time-sensitive → Medium<br/>• Cosmetic/non-urgent → Low
    PriorityAgent-->>TriageAgent: Return: Priority level + rationale
    
    TriageAgent->>TeamAgent: Request team assignment
    Note over TeamAgent: Match ticket to team:<br/>• Frontend (UI/UX issues)<br/>• Backend (API/server logic)<br/>• Infrastructure (deployment/ops)<br/>• Marketing (content/campaigns)
    TeamAgent-->>TriageAgent: Return: Team name + rationale
    
    TriageAgent->>EffortAgent: Request effort estimation
    Note over EffortAgent: Estimate work complexity:<br/>• Small: <1 day<br/>• Medium: 2-3 days<br/>• Large: Multi-day/cross-team
    EffortAgent-->>TriageAgent: Return: Effort level + justification
    
    Note over TriageAgent: Synthesize all assessments<br/>Generate comprehensive triage report
    TriageAgent-->>User: Complete triage analysis<br/>(Priority + Team + Effort)
"""
        else:
            # Simple diagram without detailed notes
            diagram = """sequenceDiagram
    participant User
    participant TriageAgent
    participant PriorityAgent
    participant TeamAgent
    participant EffortAgent
    
    User->>TriageAgent: Submit ticket
    TriageAgent->>PriorityAgent: Assess priority
    PriorityAgent-->>TriageAgent: Return priority level
    TriageAgent->>TeamAgent: Determine team
    TeamAgent-->>TriageAgent: Return team assignment
    TriageAgent->>EffortAgent: Estimate effort
    EffortAgent-->>TriageAgent: Return effort estimate
    TriageAgent-->>User: Complete triage analysis
"""
        return diagram
    
    def save_diagram_file(self, ticket_prompt: str, resolution: str = "", 
                         token_usage_in: int = 0, token_usage_out: int = 0) -> str:
        """
        Save a Mermaid diagram file for the given ticket.
        
        Args:
            ticket_prompt: The ticket description
            resolution: The resolution/result from the triage process
            token_usage_in: Number of input tokens used
            token_usage_out: Number of output tokens used
            
        Returns:
            Path to the saved diagram file
        """
        # Generate ticket number based on timestamp
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        ticket_id = timestamp
        
        # Generate both diagrams
        simple_diagram = self.generate_diagram(verbose=False)
        verbose_diagram = self.generate_diagram(verbose=True)
        
        # Calculate total tokens
        token_usage_total = token_usage_in + token_usage_out
        
        # Create the file content with both diagrams
        file_content = f"""# Ticket {ticket_id}

## Ticket Description
- **Description**: {ticket_prompt}
- **Resolution**: {resolution if resolution else "Pending"}
- **Token Usage**: In: {token_usage_in}, Out: {token_usage_out}, Total: {token_usage_total}

## Diagram
```mermaid
{simple_diagram}```

## Verbose Diagram
```mermaid
{verbose_diagram}```
"""
        
        # Ensure the ticket folder exists
        os.makedirs(self.ticket_folder_path, exist_ok=True)
        
        # Save to file with UTF-8 encoding to support Unicode characters
        filename = os.path.join(self.ticket_folder_path, f"ticket-{ticket_id}.md")
        with open(filename, "w", encoding="utf-8") as f:
            f.write(file_content)
        
        logging.info(f"Diagram file saved: {filename}")
        return filename
