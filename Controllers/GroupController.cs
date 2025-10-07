using Capstone.DTOs.Group;
using Capstone.Model;
using Capstone.Repositories.Groups;
using Microsoft.AspNetCore.Mvc;

namespace Capstone.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GroupController : ControllerBase
    {
        private readonly ILogger<GroupController> _logger;
        private readonly IGroupRepository _groupRepository;
        private readonly Redis _redis;
        private readonly IConfiguration _configuration;

        public GroupController(ILogger<GroupController> logger, IGroupRepository groupRepository, Redis redis, IConfiguration configuration)
        {
            _logger = logger;
            _groupRepository = groupRepository;
            _redis = redis;
            _configuration = configuration;
        }

        // ===== GET METHODS =====
        [HttpGet("GetGroupByTeacherId/{teacherId}")]
        public async Task<IActionResult> GetGroupByTeacherId(int teacherId)
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
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("GetAllStudentsByGroupId/{groupId}")]
        public async Task<IActionResult> GetAllStudentsByGroupId(int groupId)
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
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("GetAllGroupsByStudentId/{studentId}")]
        public async Task<IActionResult> GetAllGroupsByStudentId(int studentId)
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
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("GetGroupDetail/{groupId}")]
        public async Task<IActionResult> GetGroupDetail(int groupId)
        {
            _logger.LogInformation("getGroupDetail: Start - GroupId={GroupId}", groupId);
            try
            {
                GroupModel group = await _groupRepository.GetGroupDetailById(groupId);
                if (group == null)
                {
                    _logger.LogWarning("getGroupDetail: Group not found - GroupId={GroupId}", groupId);
                    return NotFound(new { message = "Group not found" });
                }

                List<ViewQuizDTO> quizzes = await _groupRepository.GetAllDeliveredQuizzesByGroupId(groupId);
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
                return StatusCode(500, "Internal server error");
            }
        }

        // ===== POST METHODS =====
        [HttpPost("createGroup")]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupDTO request)
        {
            _logger.LogInformation("createGroup: Start - TeacherId={TeacherId}, GroupName={GroupName}", request?.TeacherId, request?.GroupName);
            try
            {
                if (request == null)
                {
                    _logger.LogWarning("createGroup: Request body null");
                    return BadRequest(new { message = "Request body is required." });
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
                var groupId = await _groupRepository.CreateGroup(newGroup);
                if (groupId == null)
                {
                    _logger.LogWarning("createGroup: Repository returned null for TeacherId={TeacherId}", request.TeacherId);
                    return StatusCode(500, "Failed to create group");
                }
                _logger.LogInformation("createGroup: Success - GroupId={GroupId}", groupId.GroupId);
                return Ok(groupId );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "createGroup: Error creating group for TeacherId={TeacherId}", request?.TeacherId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("insert-student")]
        public async Task<IActionResult> InsertStudentToGroup(int groupId, string IdUnique)
        {
           
            try
            {
                var result = await _groupRepository.InsertStudentToGroup(groupId, IdUnique);

                switch (result)
                {
                    case ENUMs.GroupEnum.JoinGroupResult.Success:
                        _logger.LogInformation("insertStudentToGroup: Success - GroupId={GroupId}, IdUnique={IdUnique}", groupId, IdUnique);
                        return Ok(new { message = "Student added to group successfully" });

                    case ENUMs.GroupEnum.JoinGroupResult.AlreadyInGroup:
                        _logger.LogInformation("insertStudentToGroup: Student already in group - GroupId={GroupId}, IdUnique={IdUnique}", groupId, IdUnique);
                        return BadRequest(new { message = "Student is already in the group" });

                    case ENUMs.GroupEnum.JoinGroupResult.Fail:
                        _logger.LogWarning("insertStudentToGroup: Group not found - GroupId={GroupId}", groupId);
                        return NotFound(new { message = "Group not found" });

                    case ENUMs.GroupEnum.JoinGroupResult.Error:
                        _logger.LogError("insertStudentToGroup: Repository error - GroupId={GroupId}, IdUnique={IdUnique}", groupId, IdUnique);
                        return StatusCode(500, new { message = "Error adding student to group" });

                    default:
                        _logger.LogError("insertStudentToGroup: Unknown result - GroupId={GroupId}, IdUnique={IdUnique}, Result={Result}", groupId, IdUnique, result);
                        return StatusCode(500, new { message = "Unknown error" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "insertStudentToGroup: Unexpected error - GroupId={GroupId}, IdUnique={IdUnique}", groupId, IdUnique);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("InsertQuizToGroup")]
        public async Task<IActionResult> InsertQuizToGroup([FromBody] InsertQuiz request)
        {
            _logger.LogInformation("insertQuizToGroup: Start - QuizId={QuizId}, GroupId={GroupId}", request?.QuizId, request?.GroupId);
            try
            {
                if (request == null)
                {
                    _logger.LogWarning("insertQuizToGroup: Request body null");
                    return BadRequest(new { message = "Request body is required." });
                }

                var result = await _groupRepository.InsertQuizToGroup(request);
                if (result == null)
                {
                    _logger.LogWarning("insertQuizToGroup: Repository returned null - QuizId={QuizId}, GroupId={GroupId}", request.QuizId, request.GroupId);
                    return StatusCode(500, "Failed to insert quiz to group");
                }
                _logger.LogInformation("insertQuizToGroup: Success - QuizId={QuizId}, GroupId={GroupId}", request.QuizId, request.GroupId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "insertQuizToGroup: Error inserting quiz to group - QuizId={QuizId}, GroupId={GroupId}", request?.QuizId, request?.GroupId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("JoinGroupByInvite/{IdUnique}/{studentId}")]
        public async Task<IActionResult> JoinGroupByInvite(string IdUnique, int studentId)
        {
            _logger.LogInformation("joinGroupByInvite: Start - InviteCode={InviteCode}, StudentId={StudentId}", IdUnique, studentId);
            try
            {
                var result = await _groupRepository.JoinGroupByInvite(IdUnique, studentId);
                switch (result)
                {
                    case ENUMs.GroupEnum.JoinGroupResult.Success:
                        _logger.LogInformation("joinGroupByInvite: Success - InviteCode={InviteCode}, StudentId={StudentId}", IdUnique, studentId);
                        return Ok(new { message = "Joined group successfully" });
                    case ENUMs.GroupEnum.JoinGroupResult.AlreadyInGroup:
                        _logger.LogInformation("joinGroupByInvite: Student already in group - InviteCode={InviteCode}, StudentId={StudentId}", IdUnique, studentId);
                        return BadRequest(new { message = "Student is already in the group" });
                    case ENUMs.GroupEnum.JoinGroupResult.Fail:
                        _logger.LogWarning("joinGroupByInvite: Invalid invite code - InviteCode={InviteCode}", IdUnique);
                        return NotFound(new { message = "Invalid invite code" });
                    case ENUMs.GroupEnum.JoinGroupResult.Error:
                        _logger.LogError("joinGroupByInvite: Repository error - InviteCode={InviteCode}, StudentId={StudentId}", IdUnique, studentId);
                        return StatusCode(500, new { message = "Error joining group" });
                    default:
                        _logger.LogError("joinGroupByInvite: Unknown result - InviteCode={InviteCode}, StudentId={StudentId}, Result={Result}", IdUnique, studentId, result);
                        return StatusCode(500, new { message = "Unknown error" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "joinGroupByInvite: Unexpected error - InviteCode={InviteCode}, StudentId={StudentId}", IdUnique, studentId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // ===== PUT METHODS =====
        [HttpPut("updateGroup")]
        public async Task<IActionResult> UpdateGroup([FromBody] UpdateGroupDTO request)
        {
            _logger.LogInformation("updateGroup: Start - GroupId={GroupId}, GroupName={GroupName}", request?.GroupId, request?.GroupName);
            try
            {
                if (request == null)
                {
                    _logger.LogWarning("updateGroup: Request body null");
                    return BadRequest(new { message = "Request body is required." });
                }

                var updatedGroup = await _groupRepository.updateGroup(request);
                if (updatedGroup == null)
                {
                    _logger.LogWarning("updateGroup: Repository returned null for GroupId={GroupId}", request.GroupId);
                    return NotFound(new { message = "Group not found" });
                }
                _logger.LogInformation("updateGroup: Success - GroupId={GroupId}", request.GroupId);
                return Ok(updatedGroup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "updateGroup: Error updating group - GroupId={GroupId}", request?.GroupId);
                return StatusCode(500, "Internal server error");
            }
        }

        // ===== DELETE METHODS =====
        [HttpDelete("deleteGroup/{groupId}")]
        public async Task<IActionResult> DeleteGroup(int groupId)
        {
            _logger.LogInformation("deleteGroup: Start - GroupId={GroupId}", groupId);
            try
            {
                var result = await _groupRepository.DeleteGroup(groupId);
                if (result)
                {
                    _logger.LogInformation("deleteGroup: Success - GroupId={GroupId}", groupId);
                    return Ok(new { Message = "Group deleted successfully" });
                }
                else
                {
                    _logger.LogWarning("deleteGroup: Group not found - GroupId={GroupId}", groupId);
                    return NotFound(new { Message = "Group not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "deleteGroup: Error deleting group - GroupId={GroupId}", groupId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("leaveGroup/{groupId}/{studentId}")]
        public async Task<IActionResult> LeaveGroup(int groupId, int studentId)
        {
            _logger.LogInformation("leaveGroup: Start - GroupId={GroupId}, StudentId={StudentId}", groupId, studentId);
            try
            {
                var result = await _groupRepository.LeaveGroup(groupId, studentId);
                if (result)
                {
                    _logger.LogInformation("leaveGroup: Success - GroupId={GroupId}, StudentId={StudentId}", groupId, studentId);
                    return Ok(new { Message = "Left group successfully" });
                }
                else
                {
                    _logger.LogWarning("leaveGroup: Student not found in group - GroupId={GroupId}, StudentId={StudentId}", groupId, studentId);
                    return NotFound(new { Message = "Group or Student not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "leaveGroup: Error leaving group - GroupId={GroupId}, StudentId={StudentId}", groupId, studentId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("removeStudentFromGroup/{groupId}/{studentId}")]
        public async Task<IActionResult> RemoveStudentFromGroup(int groupId, int studentId)
        {
            _logger.LogInformation("removeStudentFromGroup: Start - GroupId={GroupId}, StudentId={StudentId}", groupId, studentId);
            try
            {
                var result = await _groupRepository.RemoveStudentFromGroup(groupId, studentId);
                if (result)
                {
                    _logger.LogInformation("removeStudentFromGroup: Success - GroupId={GroupId}, StudentId={StudentId}", groupId, studentId);
                    return Ok(new { Message = "Student removed from group successfully" });
                }
                else
                {
                    _logger.LogWarning("removeStudentFromGroup: Student not found in group - GroupId={GroupId}, StudentId={StudentId}", groupId, studentId);
                    return NotFound(new { Message = "Group or Student not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "removeStudentFromGroup: Error removing student from group - GroupId={GroupId}, StudentId={StudentId}", groupId, studentId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("RemoveQuizFromGroup/{groupId}/{quizId}")]
        public async Task<IActionResult> RemoveQuizFromGroup(int groupId, int quizId)
        {
            _logger.LogInformation("removeQuizFromGroup: Start - GroupId={GroupId}, QuizId={QuizId}", groupId, quizId);
            try
            {
                var result = await _groupRepository.RemoveQuizFromGroup(groupId, quizId);
                if (result)
                {
                    _logger.LogInformation("removeQuizFromGroup: Success - GroupId={GroupId}, QuizId={QuizId}", groupId, quizId);
                    return Ok(new { Message = "Quiz removed from group successfully" });
                }
                else
                {
                    _logger.LogWarning("removeQuizFromGroup: Quiz not found in group - GroupId={GroupId}, QuizId={QuizId}", groupId, quizId);
                    return NotFound(new { Message = "Group or Quiz not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "removeQuizFromGroup: Error removing quiz from group - GroupId={GroupId}, QuizId={QuizId}", groupId, quizId);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}