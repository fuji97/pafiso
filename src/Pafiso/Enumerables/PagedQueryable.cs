using System.Collections;
using System.Linq.Expressions;

namespace Pafiso.Enumerables; 

public class PagedQueryable<T> : IQueryable<T> {
    public int TotalEntries { get; }
    public IQueryable<T> Entries { get; }
    
    public PagedQueryable(int totalEntries, IQueryable<T> entries) {
        TotalEntries = totalEntries;
        Entries = entries;
    }

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