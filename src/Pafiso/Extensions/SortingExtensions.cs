namespace Pafiso.Extensions;

public static class SortingExtensions {
    extension<T>(IQueryable<T> query) {
        public IOrderedQueryable<T> OrderBy(Sorting sorting) {
            return sorting.ApplyToIQueryable(query);
        }

        public IOrderedQueryable<T> OrderBy(Sorting sorting, PafisoSettings? settings) {
            return sorting.ApplyToIQueryable(query, settings);
        }
    }

    extension<T>(IEnumerable<T> query) {
        public IEnumerable<T> OrderBy(Sorting sorting) {
            return sorting.ApplyToIQueryable(query.AsQueryable()).ToList();
        }

        public IEnumerable<T> OrderBy(Sorting sorting, PafisoSettings? settings) {
            return sorting.ApplyToIQueryable(query.AsQueryable(), settings).ToList();
        }
    }

    extension<T>(IOrderedQueryable<T> query) {
        public IOrderedQueryable<T> ThenBy(Sorting sorting) {
            return sorting.ThenApplyToIQueryable(query);
        }

        public IOrderedQueryable<T> ThenBy(Sorting sorting, PafisoSettings? settings) {
            return sorting.ThenApplyToIQueryable(query, settings);
        }
    }
}
