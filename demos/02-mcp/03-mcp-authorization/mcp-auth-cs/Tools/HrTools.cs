using System.ComponentModel;
using McpAuth.Models;
using ModelContextProtocol.Server;

namespace McpAuth.Tools;

[McpServerToolType]
public class HrTools(EmployeeStore store)
{
    [McpServerTool(Name = "list_employees", ReadOnly = true)]
    [Description("Lists every employee. Use search_employees to narrow the list down.")]
    public IReadOnlyList<Employee> ListEmployees() => store.All();

    [McpServerTool(Name = "search_employees", ReadOnly = true)]
    [Description("Finds employees whose name, email, role or skills contain the search term.")]
    public IReadOnlyList<Employee> SearchEmployees(
        [Description("Text to look for, for example a skill such as baking")] string searchTerm) => store.Search(searchTerm);

    [McpServerTool(Name = "add_employee", Destructive = false)]
    [Description("Adds a new employee. Fails when the email already exists.")]
    public string AddEmployee(
        [Description("First name")] string firstName,
        [Description("Last name")] string lastName,
        [Description("Unique email address")] string email,
        [Description("Current role")] string currentRole,
        [Description("Comma-separated skills")] string skills = "") =>
        store.Add(new Employee(firstName, lastName, email, currentRole, Split(skills), []))
            ? $"Added {firstName} {lastName}"
            : $"Employee {email} already exists";

    [McpServerTool(Name = "remove_employee", Destructive = true)]
    [Description("Removes the employee with the given email.")]
    public string RemoveEmployee([Description("Email of the employee to remove")] string email) =>
        store.Remove(email) ? $"Removed {email}" : $"Employee {email} not found";

    private static string[] Split(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
