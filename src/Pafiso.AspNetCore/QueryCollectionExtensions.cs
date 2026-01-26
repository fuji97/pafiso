using Microsoft.AspNetCore.Http;

namespace Pafiso.AspNetCore;

public static class QueryCollectionExtensions {
    /// <summary>
    /// Converts an <see cref="IQueryCollection"/> to <see cref="SearchParameters"/>.
    /// </summary>
    /// <param name="query">The query string collection from an HTTP request.</param>
    /// <returns>A <see cref="SearchParameters"/> object constructed from the query string.</returns>
    public static SearchParameters ToSearchParameters(this IQueryCollection query) {
        var dict = query.ToDictionary(x => x.Key, x => x.Value.ToString());
        return SearchParameters.FromDictionary(dict);
    }

    /// <summary>
    /// Converts an <see cref="IQueryCollection"/> to <see cref="SearchParameters"/>
    /// using the specified settings.
    /// </summary>
    /// <param name="query">The query string collection from an HTTP request.</param>
    /// <param name="settings">The settings to use for field name resolution.</param>
    /// <returns>A <see cref="SearchParameters"/> object constructed from the query string.</returns>
    public static SearchParameters ToSearchParameters(this IQueryCollection query, PafisoSettings? settings) {
        var dict = query.ToDictionary(x => x.Key, x => x.Value.ToString());
        return SearchParameters.FromDictionary(dict, settings);
    }
}
