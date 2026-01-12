/**
 * Filter operators for comparison operations
 */
export const FilterOperator = {
  Equals: 'Equals',
  NotEquals: 'NotEquals',
  GreaterThan: 'GreaterThan',
  LessThan: 'LessThan',
  GreaterThanOrEquals: 'GreaterThanOrEquals',
  LessThanOrEquals: 'LessThanOrEquals',
  Contains: 'Contains',
  NotContains: 'NotContains',
  Null: 'Null',
  NotNull: 'NotNull',
} as const;

export type FilterOperator = (typeof FilterOperator)[keyof typeof FilterOperator];

/**
 * Sort order direction
 */
export const SortOrder = {
  Ascending: 'Ascending',
  Descending: 'Descending',
} as const;

export type SortOrder = (typeof SortOrder)[keyof typeof SortOrder];

/**
 * Dictionary representation of key-value pairs
 */
export type Dictionary = Record<string, string>;
