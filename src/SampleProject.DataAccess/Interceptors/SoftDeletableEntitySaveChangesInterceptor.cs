using Kirpichyov.FriendlyJwt.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using SampleProject.Core.Contracts;

namespace SampleProject.DataAccess.Interceptors;

public sealed class SoftDeletableEntitySaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IServiceProvider _serviceProvider;

    public SoftDeletableEntitySaveChangesInterceptor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateSoftDeletableEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateSoftDeletableEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }
    
    private void UpdateSoftDeletableEntities(DbContext dbContext)
    {
        using var scope = _serviceProvider.CreateScope();
        var jwtTokenReader = scope.ServiceProvider.GetRequiredService<IJwtTokenReader>();
        
        var entries = dbContext.ChangeTracker.Entries()
            .Where(e => e is { Entity: IAuditEntity, State: EntityState.Deleted });

        foreach (var entry in entries)
        {
            var entity = (ISoftDeletable)entry.Entity;
            var userEmail = jwtTokenReader.UserEmail;

            switch (entry.State)
            {
                case EntityState.Deleted:
                    entity.MarkAsDeleted(userEmail, DateTimeOffset.UtcNow);
                    
                    entry.State = EntityState.Modified;
                    dbContext.Entry(entity).Property(x => x.IsDeleted).IsModified = true;
                    dbContext.Entry(entity).Property(x => x.DeletedAtUtc).IsModified = true;
                    dbContext.Entry(entity).Property(x => x.DeletedBy).IsModified = true;
                    
                    break;
            }
        }
    }
}