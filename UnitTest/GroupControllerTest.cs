using Capstone.Controllers;
using Capstone.Database;
using Capstone.DTOs.Group;
using Capstone.ENUMs;
using Capstone.Model;
using Capstone.Repositories.Groups;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Capstone.UnitTest
{
    public class GroupControllerTest
    {
        private readonly GroupController _controller;
        private readonly Mock<IGroupRepository> _mockRepo;
        private readonly Mock<ILogger<GroupController>> _mockLogger;
        private readonly Mock<IRedis> _mockRedis; // not used directly by controller methods under test
        private readonly Mock<IConfiguration> _mockConfig;

        public GroupControllerTest()
        {
            _mockRepo = new Mock<IGroupRepository>();
            _mockLogger = new Mock<ILogger<GroupController>>();
            _mockRedis = new Mock<IRedis>();
            _mockConfig = new Mock<IConfiguration>();
            _mockConfig.Setup(c => c["Frontend:BaseUrl"]).Returns("http://frontend.test");

            _controller = new GroupController(_mockLogger.Object, _mockRepo.Object, _mockRedis.Object, _mockConfig.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        #region GET: GetGroupByTeacherId
        [Fact]
        public async Task GetGroupByTeacherId_ReturnsOk()
        {
            int teacherId = 1;
            _mockRepo.Setup(r => r.GetAllGroupsbyTeacherId(teacherId))
                     .ReturnsAsync(new List<AllGroupDTO> { new AllGroupDTO { GroupId = 1, GroupName = "G1" } });

            var result = await _controller.GetGroupByTeacherId(teacherId);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsType<List<AllGroupDTO>>(ok.Value);
            Assert.Single(list);
        }
        #endregion

        #region GET: GetAllStudentsByGroupId
        [Fact]
        public async Task GetAllStudentsByGroupId_ReturnsOk()
        {
            int groupId = 2;
            _mockRepo.Setup(r => r.GetAllStudentsByGroupId(groupId))
                     .ReturnsAsync(new List<ViewStudentDTO> { new ViewStudentDTO { StudentId = 10, FullName = "A" } });

            var result = await _controller.GetAllStudentsByGroupId(groupId);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsType<List<ViewStudentDTO>>(ok.Value);
            Assert.Single(list);
        }
        #endregion

        #region GET: GetAllGroupsByStudentId
        [Fact]
        public async Task GetAllGroupsByStudentId_ReturnsOk()
        {
            int studentId = 3;
            _mockRepo.Setup(r => r.GetAllGroupsByStudentId(studentId))
                     .ReturnsAsync(new List<AllGroupDTO> { new AllGroupDTO { GroupId = 2, GroupName = "G2" } });

            var result = await _controller.GetAllGroupsByStudentId(studentId);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsType<List<AllGroupDTO>>(ok.Value);
            Assert.Single(list);
        }
        #endregion

        #region GET: GetGroupDetail
        [Fact]
        public async Task GetGroupDetail_GroupNotFound_ReturnsNotFound()
        {
            int groupId = 9;
            _mockRepo.Setup(r => r.GetGroupDetailById(groupId)).ReturnsAsync((GroupModel)null);

            var result = await _controller.GetGroupDetail(groupId);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetGroupDetail_WhenFound_ReturnsOk()
        {
            int groupId = 9;
            var group = new GroupModel { GroupId = groupId, GroupName = "G9", TeacherId = 1, IdUnique = "INV123" };
            _mockRepo.Setup(r => r.GetGroupDetailById(groupId)).ReturnsAsync(group);
            _mockRepo.Setup(r => r.GetAllDeliveredQuizzesByGroupId(groupId))
                     .ReturnsAsync(new List<ViewQuizDTO>());

            var result = await _controller.GetGroupDetail(groupId);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }
        #endregion

        #region POST: CreateGroup
        [Fact]
        public async Task CreateGroup_NullRequest_ReturnsBadRequest()
        {
            var result = await _controller.CreateGroup(null);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateGroup_Success_ReturnsOk()
        {
            var req = new CreateGroupDTO { TeacherId = 1, GroupName = "G" };
            var saved = new GroupModel { GroupId = 100, TeacherId = 1, GroupName = "G", CreateAt = DateTime.Now };
            _mockRepo.Setup(r => r.CreateGroup(It.IsAny<GroupModel>(), It.IsAny<string>()))
                     .ReturnsAsync(saved);

            var result = await _controller.CreateGroup(req);

            var ok = Assert.IsType<OkObjectResult>(result);
            var model = Assert.IsType<GroupModel>(ok.Value);
            Assert.Equal(100, model.GroupId);
        }

        [Fact]
        public async Task CreateGroup_RepoReturnsNull_Returns500()
        {
            var req = new CreateGroupDTO { TeacherId = 1, GroupName = "G" };
            _mockRepo.Setup(r => r.CreateGroup(It.IsAny<GroupModel>(), It.IsAny<string>()))
                     .ReturnsAsync((GroupModel)null);

            var result = await _controller.CreateGroup(req);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }
        #endregion

        #region POST: InsertStudentToGroup
        [Theory]
        [InlineData(GroupEnum.JoinGroupResult.Success, 200)]
        [InlineData(GroupEnum.JoinGroupResult.AlreadyInGroup, 400)]
        [InlineData(GroupEnum.JoinGroupResult.Fail, 404)]
        [InlineData(GroupEnum.JoinGroupResult.Error, 500)]
        public async Task InsertStudentToGroup_ResultCodes(GroupEnum.JoinGroupResult repoResult, int expectedStatus)
        {
            _mockRepo.Setup(r => r.InsertStudentToGroup(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                     .ReturnsAsync(repoResult);

            var result = await _controller.InsertStudentToGroup(1, "UUU");

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(expectedStatus, obj.StatusCode);
        }
        #endregion

        #region POST: InsertQuizToGroup
        [Fact]
        public async Task InsertQuizToGroup_NullRequest_ReturnsBadRequest()
        {
            var result = await _controller.InsertQuizToGroup(null);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task InsertQuizToGroup_Success_Returns200()
        {
            var req = new InsertQuiz { QuizId = 1, GroupId = 2, MaxAttempts = 1, ExpiredTime = DateTime.UtcNow };
            _mockRepo.Setup(r => r.InsertQuizToGroup(req, It.IsAny<int>(), It.IsAny<string>()))
                     .ReturnsAsync(req);

            var result = await _controller.InsertQuizToGroup(req);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(200, obj.StatusCode);
        }

        [Fact]
        public async Task InsertQuizToGroup_Fail_Returns500()
        {
            var req = new InsertQuiz { QuizId = 1, GroupId = 2 };
            _mockRepo.Setup(r => r.InsertQuizToGroup(req, It.IsAny<int>(), It.IsAny<string>()))
                     .ReturnsAsync((InsertQuiz)null);

            var result = await _controller.InsertQuizToGroup(req);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, obj.StatusCode);
        }
        #endregion

        #region POST: JoinGroupByInvite
        [Theory]
        [InlineData(GroupEnum.JoinGroupResult.Success, 200)]
        [InlineData(GroupEnum.JoinGroupResult.AlreadyInGroup, 400)]
        [InlineData(GroupEnum.JoinGroupResult.Fail, 404)]
        [InlineData(GroupEnum.JoinGroupResult.Error, 500)]
        public async Task JoinGroupByInvite_ResultCodes(GroupEnum.JoinGroupResult repoResult, int expectedStatus)
        {
            _mockRepo.Setup(r => r.JoinGroupByInvite(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                     .ReturnsAsync(repoResult);

            var result = await _controller.JoinGroupByInvite("INV", 10);

            var obj = Assert.IsType<ObjectResult>(result);
            Assert.Equal(expectedStatus, obj.StatusCode);
        }
        #endregion

        #region PUT: UpdateGroup
        [Fact]
        public async Task UpdateGroup_NullRequest_ReturnsBadRequest()
        {
            var result = await _controller.UpdateGroup(null);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateGroup_NotFound_ReturnsNotFound()
        {
            var req = new UpdateGroupDTO { GroupId = 5, GroupName = "X" };
            _mockRepo.Setup(r => r.updateGroup(req, It.IsAny<int>(), It.IsAny<string>()))
                     .ReturnsAsync((UpdateGroupDTO)null);

            var result = await _controller.UpdateGroup(req);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateGroup_Success_ReturnsOk()
        {
            var req = new UpdateGroupDTO { GroupId = 5, GroupName = "X" };
            _mockRepo.Setup(r => r.updateGroup(req, It.IsAny<int>(), It.IsAny<string>()))
                     .ReturnsAsync(req);

            var result = await _controller.UpdateGroup(req);

            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<UpdateGroupDTO>(ok.Value);
            Assert.Equal(5, dto.GroupId);
        }
        #endregion

        #region DELETE: DeleteGroup
        [Fact]
        public async Task DeleteGroup_Success_ReturnsOk()
        {
            _mockRepo.Setup(r => r.DeleteGroup(8, It.IsAny<int>(), It.IsAny<string>()))
                     .ReturnsAsync(true);

            var result = await _controller.DeleteGroup(8);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task DeleteGroup_NotFound_ReturnsNotFound()
        {
            _mockRepo.Setup(r => r.DeleteGroup(8, It.IsAny<int>(), It.IsAny<string>()))
                     .ReturnsAsync(false);

            var result = await _controller.DeleteGroup(8);

            Assert.IsType<NotFoundObjectResult>(result);
        }
        #endregion

        #region DELETE: LeaveGroup / RemoveStudentFromGroup / RemoveQuizFromGroup
        [Fact]
        public async Task LeaveGroup_Success_ReturnsOk()
        {
            _mockRepo.Setup(r => r.LeaveGroup(1, 2, 3, It.IsAny<string>())).ReturnsAsync(true);
            var result = await _controller.LeaveGroup(1, 2, 3);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task LeaveGroup_NotFound_ReturnsNotFound()
        {
            _mockRepo.Setup(r => r.LeaveGroup(1, 2, 3, It.IsAny<string>())).ReturnsAsync(false);
            var result = await _controller.LeaveGroup(1, 2, 3);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task RemoveStudentFromGroup_Success_ReturnsOk()
        {
            _mockRepo.Setup(r => r.RemoveStudentFromGroup(1, 2, 3, It.IsAny<string>())).ReturnsAsync(true);
            var result = await _controller.RemoveStudentFromGroup(1, 2, 3);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task RemoveStudentFromGroup_NotFound_ReturnsNotFound()
        {
            _mockRepo.Setup(r => r.RemoveStudentFromGroup(1, 2, 3, It.IsAny<string>())).ReturnsAsync(false);
            var result = await _controller.RemoveStudentFromGroup(1, 2, 3);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task RemoveQuizFromGroup_Success_ReturnsOk()
        {
            _mockRepo.Setup(r => r.RemoveQuizFromGroup(1, 2, It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(true);
            var result = await _controller.RemoveQuizFromGroup(1, 2);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task RemoveQuizFromGroup_NotFound_ReturnsNotFound()
        {
            _mockRepo.Setup(r => r.RemoveQuizFromGroup(1, 2, It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(false);
            var result = await _controller.RemoveQuizFromGroup(1, 2);
            Assert.IsType<NotFoundObjectResult>(result);
        }
        #endregion
    }
}
