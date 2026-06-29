namespace SiteAnalyzer2.Services.BusinessFocus;

internal static class BusinessFocusClassifierPrompt
{
    public const string SystemPrompt = """
        You classify what a business does based on website extraction data.
        Respond with JSON only using keys:
        businessType, primaryServices (string array), serviceArea (string or null),
        description, generatedSchemaJson (valid schema.org JSON object as a string value),
        hasExistingSchema (boolean), existingSchemaMatches (boolean or null).
        """;

    public static string BuildUserPrompt(BusinessFocusInput input) =>
        $"""
         Target site URL: {input.TargetSiteUrl}
         Has existing JSON-LD on site: {input.HasExistingSchema}

         Headings:
         {string.Join('\n', input.Headings.Take(40))}

         Meta tags:
         {string.Join('\n', input.MetaTags.Take(20))}

         JSON-LD blocks:
         {string.Join("\n---\n", input.JsonLdBlocks.Take(5))}

         Content blocks:
         {string.Join("\n---\n", input.ContentBlocks.Take(12))}
         """;
}
