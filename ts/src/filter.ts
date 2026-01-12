import { Dictionary, FilterOperator } from './types';

/**
 * Represents a filter condition for querying data
 */
export class Filter {
  readonly fields: string[];
  readonly operator: FilterOperator;
  readonly value: string | null;
  readonly caseSensitive: boolean;

  constructor(
    fields: string | string[],
    operator: FilterOperator,
    value?: string | null,
    caseSensitive = false
  ) {
    this.fields = Array.isArray(fields) ? fields : [fields];
    this.operator = operator;
    this.value = value ?? null;
    this.caseSensitive = caseSensitive;
  }

  /**
   * Convert the filter to a dictionary representation
   */
  toDictionary(): Dictionary {
    const dict: Dictionary = {
      fields: this.fields.join(','),
      op: this.operator,
    };

    if (this.value !== null) {
      dict.val = this.value;
    }

    if (this.caseSensitive) {
      dict.case = 'true';
    }

    return dict;
  }

  /**
   * Create a filter from a dictionary representation
   */
  static fromDictionary(dict: Dictionary): Filter {
    const fields = dict.fields?.split(',') ?? [];
    const operator = (dict.op as FilterOperator) ?? FilterOperator.Equals;
    const value = dict.val ?? null;
    const caseSensitive = dict.case === 'true';

    return new Filter(fields, operator, value, caseSensitive);
  }
}

/**
 * Builder for creating Filter instances with a fluent API
 */
export class FilterBuilder {
  private _fields: string[] = [];
  private _operator: FilterOperator = FilterOperator.Equals;
  private _value: string | null = null;
  private _caseSensitive = false;

  /**
   * Set the field(s) to filter on
   */
  field(field: string): this {
    this._fields = [field];
    return this;
  }

  /**
   * Set multiple fields (creates OR condition)
   */
  fields(...fields: string[]): this {
    this._fields = fields;
    return this;
  }

  /**
   * Set the filter operator
   */
  op(operator: FilterOperator): this {
    this._operator = operator;
    return this;
  }

  /**
   * Set the filter value
   */
  value(value: string | null): this {
    this._value = value;
    return this;
  }

  /**
   * Enable case-sensitive matching
   */
  caseSensitive(enabled = true): this {
    this._caseSensitive = enabled;
    return this;
  }

  // Convenience methods for common operators

  equals(value: string): this {
    return this.op(FilterOperator.Equals).value(value);
  }

  notEquals(value: string): this {
    return this.op(FilterOperator.NotEquals).value(value);
  }

  greaterThan(value: string): this {
    return this.op(FilterOperator.GreaterThan).value(value);
  }

  lessThan(value: string): this {
    return this.op(FilterOperator.LessThan).value(value);
  }

  greaterThanOrEquals(value: string): this {
    return this.op(FilterOperator.GreaterThanOrEquals).value(value);
  }

  lessThanOrEquals(value: string): this {
    return this.op(FilterOperator.LessThanOrEquals).value(value);
  }

  contains(value: string): this {
    return this.op(FilterOperator.Contains).value(value);
  }

  notContains(value: string): this {
    return this.op(FilterOperator.NotContains).value(value);
  }

  isNull(): this {
    return this.op(FilterOperator.Null);
  }

  isNotNull(): this {
    return this.op(FilterOperator.NotNull);
  }

  /**
   * Build the Filter instance
   */
  build(): Filter {
    return new Filter(this._fields, this._operator, this._value, this._caseSensitive);
  }
}

/**
 * Create a new FilterBuilder
 */
export function filter(): FilterBuilder {
  return new FilterBuilder();
}
