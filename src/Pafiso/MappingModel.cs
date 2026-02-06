namespace Pafiso;

/// <summary>
/// Abstract base class for all mapping models used with <see cref="IFieldMapper{TMapping,TEntity}"/>.
/// Mapping models represent incoming request data (DTOs) and map to entity classes.
/// </summary>
/// <remarks>
/// This class serves as a marker type and provides optional lifecycle hooks for validation
/// and transformation logic. All mapping models used with the field mapper system must
/// inherit from this class.
/// </remarks>
public abstract class MappingModel {
    /// <summary>
    /// Called before field mapping occurs. Override to perform custom validation or setup.
    /// </summary>
    /// <returns>True if validation passes and mapping should proceed, false otherwise.</returns>
    public virtual bool OnBeforeMap() => true;

    /// <summary>
    /// Called after field mapping is complete. Override to perform post-mapping validation.
    /// </summary>
    public virtual void OnAfterMap() { }

    /// <summary>
    /// Validates the mapping model. Override to implement custom validation logic.
    /// </summary>
    /// <returns>True if the model is valid, false otherwise.</returns>
    public virtual bool Validate() => true;
}
