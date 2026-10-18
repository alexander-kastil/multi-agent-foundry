from typing import Any, Callable, Set

def send_email(to: str, subject: str, body: str) -> str:
    """Sends an email.
    
    Args:
        to: Who to send the email to
        subject: The subject of the email.
        body: The text body of the email.
    
    Returns:
        A confirmation message
    """
    print("\nTo:", to)
    print("Subject:", subject)
    print(body, "\n")
    return f"Email sent successfully to {to}"

# Define the set of user functions for the agent
user_functions: Set[Callable[..., Any]] = {
    send_email
}
