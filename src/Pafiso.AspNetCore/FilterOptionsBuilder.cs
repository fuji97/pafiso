using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Pafiso.Extensions;
using Pafiso.Mapping;
using Pafiso.Util;

namespace Pafiso.AspNetCore;

/// <summary>
/// Builder for configuring filtering with field mappings.
/// </summary>
/// <typeparam name="TMapping">The mapping model type (DTO).</typeparam>
/// <typeparam name="TEntity">The entity type (database model).</typeparam>
public class FilterOptionsBuilder<TMapping, TEntity> : IFilterConfiguration
    where TMapping : MappingModel {

    private readonly FieldMapper<TMapping, TEntity> _mapper;
    private readonly PafisoSettings _settings;

    internal FilterOptionsBuilder(PafisoSettings settings) {
        _settings = settings;
        _mapper = new FieldMapper<TMapping, TEntity>(settings);
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

    List<Filter> IFilterConfiguration.ParseFilters(IQueryCollection queryCollection) {
        var dict = queryCollection.ToDictionary(x => x.Key, x => x.Value.ToString());
        var split = QueryStringHelpers.SplitQueryStringInList(dict);

        var filters = new List<Filter>();
        if (split.TryGetValue("filters", out var filterDicts)) {
            foreach (var filterDict in filterDicts) {
                var fields = filterDict["fields"]!.Split(",");
                var op = filterDict["op"]!;
                filterDict.TryGetValue("val", out var val);
                var caseSensitive = filterDict.ContainsKey("case") && filterDict["case"] == "true";

                // Create filter with mapper embedded using internal constructor
                var filter = Filter.WithMapper<TMapping, TEntity>(
                    fields,
                    EnumExtensions.ParseEnumMember<FilterOperator>(op),
                    val,
                    _mapper,
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
    List<Filter> ParseFilters(IQueryCollection queryCollection);
}
