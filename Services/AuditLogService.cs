using Capstone.Database;
using Capstone.Model;
using Capstone.Repositories;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Capstone.Services
{
    public class AuditLogService : IAuditLogRepository
    {
        private readonly ILogger<AuditLogService> _logger;
        private readonly MongoDbContext _mongoDbContext;

        public AuditLogService(ILogger<AuditLogService> logger, MongoDbContext mongoDbContext)
        {
            _logger = logger;
            _mongoDbContext = mongoDbContext;
        }
        public async Task<bool> CheckConnection()
        {
            try
            {
                var command = new BsonDocument { { "ping", 1 } };
                await _mongoDbContext.AuditLog.Database.RunCommandAsync<BsonDocument>(command);
                return true; 

            }
            catch (MongoConnectionException ex)
            {
                return false;
            }

        }
        public async Task<List<AuditLogModel>> FilterIntegration(int? accountId, DateTime? from, DateTime? to, int page, int pageSize)
        {
            try
            {
                var builder = Builders<AuditLogModel>.Filter; // dùng để tạo các đk cho filter
                var filter = builder.Empty; // ko filter theo đk

                if(accountId.HasValue)
                {
                    filter &= builder.Eq(x => x.AccountId, accountId.Value);
                }
                //if (!string.IsNullOrEmpty(action)) { }
                if (from.HasValue) { 
                
                    filter &= builder.Gte(x => x.Timestamp, from.Value.Date);
                }
                if (to.HasValue) {

                    filter &= builder.Lte(x => x.Timestamp, to.Value.Date.AddDays(1).AddTicks(-1));
                }

                var list = await _mongoDbContext.AuditLog
                    .Find(filter)
                    .SortByDescending(x => x.Timestamp)
                    .Skip((page - 1) * pageSize)             
                    .Limit(pageSize)                       
                    .ToListAsync();
                return list;
            }
            catch (Exception ex) {
                return new List<AuditLogModel>();
            }
        }

        public async Task<List<AuditLogModel>> GetAllLog(int page, int pageSize)
        {
            try
            {

                var logs = await _mongoDbContext.AuditLog
                    .Find(_ => true)                   
                    .SortByDescending(l => l.Timestamp) 
                    .Skip((page - 1) * pageSize) 
                    .Limit(pageSize)
                    .ToListAsync();                     

                return logs;
            }
            catch (Exception ex) { 
                return new List<AuditLogModel> ();
            }
        }

        public async Task<bool> InsertLog(AuditLogModel auditLog)
        {
            try
            {
                await _mongoDbContext.AuditLog.InsertOneAsync(auditLog);
                Console.WriteLine("Service true");
                return true;
            }
            catch(Exception ex)
            {
                Console.WriteLine("Service false");
                return false;
            }
        }
    }
}
