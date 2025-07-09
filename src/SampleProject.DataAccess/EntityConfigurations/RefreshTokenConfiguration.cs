using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SampleProject.Core.Models.Entities;
using SampleProject.Core.Models.Enums;

namespace SampleProject.DataAccess.EntityConfigurations;

public sealed class RefreshTokenConfiguration : AuditEntityBaseConfiguration<RefreshToken, Guid>
{
    public override void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.RefreshTokenHash)
            .IsRequired();

        builder.Property(x => x.JwtId)
            .IsRequired();

        builder.Property(x => x.ExpiresAtUtc)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.DeactivationReason)
            .HasConversion(
                enumValue => enumValue.ToStringFast(),
                stringValue => Common.ToEnumFast<RefreshTokenDeactivationReason>(stringValue, RefreshTokenDeactivationReasonExtensions.TryParse))
            .IsRequired();
        
        builder.HasIndex(x => x.RefreshTokenHash)
            .IsUnique();
        
        builder.HasIndex(x => x.JwtId)
            .IsUnique();
        
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}