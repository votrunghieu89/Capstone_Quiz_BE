using Capstone.DTOs.Notification;
using Capstone.Repositories;
using Microsoft.Data.SqlClient;
namespace Capstone.Services
{
    public class SupportService : ISupportRepository
    {
        private readonly ILogger<SupportService> _logger;
        private readonly string _connectionString;
        public SupportService( ILogger<SupportService> logger, IConfiguration configure)
        {
            _logger = logger;
            _connectionString = configure.GetConnectionString("DefaultConnection") ?? throw new ArgumentNullException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<NotificationEvaluateCVModel> NotificationEvaluateCVModel(int jdId)
        {
            NotificationEvaluateCVModel result = new NotificationEvaluateCVModel();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"SELECT TOP 1 a.AccountId, c.CompanyName, j.JDTitle FROM JDs j
                                     JOIN ProfileCompany c ON j.PCId = c.PCId
                                     JOIN Accounts a ON a.AccountId = c.AccountId WHERE j.JDId = @JDId ";
                    SqlCommand sqlCommand = new SqlCommand(query, connection);
                    sqlCommand.Parameters.AddWithValue("@JDId", jdId);
                    using (var reader = await sqlCommand.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            result.AccountId = reader.GetInt32(reader.GetOrdinal("AccountId"));
                            result.CompanyName = reader.GetString(reader.GetOrdinal("CompanyName"));
                        }
                    }
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in NotificationEvaluateCVModel with jdId: {JdId}", jdId);
                return null;
            }
        }

        public async Task<NotificationSubmitCVModel> NotificationSubmitCVModel(int cvId)
        {
            NotificationSubmitCVModel result = new NotificationSubmitCVModel();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"SELECT TOP 1 a.AccountId, c.FullName FROM CVs j
                                     JOIN ProfileCandidate c ON j.PCAId = c.PCAId
                                     JOIN Accounts a ON a.AccountId = c.AccountId WHERE j.CVId = @CVId ";
                    SqlCommand sqlCommand = new SqlCommand(query, connection);
                    sqlCommand.Parameters.AddWithValue("@CVId", cvId);
                    using (var reader = await sqlCommand.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            result.AccountId = reader.GetInt32(reader.GetOrdinal("AccountId"));
                            result.FullName = reader.GetString(reader.GetOrdinal("FullName"));
                        }
                    }
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in NotificationEvaluateCVModel with jdId: {JdId}", cvId);
                return null;
            }
        }
    }
}
