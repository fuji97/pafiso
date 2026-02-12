using Pafiso;
using Pafiso.AspNetCore;

namespace Pafiso.EntityFrameworkCore;

public static class EfFilteringExtensions {

    extension<TEntity>(SearchParametersBuilder<TEntity> builder) {
        public FilterOptionsBuilder<TMapping, TEntity> WithEfFiltering<TMapping>()
            where TMapping : MappingModel {
            return builder.WithFiltering<TMapping>(EfCoreExpressionBuilder.Instance);
        }

        public FilterOptionsBuilder<TMapping, TEntity> WithEfFiltering<TMapping>(EfFilteringSettings settings)
            where TMapping : MappingModel {
            return builder.WithFiltering<TMapping>(EfCoreExpressionBuilder.Instance, settings.CaseSensitive);
        }
    }
}
