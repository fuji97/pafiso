using System.Collections;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Pafiso.Enumerables;

namespace Pafiso.EntityFrameworkCore.Enumerables;

public class PagedQueryable<T>(IQueryable<T> countQuery, IQueryable<T> entriesQuery) : IQueryable<T> {
    private IQueryable<T> CountQuery { get; init; } = countQuery;
    private IQueryable<T> EntriesQuery { get; init; } = entriesQuery;

    public async Task<PagedList<T>> ToPagedListAsync() {
        return new PagedList<T>() {
            TotalEntries = await CountQuery.CountAsync(),
            Entries = await EntriesQuery.ToListAsync()
        };
    }

    public IEnumerator<T> GetEnumerator() {
        return EntriesQuery.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return ((IEnumerable)EntriesQuery).GetEnumerator();
    }

    public Type ElementType => EntriesQuery.ElementType;

    public Expression Expression => EntriesQuery.Expression;

    public IQueryProvider Provider => EntriesQuery.Provider;
}

public static class PagedQueryableExtensions {
    public static PagedQueryable<T> WithSearchParameters<T>(this IQueryable<T> query, SearchParameters searchParameters) {
        var (countQuery, pagedQuery) = searchParameters.ApplyToIQueryable(query);
        return new PagedQueryable<T>(countQuery, pagedQuery);
    }

    public static PagedQueryable<T> WithSearchParameters<T>(
        this IQueryable<T> query, 
        SearchParameters searchParameters,
        Action<FieldRestrictions> configureRestrictions) {
        var (countQuery, pagedQuery) = searchParameters.ApplyToIQueryable(query, configureRestrictions);
        return new PagedQueryable<T>(countQuery, pagedQuery);
    }

    public static PagedQueryable<T> WithSearchParameters<T>(
        this IQueryable<T> query, 
        SearchParameters searchParameters,
        FieldRestrictions? restrictions) {
        var (countQuery, pagedQuery) = searchParameters.ApplyToIQueryable(query, restrictions);
        return new PagedQueryable<T>(countQuery, pagedQuery);
    }

    public static PagedQueryable<T> WithSearchParameters<T>(
        this IQueryable<T> query, 
        SearchParameters searchParameters,
        PafisoSettings? settings) {
        var (countQuery, pagedQuery) = searchParameters.ApplyToIQueryable(query, settings);
        return new PagedQueryable<T>(countQuery, pagedQuery);
    }

    public static PagedQueryable<T> WithSearchParameters<T>(
        this IQueryable<T> query, 
        SearchParameters searchParameters,
        Action<FieldRestrictions> configureRestrictions,
        PafisoSettings? settings) {
        var (countQuery, pagedQuery) = searchParameters.ApplyToIQueryable(query, configureRestrictions, settings);
        return new PagedQueryable<T>(countQuery, pagedQuery);
    }

    public static PagedQueryable<T> WithSearchParameters<T>(
        this IQueryable<T> query, 
        SearchParameters searchParameters,
        FieldRestrictions? restrictions,
        PafisoSettings? settings) {
        var (countQuery, pagedQuery) = searchParameters.ApplyToIQueryable(query, restrictions, settings);
        return new PagedQueryable<T>(countQuery, pagedQuery);
    }
}
