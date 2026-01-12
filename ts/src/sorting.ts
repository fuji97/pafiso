import { Dictionary, SortOrder } from './types';

/**
 * Represents a sorting specification for ordering data
 */
export class Sorting {
  readonly property: string;
  readonly order: SortOrder;

  constructor(property: string, order: SortOrder = SortOrder.Ascending) {
    this.property = property;
    this.order = order;
  }

  /**
   * Convert the sorting to a dictionary representation
   */
  toDictionary(): Dictionary {
    return {
      prop: this.property,
      ord: this.order,
    };
  }

  /**
   * Create a sorting from a dictionary representation
   */
  static fromDictionary(dict: Dictionary): Sorting {
    const property = dict.prop ?? '';
    const order = (dict.ord as SortOrder) ?? SortOrder.Ascending;

    return new Sorting(property, order);
  }

  /**
   * Create an ascending sort for a property
   */
  static asc(property: string): Sorting {
    return new Sorting(property, SortOrder.Ascending);
  }

  /**
   * Create a descending sort for a property
   */
  static desc(property: string): Sorting {
    return new Sorting(property, SortOrder.Descending);
  }
}

/**
 * Builder for creating Sorting instances with a fluent API
 */
export class SortingBuilder {
  private _property = '';
  private _order: SortOrder = SortOrder.Ascending;

  /**
   * Set the property to sort by
   */
  by(property: string): this {
    this._property = property;
    return this;
  }

  /**
   * Set ascending order
   */
  asc(): this {
    this._order = SortOrder.Ascending;
    return this;
  }

  /**
   * Set descending order
   */
  desc(): this {
    this._order = SortOrder.Descending;
    return this;
  }

  /**
   * Build the Sorting instance
   */
  build(): Sorting {
    return new Sorting(this._property, this._order);
  }
}

/**
 * Create a new SortingBuilder
 */
export function sorting(): SortingBuilder {
  return new SortingBuilder();
}
