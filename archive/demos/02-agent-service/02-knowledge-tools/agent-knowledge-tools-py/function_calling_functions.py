import os
import json
from datetime import datetime
from dotenv import load_dotenv

# Load environment variables
load_dotenv()

def get_user_email() -> str:
    """Get the user's email address."""
    return input("Please enter your email address: ")

def get_issue_description() -> str:
    """Get a description of the issue from the user."""
    return input("Please describe the issue: ")

def submit_support_ticket(email: str, issue: str) -> str:
    """
    Submit a support ticket with the user's email and issue description.
    
    Args:
        email: The user's email address
        issue: Description of the technical issue
        
    Returns:
        A JSON string with ticket details
    """
    timestamp = datetime.now().isoformat()
    ticket_id = f"TICKET-{datetime.now().strftime('%Y%m%d%H%M%S')}"
    
    ticket_data = {
        "ticket_id": ticket_id,
        "email": email,
        "issue": issue,
        "timestamp": timestamp,
        "status": "submitted"
    }
    
    # Get output path from environment or use default
    output_path = os.getenv("OUTPUT_PATH", "./output")
    tickets_dir = os.path.join(output_path, "support_tickets")
    os.makedirs(tickets_dir, exist_ok=True)
    
    filename = os.path.join(tickets_dir, f"{ticket_id}.json")
    with open(filename, "w") as f:
        json.dump(ticket_data, f, indent=2)
    
    return json.dumps({
        "success": True,
        "ticket_id": ticket_id,
        "message": f"Support ticket submitted successfully. File saved: {filename}",
        "filename": filename
    })

# Export the functions for use by the agent
user_functions = {get_user_email, get_issue_description, submit_support_ticket}


def _demo_non_interactive(prompt: str) -> None:
    """Run a non-interactive demo given a single prompt containing email and issue.

    Expected formats:
    1. "My email is alice@contoso.com and I cannot access the portal."
    2. "alice@contoso.com: VS Code extension crashes on startup."
    Attempts to extract first email-like token then uses remainder as issue description.
    """
    import re, json
    email_match = re.search(r"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}", prompt)
    if not email_match:
        print("Could not find an email address in prompt; aborting.")
        return
    email = email_match.group(0)
    # Issue: remove leading connective words and the email portion
    issue_text = prompt.replace(email, "").strip(" :.-")
    if issue_text.lower().startswith("my email is"):
        # If pattern 1, trim leading phrase
        issue_text = re.sub(r"(?i)my email is", "", issue_text).strip()
    if not issue_text:
        issue_text = "No issue text detected"
    ticket_json = submit_support_ticket(email, issue_text)
    print(ticket_json)


if __name__ == "__main__":
    auto = os.getenv("AUTO_TEST_PROMPT")
    if auto:
        _demo_non_interactive(auto)
    else:
        print("Support Ticket Demo (interactive). Press Ctrl+C to exit.")
        try:
            email = get_user_email()
            issue = get_issue_description()
            result = submit_support_ticket(email, issue)
            print(result)
        except KeyboardInterrupt:
            print("\nExiting demo.")
