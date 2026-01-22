namespace Pafiso.Extensions;

public static class SortingExtensions {
    extension<T>(IQueryable<T> query) {
        public IOrderedQueryable<T> OrderBy(Sorting sorting) {
            return sorting.ApplyToIQueryable(query);
        }

        public IOrderedQueryable<T>? OrderBy(Sorting sorting, Action<FieldRestrictions>? configureRestrictions) {
            if (configureRestrictions == null) return sorting.ApplyToIQueryable(query);
            var restrictions = new FieldRestrictions();
            configureRestrictions(restrictions);
            return sorting.ApplyToIQueryable(query, restrictions);
        }

        public IOrderedQueryable<T>? OrderBy(Sorting sorting, FieldRestrictions? restrictions) {
            return sorting.ApplyToIQueryable(query, restrictions);
        }
    }

    extension<T>(IEnumerable<T> query) {
        public IEnumerable<T> OrderBy(Sorting sorting) {
            return sorting.ApplyToIQueryable(query.AsQueryable()).ToList();
        }

        public IEnumerable<T>? OrderBy(Sorting sorting, Action<FieldRestrictions>? configureRestrictions) {
            if (configureRestrictions == null) return sorting.ApplyToIQueryable(query.AsQueryable()).ToList();
            var restrictions = new FieldRestrictions();
            configureRestrictions(restrictions);
            return sorting.ApplyToIQueryable(query.AsQueryable(), restrictions)?.ToList();
        }

        public IEnumerable<T>? OrderBy(Sorting sorting, FieldRestrictions? restrictions) {
            return sorting.ApplyToIQueryable(query.AsQueryable(), restrictions)?.ToList();
        }
    }

    extension<T>(IOrderedQueryable<T> query) {
        public IOrderedQueryable<T> ThenBy(Sorting sorting) {
            return sorting.ThenApplyToIQueryable(query);
        }

        public IOrderedQueryable<T> ThenBy(Sorting sorting, Action<FieldRestrictions>? configureRestrictions) {
            if (configureRestrictions == null) return sorting.ThenApplyToIQueryable(query);
            var restrictions = new FieldRestrictions();
            configureRestrictions(restrictions);
            return sorting.ThenApplyToIQueryable(query, restrictions);
        }

        public IOrderedQueryable<T> ThenBy(Sorting sorting, FieldRestrictions? restrictions) {
            return sorting.ThenApplyToIQueryable(query, restrictions);
        }
    }
}
