using Microsoft.EntityFrameworkCore;
using PropertyOpsHub.Domain.Organization;
using PropertyOpsHub.Infrastructure.Persistence;

namespace PropertyOpsHub.Infrastructure.Tests;

/// <summary>Shared arrange helpers, always written with a system (unfiltered) context.</summary>
internal static class Seed
{
    public const string DefaultCode = "NBO-CBD";

    public static (Group Group, Property Property, Outlet Outlet) Organization(
        AppDbContext context,
        string groupName = "Serena Group",
        string propertyCode = DefaultCode)
    {
        var group = Group.Create(groupName, "KES");
        var property = Property.Create(group.Id, "Nairobi CBD", propertyCode, "Africa/Nairobi");
        var outlet = Outlet.Create(property.Id, "Pool Bar", OutletType.Bar);

        context.Groups.Add(group);
        context.Properties.Add(property);
        context.Outlets.Add(outlet);
        context.SaveChanges();

        return (group, property, outlet);
    }
}
