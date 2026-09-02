using Microsoft.EntityFrameworkCore;
using TCIP.Business.Modules.AccessControl.Domain.Entities;
using TCIP.Business.Modules.Calendar.Domain.Entities;
using TCIP.Business.Modules.Directory.Domain.Entities;
using TCIP.Business.Modules.Identity.Domain.Entities;

namespace TCIP.Infrastructure.Data;

public sealed class TcipDbContext(DbContextOptions<TcipDbContext> options) : DbContext(options)
{
    // Identity
    public DbSet<User> Users => Set<User>();
    public DbSet<Session> Sessions => Set<Session>();

    // Directory
    public DbSet<Principal> Principals => Set<Principal>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectTranslation> ProjectTranslations => Set<ProjectTranslation>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<UserPosition> UserPositions => Set<UserPosition>();
    public DbSet<PrincipalMembership> PrincipalMemberships => Set<PrincipalMembership>();

    // AccessControl
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RoleAssignment> RoleAssignments => Set<RoleAssignment>();
    public DbSet<PermissionGrant> PermissionGrants => Set<PermissionGrant>();

    // Calendar
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventAudience> EventAudiences => Set<EventAudience>();
    public DbSet<EventTranslation> EventTranslations => Set<EventTranslation>();
    public DbSet<EventOccurrenceException> EventOccurrenceExceptions => Set<EventOccurrenceException>();
    public DbSet<ReminderRule> ReminderRules => Set<ReminderRule>();
    public DbSet<ReminderSchedule> ReminderSchedules => Set<ReminderSchedule>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TcipDbContext).Assembly);
    }
}
