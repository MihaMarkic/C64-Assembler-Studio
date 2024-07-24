using System;
using System.Collections.Immutable;
using System.Linq;

namespace C64AssemblerStudio.Core.Common;

public abstract class EnumMapper<T>
{
    ImmutableDictionary<Type, ImmutableDictionary<Enum, T>> _cache;

    protected EnumMapper()
    {
        _cache = ImmutableDictionary<Type, ImmutableDictionary<Enum, T>>.Empty;
    }

    protected ImmutableDictionary<Enum, T> GetFromCache(Type enumType, Func<ImmutableDictionary<Enum, T>> populate)
    {
        if (!_cache.TryGetValue(enumType, out var data))
        {
            data = populate();
            _cache.Add(enumType, data);
        }
        return data;
    }

    protected abstract T Map(Type enumType, Enum value);

    public ImmutableDictionary<Enum, T> GetMapEnum(Type enumType)
    {
        return GetFromCache(enumType, () =>
        {
            var query = from v in Enum.GetValues(enumType).Cast<Enum>()
                        select new { Key = v, Value = Map(enumType, v) };
            return query.ToImmutableDictionary(p => p.Key, p => p.Value);
        });
    }
}
