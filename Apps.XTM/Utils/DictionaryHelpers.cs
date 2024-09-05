namespace Apps.XTM.Utils;

public static class DictionaryHelpers
{
    public static string ToQueryString<T1, T2>(this Dictionary<T1, T2> dictionary)
    {
        return string.Join("&", dictionary.Select(kvp => $"{kvp.Key}={kvp.Value}"));
    }
}