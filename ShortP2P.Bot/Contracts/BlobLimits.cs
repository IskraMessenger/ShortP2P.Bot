namespace ShortP2P.Bot.Contracts;

/// <summary>Limits and headers for opaque encrypted attachment blobs (mirrored from MessengerServer.Contracts).</summary>
public static class BlobLimits
{
    /// <summary>Max ciphertext size: 60 MB video plus 2 MB hybrid-envelope headroom.</summary>
    public const int MaxCiphertextBytes = 60 * 1024 * 1024 + 2 * 1024 * 1024;

    /// <summary>Bot attachment ciphertext is uploaded and downloaded in pieces of this size. The last piece may be shorter.</summary>
    public const int ChunkSizeBytes = 50 * 1024;

    public const string TargetNetworkIdHeader = "X-ShortP2P-Target-NetworkId";

    private static string BlobById(string blobId)
    {
        return string.IsNullOrWhiteSpace(blobId)
            ? throw new ArgumentException("Blob id is required.", nameof(blobId))
            : $"{ServerApiRoutes.Blobs}/{Uri.EscapeDataString(blobId.Trim())}";
    }

    public static string BlobChunks(string blobId) => $"{BlobById(blobId)}/chunks";

    public static string BlobChunk(string blobId, int index) => $"{BlobChunks(blobId)}/{index}";
}
