using System.Linq.Expressions;
using Pafiso.Util;

namespace Pafiso;

/// <summary>
/// Configures field-level restrictions for filtering and sorting operations.
/// Supports both allowlist and blocklist modes, configured separately for filters and sorting.
/// Unlike Filter, Sorting, and Paging, FieldRestrictions are server-side configuration
/// and do not support serialization to query strings.
/// </summary>
public class FieldRestrictions {
    private HashSet<string>? _allowedFilterFields;
    private HashSet<string>? _blockedFilterFields;
    private HashSet<string>? _allowedSortFields;
    private HashSet<string>? _blockedSortFields;

    #region Filtering Restrictions

    /// <summary>
    /// Allows filtering on the specified fields (expression-based, type-safe).
    /// When allowlist is used, only explicitly allowed fields can be filtered.
    /// </summary>
    public FieldRestrictions AllowFiltering<T>(params Expression<Func<T, object>>[] fieldExpressions) =>
        AddExpressionFields(ref _allowedFilterFields, fieldExpressions);

    /// <summary>
    /// Allows filtering on the specified fields (string-based).
    /// When allowlist is used, only explicitly allowed fields can be filtered.
    /// </summary>
    public FieldRestrictions AllowFiltering(params string[] fields) =>
        AddStringFields(ref _allowedFilterFields, fields);

    /// <summary>
    /// Blocks filtering on the specified fields (expression-based, type-safe).
    /// All fields are allowed except the explicitly blocked ones.
    /// </summary>
    public FieldRestrictions BlockFiltering<T>(params Expression<Func<T, object>>[] fieldExpressions) =>
        AddExpressionFields(ref _blockedFilterFields, fieldExpressions);

    /// <summary>
    /// Blocks filtering on the specified fields (string-based).
    /// All fields are allowed except the explicitly blocked ones.
    /// </summary>
    public FieldRestrictions BlockFiltering(params string[] fields) =>
        AddStringFields(ref _blockedFilterFields, fields);

    #endregion

    #region Sorting Restrictions

    /// <summary>
    /// Allows sorting on the specified fields (expression-based, type-safe).
    /// When allowlist is used, only explicitly allowed fields can be sorted.
    /// </summary>
    public FieldRestrictions AllowSorting<T>(params Expression<Func<T, object>>[] fieldExpressions) =>
        AddExpressionFields(ref _allowedSortFields, fieldExpressions);

    /// <summary>
    /// Allows sorting on the specified fields (string-based).
    /// When allowlist is used, only explicitly allowed fields can be sorted.
    /// </summary>
    public FieldRestrictions AllowSorting(params string[] fields) =>
        AddStringFields(ref _allowedSortFields, fields);

    /// <summary>
    /// Blocks sorting on the specified fields (expression-based, type-safe).
    /// All fields are allowed except the explicitly blocked ones.
    /// </summary>
    public FieldRestrictions BlockSorting<T>(params Expression<Func<T, object>>[] fieldExpressions) =>
        AddExpressionFields(ref _blockedSortFields, fieldExpressions);

    /// <summary>
    /// Blocks sorting on the specified fields (string-based).
    /// All fields are allowed except the explicitly blocked ones.
    /// </summary>
    public FieldRestrictions BlockSorting(params string[] fields) =>
        AddStringFields(ref _blockedSortFields, fields);

    #endregion

    #region Validation Methods

    /// <summary>
    /// Checks if a filter field is allowed based on the configured restrictions.
    /// </summary>
    internal bool IsFilterFieldAllowed(string field) =>
        IsFieldAllowed(field, _blockedFilterFields, _allowedFilterFields);

    /// <summary>
    /// Checks if a sort field is allowed based on the configured restrictions.
    /// </summary>
    internal bool IsSortFieldAllowed(string field) =>
        IsFieldAllowed(field, _blockedSortFields, _allowedSortFields);

    /// <summary>
    /// Gets the allowed fields from a filter.
    /// Returns only fields that pass the restriction check.
    /// </summary>
    internal List<string> GetAllowedFilterFields(Filter filter) =>
        filter.Fields.Where(IsFilterFieldAllowed).ToList();

    #endregion

    #region Private Helpers

    private FieldRestrictions AddExpressionFields<T>(
        ref HashSet<string>? targetSet,
        Expression<Func<T, object>>[] fieldExpressions) {
        targetSet ??= [];
        foreach (var expr in fieldExpressions) {
            targetSet.Add(ExpressionUtilities.ExpressionDecomposer(expr.Body));
        }
        return this;
    }

    private FieldRestrictions AddStringFields(ref HashSet<string>? targetSet, string[] fields) {
        targetSet ??= [];
        foreach (var field in fields) {
            targetSet.Add(field);
        }
        return this;
    }

    private static bool IsFieldAllowed(string field, HashSet<string>? blockedSet, HashSet<string>? allowedSet) {
        // Blocklist takes precedence
        if (blockedSet != null && blockedSet.Contains(field)) {
            return false;
        }
        // If allowlist exists, field must be in it
        if (allowedSet != null) {
            return allowedSet.Contains(field);
        }
        // No restrictions = allow all
        return true;
    }

    #endregion
}
