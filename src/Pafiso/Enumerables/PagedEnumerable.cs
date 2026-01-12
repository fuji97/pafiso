using System.Collections;

namespace Pafiso.Enumerables;

public class PagedEnumerable<T>(int totalEntries, IEnumerable<T> entries) : IEnumerable<T> {
    public int TotalEntries { get; } = totalEntries;
    public IEnumerable<T> Entries { get; } = entries;

    public IEnumerator<T> GetEnumerator() {
        return Entries.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return ((IEnumerable)Entries).GetEnumerator();
    }

    public static PagedEnumerable<T> Empty() {
        return new PagedEnumerable<T>(0, []);
    }
    
    public void Deconstruct(out int totalEntries, out IEnumerable<T> entries) {
        totalEntries = TotalEntries;
        entries = Entries;
    }
}

public static class PagedEnumerable {
    public static PagedEnumerable<T> Empty<T>() {
        return new PagedEnumerable<T>(0, []);
    }
}