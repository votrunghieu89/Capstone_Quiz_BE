using Capstone.Repositories.Folder;
using Capstone.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;

namespace Capstone.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeacherFolderController : ControllerBase
    {
        private readonly ITeacherFolder _repoFolder;
        private readonly ILogger<TeacherFolderController> _logger;
        public TeacherFolderController (ITeacherFolder teacherFolderService , ILogger<TeacherFolderController> logger)
        {
            _repoFolder = teacherFolderService;
            _logger = logger;
        }

        [HttpGet("createFolder")]
        public async Task<IActionResult> createFolder(int teacherID, string folderName, int? parentFolderID)
        {
            try
            {
                var check = await _repoFolder.createFolder(teacherID, folderName, parentFolderID);
                if (check)
                {
                    return Ok(new { message = "Folder created successfully " });
                }
                return BadRequest(new { message = "Failed to create Folder" });
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error creating folder for teacherID={TeacherID}", teacherID);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("getAllFolder")]
        public async Task<IActionResult> getAllFolder(int teacherID)
        {
            try
            {
                var folders = await _repoFolder.getAllFolder(teacherID);
                if (folders == null || !folders.Any())
                {
                    return NotFound(new { message = "No folders found for this teacher" });
                }

                return Ok(folders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting folders for teacherID={TeacherID}", teacherID);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
        [HttpGet("getFolderDetail")]
        public async Task<IActionResult> getFolderDetail (int teacherId , int folderId)
        {
            try
            {
                var quizzFolder = await _repoFolder.GetFolderDetail(teacherId, folderId);
                if(quizzFolder == null)
                {
                    return NotFound(new { message = "No quizz found for this teacher or folder" });
                }
                return Ok(quizzFolder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quizz for teacherID={TeacherID} and folderID={folderId}", teacherId , folderId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
