using StackExchange.Redis;

public class Redis
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;

    public Redis(IConnectionMultiplexer redis)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _db = _redis.GetDatabase();
    }

    public async Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null)
        => await _db.StringSetAsync(key, value, expiry);

    public async Task<string?> GetStringAsync(string key)
        => await _db.StringGetAsync(key);

    public async Task<bool> DeleteKeyAsync(string key)
        => await _db.KeyDeleteAsync(key);

    public async Task<bool> KeyExistsAsync(string key)
        => await _db.KeyExistsAsync(key);

    public async Task<long> IncrementAsync(string key, long value = 1)
        => await _db.StringIncrementAsync(key, value);

    public async Task<long> DecrementAsync(string key, long value = 1)
        => await _db.StringDecrementAsync(key, value);

    // ✅ thêm method để xoá theo pattern
    public async Task DeleteKeysByPatternAsync(string pattern)
    {
        var endpoints = _redis.GetEndPoints();
        var server = _redis.GetServer(endpoints.First());

        foreach (var key in server.Keys(pattern: pattern))
        {
            await _db.KeyDeleteAsync(key);
        }
    }
}
