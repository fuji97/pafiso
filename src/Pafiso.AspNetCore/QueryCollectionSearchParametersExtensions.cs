using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace Pafiso.AspNetCore;

/// <summary>
/// Extension methods for IQueryCollection to create SearchParameters.
/// </summary>
public static class QueryCollectionSearchParametersExtensions {
    /// <summary>
    /// Converts an IQueryCollection to SearchParameters with fluent configuration.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being queried.</typeparam>
    /// <param name="queryCollection">The HTTP query string collection (e.g., Request.Query).</param>
    /// <param name="configure">Configuration action for setting up filtering, sorting, and paging.</param>
    /// <param name="settings">Optional Pafiso settings. Uses PafisoSettings.Default if not provided.</param>
    /// <returns>A SearchParameters instance with configured filters, sortings, and paging.</returns>
    public static SearchParameters ToSearchParameters<TEntity>(
        this IQueryCollection queryCollection,
        Action<SearchParametersBuilder<TEntity>> configure,
        PafisoSettings? settings = null) {

        var builder = new SearchParametersBuilder<TEntity>(queryCollection, settings);
        configure(builder);
        return builder.Build();
    }

    /// <summary>
    /// Converts an IQueryCollection to SearchParameters without any mappings.
    /// This is useful when you want to use raw field names from the query string.
    /// </summary>
    /// <param name="queryCollection">The HTTP query string collection (e.g., Request.Query).</param>
    /// <param name="jsonOptions">Optional JSON serializer options.</param>
    /// <returns>A SearchParameters instance with raw filters and sortings (no field mapping).</returns>
    /// <remarks>
    /// Warning: This method creates SearchParameters without field mappers, which means
    /// filters and sortings will fail when applied to queryables. Use the generic overload
    /// with configuration for proper field mapping.
    /// </remarks>
    public static SearchParameters ToSearchParameters(
        this IQueryCollection queryCollection,
        JsonSerializerOptions? jsonOptions = null) {

        var dict = queryCollection.ToDictionary(x => x.Key, x => x.Value.ToString());

        // Parse paging
        var paging = Paging.FromDictionary(dict);

        // Note: We cannot create filters and sortings without a mapper
        // So this method only returns paging information
        return new SearchParameters {
            Paging = paging,
            Filters = [],
            Sortings = []
        };
    }
}

/// <summary>
/// Builder for creating SearchParameters from IQueryCollection with fluent configuration.
/// </summary>
/// <typeparam name="TEntity">The entity type being queried.</typeparam>
public class SearchParametersBuilder<TEntity> {
    private readonly IQueryCollection _queryCollection;
    private readonly PafisoSettings _settings;
    private bool _enablePaging = false;
    private readonly List<IFilterConfiguration> _filterConfigurations = [];
    private readonly List<ISortingConfiguration> _sortingConfigurations = [];

    internal SearchParametersBuilder(IQueryCollection queryCollection, PafisoSettings? settings) {
        _queryCollection = queryCollection;
        _settings = settings ?? PafisoSettings.Default;
    }

    /// <summary>
    /// Enables paging for this search. Paging parameters will be read from the query string.
    /// </summary>
    public SearchParametersBuilder<TEntity> WithPaging() {
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

    internal SearchParameters Build() {
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
