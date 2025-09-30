using Capstone.Database;

namespace Capstone.Services
{
    public class ConnectionService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ConnectionService> _logger;

        public ConnectionService(AppDbContext context, ILogger<ConnectionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> checkConnection()
        {
            try
            {
                bool isConn = await _context.Database.CanConnectAsync();
                return isConn;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking database connection");
                return false;
            }
        }
    }
}
