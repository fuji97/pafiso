using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Pafiso.AspNetCore;
using Pafiso.Enumerables;

namespace Pafiso.EntityFrameworkCore;

/// <summary>
/// Extension methods for IQueryable to support Pafiso fluent configuration with EF Core async support.
/// </summary>
public static class QueryableExtensions {
    /// <summary>
    /// Configures Pafiso filtering, sorting, and paging for this queryable with EF Core async support.
    /// </summary>
    /// <typeparam name="T">The entity type being queried.</typeparam>
    /// <param name="query">The source queryable.</param>
    /// <param name="queryCollection">The HTTP query string collection (e.g., Request.Query).</param>
    /// <param name="settings">Optional Pafiso settings. Uses PafisoSettings.Default if not provided.</param>
    /// <param name="configure">Optional configuration action for setting up filtering, sorting, and paging.</param>
    /// <returns>A PafisoQueryableAsync that can be converted to a paged list asynchronously.</returns>
    public static PafisoQueryableAsync<T> WithPafiso<T>(
        this IQueryable<T> query,
        IQueryCollection queryCollection,
        PafisoSettings? settings = null,
        Action<PafisoOptionsBuilder<T>>? configure = null) {

        var builder = new PafisoOptionsBuilder<T>(queryCollection, settings);
        configure?.Invoke(builder);
        var pafisoQueryable = builder.Build(query);
        return new PafisoQueryableAsync<T>(pafisoQueryable);
    }

    /// <summary>
    /// Applies pre-configured SearchParameters to this queryable with EF Core async support.
    /// </summary>
    /// <typeparam name="T">The entity type being queried.</typeparam>
    /// <param name="query">The source queryable.</param>
    /// <param name="searchParameters">The pre-configured search parameters.</param>
    /// <param name="settings">Optional Pafiso settings. Uses PafisoSettings.Default if not provided.</param>
    /// <returns>A PafisoQueryableAsync that can be converted to a paged list asynchronously.</returns>
    public static PafisoQueryableAsync<T> WithPafiso<T>(
        this IQueryable<T> query,
        SearchParameters searchParameters,
        PafisoSettings? settings = null) {

        settings ??= PafisoSettings.Default;
        var (countQuery, pagedQuery) = searchParameters.ApplyToIQueryable(query, settings);
        var pafisoQueryable = new PafisoQueryable<T>(pagedQuery, countQuery, searchParameters.Paging);
        return new PafisoQueryableAsync<T>(pafisoQueryable);
    }
}

/// <summary>
/// Wrapper around PafisoQueryable that provides async operations for Entity Framework Core.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public class PafisoQueryableAsync<T> {
    private readonly PafisoQueryable<T> _pafisoQueryable;

    internal PafisoQueryableAsync(PafisoQueryable<T> pafisoQueryable) {
        _pafisoQueryable = pafisoQueryable;
    }

    /// <summary>
    /// Converts the queryable to a paged list asynchronously using Entity Framework Core.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged list containing the results and pagination metadata.</returns>
    public async Task<PagedList<T>> ToPagedListAsync(CancellationToken cancellationToken = default) {
        if (_pafisoQueryable.Paging == null) {
            // No paging configured - return all items as a single page
            var items = await _pafisoQueryable.PagedQuery.ToListAsync(cancellationToken);
            return new PagedList<T>(items, items.Count, 1, items.Count);
        }

        var totalCount = await _pafisoQueryable.CountQuery.CountAsync(cancellationToken);
        var pagedItems = await _pafisoQueryable.PagedQuery.ToListAsync(cancellationToken);

        return new PagedList<T>(
            pagedItems,
            totalCount,
            _pafisoQueryable.Paging.Page,
            _pafisoQueryable.Paging.PageSize
        );
    }

    /// <summary>
    /// Gets the underlying queryable for advanced scenarios.
    /// </summary>
    /// <returns>The underlying IQueryable.</returns>
    public IQueryable<T> AsQueryable() => _pafisoQueryable.AsQueryable();
}
