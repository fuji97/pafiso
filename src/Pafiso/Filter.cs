using System.Linq.Expressions;
using System.Text.Json.Serialization;
using LinqKit;
using Pafiso.Extensions;
using Pafiso.Mapping;
using Pafiso.Util;

namespace Pafiso;

public class Filter {
    public List<string> Fields { get; } = [];
    public FilterOperator Operator { get; }
    public string? Value { get; } = null!;
    public bool CaseSensitive { get; } = false;

    // Mapper is required for all filter operations
    internal readonly object _mapper;

    /// <summary>
    /// Internal constructor for deserialization only.
    /// </summary>
    [JsonConstructor]
    internal Filter(string field, FilterOperator @operator, string? value, bool caseSensitive = false) {
        Fields = [field];
        Operator = @operator;
        Value = value;
        CaseSensitive = caseSensitive;
        _mapper = null!; // Will be set by SearchParameters.FromJson
    }

    /// <summary>
    /// Creates a filter with mapper support for mapping models.
    /// </summary>
    /// <typeparam name="TMapping">The mapping model type (DTO).</typeparam>
    /// <typeparam name="TEntity">The entity type (database model).</typeparam>
    /// <param name="field">The field name from the mapping model.</param>
    /// <param name="operator">The filter operator.</param>
    /// <param name="value">The filter value.</param>
    /// <param name="mapper">The field mapper instance.</param>
    /// <param name="caseSensitive">Whether string comparisons are case-sensitive.</param>
    /// <returns>A new filter with the mapper embedded.</returns>
    public static Filter WithMapper<TMapping, TEntity>(
        string field,
        FilterOperator @operator,
        string? value,
        IFieldMapper<TMapping, TEntity> mapper,
        bool caseSensitive = false)
        where TMapping : MappingModel {
        return new Filter(field, @operator, value, mapper, caseSensitive);
    }

    /// <summary>
    /// Creates a filter with mapper support for multiple fields.
    /// </summary>
    public static Filter WithMapper<TMapping, TEntity>(
        IEnumerable<string> fields,
        FilterOperator @operator,
        string? value,
        IFieldMapper<TMapping, TEntity> mapper,
        bool caseSensitive = false)
        where TMapping : MappingModel {
        return new Filter(fields, @operator, value, mapper, caseSensitive);
    }

    /// <summary>
    /// Internal constructor for mapper support.
    /// </summary>
    internal Filter(string field, FilterOperator @operator, string? value, object mapper, bool caseSensitive = false) {
        Fields = [field];
        Operator = @operator;
        Value = value;
        CaseSensitive = caseSensitive;
        _mapper = mapper;
    }

    /// <summary>
    /// Internal constructor for mapper support with multiple fields.
    /// </summary>
    internal Filter(IEnumerable<string> fields, FilterOperator @operator, string? value, object mapper, bool caseSensitive = false) {
        Fields = fields.ToList();
        Operator = @operator;
        Value = value;
        CaseSensitive = caseSensitive;
        _mapper = mapper;
    }

    public bool Equals(Filter other) {
        return Fields.SequenceEqual(other.Fields) && Operator == other.Operator && Value == other.Value && CaseSensitive == other.CaseSensitive;
    }

    public override bool Equals(object? obj) {
        return obj is Filter other && Equals(other);
    }

    public override int GetHashCode() {
        return HashCode.Combine(Fields, (int)Operator, Value, CaseSensitive);
    }

    public static bool operator ==(Filter left, Filter right) {
        return left.Equals(right);
    }

    public static bool operator !=(Filter left, Filter right) {
        return !left.Equals(right);
    }

    public IDictionary<string, string> ToDictionary() {
        var dict = new Dictionary<string, string>() {
            ["fields"] = string.Join(',', Fields),
            ["op"] = Operator.ToEnumMemberValue(),
        };
        if (Value != null) {
            dict["val"] = Value;
        }
        if (CaseSensitive) {
            dict["case"] = "true";
        }

        return dict;
    }


    public IQueryable<T> ApplyFilter<T>(IQueryable<T> query) {
        if (_mapper == null) {
            throw new InvalidOperationException(
                "Filter requires a mapper. Use Filter.WithMapper<TMapping, TEntity>() to create filters with mapping models.");
        }
        return ApplyFilterWithMapper<T>(query, null);
    }


    /// <summary>
    /// Applies a filter to the queryable with the specified settings.
    /// </summary>
    /// <param name="query">The source queryable to apply the filter to.</param>
    /// <param name="settings">The settings to use for field name resolution and string comparison.</param>
    /// <returns>The filtered queryable.</returns>
    public IQueryable<T> ApplyFilter<T>(IQueryable<T> query, PafisoSettings? settings) {
        if (_mapper == null) {
            throw new InvalidOperationException(
                "Filter requires a mapper. Use Filter.WithMapper<TMapping, TEntity>() to create filters with mapping models.");
        }
        return ApplyFilterWithMapper<T>(query, settings);
    }


    private Expression<Func<T, bool>> ApplyCorrectOperationWithSettings<T>(Filter filter, string field, PafisoSettings settings) {
        return ExpressionUtilities.BuildFilterExpression<T>(field, "x", filter.Operator, filter.Value, filter.CaseSensitive, settings);
    }

    /// <summary>
    /// Applies filter using the configured mapper for field resolution.
    /// </summary>
    private IQueryable<T> ApplyFilterWithMapper<T>(IQueryable<T> query, PafisoSettings? settings) {
        if (_mapper == null) {
            throw new InvalidOperationException("Mapper is not configured.");
        }

        settings ??= PafisoSettings.Default;

        // Use reflection to call ResolveToEntityField on the mapper
        var mapperType = _mapper.GetType();
        var resolveMethod = mapperType.GetMethod("ResolveToEntityField");
        if (resolveMethod == null) {
            throw new InvalidOperationException("Mapper does not have ResolveToEntityField method.");
        }

        var predicatesBuilder = PredicateBuilder.New<T>();
        var resolvedFields = new List<string>();

        // Resolve each field using the mapper
        // The mapper returns null for invalid/restricted fields, which are silently ignored
        foreach (var field in Fields) {
            var resolvedField = resolveMethod.Invoke(_mapper, new object[] { field }) as string;
            if (resolvedField != null) {
                resolvedFields.Add(resolvedField);
            }
        }

        if (resolvedFields.Count == 0) {
            return query;
        }

        // Build filter expressions for each resolved field
        foreach (var resolvedField in resolvedFields) {
            predicatesBuilder.Or(ApplyCorrectOperationWithSettings<T>(this, resolvedField, settings));
        }

        return query.Where(predicatesBuilder);
    }

    public override string ToString() {
        return $"({string.Join(" OR ", Fields.Select(field => $"{field} {Operator} {Value}"))})";
    }
}

