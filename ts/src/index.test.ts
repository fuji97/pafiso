import { describe, expect, it } from 'vitest';
import {
  Filter,
  FilterOperator,
  Paging,
  search,
  SearchParameters,
  Sorting,
  SortOrder,
  filter,
  sorting,
  paging,
} from './index';

describe('Filter', () => {
  it('should create a filter with single field', () => {
    const f = new Filter('Name', FilterOperator.Contains, 'Franco');

    expect(f.fields).toEqual(['Name']);
    expect(f.operator).toBe(FilterOperator.Contains);
    expect(f.value).toBe('Franco');
    expect(f.caseSensitive).toBe(false);
  });

  it('should create a filter with multiple fields', () => {
    const f = new Filter(['Name', 'Description'], FilterOperator.Contains, 'search');

    expect(f.fields).toEqual(['Name', 'Description']);
  });

  it('should serialize to dictionary', () => {
    const f = new Filter('Name', FilterOperator.Contains, 'Franco', true);
    const dict = f.toDictionary();

    expect(dict).toEqual({
      fields: 'Name',
      op: 'contains',
      val: 'Franco',
      case: 'true',
    });
  });

  it('should omit case when false', () => {
    const f = new Filter('Name', FilterOperator.Equals, 'test', false);
    const dict = f.toDictionary();

    expect(dict.case).toBeUndefined();
  });

  it('should omit val when null', () => {
    const f = new Filter('Name', FilterOperator.Null);
    const dict = f.toDictionary();

    expect(dict.val).toBeUndefined();
  });

  it('should deserialize from dictionary', () => {
    const dict = {
      fields: 'Name,Description',
      op: 'contains',
      val: 'test',
      case: 'true',
    };

    const f = Filter.fromDictionary(dict);

    expect(f.fields).toEqual(['Name', 'Description']);
    expect(f.operator).toBe(FilterOperator.Contains);
    expect(f.value).toBe('test');
    expect(f.caseSensitive).toBe(true);
  });
});

describe('FilterBuilder', () => {
  it('should build filter with fluent API', () => {
    const f = filter().field('Age').greaterThan('20').caseSensitive().build();

    expect(f.fields).toEqual(['Age']);
    expect(f.operator).toBe(FilterOperator.GreaterThan);
    expect(f.value).toBe('20');
    expect(f.caseSensitive).toBe(true);
  });

  it('should build filter with multiple fields', () => {
    const f = filter().fields('Name', 'Description', 'Tags').contains('search').build();

    expect(f.fields).toEqual(['Name', 'Description', 'Tags']);
    expect(f.operator).toBe(FilterOperator.Contains);
  });
});

describe('Sorting', () => {
  it('should create ascending sort', () => {
    const s = new Sorting('Name', SortOrder.Ascending);

    expect(s.property).toBe('Name');
    expect(s.order).toBe(SortOrder.Ascending);
  });

  it('should serialize to dictionary', () => {
    const s = new Sorting('DateCreated', SortOrder.Descending);
    const dict = s.toDictionary();

    expect(dict).toEqual({
      prop: 'DateCreated',
      ord: 'desc',
    });
  });

  it('should deserialize from dictionary', () => {
    const dict = { prop: 'Name', ord: 'asc' };
    const s = Sorting.fromDictionary(dict);

    expect(s.property).toBe('Name');
    expect(s.order).toBe(SortOrder.Ascending);
  });

  it('should create with static helpers', () => {
    expect(Sorting.asc('Name').order).toBe(SortOrder.Ascending);
    expect(Sorting.desc('Name').order).toBe(SortOrder.Descending);
  });
});

describe('SortingBuilder', () => {
  it('should build sorting with fluent API', () => {
    const s = sorting().by('Name').desc().build();

    expect(s.property).toBe('Name');
    expect(s.order).toBe(SortOrder.Descending);
  });
});

describe('Paging', () => {
  it('should create from skip/take', () => {
    const p = Paging.fromSkipTake(20, 10);

    expect(p.skip).toBe(20);
    expect(p.take).toBe(10);
  });

  it('should create from page number', () => {
    const p = Paging.fromPage(3, 10);

    expect(p.skip).toBe(20);
    expect(p.take).toBe(10);
    expect(p.page).toBe(3);
    expect(p.pageSize).toBe(10);
  });

  it('should serialize to dictionary', () => {
    const p = new Paging(10, 20);
    const dict = p.toDictionary();

    expect(dict).toEqual({
      skip: '10',
      take: '20',
    });
  });

  it('should deserialize from dictionary', () => {
    const dict = { skip: '30', take: '15' };
    const p = Paging.fromDictionary(dict);

    expect(p?.skip).toBe(30);
    expect(p?.take).toBe(15);
  });

  it('should return null for invalid dictionary', () => {
    const p = Paging.fromDictionary({});
    expect(p).toBeNull();
  });
});

describe('PagingBuilder', () => {
  it('should build paging with fluent API', () => {
    const p = paging().skip(20).take(10).build();

    expect(p.skip).toBe(20);
    expect(p.take).toBe(10);
  });

  it('should build paging from page number', () => {
    const p = paging().page(2, 25).build();

    expect(p.skip).toBe(25);
    expect(p.take).toBe(25);
  });
});

describe('SearchParameters', () => {
  it('should serialize to dictionary', () => {
    const sp = new SearchParameters(
      [
        new Filter('Name', FilterOperator.Contains, 'Franco'),
        new Filter('Age', FilterOperator.GreaterThan, '20', true),
      ],
      [new Sorting('Name', SortOrder.Ascending), new Sorting('Age', SortOrder.Descending)],
      Paging.fromPage(2, 10)
    );

    const dict = sp.toDictionary();

    expect(dict['filters[0][fields]']).toBe('Name');
    expect(dict['filters[0][op]']).toBe('contains');
    expect(dict['filters[0][val]']).toBe('Franco');
    expect(dict['filters[1][fields]']).toBe('Age');
    expect(dict['filters[1][op]']).toBe('gt');
    expect(dict['filters[1][val]']).toBe('20');
    expect(dict['filters[1][case]']).toBe('true');
    expect(dict['sortings[0][prop]']).toBe('Name');
    expect(dict['sortings[0][ord]']).toBe('asc');
    expect(dict['sortings[1][prop]']).toBe('Age');
    expect(dict['sortings[1][ord]']).toBe('desc');
    expect(dict['skip']).toBe('10');
    expect(dict['take']).toBe('10');
  });

  it('should deserialize from dictionary', () => {
    const dict = {
      'filters[0][fields]': 'Name',
      'filters[0][op]': 'contains',
      'filters[0][val]': 'Franco',
      'sortings[0][prop]': 'Name',
      'sortings[0][ord]': 'asc',
      skip: '10',
      take: '10',
    };

    const sp = SearchParameters.fromDictionary(dict);

    expect(sp.filters.length).toBe(1);
    expect(sp.filters[0].fields).toEqual(['Name']);
    expect(sp.filters[0].operator).toBe(FilterOperator.Contains);
    expect(sp.sortings.length).toBe(1);
    expect(sp.sortings[0].property).toBe('Name');
    expect(sp.paging?.skip).toBe(10);
    expect(sp.paging?.take).toBe(10);
  });

  it('should convert to URLSearchParams', () => {
    const sp = new SearchParameters(
      [new Filter('Name', FilterOperator.Equals, 'test')],
      [],
      null
    );

    const params = sp.toURLSearchParams();

    expect(params.get('filters[0][fields]')).toBe('Name');
    expect(params.get('filters[0][op]')).toBe('eq');
    expect(params.get('filters[0][val]')).toBe('test');
  });

  it('should convert to query string', () => {
    const sp = new SearchParameters([], [new Sorting('Name')], Paging.fromSkipTake(0, 10));

    const qs = sp.toQueryString();
    const decoded = decodeURIComponent(qs);

    expect(decoded).toContain('sortings[0][prop]=Name');
    expect(decoded).toContain('sortings[0][ord]=asc');
    expect(decoded).toContain('skip=0');
    expect(decoded).toContain('take=10');
  });

  it('should parse from query string', () => {
    const qs = 'filters[0][fields]=Name&filters[0][op]=contains&filters[0][val]=test&skip=5&take=10';

    const sp = SearchParameters.fromQueryString(qs);

    expect(sp.filters.length).toBe(1);
    expect(sp.filters[0].value).toBe('test');
    expect(sp.paging?.skip).toBe(5);
  });

  it('should remove duplicate sortings by property', () => {
    const sp = new SearchParameters(
      [],
      [new Sorting('Name', SortOrder.Ascending), new Sorting('Name', SortOrder.Descending)],
      null
    );

    const dict = sp.toDictionary();

    expect(Object.keys(dict).filter((k) => k.includes('sortings'))).toHaveLength(2);
    expect(dict['sortings[0][ord]']).toBe('asc');
  });
});

describe('SearchParametersBuilder', () => {
  it('should build with fluent API', () => {
    const sp = search()
      .filter((f) => f.field('Name').contains('Franco'))
      .filter((f) => f.field('Age').greaterThan('20'))
      .sortBy((s) => s.by('Name').asc())
      .sortBy((s) => s.by('Age').desc())
      .page(2, 10)
      .build();

    expect(sp.filters.length).toBe(2);
    expect(sp.sortings.length).toBe(2);
    expect(sp.paging?.page).toBe(2);
    expect(sp.paging?.pageSize).toBe(10);
  });

  it('should accept Filter and Sorting instances', () => {
    const sp = search()
      .filter(new Filter('Name', FilterOperator.Equals, 'test'))
      .sortBy(Sorting.desc('Name'))
      .skipTake(0, 20)
      .build();

    expect(sp.filters[0].value).toBe('test');
    expect(sp.sortings[0].order).toBe(SortOrder.Descending);
    expect(sp.paging?.take).toBe(20);
  });

  it('should convert directly to dictionary', () => {
    const dict = search()
      .filter((f) => f.field('Status').equals('active'))
      .page(1, 25)
      .toDictionary();

    expect(dict['filters[0][val]']).toBe('active');
    expect(dict['skip']).toBe('0');
    expect(dict['take']).toBe('25');
  });

  it('should convert directly to URLSearchParams', () => {
    const params = search()
      .filter((f) => f.field('Name').contains('test'))
      .toURLSearchParams();

    expect(params.get('filters[0][op]')).toBe('contains');
  });

  it('should convert directly to query string', () => {
    const qs = search()
      .sortBy(Sorting.asc('CreatedAt'))
      .page(1, 10)
      .toQueryString();

    const decoded = decodeURIComponent(qs);
    expect(decoded).toContain('sortings[0][prop]=CreatedAt');
  });
});
