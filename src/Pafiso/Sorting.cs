using System.ComponentModel;
using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Pafiso.Extensions;
using Pafiso.Mapping;
using Pafiso.Util;

namespace Pafiso;

public class Sorting {
    public string PropertyName { get; }
    public SortOrder SortOrder { get; }

    // Mapper is required for all sorting operations
    internal readonly object _mapper;

    /// <summary>
    /// Internal constructor for deserialization only.
    /// </summary>
    [JsonConstructor]
    internal Sorting(string propertyName, SortOrder sortOrder) {
        PropertyName = propertyName;
        SortOrder = sortOrder;
        _mapper = null!; // Will be set by SearchParameters.FromJson
    }

    public bool Ascending => SortOrder == SortOrder.Ascending;
    public bool Descending => SortOrder == SortOrder.Descending;

    /// <summary>
    /// Creates a sorting with mapper support for mapping models.
    /// </summary>
    /// <typeparam name="TMapping">The mapping model type (DTO).</typeparam>
    /// <typeparam name="TEntity">The entity type (database model).</typeparam>
    /// <param name="propertyName">The property name from the mapping model.</param>
    /// <param name="sortOrder">The sort order (ascending or descending).</param>
    /// <param name="mapper">The field mapper instance.</param>
    /// <returns>A new sorting with the mapper embedded.</returns>
    public static Sorting WithMapper<TMapping, TEntity>(
        string propertyName,
        SortOrder sortOrder,
        IFieldMapper<TMapping, TEntity> mapper)
        where TMapping : MappingModel {
        return new Sorting(propertyName, sortOrder, mapper);
    }

    /// <summary>
    /// Internal constructor for mapper support.
    /// </summary>
    internal Sorting(string propertyName, SortOrder sortOrder, object mapper) : this(propertyName, sortOrder) {
        _mapper = mapper;
    }

    public IOrderedQueryable<T> ApplyToIQueryable<T>(IQueryable<T> query) {
        if (_mapper == null) {
            throw new InvalidOperationException(
                "Sorting requires a mapper. Use Sorting.WithMapper<TMapping, TEntity>() to create sortings with mapping models.");
        }
        var result = ApplySortingWithMapper<T>(query, null);
        if (result == null) {
            throw new InvalidOperationException($"Cannot apply sorting: field '{PropertyName}' does not map to a valid entity property.");
        }
        return result;
    }


    /// <summary>
    /// Applies sorting to the queryable with the specified settings.
    /// </summary>
    /// <param name="query">The source queryable to apply sorting to.</param>
    /// <param name="settings">The settings to use for field name resolution.</param>
    /// <returns>The sorted queryable.</returns>
    public IOrderedQueryable<T> ApplyToIQueryable<T>(IQueryable<T> query, PafisoSettings? settings) {
        if (_mapper == null) {
            throw new InvalidOperationException(
                "Sorting requires a mapper. Use Sorting.WithMapper<TMapping, TEntity>() to create sortings with mapping models.");
        }
        var result = ApplySortingWithMapper<T>(query, settings);
        if (result == null) {
            throw new InvalidOperationException($"Cannot apply sorting: field '{PropertyName}' does not map to a valid entity property.");
        }
        return result;
    }


    public IOrderedQueryable<T> ThenApplyToIQueryable<T>(IOrderedQueryable<T> query) {
        if (_mapper == null) {
            throw new InvalidOperationException(
                "Sorting requires a mapper. Use Sorting.WithMapper<TMapping, TEntity>() to create sortings with mapping models.");
        }
        return ThenApplySortingWithMapper<T>(query, null);
    }


    /// <summary>
    /// Applies secondary sorting to the queryable with the specified settings.
    /// </summary>
    /// <param name="query">The source ordered queryable to apply sorting to.</param>
    /// <param name="settings">The settings to use for field name resolution.</param>
    /// <returns>The sorted queryable.</returns>
    public IOrderedQueryable<T> ThenApplyToIQueryable<T>(IOrderedQueryable<T> query, PafisoSettings? settings) {
        if (_mapper == null) {
            throw new InvalidOperationException(
                "Sorting requires a mapper. Use Sorting.WithMapper<TMapping, TEntity>() to create sortings with mapping models.");
        }
        return ThenApplySortingWithMapper<T>(query, settings);
    }


    /// <summary>
    /// Applies sorting using the configured mapper for field resolution.
    /// </summary>
    private IOrderedQueryable<T>? ApplySortingWithMapper<T>(IQueryable<T> query, PafisoSettings? settings) {
        if (_mapper == null) {
            throw new InvalidOperationException("Mapper is not configured.");
        }

        // Use reflection to call ResolveToEntityField on the mapper
        var mapperType = _mapper.GetType();
        var resolveMethod = mapperType.GetMethod("ResolveToEntityField");
        if (resolveMethod == null) {
            throw new InvalidOperationException("Mapper does not have ResolveToEntityField method.");
        }

        // Resolve the property name using the mapper
        // The mapper returns null for invalid/restricted fields, which are silently ignored
        var resolvedPropertyName = resolveMethod.Invoke(_mapper, new object[] { PropertyName }) as string;
        if (resolvedPropertyName == null) {
            return null;
        }

        var expr = ExpressionUtilities.BuildLambdaExpression<T, object>(resolvedPropertyName);
        return Ascending ? query.OrderBy(expr) : query.OrderByDescending(expr);
    }

    /// <summary>
    /// Applies secondary sorting using the configured mapper for field resolution.
    /// </summary>
    private IOrderedQueryable<T> ThenApplySortingWithMapper<T>(IOrderedQueryable<T> query, PafisoSettings? settings) {
        if (_mapper == null) {
            throw new InvalidOperationException("Mapper is not configured.");
        }

        // Use reflection to call ResolveToEntityField on the mapper
        var mapperType = _mapper.GetType();
        var resolveMethod = mapperType.GetMethod("ResolveToEntityField");
        if (resolveMethod == null) {
            throw new InvalidOperationException("Mapper does not have ResolveToEntityField method.");
        }

        // Resolve the property name using the mapper
        // The mapper returns null for invalid/restricted fields, which are silently ignored
        var resolvedPropertyName = resolveMethod.Invoke(_mapper, new object[] { PropertyName }) as string;
        if (resolvedPropertyName == null) {
            return query;
        }

        var expr = ExpressionUtilities.BuildLambdaExpression<T, object>(resolvedPropertyName);
        return Ascending ? query.ThenBy(expr) : query.ThenByDescending(expr);
    }

    public IDictionary<string,string> ToDictionary() {
        return new Dictionary<string, string> {
            { "prop", PropertyName },
            { "ord", SortOrder.ToEnumMemberValue() }
        };
    }


    public override string ToString() {
        return $"{PropertyName} ({SortOrder})";
    }

    public bool Equals(Sorting other) {
        return PropertyName == other.PropertyName && SortOrder == other.SortOrder;
    }

    public override bool Equals(object? obj) {
        return obj is Sorting other && Equals(other);
    }

    public override int GetHashCode() {
        return HashCode.Combine(PropertyName, (int)SortOrder);
    }

    public static bool operator ==(Sorting left, Sorting right) {
        return left.Equals(right);
    }

    public static bool operator !=(Sorting left, Sorting right) {
        return !left.Equals(right);
    }
}

