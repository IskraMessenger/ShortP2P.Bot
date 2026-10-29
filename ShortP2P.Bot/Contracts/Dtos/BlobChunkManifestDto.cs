namespace ShortP2P.Bot.Contracts.Dtos;

/// <summary>Which ciphertext chunks of a bot attachment are already stored.</summary>
public sealed class BlobChunkManifestDto
{
    public string BlobId { get; set; } = string.Empty;

    public int TotalChunks { get; set; }

    public long TotalBytes { get; set; }

    public int[] Indexes { get; set; } = Array.Empty<int>();
}
