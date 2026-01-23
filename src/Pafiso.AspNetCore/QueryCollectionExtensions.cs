using Microsoft.AspNetCore.Http;

namespace Pafiso.AspNetCore;

public static class QueryCollectionExtensions {
    /// <param name="query">The query string collection from an HTTP request.</param>
    extension(IQueryCollection query) {
        /// <summary>
        /// Converts an <see cref="IQueryCollection"/> to <see cref="SearchParameters"/>.
        /// </summary>
        /// <returns>A <see cref="SearchParameters"/> object constructed from the query string.</returns>
        public SearchParameters ToSearchParameters() {
            var dict = query.ToDictionary(x => x.Key, x => x.Value.ToString());
            return SearchParameters.FromDictionary(dict);
        }
    }
}
