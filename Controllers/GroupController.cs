using Capstone.Database;
using Capstone.DTOs.Group;
using Capstone.Model;
using Capstone.Repositories.Groups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Capstone.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GroupController : ControllerBase
    {
        private readonly ILogger<GroupController> _logger;
        private readonly IGroupRepository _groupRepository;
        private readonly IRedis _redis;
        private readonly IConfiguration _configuration;

        public GroupController(ILogger<GroupController> logger, IGroupRepository groupRepository, IRedis redis, IConfiguration configuration)
        {
            _logger = logger;
            _groupRepository = groupRepository;
            _redis = redis;
            _configuration = configuration;
        }

        // ===== GET METHODS =====
        [HttpGet("GetGroupByTeacherId/{teacherId}")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> GetGroupsByTeacherId(int teacherId)
        {
            _logger.LogInformation("GetGroupByTeacherId: Start - TeacherId={TeacherId}", teacherId);
            try
            {
                var groups = await _groupRepository.GetAllGroupsbyTeacherId(teacherId);
                _logger.LogInformation("GetGroupByTeacherId: Retrieved {Count} groups for TeacherId={TeacherId}", groups?.Count ?? 0, teacherId);
                return Ok(groups);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetGroupByTeacherId: Error retrieving groups for TeacherId={TeacherId}", teacherId);
                return StatusCode(500, "Lỗi máy chủ nội bộ");
            }
        }

        [HttpGet("GetAllStudentsByGroupId/{groupId}")]
        [Authorize(Roles = "Teacher,Student")]
        public async Task<IActionResult> GetAllStudentsByGroup(int groupId)
        {
            _logger.LogInformation("GetAllStudentsByGroupId: Start - GroupId={GroupId}", groupId);
            try
            {
                List<ViewStudentDTO> students = await _groupRepository.GetAllStudentsByGroupId(groupId);
                _logger.LogInformation("GetAllStudentsByGroupId: Retrieved {Count} students for GroupId={GroupId}", students?.Count ?? 0, groupId);
                return Ok(students);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllStudentsByGroupId: Error retrieving students for GroupId={GroupId}", groupId);
                return StatusCode(500, "Lỗi máy chủ nội bộ");
            }
        }

        [HttpGet("GetAllGroupsByStudentId/{studentId}")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetGroupsByStudentId(int studentId)
        {
            _logger.LogInformation("GetAllGroupsByStudentId: Start - StudentId={StudentId}", studentId);
            try
            {
                List<AllGroupDTO> groups = await _groupRepository.GetAllGroupsByStudentId(studentId);
                _logger.LogInformation("GetAllGroupsByStudentId: Retrieved {Count} groups for StudentId={StudentId}", groups?.Count ?? 0, studentId);
                return Ok(groups);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllGroupsByStudentId: Error retrieving groups for StudentId={StudentId}", studentId);
                return StatusCode(500, "Lỗi máy chủ nội bộ");
            }
        }

        [HttpGet("GetGroupDetail/{groupId}")]
        [Authorize(Roles = "Teacher,Student")]
        public async Task<IActionResult> GetGroupDetail(int groupId)
        {
            _logger.LogInformation("getGroupDetail: Start - GroupId={GroupId}", groupId);
            try
            {
                GroupModel group = await _groupRepository.GetGroupDetailById(groupId);
                if (group == null)
                {
                    _logger.LogWarning("getGroupDetail: Group not found - GroupId={GroupId}", groupId);
                    return NotFound(new { message = "Không tìm thấy nhóm" });
                }

                List<ViewQuizDTO> quizzes = await _groupRepository.GetAllDeliveredQuizzesByGroupId(groupId);
                foreach (var quiz in quizzes)
                {
                    quiz.DeliveredQuiz.AvatarURL = $"{Request.Scheme}://{Request.Host}/{quiz.DeliveredQuiz.AvatarURL.Replace("\\", "/")}";
                }
                var FronendURL = _configuration["Frontend:BaseUrl"];
                var urlInvite = $"{FronendURL}/join-group/{group.IdUnique}";
                var newObject = new
                {

                    group.GroupId,
                    group.TeacherId,
                    group.GroupName,
                    group.GroupDescription,
                    group.IdUnique,
                    group.CreateAt,
                    Quizzes = quizzes
                };
                
                _logger.LogInformation("getGroupDetail: Success - GroupId={GroupId}, QuizCount={QuizCount}", groupId, quizzes?.Count ?? 0);
                return Ok(newObject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "getGroupDetail: Error retrieving group details - GroupId={GroupId}", groupId);
                return StatusCode(500, "Lỗi máy chủ nội bộ");
            }
        }

        // ===== POST METHODS =====
        [HttpPost("createGroup")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupDTO request)
        {
            _logger.LogInformation("createGroup: Start - TeacherId={TeacherId}, GroupName={GroupName}", request?.TeacherId, request?.GroupName);
            try
            {
                if (request == null)
                {
                    _logger.LogWarning("createGroup: Request body null");
                    return BadRequest(new { message = "Yêu cầu phải có dữ liệu đầu vào." });
                }
                int length = 10;
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                var random = new Random();
                string inviteCode = new string(
                    Enumerable.Repeat(chars, length)
                        .Select(s => s[random.Next(s.Length)])
                        .ToArray()
                );
                GroupModel newGroup = new GroupModel
                {
                    TeacherId = request.TeacherId,
                    GroupName = request.GroupName,
                    GroupDescription = request.GroupDescription,
                    IdUnique = inviteCode,
                    CreateAt = DateTime.Now
                };
                var accountId = Convert.ToInt32(User.FindFirst("AccountId")?.Value);
                var ipAddess = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? HttpContext.Connection.RemoteIpAddress?.ToString();
                var groupId = await _groupRepository.CreateGroup(newGroup,ipAddess);
                if (groupId == null)
                {
                    _logger.LogWarning("createGroup: Repository returned null for TeacherId={TeacherId}", request.TeacherId);
                    return StatusCode(500, "Tạo nhóm thất bại");
                }
                _logger.LogInformation("createGroup: Success - GroupId={GroupId}", groupId.GroupId);
                return Ok(groupId );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "createGroup: Error creating group for TeacherId={TeacherId}", request?.TeacherId);
                return StatusCode(500, "Lỗi máy chủ nội bộ");
            }
        }

        [HttpPost("insert-student")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> InsertStudentToGroup(int groupId, string IdUnique)
        {
           
            try
            {
                var accountId = Convert.ToInt32(User.FindFirst("AccountId")?.Value);
                var ipAddess = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? HttpContext.Connection.RemoteIpAddress?.ToString();
                var result = await _groupRepository.InsertStudentToGroup(groupId, IdUnique,accountId,ipAddess);

                switch (result)
                {
                    case ENUMs.GroupEnum.JoinGroupResult.Success:
                        _logger.LogInformation("insertStudentToGroup: Success - GroupId={GroupId}, IdUnique={IdUnique}", groupId, IdUnique);
                        return Ok(new { message = "Thêm học viên vào nhóm thành công" });

                    case ENUMs.GroupEnum.JoinGroupResult.AlreadyInGroup:
                        _logger.LogInformation("insertStudentToGroup: Student already in group - GroupId={GroupId}, IdUnique={IdUnique}", groupId, IdUnique);
                        return BadRequest(new { message = "Học viên đã có trong nhóm" });

                    case ENUMs.GroupEnum.JoinGroupResult.Fail:
                        _logger.LogWarning("insertStudentToGroup: Group not found - GroupId={GroupId}", groupId);
                        return NotFound(new { message = "Không tìm thấy nhóm" });

                    case ENUMs.GroupEnum.JoinGroupResult.Error:
                        _logger.LogError("insertStudentToGroup: Repository error - GroupId={GroupId}, IdUnique={IdUnique}", groupId, IdUnique);
                        return StatusCode(500, new { message = "Lỗi khi thêm học viên vào nhóm" });

                    default:
                        _logger.LogError("insertStudentToGroup: Unknown result - GroupId={GroupId}, IdUnique={IdUnique}, Result={Result}", groupId, IdUnique, result);
                        return StatusCode(500, new { message = "Lỗi không xác định" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "insertStudentToGroup: Unexpected error - GroupId={GroupId}, IdUnique={IdUnique}", groupId, IdUnique);
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ" });
            }
        }

        [HttpPost("InsertQuizToGroup")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> InsertQuizToGroup([FromBody] InsertQuiz request)
        {
            _logger.LogInformation("insertQuizToGroup: Start - QuizId={QuizId}, GroupId={GroupId}", request?.QuizId, request?.GroupId);
            try
            {
                if (request == null)
                {
                    _logger.LogWarning("insertQuizToGroup: Request body null");
                    return BadRequest(new { message = "Yêu cầu phải có dữ liệu đầu vào." });
                }
                var accountId = Convert.ToInt32(User.FindFirst("AccountId")?.Value);
                var ipAddess = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? HttpContext.Connection.RemoteIpAddress?.ToString();
                var result = await _groupRepository.InsertQuizToGroup(request,accountId,ipAddess);
                if (result == null)
                {
                    _logger.LogWarning("insertQuizToGroup: Repository returned null - QuizId={QuizId}, GroupId={GroupId}", request.QuizId, request.GroupId);
                    return StatusCode(500, "Thêm bài kiểm tra vào nhóm thất bại");
                }
                _logger.LogInformation("insertQuizToGroup: Success - QuizId={QuizId}, GroupId={GroupId}", request.QuizId, request.GroupId);
                return StatusCode(200, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "insertQuizToGroup: Error inserting quiz to group - QuizId={QuizId}, GroupId={GroupId}", request?.QuizId, request?.GroupId);
                return StatusCode(500, "Lỗi máy chủ nội bộ");
            }
        }

        [HttpPost("JoinGroupByInvite/{IdUnique}/{studentId}")]
        [Authorize(Roles = "Teacher,Student")]
        public async Task<IActionResult> JoinGroupByInvite(string IdUnique, int studentId)
        {
            _logger.LogInformation("joinGroupByInvite: Start - InviteCode={InviteCode}, StudentId={StudentId}", IdUnique, studentId);
            try
            {
                var accountId = Convert.ToInt32(User.FindFirst("AccountId")?.Value);
                var ipAddess = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? HttpContext.Connection.RemoteIpAddress?.ToString();
                var result = await _groupRepository.JoinGroupByInvite(IdUnique, studentId,ipAddess);
                switch (result)
                {
                    case ENUMs.GroupEnum.JoinGroupResult.Success:
                        _logger.LogInformation("joinGroupByInvite: Success - InviteCode={InviteCode}, StudentId={StudentId}", IdUnique, studentId);
                        return Ok(new { message = "Tham gia nhóm thành công" });
                    case ENUMs.GroupEnum.JoinGroupResult.AlreadyInGroup:
                        _logger.LogInformation("joinGroupByInvite: Student already in group - InviteCode={InviteCode}, StudentId={StudentId}", IdUnique, studentId);
                        return BadRequest(new { message = "Học viên đã có trong nhóm" });
                    case ENUMs.GroupEnum.JoinGroupResult.Fail:
                        _logger.LogWarning("joinGroupByInvite: Invalid invite code - InviteCode={InviteCode}", IdUnique);
                        return NotFound(new { message = "Mã mời không hợp lệ" });
                    case ENUMs.GroupEnum.JoinGroupResult.Error:
                        _logger.LogError("joinGroupByInvite: Repository error - InviteCode={InviteCode}, StudentId={StudentId}", IdUnique, studentId);
                        return StatusCode(500, new { message = "Lỗi khi tham gia nhóm" });
                    default:
                        _logger.LogError("joinGroupByInvite: Unknown result - InviteCode={InviteCode}, StudentId={StudentId}, Result={Result}", IdUnique, studentId, result);
                        return StatusCode(500, new { message = "Lỗi không xác định" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "joinGroupByInvite: Unexpected error - InviteCode={InviteCode}, StudentId={StudentId}", IdUnique, studentId);
                return StatusCode(500, new { message = "Lỗi máy chủ nội bộ" });
            }
        }

        // ===== PUT METHODS =====
        [HttpPut("updateGroup")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> UpdateGroup([FromBody] UpdateGroupDTO request)
        {
            _logger.LogInformation("updateGroup: Start - GroupId={GroupId}, GroupName={GroupName}", request?.GroupId, request?.GroupName);
            try
            {
                if (request == null)
                {
                    _logger.LogWarning("updateGroup: Request body null");
                    return BadRequest(new { message = "Yêu cầu phải có dữ liệu đầu vào." });
                }
                var accountId = Convert.ToInt32(User.FindFirst("AccountId")?.Value);
                var ipAddess = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? HttpContext.Connection.RemoteIpAddress?.ToString();
                var updatedGroup = await _groupRepository.updateGroup(request,accountId,ipAddess);
                if (updatedGroup == null)
                {
                    _logger.LogWarning("updateGroup: Repository returned null for GroupId={GroupId}", request.GroupId);
                    return NotFound(new { message = "Không tìm thấy nhóm" });
                }
                _logger.LogInformation("updateGroup: Success - GroupId={GroupId}", request.GroupId);
                return Ok(updatedGroup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "updateGroup: Error updating group - GroupId={GroupId}", request?.GroupId);
                return StatusCode(500, "Lỗi máy chủ nội bộ");
            }
        }

        // ===== DELETE METHODS =====
        [HttpDelete("deleteGroup/{groupId}")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> DeleteGroup(int groupId)
        {
            _logger.LogInformation("deleteGroup: Start - GroupId={GroupId}", groupId);
            try
            {
                var accountId = Convert.ToInt32(User.FindFirst("AccountId")?.Value);
                var ipAddess = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? HttpContext.Connection.RemoteIpAddress?.ToString();
                var result = await _groupRepository.DeleteGroup(groupId,accountId,ipAddess);
                if (result)
                {
                    _logger.LogInformation("deleteGroup: Success - GroupId={GroupId}", groupId);
                    return Ok(new { Message = "Xóa nhóm thành công" });
                }
                else
                {
                    _logger.LogWarning("deleteGroup: Group not found - GroupId={GroupId}", groupId);
                    return NotFound(new { Message = "Không tìm thấy nhóm" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "deleteGroup: Error deleting group - GroupId={GroupId}", groupId);
                return StatusCode(500, "Lỗi máy chủ nội bộ");
            }
        }

        [HttpDelete("leaveGroup/{groupId}/{studentId}")]
        [Authorize(Roles = "Teacher,Student")]
        public async Task<IActionResult> LeaveGroup(int groupId, int studentId, int teacherId)
        {
            _logger.LogInformation("leaveGroup: Start - GroupId={GroupId}, StudentId={StudentId}", groupId, studentId);
            try
            {
                var accountId = Convert.ToInt32(User.FindFirst("AccountId")?.Value);
                var ipAddess = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? HttpContext.Connection.RemoteIpAddress?.ToString();
                var result = await _groupRepository.LeaveGroup(groupId, studentId, teacherId,ipAddess);
                if (result)
                {
                    _logger.LogInformation("leaveGroup: Success - GroupId={GroupId}, StudentId={StudentId}", groupId, studentId);
                    return Ok(new { Message = "Rời nhóm thành công" });
                }
                else
                {
                    _logger.LogWarning("leaveGroup: Student not found in group - GroupId={GroupId}, StudentId={StudentId}", groupId, studentId);
                    return NotFound(new { Message = "Không tìm thấy nhóm hoặc học viên" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "leaveGroup: Error leaving group - GroupId={GroupId}, StudentId={StudentId}", groupId, studentId);
                return StatusCode(500, "Lỗi máy chủ nội bộ");
            }
        }

        [HttpDelete("removeStudentFromGroup/{groupId}/{studentId}")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> RemoveStudentFromGroup(int groupId, int studentId, int teacherId)
        {
            _logger.LogInformation("removeStudentFromGroup: Start - GroupId={GroupId}, StudentId={StudentId}", groupId, studentId);
            try
            {
                var accountId = Convert.ToInt32(User.FindFirst("AccountId")?.Value);
                var ipAddess = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? HttpContext.Connection.RemoteIpAddress?.ToString();
                var result = await _groupRepository.RemoveStudentFromGroup(groupId, studentId, teacherId,ipAddess);
                if (result)
                {
                    _logger.LogInformation("removeStudentFromGroup: Success - GroupId={GroupId}, StudentId={StudentId}", groupId, studentId);
                    return Ok(new { Message = "Xóa học viên khỏi nhóm thành công" });
                }
                else
                {
                    _logger.LogWarning("removeStudentFromGroup: Student not found in group - GroupId={GroupId}, StudentId={StudentId}", groupId, studentId);
                    return NotFound(new { Message = "Không tìm thấy nhóm hoặc học viên" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "removeStudentFromGroup: Error removing student from group - GroupId={GroupId}, StudentId={StudentId}", groupId, studentId);
                return StatusCode(500, "Lỗi máy chủ nội bộ");
            }
        }

        [HttpDelete("RemoveQuizFromGroup/{QgID}/{groupId}/{quizId}")]
        [Authorize(Roles = "Teacher")]
            public async Task<IActionResult> RemoveQuizFromGroup(int QgID, int groupId, int quizId)
            {
                _logger.LogInformation("RemoveQuizFromGroup: Start - QgID={QgID}", QgID);

                try
                {
                    var accountId = Convert.ToInt32(User.FindFirst("AccountId")?.Value);
                    var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                                    ?? HttpContext.Connection.RemoteIpAddress?.ToString();

                    // Giả sử repository method trả về true/false
                    var result = await _groupRepository.RemoveQuizFromGroup(QgID, groupId, quizId,accountId, ipAddress);

                    if (result)
                    {
                        _logger.LogInformation("RemoveQuizFromGroup: Success - QgID={QgID}", QgID);
                        return Ok(new { Message = "Xóa bài kiểm tra khỏi nhóm thành công" });
                    }
                    else
                    {
                        _logger.LogWarning("RemoveQuizFromGroup: Quiz not found or nothing deleted - QgID={QgID}", QgID);
                        return NotFound(new { Message = "Không tìm thấy nhóm hoặc bài kiểm tra" });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "RemoveQuizFromGroup: Error removing QgID={QgID}", QgID);
                    return StatusCode(500, "Lỗi máy chủ nội bộ");
                }
        }
    }
}