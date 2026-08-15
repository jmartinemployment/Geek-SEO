using System.Security.Cryptography;
using System.Text;

namespace GeekSeo.Application.Models.Seo;

/// <summary>
/// Content fingerprint of a stored crawl document. Recrawl without Analyze compares this hash;
/// Analyze (force) always writes.
/// </summary>
public static class CrawlDocumentHasher
{
    public static string Sha256Hex(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content ?? ""));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Analyze / user force: always replace the stored document.
    /// Unforced recrawl: keep the stored document when the hash is unchanged.
    /// </summary>
    public static bool ShouldReplaceDocument(bool forceDocumentWrite, string? storedHash, string incomingHash)
    {
        if (forceDocumentWrite)
            return true;
        if (string.IsNullOrEmpty(storedHash))
            return true;
        return !string.Equals(storedHash, incomingHash, StringComparison.OrdinalIgnoreCase);
    }
}
