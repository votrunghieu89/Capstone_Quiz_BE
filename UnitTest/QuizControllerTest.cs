using Capstone.Controllers;
using Capstone.Database;
using Capstone.DTOs;
using Capstone.DTOs.Quizzes;
using Capstone.Repositories;
using Capstone.Repositories.Quizzes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using System.Text.Json;
using Xunit;

namespace Capstone.UnitTest
{
    public class QuizControllerTest
    {
        private readonly QuizController _controller;
        private readonly Mock<IQuizRepository> _mockRepo;
        private readonly Mock<ILogger<QuizController>> _mockLogger;
        private readonly Mock<IRedis> _mockRedis;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<IWebHostEnvironment> _mockEnv;
        private readonly Mock<IAWS> _mockAWS;

        public QuizControllerTest()
        {
            _mockRepo = new Mock<IQuizRepository>();
            _mockLogger = new Mock<ILogger<QuizController>>();
            _mockRedis = new Mock<IRedis>();
            _mockConfig = new Mock<IConfiguration>();
            _mockEnv = new Mock<IWebHostEnvironment>();
            _mockAWS = new Mock<IAWS>();
            
            _mockEnv.Setup(e => e.ContentRootPath).Returns(Directory.GetCurrentDirectory());

            _controller = new QuizController(
                _mockLogger.Object, 
                _mockRepo.Object, 
                _mockRedis.Object, 
                _mockConfig.Object, 
                _mockEnv.Object, 
                _mockAWS.Object
            );
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.Request.Scheme = "http";
            _controller.ControllerContext.HttpContext.Request.Host = new HostString("localhost");
            
            // Setup user claims for authorized endpoints
            var claims = new List<Claim>
            {
                new Claim("AccountId", "1"),
                new Claim(ClaimTypes.Role, "Teacher")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext.User = claimsPrincipal;
        }

        #region GetQuizById
        [Fact]
        public async Task GetQuizById_WhenCacheNull_CallsRepo_ReturnsOk()
        {
            _mockRedis.Setup(r => r.GetStringAsync("quiz_questions_1")).ReturnsAsync((string)null);
            _mockRepo.Setup(r => r.GetAllQuestionEachQuiz(1)).ReturnsAsync(new List<getQuizQuestionWithoutAnswerDTO> { new getQuizQuestionWithoutAnswerDTO() });

            var result = await _controller.GetQuestionOfQuiz_cache(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetQuizById_WhenCachePresent_Deserializes_ReturnsOk()
        {
            var items = new List<GetQuizQuestionsDTO> { new GetQuizQuestionsDTO() };
            _mockRedis.Setup(r => r.GetStringAsync("quiz_questions_1")).ReturnsAsync(JsonSerializer.Serialize(items));

            var result = await _controller.GetQuestionOfQuiz_cache(1);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetQuizById_WhenCacheNull_AndRepoReturnsNull_ReturnsNotFound()
        {
            _mockRedis.Setup(r => r.GetStringAsync("quiz_questions_1")).ReturnsAsync((string)null);
            _mockRepo.Setup(r => r.GetAllQuestionEachQuiz(1)).ReturnsAsync((List<getQuizQuestionWithoutAnswerDTO>)null);

            var result = await _controller.GetQuestionOfQuiz_cache(1);

            Assert.IsType<NotFoundObjectResult>(result);
        }
        #endregion

        #region getDetailOfAQuiz
        [Fact]
        public async Task GetDetailOfAQuiz_NotFound_ReturnsNotFound()
        {
            _mockRepo.Setup(r => r.getDetailOfAQuiz(1)).ReturnsAsync((ViewDetailDTO)null);
            var result = await _controller.getDetailOfAQuiz(1);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetDetailOfAQuiz_Found_ReturnsOk_WithS3Url()
        {
            var dto = new ViewDetailDTO { QuizId = 1, Title = "T", AvatarURL = "quiz/abc.jpg" };
            _mockRepo.Setup(r => r.getDetailOfAQuiz(1)).ReturnsAsync(dto);
            _mockAWS.Setup(a => a.ReadImage("quiz/abc.jpg")).ReturnsAsync("https://s3.amazonaws.com/bucket/quiz/abc.jpg");

            var result = await _controller.getDetailOfAQuiz(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            var val = Assert.IsType<ViewDetailDTO>(ok.Value);
            Assert.StartsWith("https://s3.amazonaws.com", val.AvatarURL);
        }

        [Fact]
        public async Task GetDetailOfAQuiz_NoAvatar_ReturnsOk_WithEmptyUrl()
        {
            var dto = new ViewDetailDTO { QuizId = 1, Title = "T", AvatarURL = null };
            _mockRepo.Setup(r => r.getDetailOfAQuiz(1)).ReturnsAsync(dto);

            var result = await _controller.getDetailOfAQuiz(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            var val = Assert.IsType<ViewDetailDTO>(ok.Value);
            Assert.Equal(string.Empty, val.AvatarURL);
        }
        #endregion

        #region getDetailofAHPQuiz
        [Fact]
        public async Task GetDetailofAHPQuiz_Found_ReturnsOk_WithS3Url()
        {
            var dto = new QuizDetailHPDTO { QuizId = 1, Title = "T", AvatarURL = "quiz/test.jpg" };
            _mockRepo.Setup(r => r.getDetailOfQuizHP(1)).ReturnsAsync(dto);
            _mockAWS.Setup(a => a.ReadImage("quiz/test.jpg")).ReturnsAsync("https://s3.amazonaws.com/bucket/quiz/test.jpg");

            var result = await _controller.getDetailofAHPQuiz(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            var val = Assert.IsType<QuizDetailHPDTO>(ok.Value);
            Assert.StartsWith("https://s3.amazonaws.com", val.AvatarURL);
        }

        [Fact]
        public async Task GetDetailofAHPQuiz_NoAvatar_ReturnsOk()
        {
            var dto = new QuizDetailHPDTO { QuizId = 1, Title = "T", AvatarURL = null };
            _mockRepo.Setup(r => r.getDetailOfQuizHP(1)).ReturnsAsync(dto);

            var result = await _controller.getDetailofAHPQuiz(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.IsType<QuizDetailHPDTO>(ok.Value);
        }
        #endregion

        #region GetAllQuizzes
        [Fact]
        public async Task GetAllQuizzes_InvalidPagination_ReturnsBadRequest()
        {
            var res = await _controller.GetAllQuizzes(new PaginationDTO { page = 0, pageSize = 10 });
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task GetAllQuizzes_FromRepo_ReturnsOk_WithS3Urls()
        {
            var list = new List<ViewAllQuizDTO> { new ViewAllQuizDTO { QuizId = 1, AvatarURL = "quiz/image.jpg" } };
            _mockRepo.Setup(r => r.getAllQuizzes(1, 10)).ReturnsAsync(list);
            _mockAWS.Setup(a => a.ReadImage("quiz/image.jpg")).ReturnsAsync("https://s3.amazonaws.com/bucket/quiz/image.jpg");

            var res = await _controller.GetAllQuizzes(new PaginationDTO { page = 1, pageSize = 10 });
            
            var ok = Assert.IsType<OkObjectResult>(res);
            var val = Assert.IsType<List<ViewAllQuizDTO>>(ok.Value);
            Assert.StartsWith("https://s3.amazonaws.com", val[0].AvatarURL);
        }

        [Fact]
        public async Task GetAllQuizzes_FromRepo_ReturnsNotFoundWhenEmpty()
        {
            _mockRepo.Setup(r => r.getAllQuizzes(1, 10)).ReturnsAsync(new List<ViewAllQuizDTO>());

            var res = await _controller.GetAllQuizzes(new PaginationDTO { page = 1, pageSize = 10 });
            Assert.IsType<NotFoundObjectResult>(res);
        }
        #endregion

        #region CheckQuizAnswers
        [Fact]
        public async Task CheckQuizAnswers_NullOrFalse_ReturnsBadRequest()
        {
            var dto = new CheckAnswerDTO();
            _mockRepo.Setup(r => r.checkAnswer(dto)).ReturnsAsync(false);
            var res = await _controller.CheckQuizAnswers(dto);
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task CheckQuizAnswers_True_ReturnsOk()
        {
            var dto = new CheckAnswerDTO();
            _mockRepo.Setup(r => r.checkAnswer(dto)).ReturnsAsync(true);
            var res = await _controller.CheckQuizAnswers(dto);
            Assert.IsType<OkObjectResult>(res);
        }
        #endregion

        #region GetCorrectAnswers
        [Fact]
        public async Task GetCorrectAnswers_Null_ReturnsNotFound()
        {
            _mockRepo.Setup(r => r.getCorrectAnswer(It.IsAny<GetCorrectAnswer>())).ReturnsAsync((RightAnswerDTO)null);
            var res = await _controller.GetCorrectAnswers(new GetCorrectAnswer());
            Assert.IsType<NotFoundObjectResult>(res);
        }

        [Fact]
        public async Task GetCorrectAnswers_Found_ReturnsOk()
        {
            _mockRepo.Setup(r => r.getCorrectAnswer(It.IsAny<GetCorrectAnswer>())).ReturnsAsync(new RightAnswerDTO());
            var res = await _controller.GetCorrectAnswers(new GetCorrectAnswer());
            Assert.IsType<OkObjectResult>(res);
        }
        #endregion

        #region UploadImage
        [Fact]
        public async Task UploadImage_NoFile_ReturnsDefaultImage()
        {
            var dto = new QuizCreateFormDTO { AvatarURL = null };
            
            var res = await _controller.UploadImage(dto);
            
            var ok = Assert.IsType<OkObjectResult>(res);
            Assert.Contains("quiz/Default.jpg", ok.Value.ToString());
        }

        [Fact]
        public async Task UploadImage_WithFile_ReturnsS3Url()
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1024);
            mockFile.Setup(f => f.FileName).Returns("test.jpg");
            
            var dto = new QuizCreateFormDTO { AvatarURL = mockFile.Object };
            _mockAWS.Setup(a => a.UploadQuizImageToS3(It.IsAny<IFormFile>())).ReturnsAsync("quiz/uploaded-image.jpg");

            var res = await _controller.UploadImage(dto);
            
            var ok = Assert.IsType<OkObjectResult>(res);
            Assert.Contains("quiz/uploaded-image.jpg", ok.Value.ToString());
        }
        #endregion

        #region UpdateImage
        [Fact]
        public async Task UpdateImage_NoNewFile_ReturnsOldImage()
        {
            _mockRepo.Setup(r => r.getOrlAvatarURL(1)).ReturnsAsync("quiz/old.jpg");
            var res = await _controller.UpdateImage(new QuizUpdateImageDTO { QuizId = 1, AvatarURL = null });
            var ok = Assert.IsType<OkObjectResult>(res);
            Assert.Contains("quiz/old.jpg", ok.Value.ToString());
        }

        [Fact]
        public async Task UpdateImage_WithNewFile_DeletesOldAndReturnsNewUrl()
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1024);
            mockFile.Setup(f => f.FileName).Returns("new.jpg");
            
            _mockRepo.Setup(r => r.getOrlAvatarURL(1)).ReturnsAsync("quiz/old.jpg");
            _mockAWS.Setup(a => a.UploadQuizImageToS3(It.IsAny<IFormFile>())).ReturnsAsync("quiz/new.jpg");
            _mockAWS.Setup(a => a.DeleteImage("quiz/old.jpg")).ReturnsAsync(true);

            var res = await _controller.UpdateImage(new QuizUpdateImageDTO { QuizId = 1, AvatarURL = mockFile.Object });
            
            var ok = Assert.IsType<OkObjectResult>(res);
            Assert.Contains("quiz/new.jpg", ok.Value.ToString());
            _mockAWS.Verify(a => a.DeleteImage("quiz/old.jpg"), Times.Once);
        }

        [Fact]
        public async Task UpdateImage_FileTooLarge_ReturnsBadRequest()
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(3 * 1024 * 1024); // 3MB
            mockFile.Setup(f => f.FileName).Returns("large.jpg");
            
            _mockRepo.Setup(r => r.getOrlAvatarURL(1)).ReturnsAsync("quiz/old.jpg");

            var res = await _controller.UpdateImage(new QuizUpdateImageDTO { QuizId = 1, AvatarURL = mockFile.Object });
            
            Assert.IsType<BadRequestObjectResult>(res);
        }

        [Fact]
        public async Task UpdateImage_DefaultImage_NotDeleted()
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1024);
            
            _mockRepo.Setup(r => r.getOrlAvatarURL(1)).ReturnsAsync("quiz/Default.jpg");
            _mockAWS.Setup(a => a.UploadQuizImageToS3(It.IsAny<IFormFile>())).ReturnsAsync("quiz/new.jpg");

            var res = await _controller.UpdateImage(new QuizUpdateImageDTO { QuizId = 1, AvatarURL = mockFile.Object });
            
            var ok = Assert.IsType<OkObjectResult>(res);
            _mockAWS.Verify(a => a.DeleteImage(It.IsAny<string>()), Times.Never);
        }
        #endregion

        #region UpdateQuiz
        [Fact]
        public async Task UpdateQuiz_NullRepoReturn_Returns500()
        {
            _mockRepo.Setup(r => r.UpdateQuiz(It.IsAny<QuizUpdateDTO>(), It.IsAny<string>(), It.IsAny<int>()))
                     .ReturnsAsync((QuizUpdateDTO)null);
            var res = await _controller.UpdateQuiz(new QuizzUpdateControllerDTO());
            var obj = Assert.IsType<ObjectResult>(res);
            Assert.Equal(500, obj.StatusCode);
        }

        [Fact]
        public async Task UpdateQuiz_Success_ReturnsOk_AndClearsCache()
        {
            var dto = new QuizzUpdateControllerDTO { QuizId = 1 };
            _mockRepo.Setup(r => r.UpdateQuiz(It.IsAny<QuizUpdateDTO>(), It.IsAny<string>(), It.IsAny<int>()))
                     .ReturnsAsync(new QuizUpdateDTO());

            var res = await _controller.UpdateQuiz(dto);
            
            Assert.IsType<OkObjectResult>(res);
            _mockRedis.Verify(r => r.DeleteKeysByPatternAsync($"quiz_questions_1*"), Times.Once);
        }
        #endregion

        #region DeleteQuiz
        [Fact]
        public async Task DeleteQuiz_NotFound_ReturnsNotFound()
        {
            _mockRepo.Setup(r => r.DeleteQuiz(1, It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync((string)null);
            
            var res = await _controller.DeleteQuiz(1);
            
            Assert.IsType<NotFoundObjectResult>(res);
        }

        [Fact]
        public async Task DeleteQuiz_DefaultImage_ReturnsOk_WithoutDeletingS3()
        {
            _mockRepo.Setup(r => r.DeleteQuiz(1, It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync("quiz/Default.jpg");
            
            var res = await _controller.DeleteQuiz(1);
            
            Assert.IsType<OkObjectResult>(res);
            _mockAWS.Verify(a => a.DeleteImage(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DeleteQuiz_CustomImage_ReturnsOk_AndDeletesFromS3()
        {
            _mockRepo.Setup(r => r.DeleteQuiz(1, It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync("quiz/custom.jpg");
            _mockAWS.Setup(a => a.DeleteImage("quiz/custom.jpg")).ReturnsAsync(true);
            
            var res = await _controller.DeleteQuiz(1);
            
            Assert.IsType<OkObjectResult>(res);
            _mockAWS.Verify(a => a.DeleteImage("quiz/custom.jpg"), Times.Once);
        }

        [Fact]
        public async Task DeleteQuiz_S3DeleteFails_ReturnsBadRequest()
        {
            _mockRepo.Setup(r => r.DeleteQuiz(1, It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync("quiz/custom.jpg");
            _mockAWS.Setup(a => a.DeleteImage("quiz/custom.jpg")).ReturnsAsync(false);
            
            var res = await _controller.DeleteQuiz(1);
            
            Assert.IsType<BadRequestObjectResult>(res);
        }
        #endregion

        #region DeleteQuestion
        [Fact]
        public async Task DeleteQuestion_NotDeleted_ReturnsNotFound()
        {
            _mockRepo.Setup(r => r.DeleteQuestion(1)).ReturnsAsync(false);
            var res = await _controller.DeleteQuestion(1);
            Assert.IsType<NotFoundObjectResult>(res);
        }

        [Fact]
        public async Task DeleteQuestion_Deleted_ReturnsOk()
        {
            _mockRepo.Setup(r => r.DeleteQuestion(1)).ReturnsAsync(true);
            var res = await _controller.DeleteQuestion(1);
            Assert.IsType<OkObjectResult>(res);
        }
        #endregion

        #region ClearQuizCache
        [Fact]
        public async Task ClearQuizCache_Success_ReturnsOk()
        {
            var res = await _controller.ClearQuizCache(1);
            Assert.IsType<OkObjectResult>(res);
            _mockRedis.Verify(r => r.DeleteKeysByPatternAsync("quiz_questions_1*"), Times.Once);
        }
        #endregion
    }
}
