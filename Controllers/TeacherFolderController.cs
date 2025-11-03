using Capstone.Repositories.Folder;
using Capstone.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using static Capstone.ENUMs.TeacherFolderEnum;

namespace Capstone.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeacherFolderController : ControllerBase
    {
        private readonly ITeacherFolder _repoFolder;
        private readonly ILogger<TeacherFolderController> _logger;

        public TeacherFolderController(ITeacherFolder repoFolder, ILogger<TeacherFolderController> logger)
        {
            _repoFolder = repoFolder;
            _logger = logger;
        }

        // ------------------------- CREATE FOLDER -------------------------
        [HttpPost("createFolder")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> CreateFolder(int teacherID, string folderName, int? parentFolderID)
        {
            try
            {
                var check = await _repoFolder.createFolder(teacherID, folderName, parentFolderID);
                if (check)
                {
                    _logger.LogInformation("Folder '{FolderName}' created successfully for TeacherID={TeacherID}", folderName, teacherID);
                    return Ok(new { message = "Folder created successfully." });
                }

                _logger.LogWarning("Failed to create folder '{FolderName}' for TeacherID={TeacherID}", folderName, teacherID);
                return BadRequest(new { message = "Failed to create folder." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating folder for TeacherID={TeacherID}", teacherID);
                return StatusCode(500, new { message = "Internal server error." });
            }
        }

        // ------------------------- GET ALL FOLDERS -------------------------
        [HttpGet("getAllFolder")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> GetAllFolder(int teacherID)
        {
            try
            {
                var folders = await _repoFolder.getAllFolder(teacherID);
                if (folders == null || !folders.Any())
                {
                    _logger.LogInformation("No folders found for TeacherID={TeacherID}", teacherID);
                    return NotFound(new { message = "No folders found for this teacher." });
                }

                _logger.LogInformation("Returned all folders for TeacherID={TeacherID}", teacherID);
                return Ok(folders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving folders for TeacherID={TeacherID}", teacherID);
                return StatusCode(500, new { message = "Internal server error." });
            }
        }

        // ------------------------- GET FOLDER DETAIL -------------------------
        [HttpGet("getFolderDetail")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> GetFolderDetail(int teacherId, int folderId)
        {
            try
            {
                var folderDetail = await _repoFolder.GetFolderDetail(teacherId, folderId);
                if (folderDetail == null)
                {
                    _logger.LogInformation("No folder detail found for TeacherID={TeacherID}, FolderID={FolderID}", teacherId, folderId);
                    return NotFound(new { message = "No folder detail found for this teacher or folder." });
                }

                _logger.LogInformation("Returned folder detail for TeacherID={TeacherID}, FolderID={FolderID}", teacherId, folderId);
                return Ok(folderDetail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting folder detail for TeacherID={TeacherID}, FolderID={FolderID}", teacherId, folderId);
                return StatusCode(500, new { message = "Internal server error." });
            }
        }

        // ------------------------- UPDATE FOLDER -------------------------
        [HttpPut("updateFolder")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> UpdateFolder(int folderId, string folderName)
        {
            try
            {
                bool isUpdated = await _repoFolder.UpdateFolder(folderId, folderName);
                if (isUpdated)
                {
                    _logger.LogInformation("FolderID={FolderID} updated successfully to '{FolderName}'", folderId, folderName);
                    return Ok(new { message = "Folder updated successfully." });
                }

                _logger.LogWarning("Failed to update FolderID={FolderID}", folderId);
                return BadRequest(new { message = "Failed to update folder." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating folder FolderID={FolderID}", folderId);
                return StatusCode(500, new { message = "Internal server error." });
            }
        }

        // ------------------------- REMOVE QUIZ TO OTHER FOLDER -------------------------
        [HttpPut("moveQuizToOtherFolder")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> MoveQuizToOtherFolder(int quizId, int folderId)
        {
            try
            {
                bool isMoved = await _repoFolder.RemoveQuizToOtherFolder(quizId, folderId);
                if (isMoved)
                {
                    _logger.LogInformation("QuizID={QuizID} moved to FolderID={FolderID} successfully", quizId, folderId);
                    return Ok(new { message = "Quiz moved successfully." });
                }

                _logger.LogWarning("Failed to move QuizID={QuizID} to FolderID={FolderID}", quizId, folderId);
                return BadRequest(new { message = "Failed to move quiz." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving QuizID={QuizID} to FolderID={FolderID}", quizId, folderId);
                return StatusCode(500, new { message = "Internal server error." });
            }
        }

        // ------------------------- DELETE FOLDER -------------------------
        [HttpDelete("deleteFolder")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> DeleteFolder(int folderId)
        {
            try
            {
                var result = await _repoFolder.DeleteFolder(folderId);

                switch (result)
                {
                    case CheckQuizInFolder.HasQuiz:
                        _logger.LogWarning("Cannot delete FolderID={FolderID} because it still contains quizzes.", folderId);
                        return BadRequest(new { message = "Cannot delete folder because it still contains quizzes." });

                    case CheckQuizInFolder.Success:
                        _logger.LogInformation("FolderID={FolderID} deleted successfully.", folderId);
                        return Ok(new { message = "Folder deleted successfully." });

                    default:
                        _logger.LogWarning("Failed to delete FolderID={FolderID}", folderId);
                        return BadRequest(new { message = "Failed to delete folder." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting FolderID={FolderID}", folderId);
                return StatusCode(500, new { message = "Internal server error." });
            }
        }
    }
}
