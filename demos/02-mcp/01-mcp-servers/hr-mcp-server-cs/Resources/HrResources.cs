using System.ComponentModel;
using System.Text.Json;
using HrMcpServer.Models;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace HrMcpServer.Resources;

[McpServerResourceType]
public class HrResources(EmployeeStore store)
{
    [McpServerResource(UriTemplate = "hr://policy/remote-work", Name = "remote_work_policy", MimeType = "text/markdown")]
    [Description("The company remote work policy.")]
    public string RemoteWorkPolicy() =>
        "# Remote work policy\n\n- Up to 3 remote days per week.\n- Core hours are 10:00 to 15:00 local time.\n- Kitchen and floor staff follow the posted shift plan.";

    [McpServerResource(UriTemplate = "hr://employees/{email}", Name = "employee_profile", MimeType = "application/json")]
    [Description("The profile of one employee, addressed by email.")]
    public string EmployeeProfile(string email) =>
        JsonSerializer.Serialize(store.Find(email) ?? throw new McpException($"Employee {email} not found"));
}
