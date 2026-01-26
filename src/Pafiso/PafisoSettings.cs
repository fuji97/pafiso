using System.Text.Json;

namespace Pafiso;

/// <summary>
/// Configuration settings for Pafiso operations.
/// </summary>
public class PafisoSettings {
    /// <summary>
    /// Global default settings instance. Can be overridden per-operation.
    /// </summary>
    public static PafisoSettings Default { get; set; } = new();

    /// <summary>
    /// The naming policy for mapping field names from query strings to property names.
    /// When set, incoming field names (e.g., "userName") will be mapped to properties
    /// using the inverse of this policy.
    /// </summary>
    /// <remarks>
    /// Common policies:
    /// - <see cref="JsonNamingPolicy.CamelCase"/> - Maps "userName" to "UserName"
    /// - <see cref="JsonNamingPolicy.SnakeCaseLower"/> - Maps "user_name" to "UserName"
    /// - <see cref="JsonNamingPolicy.KebabCaseLower"/> - Maps "user-name" to "UserName"
    /// </remarks>
    public JsonNamingPolicy? PropertyNamingPolicy { get; set; } = null;

    /// <summary>
    /// When true, respects <see cref="System.Text.Json.Serialization.JsonPropertyNameAttribute"/>
    /// attributes on properties for field name mapping.
    /// </summary>
    /// <remarks>
    /// This allows per-property customization of field names independent of the global naming policy.
    /// </remarks>
    public bool UseJsonPropertyNameAttributes { get; set; } = true;

    /// <summary>
    /// The <see cref="System.StringComparison"/> to use for case-insensitive string operations
    /// in IEnumerable/in-memory scenarios.
    /// </summary>
    /// <remarks>
    /// This setting is used when <see cref="UseEfCoreLikeForCaseInsensitive"/> is false,
    /// or when operating on in-memory collections.
    /// </remarks>
    public StringComparison StringComparison { get; set; } = StringComparison.OrdinalIgnoreCase;

    /// <summary>
    /// When true, uses EF.Functions.Like for case-insensitive string matching
    /// in EF Core scenarios (requires Pafiso.EntityFrameworkCore package).
    /// Falls back to <see cref="StringComparison"/> for non-EF queryables.
    /// </summary>
    public bool UseEfCoreLikeForCaseInsensitive { get; set; } = true;

    /// <summary>
    /// Creates a new instance of <see cref="PafisoSettings"/> with default values.
    /// </summary>
    public PafisoSettings() { }

    /// <summary>
    /// Creates a copy of the current settings.
    /// </summary>
    /// <returns>A new <see cref="PafisoSettings"/> instance with the same values.</returns>
    public PafisoSettings Clone() => new() {
        PropertyNamingPolicy = PropertyNamingPolicy,
        UseJsonPropertyNameAttributes = UseJsonPropertyNameAttributes,
        StringComparison = StringComparison,
        UseEfCoreLikeForCaseInsensitive = UseEfCoreLikeForCaseInsensitive
    };
}
