using System.Collections.Concurrent;

namespace HrMcpServer.Models;

public class EmployeeStore
{
    private readonly ConcurrentDictionary<string, Employee> employees = new(StringComparer.OrdinalIgnoreCase);

    public EmployeeStore()
    {
        foreach (var employee in Seed())
        {
            employees[employee.Email] = employee;
        }
    }

    public IReadOnlyList<Employee> All() => employees.Values.OrderBy(e => e.LastName).ToList();

    public Employee? Find(string email) => employees.GetValueOrDefault(email);

    public bool Add(Employee employee) => employees.TryAdd(employee.Email, employee);

    public bool Remove(string email) => employees.TryRemove(email, out _);

    public IReadOnlyList<Employee> Search(string term) => All()
        .Where(e => $"{e.FirstName} {e.LastName} {e.Email} {e.CurrentRole} {string.Join(' ', e.Skills)}"
            .Contains(term, StringComparison.OrdinalIgnoreCase))
        .ToList();

    private static Employee[] Seed() =>
    [
        new("Carlos", "Gomez", "carlos.gomez@restaurant.com", "Sous Chef", ["Menu Planning", "Food Safety"], ["Spanish", "English", "French"]),
        new("Marie", "Dubois", "marie.dubois@restaurant.com", "Pastry Chef", ["Dessert Creation", "Baking"], ["French", "English", "Italian"]),
        new("Luca", "Rossi", "luca.rossi@restaurant.com", "Head Waiter", ["Wine Pairing", "Table Management"], ["Italian", "English", "Spanish"]),
        new("Anna", "Huber", "anna.huber@restaurant.com", "Restaurant Manager", ["Scheduling", "Team Leadership"], ["German", "English"])
    ];
}
