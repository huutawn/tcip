using Microsoft.EntityFrameworkCore;
using TCIP.Business.Modules.Directory.Application.Ports;
using TCIP.Business.Modules.Directory.Domain.Entities;
using TCIP.Infrastructure.Data;

namespace TCIP.Infrastructure.Repositories.Directory;

public sealed class GroupRepository(TcipDbContext dbContext) : IGroupRepository
{
    public Task<bool> ExistsByNameAndTypeAsync(string name, string type, CancellationToken cancellationToken) =>
        dbContext.Groups.AnyAsync(x => x.Name == name && x.Type == type, cancellationToken);

    public async Task AddAsync(Group group, CancellationToken cancellationToken)
    {
        dbContext.Groups.Add(group);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
