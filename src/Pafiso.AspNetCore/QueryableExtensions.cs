using Microsoft.AspNetCore.Http;

namespace Pafiso.AspNetCore;

/// <summary>
/// Extension methods for IQueryable to support Pafiso fluent configuration.
/// </summary>
public static class QueryableExtensions {
    /// <summary>
    /// Configures Pafiso filtering, sorting, and paging for this queryable.
    /// </summary>
    /// <typeparam name="T">The entity type being queried.</typeparam>
    /// <param name="query">The source queryable.</param>
    /// <param name="queryCollection">The HTTP query string collection (e.g., Request.Query).</param>
    /// <param name="settings">Optional Pafiso settings. Uses PafisoSettings.Default if not provided.</param>
    /// <param name="configure">Optional configuration action for setting up filtering, sorting, and paging.</param>
    /// <returns>A PafisoQueryable that can be converted to a paged list.</returns>
    public static PafisoQueryable<T> WithPafiso<T>(
        this IQueryable<T> query,
        IQueryCollection queryCollection,
        PafisoSettings? settings = null,
        Action<SearchParametersBuilder<T>>? configure = null) {

        settings ??= PafisoSettings.Default;
        var builder = new SearchParametersBuilder<T>(queryCollection, settings);
        configure?.Invoke(builder);
        var searchParams = builder.Build();
        var (countQuery, pagedQuery) = searchParams.ApplyToIQueryable(query, settings);
        return new PafisoQueryable<T>(pagedQuery, countQuery, searchParams.Paging);
    }

    /// <summary>
    /// Applies pre-configured SearchParameters to this queryable.
    /// </summary>
    /// <typeparam name="T">The entity type being queried.</typeparam>
    /// <param name="query">The source queryable.</param>
    /// <param name="searchParameters">The pre-configured search parameters.</param>
    /// <param name="settings">Optional Pafiso settings. Uses PafisoSettings.Default if not provided.</param>
    /// <returns>A PafisoQueryable that can be converted to a paged list.</returns>
    public static PafisoQueryable<T> WithPafiso<T>(
        this IQueryable<T> query,
        SearchParameters searchParameters,
        PafisoSettings? settings = null) {

        settings ??= PafisoSettings.Default;
        var (countQuery, pagedQuery) = searchParameters.ApplyToIQueryable(query, settings);
        return new PafisoQueryable<T>(pagedQuery, countQuery, searchParameters.Paging);
    }
}
