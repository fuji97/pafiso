import { Filter, FilterBuilder, filter } from './filter';
import { Paging, PagingBuilder, paging } from './paging';
import { Sorting, SortingBuilder, sorting } from './sorting';
import { Dictionary } from './types';

/**
 * Combines filters, sortings, and paging into a single search specification
 */
export class SearchParameters {
  readonly filters: Filter[];
  readonly sortings: Sorting[];
  readonly paging: Paging | null;

  constructor(filters: Filter[] = [], sortings: Sorting[] = [], pagingParam: Paging | null = null) {
    this.filters = filters;
    this.sortings = sortings;
    this.paging = pagingParam;
  }

  /**
   * Convert to a flat dictionary suitable for query strings
   * Format: filters[0][fields]=Name&filters[0][op]=Contains&...
   */
  toDictionary(): Dictionary {
    const dict: Dictionary = {};

    // Add filters with indexed keys
    this.filters.forEach((f, index) => {
      const filterDict = f.toDictionary();
      for (const [key, value] of Object.entries(filterDict)) {
        dict[`filters[${index}][${key}]`] = value;
      }
    });

    // Add sortings with indexed keys (remove duplicates by property)
    const uniqueSortings = this.getUniqueSortings();
    uniqueSortings.forEach((s, index) => {
      const sortingDict = s.toDictionary();
      for (const [key, value] of Object.entries(sortingDict)) {
        dict[`sortings[${index}][${key}]`] = value;
      }
    });

    // Add paging directly (not indexed)
    if (this.paging) {
      const pagingDict = this.paging.toDictionary();
      for (const [key, value] of Object.entries(pagingDict)) {
        dict[key] = value;
      }
    }

    return dict;
  }

  /**
   * Convert to URLSearchParams for easy URL construction
   */
  toURLSearchParams(): URLSearchParams {
    const params = new URLSearchParams();
    const dict = this.toDictionary();

    for (const [key, value] of Object.entries(dict)) {
      params.append(key, value);
    }

    return params;
  }

  /**
   * Convert to a query string (without leading ?)
   */
  toQueryString(): string {
    return this.toURLSearchParams().toString();
  }

  /**
   * Create SearchParameters from a dictionary representation
   */
  static fromDictionary(dict: Dictionary): SearchParameters {
    const filters: Filter[] = [];
    const sortings: Sorting[] = [];
    let pagingResult: Paging | null = null;

    // Parse indexed entries
    const filterDicts = new Map<number, Dictionary>();
    const sortingDicts = new Map<number, Dictionary>();
    const pagingDict: Dictionary = {};

    for (const [key, value] of Object.entries(dict)) {
      // Match filters[index][property]
      const filterMatch = key.match(/^filters\[(\d+)\]\[(.+)\]$/);
      if (filterMatch) {
        const index = parseInt(filterMatch[1], 10);
        const prop = filterMatch[2];
        if (!filterDicts.has(index)) {
          filterDicts.set(index, {});
        }
        filterDicts.get(index)![prop] = value;
        continue;
      }

      // Match sortings[index][property]
      const sortingMatch = key.match(/^sortings\[(\d+)\]\[(.+)\]$/);
      if (sortingMatch) {
        const index = parseInt(sortingMatch[1], 10);
        const prop = sortingMatch[2];
        if (!sortingDicts.has(index)) {
          sortingDicts.set(index, {});
        }
        sortingDicts.get(index)![prop] = value;
        continue;
      }

      // Paging keys (skip, take)
      if (key === 'skip' || key === 'take') {
        pagingDict[key] = value;
      }
    }

    // Build filters from parsed dictionaries
    const sortedFilterIndices = [...filterDicts.keys()].sort((a, b) => a - b);
    for (const index of sortedFilterIndices) {
      filters.push(Filter.fromDictionary(filterDicts.get(index)!));
    }

    // Build sortings from parsed dictionaries
    const sortedSortingIndices = [...sortingDicts.keys()].sort((a, b) => a - b);
    for (const index of sortedSortingIndices) {
      sortings.push(Sorting.fromDictionary(sortingDicts.get(index)!));
    }

    // Build paging if present
    if (Object.keys(pagingDict).length > 0) {
      pagingResult = Paging.fromDictionary(pagingDict);
    }

    return new SearchParameters(filters, sortings, pagingResult);
  }

  /**
   * Create SearchParameters from a query string
   */
  static fromQueryString(queryString: string): SearchParameters {
    const params = new URLSearchParams(queryString);
    const dict: Dictionary = {};

    for (const [key, value] of params.entries()) {
      dict[key] = value;
    }

    return SearchParameters.fromDictionary(dict);
  }

  /**
   * Create SearchParameters from URLSearchParams
   */
  static fromURLSearchParams(params: URLSearchParams): SearchParameters {
    const dict: Dictionary = {};

    for (const [key, value] of params.entries()) {
      dict[key] = value;
    }

    return SearchParameters.fromDictionary(dict);
  }

  private getUniqueSortings(): Sorting[] {
    const seen = new Set<string>();
    return this.sortings.filter((s) => {
      if (seen.has(s.property)) {
        return false;
      }
      seen.add(s.property);
      return true;
    });
  }
}

/**
 * Builder for creating SearchParameters with a fluent API
 */
export class SearchParametersBuilder {
  private _filters: Filter[] = [];
  private _sortings: Sorting[] = [];
  private _paging: Paging | null = null;

  /**
   * Add a filter
   */
  filter(filterOrBuilder: Filter | FilterBuilder | ((fb: FilterBuilder) => FilterBuilder)): this {
    if (filterOrBuilder instanceof Filter) {
      this._filters.push(filterOrBuilder);
    } else if (filterOrBuilder instanceof FilterBuilder) {
      this._filters.push(filterOrBuilder.build());
    } else {
      this._filters.push(filterOrBuilder(filter()).build());
    }
    return this;
  }

  /**
   * Add multiple filters
   */
  filters(...filtersArray: (Filter | FilterBuilder)[]): this {
    for (const f of filtersArray) {
      this.filter(f);
    }
    return this;
  }

  /**
   * Add a sorting
   */
  sortBy(sortingOrBuilder: Sorting | SortingBuilder | ((sb: SortingBuilder) => SortingBuilder)): this {
    if (sortingOrBuilder instanceof Sorting) {
      this._sortings.push(sortingOrBuilder);
    } else if (sortingOrBuilder instanceof SortingBuilder) {
      this._sortings.push(sortingOrBuilder.build());
    } else {
      this._sortings.push(sortingOrBuilder(sorting()).build());
    }
    return this;
  }

  /**
   * Add multiple sortings
   */
  sortings(...sortingsArray: (Sorting | SortingBuilder)[]): this {
    for (const s of sortingsArray) {
      this.sortBy(s);
    }
    return this;
  }

  /**
   * Set paging
   */
  paginate(pagingOrBuilder: Paging | PagingBuilder | ((pb: PagingBuilder) => PagingBuilder)): this {
    if (pagingOrBuilder instanceof Paging) {
      this._paging = pagingOrBuilder;
    } else if (pagingOrBuilder instanceof PagingBuilder) {
      this._paging = pagingOrBuilder.build();
    } else {
      this._paging = pagingOrBuilder(paging()).build();
    }
    return this;
  }

  /**
   * Set paging using page number (1-based) and page size
   */
  page(pageNumber: number, pageSize: number): this {
    this._paging = Paging.fromPage(pageNumber, pageSize);
    return this;
  }

  /**
   * Set paging using skip and take
   */
  skipTake(skip: number, take: number): this {
    this._paging = Paging.fromSkipTake(skip, take);
    return this;
  }

  /**
   * Build the SearchParameters instance
   */
  build(): SearchParameters {
    return new SearchParameters(this._filters, this._sortings, this._paging);
  }

  /**
   * Build and return as dictionary
   */
  toDictionary(): Dictionary {
    return this.build().toDictionary();
  }

  /**
   * Build and return as URLSearchParams
   */
  toURLSearchParams(): URLSearchParams {
    return this.build().toURLSearchParams();
  }

  /**
   * Build and return as query string
   */
  toQueryString(): string {
    return this.build().toQueryString();
  }
}

/**
 * Create a new SearchParametersBuilder
 */
export function search(): SearchParametersBuilder {
  return new SearchParametersBuilder();
}
