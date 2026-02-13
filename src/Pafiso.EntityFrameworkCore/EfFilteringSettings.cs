namespace Pafiso.EntityFrameworkCore;

/// <summary>
/// Settings for EF Core filtering configuration.
/// </summary>
public class EfFilteringSettings {
    /// <summary>
    /// Whether string comparisons are case-sensitive by default.
    /// Defaults to <c>false</c> (case-insensitive).
    /// Individual filters can override this via the query string <c>case=true</c> parameter.
    /// </summary>
    public bool CaseSensitive { get; set; } = false;
}
