namespace Pafiso.Mapping;

/// <summary>
/// Non-generic interface for resolving field names from mapping models to entity properties.
/// Used internally to avoid reflection when calling ResolveToEntityField.
/// </summary>
public interface IFieldResolver {
    /// <summary>
    /// Resolves a field name from the mapping model to the corresponding entity field name.
    /// </summary>
    /// <param name="fieldName">The field name to resolve.</param>
    /// <returns>The resolved entity field name, or null if the field is invalid.</returns>
    string? ResolveToEntityField(string fieldName);
}
