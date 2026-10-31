using System;
using System.Linq;
using PropertyOpsHub.Domain.Common;
using PropertyOpsHub.Domain.Organization;

namespace PropertyOpsHub.Domain.Tests.Organization;

public class GroupTests
{
    [Fact]
    public void Create_sets_defaults_and_raises_a_domain_event()
    {
        var now = new DateTimeOffset(2026, 10, 31, 9, 30, 0, TimeSpan.Zero);

        var group = Group.Create("  Serena Group  ", " kes ", new TestTimeProvider(now));

        Assert.Equal("Serena Group", group.Name);
        Assert.Equal("KES", group.BaseCurrency.Value);
        Assert.True(group.IsActive);
        Assert.Equal(now, group.CreatedAtUtc);
        Assert.Null(group.ModifiedAtUtc);
        Assert.False(group.IsDeleted);
        Assert.False(group.Id.IsEmpty);

        var created = Assert.IsType<GroupCreated>(Assert.Single(group.DomainEvents));
        Assert.Equal(group.Id, created.GroupId);
        Assert.Equal("Serena Group", created.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_an_empty_name(string name)
    {
        var exception = Assert.Throws<DomainException>(() => Group.Create(name, "USD"));

        Assert.Equal("domain.value.required", exception.Code);
    }

    [Fact]
    public void Create_rejects_a_name_longer_than_the_limit()
    {
        var name = new string('a', Group.NameMaxLength + 1);

        var exception = Assert.Throws<DomainException>(() => Group.Create(name, "USD"));

        Assert.Equal("domain.value.too_long", exception.Code);
    }

    [Fact]
    public void Create_rejects_an_invalid_currency()
    {
        var exception = Assert.Throws<DomainException>(() => Group.Create("Serena", "KS"));

        Assert.Equal("finance.currency.invalid", exception.Code);
    }

    [Fact]
    public void Create_rejects_a_well_formed_but_unknown_currency()
    {
        // KSH is the everyday abbreviation in Kenya, but ISO-4217 says KES.
        var exception = Assert.Throws<DomainException>(() => Group.Create("Serena", "KSH"));

        Assert.Equal("finance.currency.unknown", exception.Code);
    }

    [Fact]
    public void Rename_trims_and_rejects_empty_values()
    {
        var group = Group.Create("Serena", "KES");

        group.Rename("  Serena Holdings ");

        Assert.Equal("Serena Holdings", group.Name);
        Assert.Throws<DomainException>(() => group.Rename("  "));
    }

    [Fact]
    public void ChangeBaseCurrency_normalises_the_code()
    {
        var group = Group.Create("Serena", "KES");

        group.ChangeBaseCurrency("usd");

        Assert.Equal("USD", group.BaseCurrency.Value);
    }

    [Fact]
    public void Deactivate_and_activate_toggle_the_flag()
    {
        var group = Group.Create("Serena", "KES");

        group.Deactivate();
        Assert.False(group.IsActive);

        group.Activate();
        Assert.True(group.IsActive);
    }

    [Fact]
    public void Delete_marks_the_aggregate_as_soft_deleted()
    {
        var group = Group.Create("Serena", "KES");

        group.Delete();

        Assert.True(group.IsDeleted);
    }

    [Fact]
    public void Entities_with_the_same_id_are_equal()
    {
        var created = Group.Create("Serena", "KES");
        var sameId = Group.Create("Other", "USD");

        Assert.NotEqual(created, sameId);
        Assert.Equal(created, created);
    }

    [Fact]
    public void ClearDomainEvents_empties_the_event_list()
    {
        var group = Group.Create("Serena", "KES");
        Assert.NotEmpty(group.DomainEvents);

        group.ClearDomainEvents();

        Assert.Empty(group.DomainEvents);
    }

    [Fact]
    public void Domain_events_are_value_equal_for_testing()
    {
        var first = Group.Create("Serena", "KES");
        var second = Group.Create("Serena", "KES");

        Assert.IsType<GroupCreated>(first.DomainEvents.Single());
        Assert.NotEqual(first.DomainEvents.Single(), second.DomainEvents.Single());
    }
}
