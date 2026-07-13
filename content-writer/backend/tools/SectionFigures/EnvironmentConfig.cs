namespace SectionFigures;

public static class EnvironmentConfig
{
    public static string RequireOutputRoot()
    {
        var root = Environment.GetEnvironmentVariable("CONTENT_IMAGE_OUTPUT_DIR");
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new InvalidOperationException(
                "CONTENT_IMAGE_OUTPUT_DIR is required (path to geekatyourspot/public).");
        }

        return Path.GetFullPath(root);
    }
}
