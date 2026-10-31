using System;
using System.Threading;
using System.Threading.Tasks;
using PropertyOpsHub.Application.Abstractions;
using PropertyOpsHub.Application.Common;
using PropertyOpsHub.Application.Features.Organization.Properties.CreateProperty;
using PropertyOpsHub.Application.Security;
using PropertyOpsHub.Application.Tests.Fakes;
using PropertyOpsHub.Domain.Organization;

namespace PropertyOpsHub.Application.Tests.Features;

public class CreatePropertyCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 31, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeGroupRepository _groups = new();
    private readonly FakePropertyRepository _properties = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeCurrentUser _currentUser = FakeCurrentUser.InRole(Roles.ItAdmin);
    private readonly Group _seedGroup;

    public CreatePropertyCommandHandlerTests()
    {
        _seedGroup = Group.Create("Serena Group", "KES");
        _groups.Add(_seedGroup);
    }

    [Fact]
    public async Task Handle_creates_the_property_and_commits_once()
    {
        var handler = Handler();

        var result = await handler.Handle(Command(_seedGroup.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);

        var created = Assert.Single(_properties.Items);
        Assert.Equal(result.Value, created.Id.Value);
        Assert.Equal(_seedGroup.Id, created.GroupId);
        Assert.Equal("NBO-CBD", created.Code.Value);
        Assert.Equal(Now, created.CreatedAtUtc);
        Assert.Single(created.DomainEvents);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_rejects_an_unauthenticated_caller()
    {
        var result = await Handler(FakeCurrentUser.Anonymous())
            .Handle(Command(_seedGroup.Id), CancellationToken.None);

        Assert.Equal(ErrorKind.Unauthorized, result.Error!.Kind);
        Assert.Empty(_properties.Items);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_rejects_a_caller_without_the_property_management_role()
    {
        var result = await Handler(FakeCurrentUser.InRole(Roles.StoreController))
            .Handle(Command(_seedGroup.Id), CancellationToken.None);

        Assert.Equal(ErrorKind.Forbidden, result.Error!.Kind);
        Assert.Equal("organization.property.forbidden", result.Error.Code);
        Assert.Empty(_properties.Items);
    }

    [Fact]
    public async Task Handle_returns_not_found_for_an_unknown_group()
    {
        var result = await Handler().Handle(Command(GroupId.New()), CancellationToken.None);

        Assert.Equal(ErrorKind.NotFound, result.Error!.Kind);
        Assert.Equal("organization.group.not_found", result.Error.Code);
    }

    [Fact]
    public async Task Handle_returns_not_found_for_a_soft_deleted_group()
    {
        var deleted = Group.Create("Deleted Group", "USD");
        deleted.Delete();
        _groups.Add(deleted);

        var result = await Handler().Handle(Command(deleted.Id), CancellationToken.None);

        Assert.Equal(ErrorKind.NotFound, result.Error!.Kind);
    }

    [Fact]
    public async Task Handle_returns_conflict_when_the_group_is_inactive()
    {
        var closed = Group.Create("Closed Group", "USD");
        closed.Deactivate();
        _groups.Add(closed);

        var result = await Handler().Handle(Command(closed.Id), CancellationToken.None);

        Assert.Equal(ErrorKind.Conflict, result.Error!.Kind);
        Assert.Equal("organization.group.inactive", result.Error.Code);
    }

    [Fact]
    public async Task Handle_returns_conflict_when_the_code_is_already_used()
    {
        var handler = Handler();

        await handler.Handle(Command(_seedGroup.Id, "NBO-CBD"), CancellationToken.None);
        var second = await handler.Handle(Command(_seedGroup.Id, "nbo-cbd"), CancellationToken.None);

        Assert.Equal(ErrorKind.Conflict, second.Error!.Kind);
        Assert.Equal("organization.property.code_duplicate", second.Error.Code);
        Assert.Single(_properties.Items);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_allows_the_same_code_in_a_different_group()
    {
        var otherGroup = Group.Create("Other Group", "USD");
        _groups.Add(otherGroup);
        var handler = Handler();

        await handler.Handle(Command(_seedGroup.Id, "NBO-CBD"), CancellationToken.None);
        var result = await handler.Handle(Command(otherGroup.Id, "NBO-CBD"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, _properties.Items.Count);
    }

    [Fact]
    public async Task Handle_returns_a_validation_failure_for_a_malformed_code()
    {
        var result = await Handler().Handle(Command(_seedGroup.Id, "NBO CBD"), CancellationToken.None);

        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Equal("organization.property.code_invalid", result.Error.Code);
    }

    private CreatePropertyCommandHandler Handler(ICurrentUser? user = null) =>
        new(
            _groups,
            _properties,
            _unitOfWork,
            user ?? _currentUser,
            new TestTimeProvider(Now));

    private static CreatePropertyCommand Command(GroupId groupId, string code = "NBO-CBD") =>
        new(groupId.Value, "Nairobi CBD", code, "Africa/Nairobi");
}
