using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TCIP.Business.Modules.AccessControl.Domain.Entities;
using TCIP.Business.Modules.AccessControl.Domain.Models;
using TCIP.Infrastructure.Data;

namespace TCIP.Api.Security;

public static class RbacBootstrapper
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TcipDbContext>();

        if (db.Database.IsRelational())
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(74291823);", cancellationToken);

            await SeedCoreAsync(db, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        else
        {
            await SeedCoreAsync(db, cancellationToken);
        }
    }

    private static async Task SeedCoreAsync(TcipDbContext db, CancellationToken cancellationToken)
    {
        var existingPermissions = await db.Permissions.ToDictionaryAsync(x => x.Name, StringComparer.OrdinalIgnoreCase, cancellationToken);
        foreach (var definition in BuiltInRbacCatalog.AllPermissions)
        {
            if (existingPermissions.ContainsKey(definition.Name)) continue;
            var permission = new Permission { Id = Guid.NewGuid(), Name = definition.Name, Description = definition.Description };
            db.Permissions.Add(permission);
            existingPermissions[permission.Name] = permission;
        }
        await db.SaveChangesAsync(cancellationToken);

        var existingRoles = await db.Roles.Include(x => x.RolePermissions).ToDictionaryAsync(x => x.Name, StringComparer.OrdinalIgnoreCase, cancellationToken);
        foreach (var definition in BuiltInRbacCatalog.Roles)
        {
            if (!existingRoles.TryGetValue(definition.Name, out var role))
            {
                role = new Role { Id = Guid.NewGuid(), Name = definition.Name, Description = definition.Description };
                db.Roles.Add(role);
                existingRoles[role.Name] = role;
            }

            var permissionIds = definition.Permissions.Select(name => existingPermissions[name].Id).ToHashSet();
            foreach (var permission in existingPermissions.Values.Where(x => definition.Name == BuiltInRbacCatalog.AdminRole || permissionIds.Contains(x.Id)))
            {
                if (role.RolePermissions.Any(x => x.PermissionId == permission.Id)) continue;
                role.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
