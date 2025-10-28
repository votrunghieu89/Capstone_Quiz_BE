using System;
using System.Collections.Generic;
using System.Threading.Tasks;

    namespace Capstone.Database
{
    public interface IRedis
    {
        // --------------------------
        // 🔹 STRING operations
        // --------------------------
        Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null);
        Task<string?> GetStringAsync(string key);
        Task<bool> DeleteKeyAsync(string key);
        Task<bool> KeyExistsAsync(string key);
        Task<long> IncrementAsync(string key, long value = 1);
        Task<long> DecrementAsync(string key, long value = 1);
        Task DeleteKeysByPatternAsync(string pattern);

        // --------------------------
        // 🔹 SET operations
        // --------------------------
        Task<bool> SAddAsync(string key, string value);
        Task<bool> SAddAsync(string key, string value, TimeSpan? expiry);
        Task<long> SAddRangeAsync(string key, IEnumerable<string> values);
        Task<long> SAddRangeAsync(string key, IEnumerable<string> values, TimeSpan? expiry);
        Task<List<string>> SMembersAsync(string key);
        Task<bool> SRemAsync(string key, string value);
        Task<bool> SIsMemberAsync(string key, string value);
        Task<long> SCardAsync(string key);

        // --------------------------
        // 🔹 HASH operations
        // --------------------------
        Task<bool> HSetAsync(string key, Dictionary<string, string> fields);
        Task<bool> HSetAsync(string key, Dictionary<string, string> fields, TimeSpan? expiry);
        Task<bool> HSetAsync(string key, string field, string value, TimeSpan? expiry = null);
        Task<string?> HGetAsync(string key, string field);
        Task<Dictionary<string, string>> HGetAllAsync(string key);
        Task<bool> HDelAsync(string key, string field);
        Task<long> HIncrByAsync(string key, string field, long increment);
        Task<long> HashIncrementAsync(string key, string field, long increment);

        // --------------------------
        // 🔹 ZSET (Sorted Set) operations
        // --------------------------
        Task<bool> ZAddAsync(string key, string member, double score);
        Task<bool> ZAddAsync(string key, string member, double score, TimeSpan? expiry);
        Task<long> ZAddRangeAsync(string key, IEnumerable<(string member, double score)> items);
        Task<long> ZAddRangeAsync(string key, IEnumerable<(string member, double score)> items, TimeSpan? expiry);
        Task<long> ZRemRangeByRankAsync(string key, long startRank, long stopRank);
        Task<List<string>> ZRangeAsync(string key, long start = 0, long stop = -1);
        Task<List<string>> ZRevRangeAsync(string key, long start = 0, long stop = -1);
        Task<List<(string member, double score)>> ZRangeWithScoresAsync(string key, long start = 0, long stop = -1);
        Task<List<(string member, double score)>> ZRevRangeWithScoresAsync(string key, long start = 0, long stop = -1);
        Task<List<(string member, double score)>> ZTopNAsync(string key, int topN);
        Task<bool> ZRemAsync(string key, string member);
        Task<double?> ZScoreAsync(string key, string member);
        Task<double> ZIncrByAsync(string key, string member, double increment);
        Task<long?> ZRankAsync(string key, string member, bool descending = true);
        Task<long> ZCountByScoreAsync(string key, double minScore, double maxScore);
    }
}
