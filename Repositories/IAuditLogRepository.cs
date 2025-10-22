using Capstone.Model;

namespace Capstone.Repositories
{
    public interface IAuditLogRepository
    {
        public Task<bool> CheckConnection();
        public Task<bool> InsertLog(AuditLogModel auditLog);
        public Task<List<AuditLogModel>> GetAllLog(int page, int pageSize, int adminID);
        //public Task<List<AuditLogModel>> SearchLogByAccountId(int accountId, int page, int pageSize);
        //public Task<List<AuditLogModel>> FilterLogByDateTime(DateTime? from, DateTime? to, int page, int pageSize);
        Task<List<AuditLogModel>> FilterIntegration(int? accountId, DateTime? from, DateTime? to, int page, int pageSize);

    }
}
