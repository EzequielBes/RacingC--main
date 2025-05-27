public class TelemetryCache
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions
    {
        SizeLimit = 100_000_000 // 100MB limit
    });

    public async Task<T> GetOrComputeAsync<T>(
        string key, 
        Func<Task<T>> computeFunc, 
        TimeSpan? expiration = null)
    {
        if (_cache.TryGetValue(key, out T cachedValue))
            return cachedValue;

        var value = await computeFunc();
        
        var options = new MemoryCacheEntryOptions
        {
            Size = EstimateSize(value),
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(30)
        };
        
        _cache.Set(key, value, options);
        return value;
    }

    private long EstimateSize(object obj)
    {
        // Estimativa simples de tamanho em bytes
        return obj switch
        {
            string str => str.Length * 2,
            ICollection collection => collection.Count * 100,
            _ => 1000
        };
    }
}