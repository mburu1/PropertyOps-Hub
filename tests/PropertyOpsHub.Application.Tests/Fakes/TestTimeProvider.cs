using System;

namespace PropertyOpsHub.Application.Tests.Fakes;

/// <summary>Deterministic clock so timestamps are assertable.</summary>
internal sealed class TestTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _now;

    public TestTimeProvider(DateTimeOffset now) => _now = now;

    public override DateTimeOffset GetUtcNow() => _now;
}
