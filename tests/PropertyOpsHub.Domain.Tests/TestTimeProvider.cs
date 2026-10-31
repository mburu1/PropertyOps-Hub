using System;
using PropertyOpsHub.Domain.Organization;

namespace PropertyOpsHub.Domain.Tests;

/// <summary>Deterministic clock for aggregate tests.</summary>
internal sealed class TestTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _now;

    public TestTimeProvider(DateTimeOffset now) => _now = now;

    public override DateTimeOffset GetUtcNow() => _now;
}
