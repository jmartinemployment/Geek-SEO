using NeoSolve.ImageSharp.AVIF;
using SixLabors.ImageSharp;

namespace SectionFigures;

public interface IFigureAvifEncoder
{
    Task<byte[]> EncodePngAsync(byte[] pngBytes, CancellationToken cancellationToken = default);
}

public static class FigureAvifEncoder
{
    public static IFigureAvifEncoder Default { get; } = new ImageSharpAvifEncoder();

    private sealed class ImageSharpAvifEncoder : IFigureAvifEncoder
    {
        public async Task<byte[]> EncodePngAsync(byte[] pngBytes, CancellationToken cancellationToken = default)
        {
            await using var input = new MemoryStream(pngBytes);
            using var image = await Image.LoadAsync(input, cancellationToken);
            await using var output = new MemoryStream();
            var encoder = new AVIFEncoder { CQLevel = 32 };
            await image.SaveAsync(output, encoder, cancellationToken);
            if (output.Length == 0)
            {
                throw new InvalidOperationException("AVIF encoding produced an empty file.");
            }

            return output.ToArray();
        }
    }

    public static Task<byte[]> EncodePngAsync(byte[] pngBytes, CancellationToken cancellationToken = default) =>
        Default.EncodePngAsync(pngBytes, cancellationToken);
}
