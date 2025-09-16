using StackExchange.Redis;

namespace Capstone.Database
{
    public class Redis
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;

        public Redis(IConnectionMultiplexer redis)
        {
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            _db = _redis.GetDatabase();
        }

        // Lưu key/value (string) async
        public async Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null)
        {
            return await _db.StringSetAsync(key, value, expiry);
        }

        // Lấy value theo key async
        public async Task<string?> GetStringAsync(string key)
        {
            return await _db.StringGetAsync(key);
        }

        // Xóa key async
        public async Task<bool> DeleteKeyAsync(string key)
        {
            return await _db.KeyDeleteAsync(key);
        }

        // Kiểm tra key tồn tại async
        public async Task<bool> KeyExistsAsync(string key)
        {
            return await _db.KeyExistsAsync(key);
        }

        // Tăng giá trị key kiểu int
        public async Task<long> IncrementAsync(string key, long value = 1)
        {
            return await _db.StringIncrementAsync(key, value);
        }

        // Giảm giá trị key kiểu int
        public async Task<long> DecrementAsync(string key, long value = 1)
        {
            return await _db.StringDecrementAsync(key, value);
        }
    }
}
