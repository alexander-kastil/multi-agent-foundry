namespace HrMcpServer.Models;

public record Employee(string FirstName, string LastName, string Email, string CurrentRole, string[] Skills, string[] SpokenLanguages);
