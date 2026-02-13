namespace Pafiso.Extensions;

public static class FilterExtensions {
    extension<T>(IQueryable<T> query) {
        public IQueryable<T> Where(Filter filter) {
            return filter.ApplyFilter(query);
        }

        public IQueryable<T> Where(Filter filter, PafisoSettings? settings) {
            return filter.ApplyFilter(query, settings);
        }
    }

    extension<T>(IEnumerable<T> query) {
        public IEnumerable<T> Where(Filter filter) {
            return filter.ApplyFilter(query.AsQueryable());
        }

        public IEnumerable<T> Where(Filter filter, PafisoSettings? settings) {
            return filter.ApplyFilter(query.AsQueryable(), settings);
        }
    }
}
