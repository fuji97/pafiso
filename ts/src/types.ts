/**
 * Filter operators for comparison operations
 */
export const FilterOperator = {
  Equals: 'eq',
  NotEquals: 'neq',
  GreaterThan: 'gt',
  LessThan: 'lt',
  GreaterThanOrEquals: 'gte',
  LessThanOrEquals: 'lte',
  Contains: 'contains',
  NotContains: 'ncontains',
  Null: 'null',
  NotNull: 'notnull',
} as const;

export type FilterOperator = (typeof FilterOperator)[keyof typeof FilterOperator];

/**
 * Sort order direction
 */
export const SortOrder = {
  Ascending: 'asc',
  Descending: 'desc',
} as const;

export type SortOrder = (typeof SortOrder)[keyof typeof SortOrder];

/**
 * Dictionary representation of key-value pairs
 */
export type Dictionary = Record<string, string>;
