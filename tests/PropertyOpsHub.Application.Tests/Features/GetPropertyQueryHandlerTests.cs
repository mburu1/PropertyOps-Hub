using System;
using System.Threading;
using System.Threading.Tasks;
using PropertyOpsHub.Application.Abstractions;
using PropertyOpsHub.Application.Common;
using PropertyOpsHub.Application.Features.Organization.Properties;
using PropertyOpsHub.Application.Features.Organization.Properties.GetProperty;
using PropertyOpsHub.Application.Security;
using PropertyOpsHub.Application.Tests.Fakes;
using PropertyOpsHub.Domain.Organization;

namespace PropertyOpsHub.Application.Tests.Features;

public class GetPropertyQueryHandlerTests
{
    private readonly FakePropertyRepository _properties = new();
    private readonly Property _property;

    public GetPropertyQueryHandlerTests()
    {
        _property = Property.Create(GroupId.New(), "Nairobi CBD", "NBO-CBD", "Africa/Nairobi");
        _properties.Add(_property);
    }

    [Fact]
    public async Task Group_finance_may_read_any_property()
    {
        var result = await Handle(FakeCurrentUser.InRole(Roles.GroupFinance));

        Assert.True(result.IsSuccess);
        Assert.Equal(_property.Id.Value, result.Value.Id);
        Assert.Equal("NBO-CBD", result.Value.Code);
    }

    [Fact]
    public async Task A_property_scoped_user_may_read_their_own_property()
    {
        var result = await Handle(FakeCurrentUser.WithProperties(_property.Id.Value));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task A_property_scoped_user_gets_not_found_for_another_property()
    {
        // 404 rather than 403: a 403 would confirm the id exists.
        var result = await Handle(FakeCurrentUser.WithProperties(Guid.CreateVersion7()));

        Assert.Equal(ErrorKind.NotFound, result.Error!.Kind);
        Assert.Equal("organization.property.not_found", result.Error.Code);
    }

    [Fact]
    public async Task Anonymous_callers_are_rejected_before_any_lookup()
    {
        var result = await Handle(FakeCurrentUser.Anonymous());

        Assert.Equal(ErrorKind.Unauthorized, result.Error!.Kind);
    }

    [Fact]
    public async Task Missing_properties_are_not_found()
    {
        var handler = new GetPropertyQueryHandler(_properties, FakeCurrentUser.InRole(Roles.GroupFinance));

        var result = await handler.Handle(new GetPropertyQuery(Guid.CreateVersion7()), CancellationToken.None);

        Assert.Equal(ErrorKind.NotFound, result.Error!.Kind);
    }

    [Fact]
    public async Task Soft_deleted_properties_are_not_found()
    {
        _property.Delete();

        var result = await Handle(FakeCurrentUser.InRole(Roles.GroupFinance));

        Assert.Equal(ErrorKind.NotFound, result.Error!.Kind);
    }

    private Task<Result<PropertyDto>> Handle(ICurrentUser user) =>
        new GetPropertyQueryHandler(_properties, user).Handle(
            new GetPropertyQuery(_property.Id.Value),
            CancellationToken.None);
}
