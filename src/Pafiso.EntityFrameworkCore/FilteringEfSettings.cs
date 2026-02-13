namespace Pafiso.EntityFrameworkCore;

/// <summary>
/// EF Core-specific settings for Pafiso.
/// </summary>
public class FilteringEfSettings {
    /// <summary>
    /// When true, the EF LIKE comparison will be case-sensitive.
    /// </summary>
    public bool CaseSensitive { get; set; } = false;
}
