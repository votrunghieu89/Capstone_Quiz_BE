using Capstone.DTOs;
using Capstone.Model;
using Capstone.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Capstone.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuditController : ControllerBase
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly ILogger<AuditController> _logger;

        public AuditController(IAuditLogRepository auditLogRepository, ILogger<AuditController> logger)
        {
            _auditLogRepository = auditLogRepository;
            _logger = logger;
        }

        // ===== GET METHODS =====

        /// <summary>
        /// Check connection to MongoDB
        /// </summary>
        [HttpGet("check-connection")]
        public async Task<IActionResult> CheckConnection()
        {
            try
            {
                bool isConnect = await _auditLogRepository.CheckConnection();

                if (isConnect)
                {
                    _logger.LogInformation("MongoDB connection successful");
                    return Ok(new { message = "Connected to MongoDB", isConnected = true });
                }
                else
                {
                    _logger.LogWarning("Failed to connect to MongoDB");
                    return StatusCode(503, new { message = "Cannot connect to MongoDB", isConnected = false });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking MongoDB connection");
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ" });
            }
        }

        /// <summary>
        /// Get all audit logs with pagination
        /// </summary>
        [HttpGet("audit-logs")]
        public async Task<IActionResult> GetAllAuditLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                if (page <= 0 || pageSize <= 0)
                {
                    _logger.LogWarning("Invalid pagination parameters: Page={Page}, PageSize={PageSize}", page, pageSize);
                    return BadRequest(new { message = "Page and PageSize must be greater than 0" });
                }

                if (pageSize > 100)
                {
                    _logger.LogWarning("PageSize too large: PageSize={PageSize}", pageSize);
                    return BadRequest(new { message = "PageSize cannot exceed 100" });
                }

                List<AuditLogModel> logs = await _auditLogRepository.GetAllLog(page, pageSize);
                if (logs != null)
                {

                    return Ok(logs);
                }
                else
                {
                    return BadRequest(new { message = "Not Found Logs" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs: Page={Page}, PageSize={PageSize}", page, pageSize);
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ" });
            }
        }

        /// <summary>
        /// Filter audit logs by AccountId, From Date, To Date with pagination
        /// </summary>
        [HttpGet("audit-logs/filter")]
        public async Task<IActionResult> FilterAuditLogs(
            [FromQuery] int? accountId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                // Validation
                if (page <= 0 || pageSize <= 0)
                {
                    _logger.LogWarning("Invalid pagination parameters: Page={Page}, PageSize={PageSize}", page, pageSize);
                    return BadRequest(new { message = "Page and PageSize must be greater than 0" });
                }

                if (pageSize > 100)
                {
                    _logger.LogWarning("PageSize too large: PageSize={PageSize}", pageSize);
                    return BadRequest(new { message = "PageSize cannot exceed 100" });
                }

                if (accountId.HasValue && accountId.Value <= 0)
                {
                    _logger.LogWarning("Invalid AccountId: AccountId={AccountId}", accountId);
                    return BadRequest(new { message = "AccountId must be greater than 0" });
                }

                if (from.HasValue && to.HasValue && from.Value > to.Value)
                {
                    _logger.LogWarning("Invalid date range: From={From}, To={To}", from, to);
                    return BadRequest(new { message = "From date cannot be later than To date" });
                }

                // Call service
                var logs = await _auditLogRepository.FilterIntegration(accountId, from, to, page, pageSize);

                // Map to DTO
                var response = logs.Select(log => new ViewingAuditLogDTO
                {
                    AccountId = log.AccountId,
                    Action = log.Action,
                    Description = log.Description,
                    CreatAt = log.CreatAt,
                    IpAddress = log.IpAddress
                }).ToList();

                _logger.LogInformation(
                    "Filtered audit logs: AccountId={AccountId}, From={From}, To={To}, Page={Page}, PageSize={PageSize}, Count={Count}",
                    accountId, from, to, page, pageSize, response.Count);

                return Ok(new
                {
                    filters = new
                    {
                        accountId,
                        from = from?.ToString("yyyy-MM-dd"),
                        to = to?.ToString("yyyy-MM-dd")
                    },
                    page,
                    pageSize,
                    totalRecords = response.Count,
                    data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error filtering audit logs: AccountId={AccountId}, From={From}, To={To}, Page={Page}, PageSize={PageSize}",
                    accountId, from, to, page, pageSize);
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ" });
            }
        }

        /// <summary>
        /// Get audit logs by specific AccountId
        /// </summary>
        [HttpGet("audit-logs/account/{accountId:int}")]
        public async Task<IActionResult> GetAuditLogsByAccountId(
            int accountId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (accountId <= 0)
                {
                    _logger.LogWarning("Invalid AccountId: AccountId={AccountId}", accountId);
                    return BadRequest(new { message = "AccountId must be greater than 0" });
                }

                if (page <= 0 || pageSize <= 0)
                {
                    _logger.LogWarning("Invalid pagination parameters: Page={Page}, PageSize={PageSize}", page, pageSize);
                    return BadRequest(new { message = "Page and PageSize must be greater than 0" });
                }

                if (pageSize > 100)
                {
                    _logger.LogWarning("PageSize too large: PageSize={PageSize}", pageSize);
                    return BadRequest(new { message = "PageSize cannot exceed 100" });
                }

                // Use FilterIntegration with only accountId
                var logs = await _auditLogRepository.FilterIntegration(accountId, null, null, page, pageSize);

                var response = logs.Select(log => new ViewingAuditLogDTO
                {
                    AccountId = log.AccountId,
                    Action = log.Action,
                    Description = log.Description,
                    CreatAt = log.CreatAt,
                    IpAddress = log.IpAddress
                }).ToList();

                _logger.LogInformation("Retrieved audit logs by AccountId: AccountId={AccountId}, Page={Page}, PageSize={PageSize}, Count={Count}",
                    accountId, page, pageSize, response.Count);

                return Ok(new
                {
                    accountId,
                    page,
                    pageSize,
                    totalRecords = response.Count,
                    data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs by AccountId: AccountId={AccountId}, Page={Page}, PageSize={PageSize}",
                    accountId, page, pageSize);
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ" });
            }
        }

        /// <summary>
        /// Get audit logs within a date range
        /// </summary>
        [HttpGet("audit-logs/date-range")]
        public async Task<IActionResult> GetAuditLogsByDateRange(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (!from.HasValue && !to.HasValue)
                {
                    _logger.LogWarning("No date parameters provided");
                    return BadRequest(new { message = "At least one date parameter (from or to) is required" });
                }

                if (from.HasValue && to.HasValue && from.Value > to.Value)
                {
                    _logger.LogWarning("Invalid date range: From={From}, To={To}", from, to);
                    return BadRequest(new { message = "From date cannot be later than To date" });
                }

                if (page <= 0 || pageSize <= 0)
                {
                    _logger.LogWarning("Invalid pagination parameters: Page={Page}, PageSize={PageSize}", page, pageSize);
                    return BadRequest(new { message = "Page and PageSize must be greater than 0" });
                }

                if (pageSize > 100)
                {
                    _logger.LogWarning("PageSize too large: PageSize={PageSize}", pageSize);
                    return BadRequest(new { message = "PageSize cannot exceed 100" });
                }

                // Use FilterIntegration with only date range
                var logs = await _auditLogRepository.FilterIntegration(null, from, to, page, pageSize);

                var response = logs.Select(log => new ViewingAuditLogDTO
                {
                    AccountId = log.AccountId,
                    Action = log.Action,
                    Description = log.Description,
                    CreatAt = log.CreatAt,
                    IpAddress = log.IpAddress
                }).ToList();

                _logger.LogInformation("Retrieved audit logs by date range: From={From}, To={To}, Page={Page}, PageSize={PageSize}, Count={Count}",
                    from, to, page, pageSize, response.Count);

                return Ok(new
                {
                    dateRange = new
                    {
                        from = from?.ToString("yyyy-MM-dd"),
                        to = to?.ToString("yyyy-MM-dd")
                    },
                    page,
                    pageSize,
                    totalRecords = response.Count,
                    data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs by date range: From={From}, To={To}, Page={Page}, PageSize={PageSize}",
                    from, to, page, pageSize);
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ" });
            }
        }

        // ===== POST METHODS =====

        /// <summary>
        /// Insert a new audit log manually (for testing or manual entries)
        /// </summary>
        [HttpPost("audit-logs")]
        public async Task<IActionResult> InsertAuditLog([FromBody] InsertAuditLogDTO auditLogDto)
        {
            try
            {
                // Validation
                if (auditLogDto == null)
                {
                    _logger.LogWarning("Audit log data is null");
                    return BadRequest(new { message = "Audit log data is required" });
                }

                if (auditLogDto.AccountId <= 0)
                {
                    _logger.LogWarning("Invalid AccountId: AccountId={AccountId}", auditLogDto.AccountId);
                    return BadRequest(new { message = "AccountId must be greater than 0" });
                }

                if (string.IsNullOrWhiteSpace(auditLogDto.Action))
                {
                    _logger.LogWarning("Action is required but was empty");
                    return BadRequest(new { message = "Action is required" });
                }

                if (auditLogDto.Action.Length > 100)
                {
                    _logger.LogWarning("Action too long: Length={Length}", auditLogDto.Action.Length);
                    return BadRequest(new { message = "Action cannot exceed 100 characters" });
                }

                if (string.IsNullOrWhiteSpace(auditLogDto.Description))
                {
                    _logger.LogWarning("Description is required but was empty");
                    return BadRequest(new { message = "Description is required" });
                }

                if (auditLogDto.Description.Length > 500)
                {
                    _logger.LogWarning("Description too long: Length={Length}", auditLogDto.Description.Length);
                    return BadRequest(new { message = "Description cannot exceed 500 characters" });
                }

                // Get IP Address from request if not provided
                string ipAddress = auditLogDto.IpAddress;
                if (string.IsNullOrWhiteSpace(ipAddress))
                {
                    ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                }
                Console.WriteLine("ip" +  ipAddress);
                // Create AuditLogModel
                var auditLog = new AuditLogModel
                {
                    AccountId = auditLogDto.AccountId,
                    Action = auditLogDto.Action,
                    Description = auditLogDto.Description,
                    CreatAt = DateTime.Now,
                    IpAddress = ipAddress
                };
                Console.WriteLine("step 1");
                Console.WriteLine(auditLog);
                // Insert to MongoDB
                bool success = await _auditLogRepository.InsertLog(auditLog);
                Console.WriteLine("step 2");
                if (success)
                {
                    _logger.LogInformation(
                        "Successfully inserted audit log: AccountId={AccountId}, Action={Action}, IpAddress={IpAddress}",
                        auditLog.AccountId, auditLog.Action, auditLog.IpAddress);
                    Console.WriteLine("step 3");
                    return Ok(new
                    {
                        message = "Audit log inserted successfully",
                        data = new ViewingAuditLogDTO
                        {
                            AccountId = auditLog.AccountId,
                            Action = auditLog.Action,
                            Description = auditLog.Description,
                            CreatAt  = auditLog.CreatAt,
                            IpAddress = auditLog.IpAddress
                        }
                    });
                }
                else
                {
                    _logger.LogError(
                        "Failed to insert audit log: AccountId={AccountId}, Action={Action}",
                        auditLog.AccountId, auditLog.Action);

                    return StatusCode(500, new { message = "Failed to insert audit log" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error inserting audit log: AccountId={AccountId}, Action={Action}",
                    auditLogDto?.AccountId, auditLogDto?.Action);
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ" });
            }
        }
    }
}