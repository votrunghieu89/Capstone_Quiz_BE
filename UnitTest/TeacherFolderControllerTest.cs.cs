using Capstone.Controllers;
using Capstone.DTOs.Folder.Teacher;
using Capstone.Repositories;
using Capstone.Repositories.Folder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Capstone.UnitTest
{
    public class TeacherFolderControllerTest
    {
        private readonly TeacherFolderController _controller;
        private readonly Mock<ITeacherFolder> _mockRepo;
        private readonly Mock<ILogger<TeacherFolderController>> _mockLogger;
        private readonly Mock<IAWS> _mockAWS;

        public TeacherFolderControllerTest()
        {
            _mockRepo = new Mock<ITeacherFolder>();
            _mockLogger = new Mock<ILogger<TeacherFolderController>>();
            _mockAWS = new Mock<IAWS>();
            
            _controller = new TeacherFolderController(
                _mockRepo.Object, 
                _mockLogger.Object, 
                _mockAWS.Object
            );
        }

        [Fact]
        public async Task CreateFolder_Success_ReturnsOk()
        {
            _mockRepo.Setup(r => r.createFolder(1, "Folder A", null)).ReturnsAsync(true);

            var result = await _controller.CreateFolder(1, "Folder A", null);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task CreateFolder_Fail_ReturnsBadRequest()
        {
            _mockRepo.Setup(r => r.createFolder(1, "Folder A", null)).ReturnsAsync(false);

            var result = await _controller.CreateFolder(1, "Folder A", null);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateFolder_Exception_Returns500()
        {
            _mockRepo.Setup(r => r.createFolder(1, "Folder A", null)).ThrowsAsync(new Exception("e"));

            var result = await _controller.CreateFolder(1, "Folder A", null);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }

        [Fact]
        public async Task GetAllFolder_WhenHasData_ReturnsOk()
        {
            var list = new List<getAllFolderDTO?> { new getAllFolderDTO { FolderId = 1, FolderName = "F" } };
            _mockRepo.Setup(r => r.getAllFolder(1)).ReturnsAsync(list);

            var result = await _controller.GetAllFolder(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            var val = Assert.IsType<List<getAllFolderDTO?>>(ok.Value);
            Assert.Single(val);
        }

        [Fact]
        public async Task GetAllFolder_WhenNullOrEmpty_ReturnsNotFound()
        {
            _mockRepo.Setup(r => r.getAllFolder(1)).ReturnsAsync((List<getAllFolderDTO?>)null);
            var result1 = await _controller.GetAllFolder(1);
            Assert.IsType<NotFoundObjectResult>(result1);

            _mockRepo.Setup(r => r.getAllFolder(2)).ReturnsAsync(new List<getAllFolderDTO?>());
            var result2 = await _controller.GetAllFolder(2);
            Assert.IsType<NotFoundObjectResult>(result2);
        }

        [Fact]
        public async Task GetFolderDetail_WhenFound_ReturnsOk()
        {
            var detail = new FolderDetailDTO { FolderID = 1, FolderName = "F" };
            _mockRepo.Setup(r => r.GetFolderDetail(1, 1)).ReturnsAsync(detail);

            var result = await _controller.GetFolderDetail(1, 1);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task GetFolderDetail_NotFound_ReturnsNotFound()
        {
            _mockRepo.Setup(r => r.GetFolderDetail(1, 1)).ReturnsAsync((FolderDetailDTO)null);

            var result = await _controller.GetFolderDetail(1, 1);

            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}
