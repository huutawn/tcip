using Microsoft.EntityFrameworkCore;
using TCIP.Business.Modules.Directory.Application.Ports;
using TCIP.Business.Modules.Directory.Domain.Entities;
using TCIP.Infrastructure.Data;

namespace TCIP.Infrastructure.Repositories.Directory;

public sealed class DepartmentRepository(TcipDbContext dbContext) : IDepartmentRepository
{
    public Task<bool> ExistsByNameAsync(string name, Guid? exceptId, CancellationToken cancellationToken) =>
        dbContext.Departments.AnyAsync(x => x.Name == name && (!exceptId.HasValue || x.Id != exceptId.Value), cancellationToken);

    public async Task CreateDepartmentAsync(Department department, PrincipalMembership membership, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.Departments.Add(department);
        dbContext.PrincipalMemberships.Add(membership);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public Task<Department?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Departments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Department?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Departments.Include(x => x.Principal).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task DeleteAsync(Department department, CancellationToken cancellationToken)
    {
        dbContext.Principals.Remove(department.Principal);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
