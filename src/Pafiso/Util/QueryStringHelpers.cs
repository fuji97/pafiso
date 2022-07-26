using System.Text.RegularExpressions;

namespace Pafiso.Util; 

public static class QueryStringHelpers {
    private static readonly Regex QueryStringRegex = new Regex(@"^(.+)\[(\d+)\]\[(.+)\]$", RegexOptions.Compiled);
    
    public static IDictionary<string,string> MergeListOfQueryStrings(string name, IEnumerable<IDictionary<string,string>> queryStrings) {
        var result = new Dictionary<string,string>();
        var i = 0;
        foreach (var queryString in queryStrings) {
            foreach (var (key, value) in queryString) {
                result[$"{name}[{i}][{key}]"] = value;
            }
            i++;
        }
        return result;
    }
    
    public static IDictionary<string,List<IDictionary<string,string>>> SplitQueryStringInList(IDictionary<string,string> queryString) {
        var result = new Dictionary<string, IDictionary<int, IDictionary<string,string>>>();
        
        foreach (var (key, value) in queryString) {
            var match = QueryStringRegex.Match(key);
            
            if (!match.Success) 
                continue;
            
            var listKey = match.Groups[1].Value;
            var index = int.Parse(match.Groups[2].Value);
            var itemKey = match.Groups[3].Value;

            IDictionary<int, IDictionary<string,string>> dc;
            if (!result.ContainsKey(listKey)) {
                dc = new Dictionary<int, IDictionary<string,string>>();
                result[listKey] = dc;
            } else {
                dc = result[listKey];
            }
            if (dc.TryGetValue(index, out var item)) {
                item[itemKey] = value;
            } else {
                item = new Dictionary<string,string>();
                item[itemKey] = value;
                dc[index] = item;
            }
        }
        return result.ToDictionary(root => root.Key,
            root => 
                root.Value.OrderBy(ls => ls.Key)
                    .Select(ls => ls.Value)
                    .ToList());
    }
}