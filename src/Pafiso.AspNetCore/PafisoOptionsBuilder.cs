using Microsoft.AspNetCore.Http;
using Pafiso.Enumerables;

namespace Pafiso.AspNetCore;

/// <summary>
/// Builder class for configuring Pafiso filtering, sorting, and paging options.
/// </summary>
/// <typeparam name="TEntity">The entity type being queried.</typeparam>
public class PafisoOptionsBuilder<TEntity> {
    private readonly IQueryCollection _queryCollection;
    private readonly PafisoSettings _settings;
    private bool _enablePaging = false;
    private readonly List<IFilterConfiguration> _filterConfigurations = [];
    private readonly List<ISortingConfiguration> _sortingConfigurations = [];

    /// <summary>
    /// Creates a new instance of PafisoOptionsBuilder.
    /// </summary>
    public PafisoOptionsBuilder(IQueryCollection queryCollection, PafisoSettings? settings) {
        _queryCollection = queryCollection;
        _settings = settings ?? PafisoSettings.Default;
    }

    /// <summary>
    /// Enables paging for this query. Paging parameters will be read from the query string.
    /// </summary>
    public PafisoOptionsBuilder<TEntity> WithPaging() {
        _enablePaging = true;
        return this;
    }

    /// <summary>
    /// Configures filtering using a mapping model (DTO).
    /// </summary>
    /// <typeparam name="TMapping">The mapping model type (DTO) that must inherit from MappingModel.</typeparam>
    /// <returns>A filter options builder for configuring field mappings.</returns>
    public FilterOptionsBuilder<TMapping, TEntity> WithFiltering<TMapping>()
        where TMapping : MappingModel {
        var filterBuilder = new FilterOptionsBuilder<TMapping, TEntity>(_settings);
        _filterConfigurations.Add(filterBuilder);
        return filterBuilder;
    }

    /// <summary>
    /// Configures sorting using a mapping model (DTO).
    /// </summary>
    /// <typeparam name="TMapping">The mapping model type (DTO) that must inherit from MappingModel.</typeparam>
    /// <returns>A sorting options builder for configuring field mappings.</returns>
    public SortingOptionsBuilder<TMapping, TEntity> WithSorting<TMapping>()
        where TMapping : MappingModel {
        var sortingBuilder = new SortingOptionsBuilder<TMapping, TEntity>(_settings);
        _sortingConfigurations.Add(sortingBuilder);
        return sortingBuilder;
    }

    /// <summary>
    /// Builds the PafisoQueryable from the configured options.
    /// </summary>
    public PafisoQueryable<TEntity> Build(IQueryable<TEntity> query) {
        // 1. Parse query string and build SearchParameters with all mappers
        var searchParams = BuildSearchParameters();

        // 2. Apply filters, sorting, and paging
        var (countQuery, pagedQuery) = searchParams.ApplyToIQueryable(query, _settings);

        // 3. Return wrapped queryable
        return new PafisoQueryable<TEntity>(pagedQuery, countQuery, searchParams.Paging);
    }

    private SearchParameters BuildSearchParameters() {
        var allFilters = new List<Filter>();
        var allSortings = new List<Sorting>();
        Paging? paging = null;

        // Parse query collection for each configuration
        foreach (var filterConfig in _filterConfigurations) {
            var filters = filterConfig.ParseFilters(_queryCollection);
            allFilters.AddRange(filters);
        }

        foreach (var sortingConfig in _sortingConfigurations) {
            var sortings = sortingConfig.ParseSortings(_queryCollection);
            allSortings.AddRange(sortings);
        }

        if (_enablePaging) {
            var dict = _queryCollection.ToDictionary(x => x.Key, x => x.Value.ToString());
            paging = Paging.FromDictionary(dict);
        }

        return new SearchParameters {
            Filters = allFilters,
            Sortings = allSortings,
            Paging = paging
        };
    }
}
