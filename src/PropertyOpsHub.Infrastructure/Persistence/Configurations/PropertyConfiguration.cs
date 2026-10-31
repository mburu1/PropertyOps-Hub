using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOpsHub.Domain.Organization;

namespace PropertyOpsHub.Infrastructure.Persistence.Configurations;

/// <summary>
/// Persistence mapping for <see cref="Property"/>. The property code is unique per group
/// among live rows; the filtered unique index is what actually enforces it under concurrency.
/// </summary>
internal sealed class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("Properties", "org");
        builder.HasKey(property => property.Id);

        builder.Property(property => property.Id)
            .HasConversion(id => id.Value, value => PropertyId.From(value))
            .ValueGeneratedNever();

        builder.Property(property => property.GroupId)
            .HasConversion(id => id.Value, value => GroupId.From(value))
            .IsRequired();

        builder.Property(property => property.Name)
            .HasMaxLength(Property.NameMaxLength)
            .IsRequired();

        builder.Property(property => property.Code)
            .HasConversion(code => code.Value, value => PropertyCode.Create(value))
            .HasMaxLength(PropertyCode.MaxLength)
            .IsRequired();

        builder.Property(property => property.TimeZoneId)
            .HasMaxLength(Property.TimeZoneIdMaxLength)
            .IsRequired();

        builder.Property(property => property.IsActive).IsRequired();
        builder.Property(property => property.CreatedAtUtc).IsRequired();
        builder.Property(property => property.ModifiedAtUtc);
        builder.Property(property => property.IsDeleted).IsRequired();

        // Soft-deleted rows keep their code so history stays readable; only live rows compete.
        builder.HasIndex(property => new { property.GroupId, property.Code })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_Properties_GroupId_Code");

        builder.HasOne<Group>()
            .WithMany()
            .HasForeignKey(property => property.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(property => property.DomainEvents);
    }
}
