using Pafiso.Enumerables;

namespace Pafiso.Util;

public static class PagedEnumerableExtensions {
    extension<T>(IQueryable<T> query) {
        public PagedEnumerable<T> ToPagedEnumerable(IQueryable<T> source) {
            return new PagedEnumerable<T>(source.Count(), query.ToList());
        }

        public PagedEnumerable<T> ToPagedEnumerable(int count) {
            return new PagedEnumerable<T>(count, query.ToList());
        }

        public PagedEnumerable<T> ToPagedEnumerable() {
            return new PagedEnumerable<T>(query.Count(), query.ToList());
        }
    }

    extension<T>(IEnumerable<T> query) {
        public PagedEnumerable<T> ToPagedEnumerable(IEnumerable<T> source) {
            return new PagedEnumerable<T>(source.Count(), query.ToList());
        }

        public PagedEnumerable<T> ToPagedEnumerable(int count) {
            return new PagedEnumerable<T>(count, query.ToList());
        }

        public PagedEnumerable<T> ToPagedEnumerable() {
            var list = query.ToList();
            return new PagedEnumerable<T>(list.Count, list);
        }
    }
}