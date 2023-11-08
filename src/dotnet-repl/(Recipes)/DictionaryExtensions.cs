using System;
using System.Collections.Generic;

namespace dotnet_repl;

internal static class DictionaryExtensions
{
    public static TValue GetOrAdd<TValue>(
        this IDictionary<string, object> dictionary,
        string key,
        Func<string, TValue> getValue)
    {
        if (!dictionary.TryGetValue(key, out var value))
        {
            value = getValue(key);
            dictionary.Add(key, value!);
        }

        return (TValue)value!;
    }

    public static void MergeWith<TKey, TValue>(
        this IDictionary<TKey, TValue> target,
        IDictionary<TKey, TValue> source,
        bool replace = false)
    {
        foreach (var pair in source)
        {
            if (replace || !target.ContainsKey(pair.Key))
            {
                target[pair.Key] = pair.Value;
            }
        }
    }
}