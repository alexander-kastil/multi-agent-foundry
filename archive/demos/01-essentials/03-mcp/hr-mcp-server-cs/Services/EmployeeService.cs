using HRMCPServer.Data;
using Microsoft.EntityFrameworkCore;

namespace HRMCPServer.Services;

/// <summary>
/// Service for managing employee data in memory
/// </summary>
public class EmployeeService(EmployeeDbContext dbContext, ILogger<EmployeeService> logger) : IEmployeeService
{
    private readonly EmployeeDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly ILogger<EmployeeService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<List<Employee>> GetAllEmployeesAsync()
    {
        return await _dbContext.Employees
            .AsNoTracking()
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .ToListAsync();
    }

    public async Task<bool> AddEmployeeAsync(Employee employee)
    {
        ArgumentNullException.ThrowIfNull(employee);

        var email = employee.Email.Trim();

        if (await _dbContext.Employees.AnyAsync(c => c.Email == email))
        {
            return false;
        }

        employee.Email = email;

        await _dbContext.Employees.AddAsync(employee);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Added new employee: {FullName} ({Email})", employee.FullName, employee.Email);
        return true;
    }

    public async Task<bool> UpdateEmployeeAsync(string email, Action<Employee> updateAction)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be null or empty", nameof(email));

        ArgumentNullException.ThrowIfNull(updateAction);

        var normalizedEmail = email.Trim();

        var employee = await _dbContext.Employees
            .FirstOrDefaultAsync(c => c.Email == normalizedEmail);

        if (employee == null)
        {
            return false;
        }

        updateAction(employee);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Updated employee with email: {Email}", normalizedEmail);
        return true;
    }

    public async Task<bool> RemoveEmployeeAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be null or empty", nameof(email));

        var normalizedEmail = email.Trim();

        var employee = await _dbContext.Employees
            .FirstOrDefaultAsync(c => c.Email == normalizedEmail);

        if (employee == null)
        {
            return false;
        }

        _dbContext.Employees.Remove(employee);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Removed employee with email: {Email}", normalizedEmail);
        return true;
    }

    public async Task<List<Employee>> SearchEmployeesAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await GetAllEmployeesAsync();
        }

        var searchTermLower = searchTerm.Trim().ToLowerInvariant();

        // Use EF.Functions.Like for better performance on supported databases
        var employees = await _dbContext.Employees
            .AsNoTracking()
            .Where(c =>
                EF.Functions.Like(c.FirstName, $"%{searchTerm}%") ||
                EF.Functions.Like(c.LastName, $"%{searchTerm}%") ||
                EF.Functions.Like(c.Email, $"%{searchTerm}%") ||
                EF.Functions.Like(c.CurrentRole, $"%{searchTerm}%"))
            .ToListAsync();

        // Client-side filtering for JSON arrays (Skills and SpokenLanguages)
        var clientSideMatches = await _dbContext.Employees
            .AsNoTracking()
            .ToListAsync()
            .ContinueWith(t => t.Result.Where(c =>
                c.Skills.Exists(skill => skill.Contains(searchTermLower, StringComparison.OrdinalIgnoreCase)) ||
                c.SpokenLanguages.Exists(lang => lang.Contains(searchTermLower, StringComparison.OrdinalIgnoreCase))
            ).ToList());

        return [.. employees.Union(clientSideMatches).DistinctBy(e => e.Id)];
    }
}
