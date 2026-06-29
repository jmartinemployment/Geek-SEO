namespace GeekSeo.Application.Models.Seo;

/// <summary>Provenance tag for spoke documents created from cluster planning.</summary>
public static class SpokeSourceTypes
{
    public const string Paa = "paa";
    public const string Pasf = "pasf";
    public const string Manual = "manual";
    public const string Migrated = "migrated";

    public static bool IsKnown(string? value) =>
        value is Paa or Pasf or Manual or Migrated;
}
