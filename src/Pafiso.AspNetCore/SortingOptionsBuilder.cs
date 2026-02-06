using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Pafiso.Extensions;
using Pafiso.Mapping;
using Pafiso.Util;

namespace Pafiso.AspNetCore;

/// <summary>
/// Builder for configuring sorting with field mappings.
/// </summary>
/// <typeparam name="TMapping">The mapping model type (DTO).</typeparam>
/// <typeparam name="TEntity">The entity type (database model).</typeparam>
public class SortingOptionsBuilder<TMapping, TEntity> : ISortingConfiguration
    where TMapping : MappingModel {

    private readonly FieldMapper<TMapping, TEntity> _mapper;
    private readonly PafisoSettings _settings;

    internal SortingOptionsBuilder(PafisoSettings settings) {
        _settings = settings;
        _mapper = new FieldMapper<TMapping, TEntity>(settings);
    }

    /// <summary>
    /// Maps a field from the mapping model to a corresponding field in the entity.
    /// </summary>
    /// <param name="mappingField">Expression selecting the mapping model field.</param>
    /// <param name="entityField">Expression selecting the entity field.</param>
    /// <returns>This builder instance for fluent chaining.</returns>
    public SortingOptionsBuilder<TMapping, TEntity> Map(
        Expression<Func<TMapping, object?>> mappingField,
        Expression<Func<TEntity, object?>> entityField) {

        _mapper.Map(mappingField, entityField);
        return this;
    }

    List<Sorting> ISortingConfiguration.ParseSortings(IQueryCollection queryCollection) {
        var dict = queryCollection.ToDictionary(x => x.Key, x => x.Value.ToString());
        var split = QueryStringHelpers.SplitQueryStringInList(dict);

        var sortings = new List<Sorting>();
        if (split.TryGetValue("sortings", out var sortingDicts)) {
            foreach (var sortingDict in sortingDicts) {
                var propertyName = sortingDict["prop"];
                var sortOrder = EnumExtensions.ParseEnumMember<SortOrder>(sortingDict["ord"]);

                // Create sorting with mapper embedded using static factory method
                var sorting = Sorting.WithMapper<TMapping, TEntity>(
                    propertyName,
                    sortOrder,
                    _mapper);
                sortings.Add(sorting);
            }
        }

        return sortings;
    }
}

/// <summary>
/// Internal interface for sorting configuration.
/// </summary>
internal interface ISortingConfiguration {
    List<Sorting> ParseSortings(IQueryCollection queryCollection);
}
