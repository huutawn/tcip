using Microsoft.EntityFrameworkCore;
using TCIP.Business.Modules.AccessControl.Application.Ports;
using TCIP.Business.Modules.AccessControl.Domain.Entities;
using TCIP.Infrastructure.Data;

namespace TCIP.Infrastructure.Repositories.AccessControl;

public sealed class RoleAssignmentRepository(TcipDbContext dbContext) : IRoleAssignmentRepository
{
    public async Task<RoleAssignment> CreateRoleAssignmentAsync(RoleAssignment item, CancellationToken ct = default)
    {
        dbContext.RoleAssignments.Add(item);
        await dbContext.SaveChangesAsync(ct);
        return item;
    }

    public Task<RoleAssignment?> GetRoleAssignmentByIdAsync(Guid id, CancellationToken ct = default) =>
        dbContext.RoleAssignments.Include(x => x.Role).FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<RoleAssignment?> GetRoleAssignmentAsync(Guid subjectId, Guid roleId, Guid? resourceId, CancellationToken ct = default) =>
        dbContext.RoleAssignments.Include(x => x.Role).FirstOrDefaultAsync(x => x.SubjectPrincipalId == subjectId && x.RoleId == roleId && x.ResourcePrincipalId == resourceId, ct);

    public async Task<IEnumerable<Role>> GetRolesByPrincipalIdAsync(Guid id, CancellationToken ct = default) =>
        await dbContext.RoleAssignments.Where(x => x.SubjectPrincipalId == id).Select(x => x.Role).Distinct().ToListAsync(ct);

    public async Task<IEnumerable<RoleAssignment>> GetRoleAssignmentsByPrincipalIdAsync(Guid id, CancellationToken ct = default) =>
        await dbContext.RoleAssignments.Include(x => x.Role).Where(x => x.SubjectPrincipalId == id).AsNoTracking().ToListAsync(ct);

    public async Task<IEnumerable<RoleAssignment>> GetRoleAssignmentsByRoleIdAsync(Guid id, CancellationToken ct = default) =>
        await dbContext.RoleAssignments.Include(x => x.Role).Where(x => x.RoleId == id).AsNoTracking().ToListAsync(ct);

    public async Task DeleteRoleAssignmentAsync(RoleAssignment item, CancellationToken ct = default)
    {
        dbContext.RoleAssignments.Remove(item);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteRoleAssignmentsAsync(Guid subjectId, Guid? resourceId, CancellationToken ct = default)
    {
        var items = await dbContext.RoleAssignments.Where(x => x.SubjectPrincipalId == subjectId && x.ResourcePrincipalId == resourceId).ToListAsync(ct);
        if (items.Count == 0) return false;
        dbContext.RoleAssignments.RemoveRange(items);
        await dbContext.SaveChangesAsync(ct);
        return true;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => dbContext.SaveChangesAsync(ct);
}
