using Capstone.Controllers;
using Capstone.DTOs.StudentProfile;
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
    public class StudentProfileControllerTest
    {
        private readonly StudentProfileController _controller;
        private readonly Mock<IStudentProfileRepository> _mockRepo;
        private readonly Mock<ILogger<StudentProfileController>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<IWebHostEnvironment> _mockEnv;

        public StudentProfileControllerTest()
        {
            _mockRepo = new Mock<IStudentProfileRepository>();
            _mockLogger = new Mock<ILogger<StudentProfileController>>();
            _mockConfig = new Mock<IConfiguration>();
            _mockEnv = new Mock<IWebHostEnvironment>();
            _mockEnv.Setup(e => e.ContentRootPath).Returns(Directory.GetCurrentDirectory());
            _mockConfig.Setup(c => c["UploadSettings:AvatarFolder"]).Returns("Avatars");

            _controller = new StudentProfileController(_mockLogger.Object, _mockRepo.Object, _mockConfig.Object, _mockEnv.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.Request.Scheme = "http";
            _controller.ControllerContext.HttpContext.Request.Host = new HostString("localhost");
        }

        [Fact]
        public async Task GetStudentProfile_NotFound_WhenNull()
        {
            _mockRepo.Setup(r => r.getStudentProfile(1)).ReturnsAsync((StudentProfileModel)null);

            var res = await _controller.getStudentProfile(1);
            Assert.IsType<NotFoundObjectResult>(res);
        }

        [Fact]
        public async Task GetStudentProfile_Ok_WithTransformedUrl()
        {
            var profile = new StudentProfileModel { StudentId = 1, FullName = "A", AvatarURL = "Avatar\\a.jpg", IdUnique = "U" };
            _mockRepo.Setup(r => r.getStudentProfile(1)).ReturnsAsync(profile);

            var res = await _controller.getStudentProfile(1);

            var ok = Assert.IsType<OkObjectResult>(res);
            dynamic obj = ok.Value;
            string url = obj.profile.AvatarURL;
            Assert.StartsWith("http://localhost/", url);
            Assert.DoesNotContain("\\", url);
        }

        [Fact]
        public async Task UpdateStudentProfile_NullRequest_BadRequest()
        {
            var res = await _controller.updateStudentProfile(null);
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task UpdateStudentProfile_RepoNull_Returns500()
        {
            var dto = CreateFormDto();
            _mockRepo.Setup(r => r.updateStudentProfile(It.IsAny<StudentProfileModel>(), It.IsAny<int>(), It.IsAny<string>()))
                     .ReturnsAsync((StudentProfileResponseDTO)null);

            var res = await _controller.updateStudentProfile(dto);
            var obj = Assert.IsType<ObjectResult>(res);
            Assert.Equal(500, obj.StatusCode);
        }

        [Fact]
        public async Task UpdateStudentProfile_Success_ReturnsOk()
        {
            var dto = CreateFormDto();
            _mockRepo.Setup(r => r.updateStudentProfile(It.IsAny<StudentProfileModel>(), It.IsAny<int>(), It.IsAny<string>()))
                     .ReturnsAsync(new StudentProfileResponseDTO { FullName = dto.FullName, AvatarURL = "Avatars/new.png", oldAvatar = null });

            var res = await _controller.updateStudentProfile(dto);
            Assert.IsType<OkObjectResult>(res);
        }

        private StudenProfileUpdateDTO CreateFormDto()
        {
            var bytes = Encoding.UTF8.GetBytes("fake");
            var stream = new MemoryStream(bytes);
            IFormFile file = new FormFile(stream, 0, bytes.Length, "file", "avatar.png");
            return new StudenProfileUpdateDTO
            {
                StudentId = 1,
                FullName = "A",
                FormFile = file
            };
        }
    }
}
