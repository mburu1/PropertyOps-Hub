using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyOpsHub.Domain.Organization;

namespace PropertyOpsHub.Infrastructure.Persistence.Configurations;

/// <summary>Persistence mapping for <see cref="Outlet"/>.</summary>
internal sealed class OutletConfiguration : IEntityTypeConfiguration<Outlet>
{
    public void Configure(EntityTypeBuilder<Outlet> builder)
    {
        builder.ToTable("Outlets", "org");
        builder.HasKey(outlet => outlet.Id);

        builder.Property(outlet => outlet.Id)
            .HasConversion(id => id.Value, value => OutletId.From(value))
            .ValueGeneratedNever();

        builder.Property(outlet => outlet.PropertyId)
            .HasConversion(id => id.Value, value => PropertyId.From(value))
            .IsRequired();

        builder.Property(outlet => outlet.Name)
            .HasMaxLength(Outlet.NameMaxLength)
            .IsRequired();

        // Stored by name, not by number, so a database reader can see what an outlet is.
        builder.Property(outlet => outlet.Type)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(outlet => outlet.IsActive).IsRequired();
        builder.Property(outlet => outlet.CreatedAtUtc).IsRequired();
        builder.Property(outlet => outlet.ModifiedAtUtc);
        builder.Property(outlet => outlet.IsDeleted).IsRequired();

        builder.HasIndex(outlet => new { outlet.PropertyId, outlet.IsDeleted })
            .HasDatabaseName("IX_Outlets_PropertyId_IsDeleted");

        builder.HasOne<Property>()
            .WithMany()
            .HasForeignKey(outlet => outlet.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(outlet => outlet.DomainEvents);
    }
}
