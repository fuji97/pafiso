using System.Linq.Expressions;
using Pafiso.Enums;
using Pafiso.Expressions;
using Pafiso.Extensions;
using Pafiso.Mapping;

namespace Pafiso.EntityFrameworkCore;

/// <summary>
/// Builder for configuring filtering with field mappings.
/// </summary>
/// <typeparam name="TMapping">The mapping model type (DTO).</typeparam>
/// <typeparam name="TEntity">The entity type (database model).</typeparam>
public class FilterOptionsBuilder<TMapping, TEntity> : IFilterConfiguration
    where TMapping : MappingModel {

    private readonly FieldMapper<TMapping, TEntity> _mapper;
    private readonly PafisoSettings _settings;
    private readonly IFilterExpressionBuilder? _expressionBuilder;
    private readonly bool _defaultCaseSensitive;

    internal FilterOptionsBuilder(PafisoSettings settings, IFilterExpressionBuilder? expressionBuilder = null, bool defaultCaseSensitive = false) {
        _settings = settings;
        _mapper = new FieldMapper<TMapping, TEntity>(settings);
        _expressionBuilder = expressionBuilder;
        _defaultCaseSensitive = defaultCaseSensitive;
    }

    /// <summary>
    /// Maps a field from the mapping model to a corresponding field in the entity.
    /// </summary>
    /// <param name="mappingField">Expression selecting the mapping model field.</param>
    /// <param name="entityField">Expression selecting the entity field.</param>
    /// <returns>This builder instance for fluent chaining.</returns>
    public FilterOptionsBuilder<TMapping, TEntity> Map(
        Expression<Func<TMapping, object?>> mappingField,
        Expression<Func<TEntity, object?>> entityField) {

        _mapper.Map(mappingField, entityField);
        return this;
    }

    /// <summary>
    /// Maps a field with a custom value transformation function.
    /// </summary>
    /// <typeparam name="TValue">The type of the transformed value.</typeparam>
    /// <param name="mappingField">Expression selecting the mapping model field.</param>
    /// <param name="entityField">Expression selecting the entity field.</param>
    /// <param name="transformer">Function to transform the raw string value.</param>
    /// <returns>This builder instance for fluent chaining.</returns>
    public FilterOptionsBuilder<TMapping, TEntity> MapWithTransform<TValue>(
        Expression<Func<TMapping, object?>> mappingField,
        Expression<Func<TEntity, object?>> entityField,
        Func<string?, TValue> transformer) {

        _mapper.MapWithTransform(mappingField, entityField, transformer);
        return this;
    }

    List<Filter> IFilterConfiguration.ParseFilters(ParsedQueryData data) {
        var filters = new List<Filter>();
        if (data.Split.TryGetValue("filters", out var filterDicts)) {
            foreach (var filterDict in filterDicts) {
                var fields = filterDict["fields"]!.Split(",");
                var op = filterDict["op"]!;
                filterDict.TryGetValue("val", out var val);
                var caseSensitive = filterDict.ContainsKey("case")
                    ? filterDict["case"] == "true"
                    : _defaultCaseSensitive;

                // Create filter with mapper embedded using static factory method
                var filter = Filter.WithMapper<TMapping, TEntity>(
                    fields,
                    EnumExtensions.ParseEnumMember<FilterOperator>(op),
                    val,
                    _mapper,
                    _expressionBuilder,
                    caseSensitive);
                filters.Add(filter);
            }
        }

        return filters;
    }
}

/// <summary>
/// Internal interface for filter configuration.
/// </summary>
internal interface IFilterConfiguration {
    List<Filter> ParseFilters(ParsedQueryData data);
}
