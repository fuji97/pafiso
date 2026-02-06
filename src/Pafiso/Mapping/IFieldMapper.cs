namespace Pafiso.Mapping;

/// <summary>
/// Interface for mapping field names from mapping models to entity properties.
/// Supports custom field mappings and value transformations.
/// </summary>
/// <typeparam name="TMapping">The mapping model type (DTO) that must inherit from <see cref="MappingModel"/>.</typeparam>
/// <typeparam name="TEntity">The entity type (database model) to map to.</typeparam>
public interface IFieldMapper<TMapping, TEntity> where TMapping : MappingModel {
    /// <summary>
    /// Resolves a field name from the mapping model to the corresponding entity field name.
    /// </summary>
    /// <param name="mappingFieldName">The field name from the mapping model (e.g., from query string).</param>
    /// <returns>
    /// The resolved entity field name, or null if the field is invalid or doesn't exist.
    /// Null values should be silently ignored by consumers.
    /// </returns>
    /// <remarks>
    /// Resolution order:
    /// 1. Check custom field mappings (highest priority)
    /// 2. Resolve mapping model field name via DefaultFieldNameResolver
    /// 3. Try to find matching entity property (1:1 by name)
    /// 4. Verify property exists on entity
    /// Returns null for invalid fields rather than throwing exceptions.
    /// </remarks>
    string? ResolveToEntityField(string mappingFieldName);

    /// <summary>
    /// Transforms a raw value from the request into a typed value for filtering or sorting.
    /// </summary>
    /// <typeparam name="TProperty">The target property type.</typeparam>
    /// <param name="mappingFieldName">The field name from the mapping model.</param>
    /// <param name="rawValue">The raw string value from the request.</param>
    /// <returns>
    /// The transformed value if a custom transformer is registered, otherwise returns the raw value.
    /// Returns null if transformation fails.
    /// </returns>
    /// <remarks>
    /// This method is used to convert string values from query strings or JSON into
    /// strongly-typed values before they are used in filter or sort expressions.
    /// For example, "50" → decimal 50, or "true" → boolean true.
    /// </remarks>
    object? TransformValue<TProperty>(string mappingFieldName, string? rawValue);

    /// <summary>
    /// Gets all valid field names from the mapping model that can be used for filtering/sorting.
    /// </summary>
    /// <returns>
    /// A read-only collection of field names that are valid for this mapper.
    /// These represent the fields that can be resolved to entity properties.
    /// </returns>
    IReadOnlyCollection<string> GetMappedFields();
}
