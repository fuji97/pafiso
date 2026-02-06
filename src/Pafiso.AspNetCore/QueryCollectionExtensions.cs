using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Pafiso.Mapping;

namespace Pafiso.AspNetCore;

public static class QueryCollectionExtensions {
    /// <summary>
    /// Converts an <see cref="IQueryCollection"/> to <see cref="SearchParameters"/>
    /// using a field mapper for strongly-typed field resolution.
    /// </summary>
    /// <typeparam name="TMapping">The mapping model type (DTO) that must inherit from <see cref="MappingModel"/>.</typeparam>
    /// <typeparam name="TEntity">The entity type (database model) to map to.</typeparam>
    /// <param name="query">The query string collection from an HTTP request.</param>
    /// <param name="mapper">The field mapper instance for resolving field names.</param>
    /// <param name="jsonOptions">Optional JSON serializer options.</param>
    /// <returns>A <see cref="SearchParameters"/> object with mapper-enabled filters and sortings.</returns>
    public static SearchParameters ToSearchParameters<TMapping, TEntity>(
        this IQueryCollection query,
        IFieldMapper<TMapping, TEntity> mapper,
        JsonSerializerOptions? jsonOptions = null)
        where TMapping : MappingModel {
        var dict = query.ToDictionary(x => x.Key, x => x.Value.ToString());
        return SearchParameters.FromDictionary(dict, mapper, jsonOptions);
    }
}
