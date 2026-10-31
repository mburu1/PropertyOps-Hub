namespace PropertyOpsHub.Domain.MaterialControl;

/// <summary>
/// Coarse grouping for reporting and for default ledger accounts. Values are persisted, so a
/// number must never be reused for a different meaning.
/// </summary>
public enum ItemCategory
{
    Food = 1,
    Beverage = 2,
    Consumable = 3,
    Amenity = 4,
    Maintenance = 5,
    Other = 99
}

/// <summary>How the cost of an item is derived when stock leaves the store.</summary>
public enum CostingMethod
{
    /// <summary>Value is blended on every receipt; issues take the current average.</summary>
    WeightedAverage = 1
}
