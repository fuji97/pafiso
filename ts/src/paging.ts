import { Dictionary } from './types';

/**
 * Represents pagination parameters for limiting query results
 */
export class Paging {
  readonly skip: number;
  readonly take: number;

  constructor(skip: number, take: number) {
    this.skip = skip;
    this.take = take;
  }

  /**
   * Get the current page number (1-based)
   */
  get page(): number {
    if (this.take === 0) return 1;
    return Math.floor(this.skip / this.take) + 1;
  }

  /**
   * Get the page size
   */
  get pageSize(): number {
    return this.take;
  }

  /**
   * Convert the paging to a dictionary representation
   */
  toDictionary(): Dictionary {
    return {
      skip: this.skip.toString(),
      take: this.take.toString(),
    };
  }

  /**
   * Create paging from a dictionary representation
   */
  static fromDictionary(dict: Dictionary): Paging | null {
    const skip = dict.skip !== undefined ? parseInt(dict.skip, 10) : NaN;
    const take = dict.take !== undefined ? parseInt(dict.take, 10) : NaN;

    if (isNaN(skip) || isNaN(take)) {
      return null;
    }

    return new Paging(skip, take);
  }

  /**
   * Create paging from page number and page size (1-based page)
   */
  static fromPage(page: number, pageSize: number): Paging {
    const skip = (page - 1) * pageSize;
    return new Paging(skip, pageSize);
  }

  /**
   * Create paging from skip and take values
   */
  static fromSkipTake(skip: number, take: number): Paging {
    return new Paging(skip, take);
  }
}

/**
 * Builder for creating Paging instances with a fluent API
 */
export class PagingBuilder {
  private _skip = 0;
  private _take = 10;

  /**
   * Set the number of records to skip
   */
  skip(count: number): this {
    this._skip = count;
    return this;
  }

  /**
   * Set the number of records to take
   */
  take(count: number): this {
    this._take = count;
    return this;
  }

  /**
   * Set page number (1-based) and page size
   */
  page(pageNumber: number, pageSize: number): this {
    this._skip = (pageNumber - 1) * pageSize;
    this._take = pageSize;
    return this;
  }

  /**
   * Build the Paging instance
   */
  build(): Paging {
    return new Paging(this._skip, this._take);
  }
}

/**
 * Create a new PagingBuilder
 */
export function paging(): PagingBuilder {
  return new PagingBuilder();
}
