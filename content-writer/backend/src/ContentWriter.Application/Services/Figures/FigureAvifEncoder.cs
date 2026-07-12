using ContentWriter.Application.Providers;
using NeoSolve.ImageSharp.AVIF;
using SixLabors.ImageSharp;

namespace ContentWriter.Application.Services.Figures;

public static class FigureAvifEncoder
{
    public static async Task<byte[]> EncodeAsync(Image image, CancellationToken cancellationToken = default)
    {
        await using var output = new MemoryStream();
        var encoder = new AVIFEncoder { CQLevel = 32 };
        await image.SaveAsync(output, encoder, cancellationToken);
        if (output.Length == 0)
        {
            throw new ContentGenerationException("AVIF encoding produced an empty file.");
        }

        return output.ToArray();
    }

    public static async Task<byte[]> EncodePngAsync(byte[] pngBytes, CancellationToken cancellationToken = default)
    {
        await using var input = new MemoryStream(pngBytes);
        using var image = await Image.LoadAsync(input, cancellationToken);
        return await EncodeAsync(image, cancellationToken);
    }
}
