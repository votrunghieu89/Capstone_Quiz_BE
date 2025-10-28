using Capstone.Controllers;
using Capstone.DTOs.TeacherProfile;
using Capstone.Model;
using Capstone.Repositories.Profiles;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using Xunit;

namespace Capstone.UnitTest
{
    public class TeacherProfileControllerTest
    {
        private readonly TeacherProfileController _controller;
        private readonly Mock<ITeacherProfileRepository> _mockRepo;
        private readonly Mock<ILogger<TeacherProfileController>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<IWebHostEnvironment> _mockEnv;

        public TeacherProfileControllerTest()
        {
            _mockRepo = new Mock<ITeacherProfileRepository>();
            _mockLogger = new Mock<ILogger<TeacherProfileController>>();
            _mockConfig = new Mock<IConfiguration>();
            _mockEnv = new Mock<IWebHostEnvironment>();
            _mockEnv.Setup(e => e.ContentRootPath).Returns(Directory.GetCurrentDirectory());
            _mockConfig.Setup(c => c["UploadSettings:AvatarFolder"]).Returns("Avatars");

            _controller = new TeacherProfileController(_mockLogger.Object, _mockRepo.Object, _mockConfig.Object, _mockEnv.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.Request.Scheme = "http";
            _controller.ControllerContext.HttpContext.Request.Host = new HostString("localhost");
        }

        [Fact]
        public async Task GetTeacherProfile_NotFound_WhenNull()
        {
            _mockRepo.Setup(r => r.getTeacherProfile(1)).ReturnsAsync((TeacherProfileModel)null);
            var res = await _controller.getTeacherProfile(1);
            Assert.IsType<NotFoundObjectResult>(res);
        }

        [Fact]
        public async Task GetTeacherProfile_Ok_WithTransformedUrl()
        {
            var profile = new TeacherProfileModel { TeacherId = 1, FullName = "T", AvatarURL = "Avatar\\a.jpg" };
            _mockRepo.Setup(r => r.getTeacherProfile(1)).ReturnsAsync(profile);
            var res = await _controller.getTeacherProfile(1);
            var ok = Assert.IsType<OkObjectResult>(res);
            dynamic obj = ok.Value;
            string url = obj.profile.AvatarURL;
            Assert.StartsWith("http://localhost/", url);
            Assert.DoesNotContain("\\", url);
        }

        [Fact]
        public async Task UpdateTeacherProfile_NullRequest_BadRequest()
        {
            var res = await _controller.updateTeacherProfile(null);
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task UpdateTeacherProfile_RepoNull_Returns500()
        {
            var dto = CreateFormDto();
            _mockRepo.Setup(r => r.updateTeacherProfile(It.IsAny<TeacherProfileModel>(), It.IsAny<int>(), It.IsAny<string>()))
                     .ReturnsAsync((TeacherProfileResponseDTO)null);
            var res = await _controller.updateTeacherProfile(dto);
            var obj = Assert.IsType<ObjectResult>(res);
            Assert.Equal(500, obj.StatusCode);
        }

        [Fact]
        public async Task UpdateTeacherProfile_Success_ReturnsOk()
        {
            var dto = CreateFormDto();
            _mockRepo.Setup(r => r.updateTeacherProfile(It.IsAny<TeacherProfileModel>(), It.IsAny<int>(), It.IsAny<string>()))
                     .ReturnsAsync(new TeacherProfileResponseDTO { FullName = dto.FullName, AvatarURL = "Avatars/new.png", oldAvatar = null });
            var res = await _controller.updateTeacherProfile(dto);
            Assert.IsType<OkObjectResult>(res);
        }

        private TeacherProfileUpdateDTO CreateFormDto()
        {
            var bytes = Encoding.UTF8.GetBytes("fake");
            var stream = new MemoryStream(bytes);
            IFormFile file = new FormFile(stream, 0, bytes.Length, "file", "avatar.png");
            return new TeacherProfileUpdateDTO
            {
                TeacherId = 1,
                FullName = "T",
                FormFile = file
            };
        }
    }
}
