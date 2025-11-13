using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Capstone.Database
{
    public class Redis : IRedis
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;

        public Redis(IConnectionMultiplexer redis)
        {
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            _db = _redis.GetDatabase();
        }

        // --------------------------
        // 🔹 HELPER method for setting TTL
        // --------------------------
        private async Task SetExpiryIfNeeded(string key, TimeSpan? expiry)
        {
            if (expiry.HasValue)
            {
                await _db.KeyExpireAsync(key, expiry.Value);
            }
        }

        // --------------------------
        // 🔹 STRING operations
        // --------------------------
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

        public async Task DeleteKeysByPatternAsync(string pattern)
        {
            var endpoints = _redis.GetEndPoints();
            var server = _redis.GetServer(endpoints.First());

            foreach (var key in server.Keys(pattern: pattern))
            {
                await _db.KeyDeleteAsync(key);
            }
        }

        // --------------------------
        // 🔹 SET operations
        // --------------------------
        public async Task<bool> SAddAsync(string key, string value)
            => await _db.SetAddAsync(key, value);

        public async Task<bool> SAddAsync(string key, string value, TimeSpan? expiry)
        {
            var result = await _db.SetAddAsync(key, value);
            await SetExpiryIfNeeded(key, expiry);
            return result;
        }

        public async Task<long> SAddRangeAsync(string key, IEnumerable<string> values)
        {
            var redisValues = values.Select(v => (RedisValue)v).ToArray();
            return await _db.SetAddAsync(key, redisValues);
        }

        public async Task<long> SAddRangeAsync(string key, IEnumerable<string> values, TimeSpan? expiry)
        {
            var redisValues = values.Select(v => (RedisValue)v).ToArray();
            var result = await _db.SetAddAsync(key, redisValues);
            await SetExpiryIfNeeded(key, expiry);
            return result;
        }

        public async Task<List<string>> SMembersAsync(string key)
        {
            var members = await _db.SetMembersAsync(key);
            return members.Select(v => v.ToString()).ToList();
        }

        public async Task<bool> SRemAsync(string key, string value)
            => await _db.SetRemoveAsync(key, value);

        public async Task<bool> SIsMemberAsync(string key, string value)
            => await _db.SetContainsAsync(key, value);

        public async Task<long> SCardAsync(string key)
            => await _db.SetLengthAsync(key);

        // --------------------------
        // 🔹 HASH operations
        // --------------------------
        public async Task<bool> HSetAsync(string key, Dictionary<string, string> fields)
        {
            var entries = fields.Select(kv => new HashEntry(kv.Key, kv.Value)).ToArray();
            await _db.HashSetAsync(key, entries);
            return true;
        }

        public async Task<bool> HSetAsync(string key, Dictionary<string, string> fields, TimeSpan? expiry)
        {
            var entries = fields.Select(kv => new HashEntry(kv.Key, kv.Value)).ToArray();
            await _db.HashSetAsync(key, entries);
            await SetExpiryIfNeeded(key, expiry);
            return true;
        }

        public async Task<bool> HSetAsync(string key, string field, string value, TimeSpan? expiry = null)
        {
            // 1. Thực hiện lệnh HSET (Hash Set) cho một trường duy nhất
            var result = await _db.HashSetAsync(key, field, value);

            // 2. Thiết lập thời gian hết hạn nếu được cung cấp
            if (expiry.HasValue)
            {
                await _db.KeyExpireAsync(key, expiry.Value);
            }

            // StackExchange.Redis HashSetAsync trả về true nếu trường là mới và được thêm vào, 
            // false nếu nó đã tồn tại và được cập nhật. Trả về true để chỉ báo hoạt động thành công.
            return true;
        }

        public async Task<string?> HGetAsync(string key, string field)
            => await _db.HashGetAsync(key, field);

        public async Task<Dictionary<string, string>> HGetAllAsync(string key)
        {
            var entries = await _db.HashGetAllAsync(key);
            return entries.ToDictionary(e => e.Name.ToString(), e => e.Value.ToString());
        }

        public async Task<bool> HDelAsync(string key, string field)
            => await _db.HashDeleteAsync(key, field);

        public async Task<long> HashIncrementAsync(string key, string field, long increment)
            => await _db.HashIncrementAsync(key, field, increment);

        // --------------------------
        // 🔹 ZSET (Sorted Set) operations
        // --------------------------

        // ✅ Thêm 1 phần tử vào ZSet (với điểm score)
        public async Task<bool> ZAddAsync(string key, string member, double score)
            => await _db.SortedSetAddAsync(key, member, score);

        public async Task<bool> ZAddAsync(string key, string member, double score, TimeSpan? expiry)
        {
            var result = await _db.SortedSetAddAsync(key, member, score);
            await SetExpiryIfNeeded(key, expiry);
            return result;
        }

        // ✅ Thêm nhiều phần tử 1 lúc
        public async Task<long> ZAddRangeAsync(string key, IEnumerable<(string member, double score)> items)
        {
            var entries = items.Select(i => new SortedSetEntry(i.member, i.score)).ToArray();
            return await _db.SortedSetAddAsync(key, entries);
        }

        public async Task<long> ZAddRangeAsync(string key, IEnumerable<(string member, double score)> items, TimeSpan? expiry)
        {
            var entries = items.Select(i => new SortedSetEntry(i.member, i.score)).ToArray();
            var result = await _db.SortedSetAddAsync(key, entries);
            await SetExpiryIfNeeded(key, expiry);
            return result;
        }

        public async Task<long> ZRemRangeByRankAsync(string key, long startRank, long stopRank)
            => await _db.SortedSetRemoveRangeByRankAsync(key, startRank, stopRank);

        // ✅ Lấy danh sách phần tử theo thứ tự tăng dần
        public async Task<List<string>> ZRangeAsync(string key, long start = 0, long stop = -1)
        {
            var results = await _db.SortedSetRangeByRankAsync(key, start, stop, Order.Ascending);
            return results.Select(v => v.ToString()).ToList();
        }

        // ✅ Lấy danh sách phần tử theo thứ tự giảm dần (ví dụ bảng xếp hạng)
        public async Task<List<string>> ZRevRangeAsync(string key, long start = 0, long stop = -1)
        {
            var results = await _db.SortedSetRangeByRankAsync(key, start, stop, Order.Descending);
            return results.Select(v => v.ToString()).ToList();
        }

        // ✅ Lấy cả member + score (ascending)
        public async Task<List<(string member, double score)>> ZRangeWithScoresAsync(string key, long start = 0, long stop = -1)
        {
            var entries = await _db.SortedSetRangeByRankWithScoresAsync(key, start, stop, Order.Ascending);
            return entries.Select(e => (e.Element.ToString(), e.Score)).ToList();
        }

        // ✅ Lấy cả member + score (descending)
        public async Task<List<(string member, double score)>> ZRevRangeWithScoresAsync(string key, long start = 0, long stop = -1)
        {
            var entries = await _db.SortedSetRangeByRankWithScoresAsync(key, start, stop, Order.Descending);
            return entries.Select(e => (e.Element.ToString(), e.Score)).ToList();
        }

        // ✅ Lấy top N (cao nhất)
        public async Task<List<(string member, double score)>> ZTopNAsync(string key, int topN)
            => await ZRevRangeWithScoresAsync(key, 0, topN - 1);

        // ✅ Xoá phần tử
        public async Task<bool> ZRemAsync(string key, string member)
            => await _db.SortedSetRemoveAsync(key, member);

        // ✅ Lấy điểm (score) của 1 phần tử
        public async Task<double?> ZScoreAsync(string key, string member)
            => await _db.SortedSetScoreAsync(key, member);

        // ✅ Tăng điểm (ví dụ điểm quiz)
        public async Task<double> ZIncrByAsync(string key, string member, double increment)
            => await _db.SortedSetIncrementAsync(key, member, increment);

        // ✅ Lấy rank (thứ hạng)
        public async Task<long?> ZRankAsync(string key, string member, bool descending = true)
            => descending
                ? await _db.SortedSetRankAsync(key, member, Order.Descending)
                : await _db.SortedSetRankAsync(key, member, Order.Ascending);

        // ✅ Đếm số lượng phần tử trong khoảng điểm
        public async Task<long> ZCountByScoreAsync(string key, double minScore, double maxScore)
            => await _db.SortedSetLengthByValueAsync(key, minScore, maxScore);
    }
}
