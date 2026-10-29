using ShortP2P.Bot.Contracts;

namespace ShortP2P.Bot.Sample.Meteo.Media;

internal static class BlobChunks
{
    public static byte[][] Split(ReadOnlySpan<byte> ciphertext)
    {
        if (ciphertext.Length == 0)
            throw new ArgumentException("Ciphertext is empty.", nameof(ciphertext));

        var size = BlobLimits.ChunkSizeBytes;
        var count = (ciphertext.Length + size - 1) / size;
        var chunks = new byte[count][];
        for (var i = 0; i < count; i++)
        {
            var offset = i * size;
            var length = Math.Min(size, ciphertext.Length - offset);
            chunks[i] = ciphertext.Slice(offset, length).ToArray();
        }

        return chunks;
    }
}
