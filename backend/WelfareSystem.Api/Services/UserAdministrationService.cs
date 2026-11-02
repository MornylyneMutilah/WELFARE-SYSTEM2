using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WelfareSystem.Api.Data;
using WelfareSystem.Api.Models.DTOs.User;
using WelfareSystem.Api.Models.Entities;
using WelfareSystem.Api.Services.Interfaces;

namespace WelfareSystem.Api.Services;

public class UserAdministrationException(string message, int status = 400) : Exception(message)
{
    public int Status { get; } = status;
}

public class UserAdministrationService(AppDbContext context, IPasswordHasher hasher)
{
    private static readonly HashSet<string> AllowedRoles = new() { "Employee", "HROfficer", "CommitteeMember", "Management", "Administrator" };
    private async Task BeginChecks(int actorId)
    {
        // All user administration paths share a transaction lock to protect the last active administrator.
        await context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(81542001)");
        if (!await context.Users.AnyAsync(u => u.Id == actorId && u.IsActive && u.Role.Name == "Administrator"))
            throw new UserAdministrationException("Administrator access is required.", 403);
    }

    private async Task Validate(string email, string employeeId, int departmentId, int roleId, DateTime? hireDate, int? exceptId = null)
    {
        if (hireDate == null || hireDate.Value.Date > DateTime.UtcNow.Date || hireDate.Value.Year < 1900)
            throw new UserAdministrationException("Enter a valid hire date that is not in the future.");
        if (!await context.Departments.AnyAsync(d => d.Id == departmentId)) throw new UserAdministrationException("Select an existing department.");
        var role = await context.Roles.FirstOrDefaultAsync(r => r.Id == roleId);
        if (role == null || !AllowedRoles.Contains(role.Name)) throw new UserAdministrationException("Select a permitted account role.");
        if (await context.Users.AnyAsync(u => u.Id != exceptId && u.Email.ToLower() == email.Trim().ToLower()))
            throw new UserAdministrationException("This email address is already in use.", 409);
        if (await context.Users.AnyAsync(u => u.Id != exceptId && u.EmployeeId.ToLower() == employeeId.Trim().ToLower()))
            throw new UserAdministrationException("This employee ID is already in use.", 409);
    }

    private static object Snapshot(User user) => new { user.Email, user.FirstName, user.LastName, user.EmployeeId,
        user.RoleId, user.DepartmentId, user.HireDate, user.IsActive };
    private static DateTime Now() { var now = DateTime.UtcNow; return now.AddTicks(-(now.Ticks % 10)); }
    private void Audit(User user, int actorId, string action, string reason, object? before) => context.AuditLogs.Add(new AuditLog {
        UserId = actorId, Action = action, EntityType = "User", EntityId = user.Id.ToString(),
        Details = JsonSerializer.Serialize(new { reason = reason.Trim(), before, after = Snapshot(user) })
    });
    private static void ValidateReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 3 || reason.Length > 1000)
            throw new UserAdministrationException("Provide a reason of 3–1000 characters for the audit record.");
    }

    public async Task<UserResponse> UpdateAsync(int id, UpdateUserRequest request, int actorId)
    {
        ValidateReason(request.Reason);
        await using var tx = context.Database.CurrentTransaction == null ? await context.Database.BeginTransactionAsync() : null;
        await BeginChecks(actorId);
        var user = await context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new UserAdministrationException("User not found.", 404);
        await context.Entry(user).ReloadAsync();
        if (request.ExpectedUpdatedAt.HasValue && Math.Abs((request.ExpectedUpdatedAt.Value.ToUniversalTime() - user.UpdatedAt).Ticks) > 9)
            throw new UserAdministrationException("This user was changed by another administrator. Refresh and reopen the edit form.", 409);
        await Validate(request.Email, request.EmployeeId, request.DepartmentId, request.RoleId, request.HireDate, id);
        var administratorRole = await context.Roles.FirstAsync(r => r.Name == "Administrator");
        var removesAdmin = !request.IsActive || request.RoleId != administratorRole.Id;
        if (id == actorId && removesAdmin)
            throw new UserAdministrationException("You cannot suspend or remove your own administrator access. Ask another administrator.");
        if (user.IsActive && user.RoleId == administratorRole.Id && removesAdmin &&
            await context.Users.CountAsync(u => u.IsActive && u.RoleId == administratorRole.Id) <= 1)
            throw new UserAdministrationException("The last active administrator cannot be suspended or demoted.");
        var before = Snapshot(user);
        var wasActive = user.IsActive;
        if (user.IsActive != request.IsActive || user.RoleId != request.RoleId || user.DepartmentId != request.DepartmentId)
            user.TokenVersion++;
        user.Email = request.Email.Trim(); user.FirstName = request.FirstName.Trim(); user.LastName = request.LastName.Trim();
        user.EmployeeId = request.EmployeeId.Trim(); user.RoleId = request.RoleId; user.DepartmentId = request.DepartmentId;
        user.HireDate = DateTime.SpecifyKind(request.HireDate!.Value.Date, DateTimeKind.Utc);
        user.IsActive = request.IsActive; user.UpdatedAt = Now();
        Audit(user, actorId, wasActive != user.IsActive ? (user.IsActive ? "UserReactivated" : "UserSuspended") : "UserUpdated", request.Reason, before);
        await context.SaveChangesAsync();
        if (tx != null) await tx.CommitAsync();
        return await Response(id);
    }

    public async Task<UserResponse> SetStatusAsync(int id, bool active, string reason, DateTime? expectedUpdatedAt, int actorId)
    {
        await using var tx = context.Database.CurrentTransaction == null ? await context.Database.BeginTransactionAsync() : null;
        await BeginChecks(actorId);
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id) ?? throw new UserAdministrationException("User not found.", 404);
        await context.Entry(user).ReloadAsync();
        var result = await UpdateAsync(id, new UpdateUserRequest { Email = user.Email, FirstName = user.FirstName, LastName = user.LastName,
            EmployeeId = user.EmployeeId, DepartmentId = user.DepartmentId, RoleId = user.RoleId, HireDate = user.HireDate,
            IsActive = active, Reason = reason, ExpectedUpdatedAt = expectedUpdatedAt }, actorId);
        if (tx != null) await tx.CommitAsync();
        return result;
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request, int actorId)
    {
        ValidateReason(request.Reason);
        await using var tx = context.Database.CurrentTransaction == null ? await context.Database.BeginTransactionAsync() : null;
        await BeginChecks(actorId);
        await Validate(request.Email, request.EmployeeId, request.DepartmentId, request.RoleId, request.HireDate);
        var user = new User { Email = request.Email.Trim(), FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(),
            EmployeeId = request.EmployeeId.Trim(), DepartmentId = request.DepartmentId, RoleId = request.RoleId,
            HireDate = DateTime.SpecifyKind(request.HireDate!.Value.Date, DateTimeKind.Utc), PasswordHash = hasher.HashPassword(request.Password),
            IsActive = true, CreatedAt = Now(), UpdatedAt = Now() };
        context.Users.Add(user); await context.SaveChangesAsync();
        Audit(user, actorId, "UserCreated", request.Reason, null);
        await context.SaveChangesAsync();
        if (tx != null) await tx.CommitAsync();
        return await Response(user.Id);
    }

    private async Task<UserResponse> Response(int id)
    {
        var u = await context.Users.Include(u => u.Role).Include(u => u.Department).FirstAsync(u => u.Id == id);
        return new() { Id = u.Id, Email = u.Email, FirstName = u.FirstName, LastName = u.LastName, EmployeeId = u.EmployeeId,
            DepartmentId = u.DepartmentId, DepartmentName = u.Department.Name, RoleId = u.RoleId, RoleName = u.Role.Name,
            HireDate = u.HireDate, IsActive = u.IsActive, CreatedAt = u.CreatedAt, UpdatedAt = u.UpdatedAt };
    }
}
