using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pafiso;

/// <summary>
/// Interface for resolving field names from query strings to property names.
/// </summary>
public interface IFieldNameResolver {
    /// <summary>
    /// Resolves an incoming field name (from query string) to a property path.
    /// </summary>
    /// <typeparam name="T">The target type to resolve the property on.</typeparam>
    /// <param name="fieldName">The incoming field name (e.g., from query string).</param>
    /// <returns>The resolved property path (e.g., "UserName" or "Address.City").</returns>
    string ResolvePropertyName<T>(string fieldName);

    /// <summary>
    /// Resolves an incoming field name (from query string) to a property path.
    /// </summary>
    /// <param name="targetType">The target type to resolve the property on.</param>
    /// <param name="fieldName">The incoming field name (e.g., from query string).</param>
    /// <returns>The resolved property path (e.g., "UserName" or "Address.City").</returns>
    string ResolvePropertyName(Type targetType, string fieldName);
}

/// <summary>
/// Default implementation of <see cref="IFieldNameResolver"/> that uses
/// <see cref="JsonNamingPolicy"/> and <see cref="JsonPropertyNameAttribute"/> for resolution.
/// </summary>
public class DefaultFieldNameResolver : IFieldNameResolver {
    private readonly PafisoSettings _settings;

    /// <summary>
    /// Creates a new instance using <see cref="PafisoSettings.Default"/>.
    /// </summary>
    public DefaultFieldNameResolver() : this(null) { }

    /// <summary>
    /// Creates a new instance with the specified settings.
    /// </summary>
    /// <param name="settings">The settings to use, or null to use <see cref="PafisoSettings.Default"/>.</param>
    public DefaultFieldNameResolver(PafisoSettings? settings) {
        _settings = settings ?? PafisoSettings.Default;
    }

    /// <inheritdoc />
    public string ResolvePropertyName<T>(string fieldName) {
        return ResolvePropertyName(typeof(T), fieldName);
    }

    /// <inheritdoc />
    public string ResolvePropertyName(Type targetType, string fieldName) {
        if (string.IsNullOrEmpty(fieldName)) {
            return fieldName;
        }

        // Handle nested properties (e.g., "address.city" -> "Address.City")
        if (fieldName.Contains('.')) {
            var parts = fieldName.Split('.', 2);
            var resolvedFirst = ResolveSingleProperty(targetType, parts[0]);

            // Get the type of the first property for nested resolution
            var firstProperty = FindPropertyByResolvedName(targetType, resolvedFirst);
            if (firstProperty != null) {
                var nestedResolved = ResolvePropertyName(firstProperty.PropertyType, parts[1]);
                return $"{resolvedFirst}.{nestedResolved}";
            }

            return $"{resolvedFirst}.{parts[1]}";
        }

        return ResolveSingleProperty(targetType, fieldName);
    }

    private string ResolveSingleProperty(Type targetType, string fieldName) {
        // First, check for JsonPropertyName attribute match
        if (_settings.UseJsonPropertyNameAttributes) {
            var propertyByAttribute = FindPropertyByJsonPropertyName(targetType, fieldName);
            if (propertyByAttribute != null) {
                return propertyByAttribute.Name;
            }
        }

        // Try direct property name match (case-insensitive)
        var directMatch = targetType.GetProperty(fieldName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (directMatch != null) {
            return directMatch.Name;
        }

        // Apply naming policy transformation if configured
        if (_settings.PropertyNamingPolicy != null) {
            // Try to find a property whose transformed name matches the incoming field name
            var properties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties) {
                var transformedName = _settings.PropertyNamingPolicy.ConvertName(prop.Name);
                if (string.Equals(transformedName, fieldName, StringComparison.OrdinalIgnoreCase)) {
                    return prop.Name;
                }
            }
        }

        // Fallback: return original field name (will fail later if property doesn't exist)
        return fieldName;
    }

    private static PropertyInfo? FindPropertyByJsonPropertyName(Type targetType, string jsonPropertyName) {
        var properties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties) {
            var attr = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
            if (attr != null && string.Equals(attr.Name, jsonPropertyName, StringComparison.OrdinalIgnoreCase)) {
                return prop;
            }
        }
        return null;
    }

    private static PropertyInfo? FindPropertyByResolvedName(Type targetType, string propertyName) {
        return targetType.GetProperty(propertyName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
    }
}

/// <summary>
/// A field name resolver that returns the field name unchanged (no transformation).
/// </summary>
public class PassThroughFieldNameResolver : IFieldNameResolver {
    /// <summary>
    /// Singleton instance of <see cref="PassThroughFieldNameResolver"/>.
    /// </summary>
    public static PassThroughFieldNameResolver Instance { get; } = new();

    /// <inheritdoc />
    public string ResolvePropertyName<T>(string fieldName) => fieldName;

    /// <inheritdoc />
    public string ResolvePropertyName(Type targetType, string fieldName) => fieldName;
}
