namespace ShortP2P.Bot.Security;

/// <summary>
/// Bot TLS certificate settings: provide an existing PFX or let the library auto-generate one.
/// Bound from configuration section <see cref="SectionName"/>.
/// </summary>
public sealed class BotCertificateOptions
{
    public const string SectionName = "ShortP2PBot:Certificate";

    /// <summary>
    /// Path to an existing PKCS#12 (.pfx) file. When empty, a self-signed certificate is generated
    /// (or reused) at <see cref="AutoGeneratePfxPath"/>.
    /// </summary>
    public string? PfxPath { get; set; }

    /// <summary>Optional password for <see cref="PfxPath"/> or the auto-generated PFX.</summary>
    public string? PfxPassword { get; set; }

    /// <summary>
    /// Path used when <see cref="PfxPath"/> is not set. Defaults to
    /// <c>{BaseDirectory}/shortp2p-bot-certificate.pfx</c>.
    /// </summary>
    public string? AutoGeneratePfxPath { get; set; }
}
