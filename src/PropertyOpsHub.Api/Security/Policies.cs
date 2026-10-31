namespace PropertyOpsHub.Api.Security;

/// <summary>Authorization policy names, kept as constants so a typo fails to compile.</summary>
public static class Policies
{
    /// <summary>Group finance or IT administration: create and retire properties.</summary>
    public const string ManageProperties = "properties:manage";

    /// <summary>Any authenticated caller with a role this API knows about.</summary>
    public const string AuthenticatedUser = "authenticated";
}
