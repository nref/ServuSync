namespace ServuSync;

public static class EnumerableExtensions
{
  public static Dictionary<TKey, TValue> ToDictionaryAllowDuplicateKeys<TKey, TValue, TEnum>(this IEnumerable<TEnum> e, Func<TEnum, TKey> getKey, Func<TEnum, TValue> getValue) where TKey : notnull
  {
    var dict = new Dictionary<TKey, TValue>();
    foreach (TEnum item in e)
    {
      dict[getKey(item)] = getValue(item);
    }
    return dict;
  }
}
