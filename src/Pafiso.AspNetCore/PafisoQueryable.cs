using Pafiso.Enumerables;

namespace Pafiso.AspNetCore;

/// <summary>
/// Wrapper around IQueryable that provides Pafiso-specific operations.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public class PafisoQueryable<T> {
    /// <summary>
    /// Gets the paged query (after filters, sorting, and paging applied).
    /// </summary>
    public IQueryable<T> PagedQuery { get; }

    /// <summary>
    /// Gets the count query (filters and sorting applied, but before paging).
    /// </summary>
    public IQueryable<T> CountQuery { get; }

    /// <summary>
    /// Gets the paging configuration, if any.
    /// </summary>
    public Paging? Paging { get; }

    /// <summary>
    /// Creates a new instance of PafisoQueryable.
    /// </summary>
    /// <param name="pagedQuery">The query after filters, sorting, and paging.</param>
    /// <param name="countQuery">The query for counting (before paging).</param>
    /// <param name="paging">The paging configuration.</param>
    public PafisoQueryable(IQueryable<T> pagedQuery, IQueryable<T> countQuery, Paging? paging) {
        PagedQuery = pagedQuery;
        CountQuery = countQuery;
        Paging = paging;
    }

    /// <summary>
    /// Converts the queryable to a paged list synchronously.
    /// </summary>
    /// <returns>A paged list containing the results and pagination metadata.</returns>
    public PagedList<T> ToPagedList() {
        if (Paging == null) {
            // No paging configured - return all items as a single page
            var items = PagedQuery.ToList();
            return new PagedList<T>(items, items.Count, 1, items.Count);
        }

        var totalCount = CountQuery.Count();
        var pagedItems = PagedQuery.ToList();

        return new PagedList<T>(
            pagedItems,
            totalCount,
            Paging.Page,
            Paging.PageSize
        );
    }

    /// <summary>
    /// Gets the underlying queryable for advanced scenarios.
    /// </summary>
    /// <returns>The underlying IQueryable.</returns>
    public IQueryable<T> AsQueryable() => PagedQuery;
}
