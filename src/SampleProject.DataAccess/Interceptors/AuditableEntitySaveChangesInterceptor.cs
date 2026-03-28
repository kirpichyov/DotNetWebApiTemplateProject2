using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using SampleProject.Core.Contracts;

namespace SampleProject.DataAccess.Interceptors;

public sealed class AuditableEntitySaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IServiceProvider _serviceProvider;

    public AuditableEntitySaveChangesInterceptor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateAuditableEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateAuditableEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }
    
    private void UpdateAuditableEntities(DbContext dbContext)
    {
        using var scope = _serviceProvider.CreateScope();
        var authContext = scope.ServiceProvider.GetRequiredService<IAuthContext>();
        var actor = authContext.IsLoggedIn ? authContext.Username : "system";
        
        var entries = dbContext.ChangeTracker.Entries()
            .Where(e => e is { Entity: IAuditEntity, State: EntityState.Added or EntityState.Modified });

        foreach (var entry in entries)
        {
            var entity = (IAuditEntity)entry.Entity;

            switch (entry.State)
            {
                case EntityState.Added:
                    entity.CreatedAtUtc = DateTimeOffset.UtcNow;
                    entity.CreatedBy = actor;
                    break;
                case EntityState.Modified:
                    entity.UpdatedAtUtc = DateTimeOffset.UtcNow;
                    entity.UpdatedBy = actor;
                    break;
            }
        }
    }
}