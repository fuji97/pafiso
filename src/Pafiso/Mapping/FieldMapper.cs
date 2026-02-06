using System.Linq.Expressions;
using System.Reflection;
using Pafiso.Util;

namespace Pafiso.Mapping;

/// <summary>
/// Default implementation of <see cref="IFieldMapper{TMapping,TEntity}"/> that provides
/// automatic 1:1 name-based mapping and supports custom field mappings and value transformations.
/// </summary>
/// <typeparam name="TMapping">The mapping model type (DTO) that must inherit from <see cref="MappingModel"/>.</typeparam>
/// <typeparam name="TEntity">The entity type (database model) to map to.</typeparam>
public class FieldMapper<TMapping, TEntity> : IFieldMapper<TMapping, TEntity> where TMapping : MappingModel {
    private readonly Dictionary<string, string> _customFieldMappings = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Func<string?, object?>> _valueTransformers = new(StringComparer.OrdinalIgnoreCase);
    private readonly DefaultFieldNameResolver _resolver;
    private readonly PafisoSettings _settings;

    /// <summary>
    /// Creates a new instance of <see cref="FieldMapper{TMapping,TEntity}"/> with default settings.
    /// </summary>
    public FieldMapper() : this(null) { }

    /// <summary>
    /// Creates a new instance of <see cref="FieldMapper{TMapping,TEntity}"/> with the specified settings.
    /// </summary>
    /// <param name="settings">The settings to use for field name resolution, or null to use <see cref="PafisoSettings.Default"/>.</param>
    public FieldMapper(PafisoSettings? settings) {
        _settings = settings ?? PafisoSettings.Default;
        _resolver = new DefaultFieldNameResolver(_settings);
    }

    /// <inheritdoc />
    public string? ResolveToEntityField(string mappingFieldName) {
        if (string.IsNullOrEmpty(mappingFieldName)) {
            return null;
        }

        // 1. Check custom field mappings first (highest priority)
        if (_customFieldMappings.TryGetValue(mappingFieldName, out var customMapping)) {
            return customMapping;
        }

        // 2. Resolve mapping model field name via DefaultFieldNameResolver
        var resolvedMappingField = _resolver.ResolvePropertyName<TMapping>(mappingFieldName);

        // 3. Check if this resolved mapping field has a custom mapping
        if (_customFieldMappings.TryGetValue(resolvedMappingField, out var customMappingForResolved)) {
            return customMappingForResolved;
        }

        // 4. Try to find matching entity property (1:1 by name)
        // Use the resolver to find the property on the entity type
        var entityField = _resolver.ResolvePropertyName<TEntity>(resolvedMappingField);

        // 5. Verify property exists on entity
        if (PropertyExists<TEntity>(entityField)) {
            return entityField;
        }

        // Field is invalid - return null to be silently ignored
        return null;
    }

    /// <inheritdoc />
    public object? TransformValue<TProperty>(string mappingFieldName, string? rawValue) {
        if (string.IsNullOrEmpty(mappingFieldName)) {
            return rawValue;
        }

        // Check if custom transformer is registered for this field
        if (_valueTransformers.TryGetValue(mappingFieldName, out var transformer)) {
            try {
                return transformer(rawValue);
            }
            catch {
                // Transformation failed - return null
                return null;
            }
        }

        // No custom transformer - return raw value unchanged
        return rawValue;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> GetMappedFields() {
        // Get all public instance properties from the mapping model
        var mappingProperties = typeof(TMapping)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToList();

        // Add custom mapped field names
        mappingProperties.AddRange(_customFieldMappings.Keys);

        return mappingProperties.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// Maps a field from the mapping model to a corresponding field in the entity.
    /// </summary>
    /// <param name="mappingField">Expression selecting the mapping model field.</param>
    /// <param name="entityField">Expression selecting the entity field.</param>
    /// <returns>This mapper instance for fluent chaining.</returns>
    public FieldMapper<TMapping, TEntity> Map(
        Expression<Func<TMapping, object?>> mappingField,
        Expression<Func<TEntity, object?>> entityField) {

        var mappingFieldName = ExpressionUtilities.ExpressionDecomposer(mappingField.Body);
        var entityFieldName = ExpressionUtilities.ExpressionDecomposer(entityField.Body);

        return Map(mappingFieldName, entityFieldName);
    }

    /// <summary>
    /// Maps a field from the mapping model to a corresponding field in the entity using string names.
    /// </summary>
    /// <param name="mappingFieldName">The name of the mapping model field.</param>
    /// <param name="entityFieldName">The name of the entity field.</param>
    /// <returns>This mapper instance for fluent chaining.</returns>
    public FieldMapper<TMapping, TEntity> Map(string mappingFieldName, string entityFieldName) {
        if (string.IsNullOrEmpty(mappingFieldName)) {
            throw new ArgumentException("Mapping field name cannot be null or empty.", nameof(mappingFieldName));
        }

        if (string.IsNullOrEmpty(entityFieldName)) {
            throw new ArgumentException("Entity field name cannot be null or empty.", nameof(entityFieldName));
        }

        // Verify the entity field exists
        if (!PropertyExists<TEntity>(entityFieldName)) {
            throw new ArgumentException(
                $"Entity field '{entityFieldName}' does not exist on type '{typeof(TEntity).Name}'.",
                nameof(entityFieldName));
        }

        _customFieldMappings[mappingFieldName] = entityFieldName;
        return this;
    }

    /// <summary>
    /// Maps a field with a custom value transformation function.
    /// </summary>
    /// <typeparam name="TValue">The type of the transformed value.</typeparam>
    /// <param name="mappingField">Expression selecting the mapping model field.</param>
    /// <param name="entityField">Expression selecting the entity field.</param>
    /// <param name="transformer">Function to transform the raw string value.</param>
    /// <returns>This mapper instance for fluent chaining.</returns>
    public FieldMapper<TMapping, TEntity> MapWithTransform<TValue>(
        Expression<Func<TMapping, object?>> mappingField,
        Expression<Func<TEntity, object?>> entityField,
        Func<string?, TValue> transformer) {

        var mappingFieldName = ExpressionUtilities.ExpressionDecomposer(mappingField.Body);
        var entityFieldName = ExpressionUtilities.ExpressionDecomposer(entityField.Body);

        // First register the field mapping
        Map(mappingFieldName, entityFieldName);

        // Then register the transformer
        _valueTransformers[mappingFieldName] = rawValue => transformer(rawValue);

        return this;
    }

    /// <summary>
    /// Registers a value transformer for a specific field without changing the field mapping.
    /// </summary>
    /// <typeparam name="TValue">The type of the transformed value.</typeparam>
    /// <param name="mappingField">Expression selecting the mapping model field.</param>
    /// <param name="transformer">Function to transform the raw string value.</param>
    /// <returns>This mapper instance for fluent chaining.</returns>
    public FieldMapper<TMapping, TEntity> WithTransform<TValue>(
        Expression<Func<TMapping, object?>> mappingField,
        Func<string?, TValue> transformer) {

        var mappingFieldName = ExpressionUtilities.ExpressionDecomposer(mappingField.Body);
        _valueTransformers[mappingFieldName] = rawValue => transformer(rawValue);

        return this;
    }

    /// <summary>
    /// Checks if a property exists on the specified type (supports nested properties with dot notation).
    /// </summary>
    private static bool PropertyExists<T>(string propertyPath) {
        if (string.IsNullOrEmpty(propertyPath)) {
            return false;
        }

        var type = typeof(T);
        var parts = propertyPath.Split('.');

        foreach (var part in parts) {
            var property = type.GetProperty(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property == null) {
                return false;
            }
            type = property.PropertyType;
        }

        return true;
    }
}
