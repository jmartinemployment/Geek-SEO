namespace GeekSeoBackend.Services.SiteExtraction;

/// <summary>
/// Dual-viewport classification written onto <c>data-gsv</c>. Nearest annotation wins.
/// </summary>
internal static class VisibilityClassifier
{
    public const string Visible = "visible";
    public const string Collapsed = "collapsed";
    public const string DesktopOnly = "desktop-only";

    public static string Classify(bool hiddenAtMobile, bool hiddenAtDesktop)
    {
        if (!hiddenAtMobile)
            return Visible;
        return hiddenAtDesktop ? Collapsed : DesktopOnly;
    }

    public static string[] ClassifyAll(IReadOnlyList<bool> hiddenAtMobile, IReadOnlyList<bool> hiddenAtDesktop)
    {
        var n = Math.Min(hiddenAtMobile.Count, hiddenAtDesktop.Count);
        var result = new string[n];
        for (var i = 0; i < n; i++)
            result[i] = Classify(hiddenAtMobile[i], hiddenAtDesktop[i]);
        return result;
    }
}
