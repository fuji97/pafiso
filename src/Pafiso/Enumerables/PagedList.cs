using System.Collections;
using System.Text.Json.Serialization;

namespace Pafiso.Enumerables;

/// <summary>
/// A paged list that wraps a collection of items with pagination metadata.
/// </summary>
/// <remarks>
/// <para>
/// A custom <see cref="PagedListJsonConverterFactory"/> handles System.Text.Json serialization,
/// ensuring the full object (entries, totalEntries, pageNumber, pageSize) is serialized correctly.
/// </para>
/// <para>
/// Because this class implements <see cref="IList{T}"/>, other serializers (e.g., Newtonsoft.Json)
/// may serialize it as a plain array, losing the pagination metadata.
/// Users of other serializers should map to a POCO or register a custom converter.
/// </para>
/// </remarks>
[JsonConverter(typeof(PagedListJsonConverterFactory))]
public class PagedList<T> : IList<T> {
    public int TotalEntries { get; init; } = 0;
    public IList<T> Entries { get; init; } = [];
    public int? PageNumber { get; init; } = null;
    public int? PageSize { get; init; } = null;

    public PagedList() { }

    public PagedList(IList<T> entries, int totalEntries, int? pageNumber, int? pageSize) {
        Entries = entries;
        TotalEntries = totalEntries;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

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