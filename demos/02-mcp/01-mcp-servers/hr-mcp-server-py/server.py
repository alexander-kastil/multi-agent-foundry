import sys

from mcp.server.mcpserver import MCPServer
from mcp.types import ToolAnnotations

from store import Employee, EmployeeStore

store = EmployeeStore()
mcp = MCPServer("hr-mcp-server-py", instructions="HR directory for a restaurant team.")


@mcp.tool(name="list_employees", annotations=ToolAnnotations(read_only_hint=True))
def list_employees() -> list[dict]:
    """Lists every employee. Use search_employees to narrow the list down."""
    return [e.to_dict() for e in store.all()]


@mcp.tool(name="search_employees", annotations=ToolAnnotations(read_only_hint=True))
def search_employees(search_term: str) -> list[dict]:
    """Finds employees whose name, email, role or skills contain the search term."""
    return [e.to_dict() for e in store.search(search_term)]


@mcp.tool(name="add_employee", annotations=ToolAnnotations(destructive_hint=False))
def add_employee(first_name: str, last_name: str, email: str, current_role: str, skills: str = "") -> str:
    """Adds a new employee. Fails when the email already exists."""
    employee = Employee(first_name, last_name, email, current_role, [s.strip() for s in skills.split(",") if s.strip()])
    return f"Added {first_name} {last_name}" if store.add(employee) else f"Employee {email} already exists"


@mcp.tool(name="remove_employee", annotations=ToolAnnotations(destructive_hint=True))
def remove_employee(email: str) -> str:
    """Removes the employee with the given email."""
    return f"Removed {email}" if store.remove(email) else f"Employee {email} not found"


@mcp.resource("hr://policy/remote-work", name="remote_work_policy", mime_type="text/markdown")
def remote_work_policy() -> str:
    """The company remote work policy."""
    return "# Remote work policy\n\n- Up to 3 remote days per week.\n- Core hours are 10:00 to 15:00 local time.\n- Kitchen and floor staff follow the posted shift plan."


@mcp.resource("hr://employees/{email}", name="employee_profile", mime_type="application/json")
def employee_profile(email: str) -> dict:
    """The profile of one employee, addressed by email."""
    employee = store.find(email)
    if employee is None:
        raise ValueError(f"Employee {email} not found")
    return employee.to_dict()


@mcp.prompt(name="onboarding_plan")
def onboarding_plan(role: str, buddy: str) -> str:
    """Drafts a first-week onboarding plan for a new employee."""
    return (
        f"Draft a first-week onboarding plan for a new {role}. Pair them with {buddy}. "
        "Use list_employees to find colleagues they should meet and read hr://policy/remote-work for scheduling rules."
    )


if __name__ == "__main__":
    if "--http" in sys.argv:
        mcp.run("streamable-http", host="127.0.0.1", port=47003)
    else:
        mcp.run("stdio")
