using System.Collections;
using System.Linq.Expressions;

namespace Pafiso.Enumerables; 

public class PagedQueryable<T>(int totalEntries, IQueryable<T> entries) : IQueryable<T> {
    public int TotalEntries { get; } = totalEntries;
    public IQueryable<T> Entries { get; } = entries;

    public IEnumerator<T> GetEnumerator() {
        return Entries.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return ((IEnumerable)Entries).GetEnumerator();
    }

    public Type ElementType => Entries.ElementType;

    public Expression Expression => Entries.Expression;

    public IQueryProvider Provider => Entries.Provider;
    
    public static PagedQueryable<T> Empty() {
        return new PagedQueryable<T>(0, Enumerable.Empty<T>().AsQueryable());
    }
    
    public void Deconstruct(out int totalEntries, out IQueryable<T> entries) {
        totalEntries = TotalEntries;
        entries = Entries;
    }
    
    public static implicit operator PagedEnumerable<T>(PagedQueryable<T> pagedQueryable) {
        return new PagedEnumerable<T>(pagedQueryable.TotalEntries, pagedQueryable.Entries);
    }
}

public static class PagedQueryable {
    public static PagedQueryable<T> Empty<T>() {
        return new PagedQueryable<T>(0, Enumerable.Empty<T>().AsQueryable());
    }
}