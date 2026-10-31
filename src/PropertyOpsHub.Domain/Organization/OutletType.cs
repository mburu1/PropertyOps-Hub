namespace PropertyOpsHub.Domain.Organization;

/// <summary>
/// Revenue-generating unit inside a property. Values are persisted as strings, so the
/// numbers must never be reused for a different meaning.
/// </summary>
public enum OutletType
{
    Restaurant = 1,
    Bar = 2,
    Banquet = 3,
    RoomService = 4,
    Spa = 5,
    Retail = 6
}
