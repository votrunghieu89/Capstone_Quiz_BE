using Capstone.Services;
using Microsoft.AspNetCore.Mvc;

namespace Capstone.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConnectionController : ControllerBase
    {
        private readonly ILogger<ConnectionController> _logger;
        private readonly ConnectionService _connection;

        public ConnectionController(ILogger<ConnectionController> logger, ConnectionService connection)
        {
            _logger = logger;
            _connection = connection;
        }

        // ===== GET METHODS =====
        [HttpGet("check-db")]
        public async Task<IActionResult> CheckDbConnection()
        {
            bool isConnected = await _connection.checkConnection();

            if (isConnected)
                return Ok(new { status = "Kết nối tốt", dbConnection = true });
            else
                return StatusCode(503, new { status = "Kết nối thất bại", dbConnection = false });
        }
    }
}
