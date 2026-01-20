using System.Collections;

namespace Pafiso.Enumerables;

public class PagedList<T> : IList<T> {
    public int TotalEntries { get; init; } = 0;
    public IList<T> Entries { get; init; } = [];
    public IEnumerator<T> GetEnumerator() {
        return Entries.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return ((IEnumerable)Entries).GetEnumerator();
    }

    public void Add(T item) {
        Entries.Add(item);
    }

    public void Clear() {
        Entries.Clear();
    }

    public bool Contains(T item) {
        return Entries.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex) {
        Entries.CopyTo(array, arrayIndex);
    }

    public bool Remove(T item) {
        return Entries.Remove(item);
    }

    public int Count => Entries.Count;

    public bool IsReadOnly => Entries.IsReadOnly;

    public int IndexOf(T item) {
        return Entries.IndexOf(item);
    }

    public void Insert(int index, T item) {
        Entries.Insert(index, item);
    }

    public void RemoveAt(int index) {
        Entries.RemoveAt(index);
    }

    public T this[int index] {
        get => Entries[index];
        set => Entries[index] = value;
    }
}