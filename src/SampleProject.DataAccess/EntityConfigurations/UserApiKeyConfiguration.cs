using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SampleProject.Core.Models.Entities;

namespace SampleProject.DataAccess.EntityConfigurations;

public sealed class UserApiKeyConfiguration : AuditEntityBaseConfiguration<UserApiKey, Guid>
{
    public override void Configure(EntityTypeBuilder<UserApiKey> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.SecretHash).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasIndex(x => x.UserId);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
