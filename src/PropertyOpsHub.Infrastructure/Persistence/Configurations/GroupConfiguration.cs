using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOpsHub.Domain.Common;
using PropertyOpsHub.Domain.Organization;

namespace PropertyOpsHub.Infrastructure.Persistence.Configurations;

/// <summary>
/// Persistence mapping for <see cref="Group"/>. Strongly typed ids and value objects are
/// stored as their underlying primitives, so the schema stays plain SQL Server and reporting
/// tools can read it.
/// </summary>
internal sealed class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("Groups", "org");
        builder.HasKey(group => group.Id);

        builder.Property(group => group.Id)
            .HasConversion(id => id.Value, value => GroupId.From(value))
            .ValueGeneratedNever();

        builder.Property(group => group.Name)
            .HasMaxLength(Group.NameMaxLength)
            .IsRequired();

        builder.Property(group => group.BaseCurrency)
            .HasConversion(currency => currency.Value, value => CurrencyCode.Create(value))
            .HasMaxLength(3)
            .IsFixedLength()
            .IsRequired();

        builder.Property(group => group.IsActive).IsRequired();
        builder.Property(group => group.CreatedAtUtc).IsRequired();
        builder.Property(group => group.ModifiedAtUtc);
        builder.Property(group => group.IsDeleted).IsRequired();

        builder.HasIndex(group => group.IsDeleted)
            .HasDatabaseName("IX_Groups_IsDeleted");

        builder.Ignore(group => group.DomainEvents);
    }
}
