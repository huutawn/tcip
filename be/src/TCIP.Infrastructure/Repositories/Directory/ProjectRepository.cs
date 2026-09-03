using Microsoft.EntityFrameworkCore;
using TCIP.Business.Modules.Directory.Application.Ports;
using TCIP.Business.Modules.Directory.Domain.Entities;
using TCIP.Infrastructure.Data;

namespace TCIP.Infrastructure.Repositories.Directory;

public sealed class ProjectRepository(TcipDbContext dbContext) : IProjectRepository
{
    public async Task AddAsync(Project project, CancellationToken cancellationToken)
    {
        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Projects.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Project?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Projects.Include(x => x.Principal).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> OwnerExistsAsync(Guid ownerId, CancellationToken cancellationToken) =>
        dbContext.Users.AnyAsync(x => x.Id == ownerId, cancellationToken);

    public async Task DeleteAsync(Project project, CancellationToken cancellationToken)
    {
        dbContext.Principals.Remove(project.Principal);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
