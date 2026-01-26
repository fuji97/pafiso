namespace Pafiso.Extensions;

public static class FilterExtensions {
    extension<T>(IQueryable<T> query) {
        public IQueryable<T> Where(Filter filter) {
            return filter.ApplyFilter(query);
        }

        public IQueryable<T> Where(Filter filter, Action<FieldRestrictions>? configureRestrictions) {
            if (configureRestrictions == null) return filter.ApplyFilter(query);
            var restrictions = new FieldRestrictions();
            configureRestrictions(restrictions);
            return filter.ApplyFilter(query, restrictions);
        }

        public IQueryable<T> Where(Filter filter, FieldRestrictions? restrictions) {
            return filter.ApplyFilter(query, restrictions);
        }

        public IQueryable<T> Where(Filter filter, PafisoSettings? settings) {
            return filter.ApplyFilter(query, settings);
        }

        public IQueryable<T> Where(Filter filter, FieldRestrictions? restrictions, PafisoSettings? settings) {
            return filter.ApplyFilter(query, restrictions, settings);
        }

        public IQueryable<T> Where(Filter filter, Action<FieldRestrictions>? configureRestrictions, PafisoSettings? settings) {
            if (configureRestrictions == null) return filter.ApplyFilter(query, settings);
            var restrictions = new FieldRestrictions();
            configureRestrictions(restrictions);
            return filter.ApplyFilter(query, restrictions, settings);
        }
    }

    extension<T>(IEnumerable<T> query) {
        public IEnumerable<T> Where(Filter filter) {
            return filter.ApplyFilter(query.AsQueryable());
        }

        public IEnumerable<T> Where(Filter filter, Action<FieldRestrictions>? configureRestrictions) {
            return Where(query.AsQueryable(), filter, configureRestrictions);
        }

        public IEnumerable<T> Where(Filter filter, FieldRestrictions? restrictions) {
            return filter.ApplyFilter(query.AsQueryable(), restrictions);
        }

        public IEnumerable<T> Where(Filter filter, PafisoSettings? settings) {
            return filter.ApplyFilter(query.AsQueryable(), settings);
        }

        public IEnumerable<T> Where(Filter filter, FieldRestrictions? restrictions, PafisoSettings? settings) {
            return filter.ApplyFilter(query.AsQueryable(), restrictions, settings);
        }

        public IEnumerable<T> Where(Filter filter, Action<FieldRestrictions>? configureRestrictions, PafisoSettings? settings) {
            return Where(query.AsQueryable(), filter, configureRestrictions, settings);
        }
    }
}
