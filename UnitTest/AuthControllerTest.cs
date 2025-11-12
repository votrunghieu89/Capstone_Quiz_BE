using Capstone.Controllers;
using Capstone.DTOs.Auth;
using Capstone.ENUMs;
using Capstone.Repositories;
using Capstone.Services;
using Capstone.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Newtonsoft.Json;

namespace Capstone.UnitTest
{
    public class AuthControllerTest
    {
        private readonly AuthController _authController;
        private readonly Mock<IAuthRepository> _mockAuthRepository;
        private readonly Mock<IRedis> _mockRedis;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IGoogleService> _mockGoogleService;

        public AuthControllerTest()
        {
            _mockAuthRepository = new Mock<IAuthRepository>();
            _mockRedis = new Mock<IRedis>();
            _mockLogger = new Mock<ILogger<AuthController>>();
            _mockEmailService = new Mock<IEmailService>();
            _mockGoogleService = new Mock<IGoogleService>();

            _authController = new AuthController(
                _mockAuthRepository.Object,
                _mockRedis.Object,
                _mockLogger.Object,
                _mockEmailService.Object,
                _mockGoogleService.Object
            );

            // Setup HttpContext for IP address
            _authController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        #region GenerateOTP Tests
        [Fact]
        public void GenerateOTP_DefaultLength_ReturnsSixDigits()
        {
            // Act
            var otp = AuthController.GenerateOTP();

            // Assert
            Assert.NotNull(otp);
            Assert.Equal(6, otp.Length);
            Assert.True(int.TryParse(otp, out _));
        }

        [Fact]
        public void GenerateOTP_CustomLength_ReturnsCorrectLength()
        {
            // Arrange
            var length = 8;

            // Act
            var otp = AuthController.GenerateOTP(length);

            // Assert
            Assert.NotNull(otp);
            Assert.Equal(length, otp.Length);
            Assert.True(int.TryParse(otp, out _));
        }

        [Fact]
        public void GenerateOTP_InvalidLength_ThrowsException()
        {
            // Arrange
            var length = -1;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => AuthController.GenerateOTP(length));
        }
        #endregion

        #region SendOTPTeacher Tests
        [Fact]
        public async Task SendOTPTeacher_ValidData_ReturnsOk()
        {
            // Arrange
            var registerDto = new AuthRegisterTeacherDTO
            {
                Email = "teacher@example.com",
                PasswordHash = "password123",
                FullName = "Test Teacher",
                OrganizationName = "Test School",
                OrganizationAddress = "123 Test St"
            };

            _mockAuthRepository.Setup(r => r.isEmailExist(registerDto.Email))
                .ReturnsAsync(0);
            _mockRedis.Setup(r => r.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(true);
            _mockEmailService.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _authController.SendOTPTeacher(registerDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _mockEmailService.Verify(e => e.SendEmailAsync(registerDto.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task SendOTPTeacher_EmailExists_ReturnsBadRequest()
        {
            // Arrange
            var registerDto = new AuthRegisterTeacherDTO
            {
                Email = "existing@example.com",
                PasswordHash = "password123",
                FullName = "Test Teacher",
                OrganizationName = "Test School",
                OrganizationAddress = "123 Test St"
            };

            _mockAuthRepository.Setup(r => r.isEmailExist(registerDto.Email))
                .ReturnsAsync(123);

            // Act
            var result = await _authController.SendOTPTeacher(registerDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task SendOTPTeacher_NullRequest_ReturnsBadRequest()
        {
            // Arrange
            AuthRegisterTeacherDTO registerDto = null;

            // Act
            var result = await _authController.SendOTPTeacher(registerDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task SendOTPTeacher_MissingOrganizationName_ReturnsBadRequest()
        {
            // Arrange
            var registerDto = new AuthRegisterTeacherDTO
            {
                Email = "teacher@example.com",
                PasswordHash = "password123",
                FullName = "Test Teacher",
                OrganizationName = "",
                OrganizationAddress = "123 Test St"
            };

            // Act
            var result = await _authController.SendOTPTeacher(registerDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }
        #endregion

        #region SendOTPStudent Tests
        [Fact]
        public async Task SendOTPStudent_ValidData_ReturnsOk()
        {
            // Arrange
            var registerDto = new AuthRegisterStudentDTO
            {
                Email = "student@example.com",
                PasswordHash = "password123",
                FullName = "Test Student"
            };

            _mockAuthRepository.Setup(r => r.isEmailExist(registerDto.Email))
                .ReturnsAsync(0);
            _mockRedis.Setup(r => r.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(true);
            _mockEmailService.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _authController.SendOTPStudent(registerDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _mockEmailService.Verify(e => e.SendEmailAsync(registerDto.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task SendOTPStudent_EmailExists_ReturnsBadRequest()
        {
            // Arrange
            var registerDto = new AuthRegisterStudentDTO
            {
                Email = "existing@example.com",
                PasswordHash = "password123",
                FullName = "Test Student"
            };

            _mockAuthRepository.Setup(r => r.isEmailExist(registerDto.Email))
                .ReturnsAsync(123);

            // Act
            var result = await _authController.SendOTPStudent(registerDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task SendOTPStudent_NullRequest_ReturnsBadRequest()
        {
            // Arrange
            AuthRegisterStudentDTO registerDto = null;

            // Act
            var result = await _authController.SendOTPStudent(registerDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task SendOTPStudent_EmptyFullName_ReturnsBadRequest()
        {
            // Arrange
            var registerDto = new AuthRegisterStudentDTO
            {
                Email = "student@example.com",
                PasswordHash = "password123",
                FullName = ""
            };

            // Act
            var result = await _authController.SendOTPStudent(registerDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }
        #endregion

        #region CheckEmail Tests
        [Fact]
        public async Task CheckEmail_ValidEmail_ReturnsOkWithAccountId()
        {
            // Arrange
            var email = "test@example.com";
            var accountId = 123;

            _mockAuthRepository.Setup(r => r.isEmailExist(email))
                .ReturnsAsync(accountId);
            _mockRedis.Setup(r => r.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(true);
            _mockEmailService.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _authController.isEmailExist(email);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _mockEmailService.Verify(e => e.SendEmailAsync(email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task CheckEmail_EmailNotFound_ReturnsNotFound()
        {
            // Arrange
            var email = "notfound@example.com";

            _mockAuthRepository.Setup(r => r.isEmailExist(email))
                .ReturnsAsync(0);

            // Act
            var result = await _authController.isEmailExist(email);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.NotNull(notFoundResult.Value);
        }

        [Fact]
        public async Task CheckEmail_EmptyEmail_ReturnsBadRequest()
        {
            // Arrange
            var email = "";

            // Act
            var result = await _authController.isEmailExist(email);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CheckEmail_NullEmail_ReturnsBadRequest()
        {
            // Arrange
            string email = null;

            // Act
            var result = await _authController.isEmailExist(email);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }
        #endregion

        #region VerifyOTP Tests
        [Fact]
        public async Task VerifyOTP_ValidOTP_ReturnsOk()
        {
            // Arrange
            var verifyOTP = new VerifyOTP
            {
                Email = "test@example.com",
                OTP = "123456"
            };

            _mockAuthRepository.Setup(r => r.verifyOTP(verifyOTP.Email, verifyOTP.OTP))
                .ReturnsAsync(true);

            // Act
            var result = await _authController.verifyOTP(verifyOTP);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task VerifyOTP_InvalidOTP_ReturnsBadRequest()
        {
            // Arrange
            var verifyOTP = new VerifyOTP
            {
                Email = "test@example.com",
                OTP = "000000"
            };

            _mockAuthRepository.Setup(r => r.verifyOTP(verifyOTP.Email, verifyOTP.OTP))
                .ReturnsAsync(false);

            // Act
            var result = await _authController.verifyOTP(verifyOTP);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task VerifyOTP_NullRequest_ReturnsBadRequest()
        {
            // Arrange
            VerifyOTP verifyOTP = null;

            // Act
            var result = await _authController.verifyOTP(verifyOTP);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task VerifyOTP_EmptyOTP_ReturnsBadRequest()
        {
            // Arrange
            var verifyOTP = new VerifyOTP
            {
                Email = "test@example.com",
                OTP = ""
            };

            // Act
            var result = await _authController.verifyOTP(verifyOTP);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }
        #endregion

        #region ResetPassword Tests
        [Fact]
        public async Task ResetPassword_ValidData_ReturnsOk()
        {
            // Arrange
            var resetPasswordDto = new ResetPasswordDTO
            {
                accountId = 123,
                PasswordReset = "newPassword123"
            };

            _mockAuthRepository.Setup(r => r.updateNewPassword(resetPasswordDto.accountId, resetPasswordDto.PasswordReset))
                .ReturnsAsync(true);
            _mockRedis.Setup(r => r.DeleteKeyAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var result = await _authController.resetPassword(resetPasswordDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _mockRedis.Verify(r => r.DeleteKeyAsync($"OTP_{resetPasswordDto.accountId}"), Times.Once);
        }

        [Fact]
        public async Task ResetPassword_NullRequest_ReturnsBadRequest()
        {
            // Arrange
            ResetPasswordDTO resetPasswordDto = null;

            // Act
            var result = await _authController.resetPassword(resetPasswordDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ResetPassword_EmptyPassword_ReturnsBadRequest()
        {
            // Arrange
            var resetPasswordDto = new ResetPasswordDTO
            {
                accountId = 123,
                PasswordReset = ""
            };

            // Act
            var result = await _authController.resetPassword(resetPasswordDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ResetPassword_FailedUpdate_ReturnsBadRequest()
        {
            // Arrange
            var resetPasswordDto = new ResetPasswordDTO
            {
                accountId = 123,
                PasswordReset = "newPassword123"
            };

            _mockAuthRepository.Setup(r => r.updateNewPassword(resetPasswordDto.accountId, resetPasswordDto.PasswordReset))
                .ReturnsAsync(false);

            // Act
            var result = await _authController.resetPassword(resetPasswordDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }
        #endregion

        #region Login Tests
        [Fact]
        public async Task Login_ValidCredentials_ReturnsOk()
        {
            // Arrange
            var loginDto = new AuthLoginDTO("test@example.com", "password123");
            var loginResponse = new AuthLoginResultDTO
            {
                Status = AuthEnum.Login.Success,
                AuthLoginResponse = new AuthLoginResponse
                {
                    AccountId = 123,
                    Email = "test@example.com",
                    Role = "Student",
                    AccesToken = "access-token",
                    RefreshToken = "refresh-token"
                }
            };

            _mockAuthRepository.Setup(r => r.Login(It.IsAny<AuthLoginDTO>(), It.IsAny<string>()))
                .ReturnsAsync(loginResponse);

            // Act
            var result = await _authController.Login(loginDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task Login_WrongCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var loginDto = new AuthLoginDTO("test@example.com", "wrongpassword");
            var loginResponse = new AuthLoginResultDTO
            {
                Status = AuthEnum.Login.WrongEmailOrPassword
            };

            _mockAuthRepository.Setup(r => r.Login(It.IsAny<AuthLoginDTO>(), It.IsAny<string>()))
                .ReturnsAsync(loginResponse);

            // Act
            var result = await _authController.Login(loginDto);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Login_AccountBanned_ReturnsForbidden()
        {
            // Arrange
            var loginDto = new AuthLoginDTO("banned@example.com", "password123");
            var loginResponse = new AuthLoginResultDTO
            {
                Status = AuthEnum.Login.AccountHasBanned
            };

            _mockAuthRepository.Setup(r => r.Login(It.IsAny<AuthLoginDTO>(), It.IsAny<string>()))
                .ReturnsAsync(loginResponse);

            // Act
            var result = await _authController.Login(loginDto);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task Login_NullRequest_ReturnsBadRequest()
        {
            // Arrange
            AuthLoginDTO loginDto = null;

            // Act
            var result = await _authController.Login(loginDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Login_EmptyEmail_ReturnsBadRequest()
        {
            // Arrange
            var loginDto = new AuthLoginDTO("", "password123");

            // Act
            var result = await _authController.Login(loginDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Login_EmptyPassword_ReturnsBadRequest()
        {
            // Arrange
            var loginDto = new AuthLoginDTO("test@example.com", "");

            // Act
            var result = await _authController.Login(loginDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }
        #endregion

        #region RegisterStudent Tests
        [Fact]
        public async Task RegisterStudent_ValidOTP_ReturnsOk()
        {
            // Arrange
            var email = "student@example.com";
            var otp = "123456";
            var registerDto = new AuthRegisterStudentDTO
            {
                Email = email,
                PasswordHash = "password123",
                FullName = "Test Student"
            };

            _mockAuthRepository.Setup(r => r.verifyOTP(email, otp))
                .ReturnsAsync(true);
            _mockRedis.Setup(r => r.GetStringAsync($"Account_{email}"))
                .ReturnsAsync(JsonConvert.SerializeObject(registerDto));
            _mockAuthRepository.Setup(r => r.RegisterStudent(It.IsAny<AuthRegisterStudentDTO>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            _mockRedis.Setup(r => r.DeleteKeyAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var result = await _authController.registerStudent(email, otp);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task RegisterStudent_InvalidOTP_ReturnsBadRequest()
        {
            // Arrange
            var email = "student@example.com";
            var otp = "000000";

            _mockAuthRepository.Setup(r => r.verifyOTP(email, otp))
                .ReturnsAsync(false);

            // Act
            var result = await _authController.registerStudent(email, otp);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task RegisterStudent_NoDataInRedis_ReturnsBadRequest()
        {
            // Arrange
            var email = "student@example.com";
            var otp = "123456";

            _mockAuthRepository.Setup(r => r.verifyOTP(email, otp))
                .ReturnsAsync(true);
            _mockRedis.Setup(r => r.GetStringAsync($"Account_{email}"))
                .ReturnsAsync((string)null);

            // Act
            var result = await _authController.registerStudent(email, otp);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task RegisterStudent_RegistrationFailed_ReturnsBadRequest()
        {
            // Arrange
            var email = "student@example.com";
            var otp = "123456";
            var registerDto = new AuthRegisterStudentDTO
            {
                Email = email,
                PasswordHash = "password123",
                FullName = "Test Student"
            };

            _mockAuthRepository.Setup(r => r.verifyOTP(email, otp))
                .ReturnsAsync(true);
            _mockRedis.Setup(r => r.GetStringAsync($"Account_{email}"))
                .ReturnsAsync(JsonConvert.SerializeObject(registerDto));
            _mockAuthRepository.Setup(r => r.RegisterStudent(It.IsAny<AuthRegisterStudentDTO>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            // Act
            var result = await _authController.registerStudent(email, otp);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }
        #endregion

        #region RegisterTeacher Tests
        [Fact]
        public async Task RegisterTeacher_ValidOTP_ReturnsOk()
        {
            // Arrange
            var email = "teacher@example.com";
            var otp = "123456";
            var registerDto = new AuthRegisterTeacherDTO
            {
                Email = email,
                PasswordHash = "password123",
                FullName = "Test Teacher",
                OrganizationName = "Test School",
                OrganizationAddress = "123 Test St"
            };

            _mockAuthRepository.Setup(r => r.verifyOTP(email, otp))
                .ReturnsAsync(true);
            _mockRedis.Setup(r => r.GetStringAsync($"Account_{email}"))
                .ReturnsAsync(JsonConvert.SerializeObject(registerDto));
            _mockAuthRepository.Setup(r => r.RegisterTeacher(It.IsAny<AuthRegisterTeacherDTO>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            _mockRedis.Setup(r => r.DeleteKeyAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var result = await _authController.RegisterTeacher(email, otp);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task RegisterTeacher_InvalidOTP_ReturnsBadRequest()
        {
            // Arrange
            var email = "teacher@example.com";
            var otp = "000000";

            _mockAuthRepository.Setup(r => r.verifyOTP(email, otp))
                .ReturnsAsync(false);

            // Act
            var result = await _authController.RegisterTeacher(email, otp);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task RegisterTeacher_NoDataInRedis_ReturnsBadRequest()
        {
            // Arrange
            var email = "teacher@example.com";
            var otp = "123456";

            _mockAuthRepository.Setup(r => r.verifyOTP(email, otp))
                .ReturnsAsync(true);
            _mockRedis.Setup(r => r.GetStringAsync($"Account_{email}"))
                .ReturnsAsync((string)null);

            // Act
            var result = await _authController.RegisterTeacher(email, otp);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task RegisterTeacher_RegistrationFailed_ReturnsBadRequest()
        {
            // Arrange
            var email = "teacher@example.com";
            var otp = "123456";
            var registerDto = new AuthRegisterTeacherDTO
            {
                Email = email,
                PasswordHash = "password123",
                FullName = "Test Teacher",
                OrganizationName = "Test School",
                OrganizationAddress = "123 Test St"
            };

            _mockAuthRepository.Setup(r => r.verifyOTP(email, otp))
                .ReturnsAsync(true);
            _mockRedis.Setup(r => r.GetStringAsync($"Account_{email}"))
                .ReturnsAsync(JsonConvert.SerializeObject(registerDto));
            _mockAuthRepository.Setup(r => r.RegisterTeacher(It.IsAny<AuthRegisterTeacherDTO>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            // Act
            var result = await _authController.RegisterTeacher(email, otp);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }
        #endregion

        #region ChangePassword Tests
        [Fact]
        public async Task ChangePassword_ValidData_ReturnsOk()
        {
            // Arrange
            var changePasswordDto = new AuthChangePasswordDTO(
                "test@example.com",
                "oldPassword123",
                "newPassword123"
            );

            _mockAuthRepository.Setup(r => r.ChangePassword(It.IsAny<AuthChangePasswordDTO>()))
                .ReturnsAsync(true);

            // Act
            var result = await _authController.ChangePassword(changePasswordDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task ChangePassword_NullRequest_ReturnsBadRequest()
        {
            // Arrange
            AuthChangePasswordDTO changePasswordDto = null;

            // Act
            var result = await _authController.ChangePassword(changePasswordDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ChangePassword_MissingEmail_ReturnsBadRequest()
        {
            // Arrange
            var changePasswordDto = new AuthChangePasswordDTO(
                "",
                "oldPassword123",
                "newPassword123"
            );

            // Act
            var result = await _authController.ChangePassword(changePasswordDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ChangePassword_FailedChange_ReturnsBadRequest()
        {
            // Arrange
            var changePasswordDto = new AuthChangePasswordDTO(
                "test@example.com",
                "wrongOldPassword",
                "newPassword123"
            );

            _mockAuthRepository.Setup(r => r.ChangePassword(It.IsAny<AuthChangePasswordDTO>()))
                .ReturnsAsync(false);

            // Act
            var result = await _authController.ChangePassword(changePasswordDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }
        #endregion

        #region Logout Tests
        [Fact]
        public async Task Logout_ValidAccountId_ReturnsOk()
        {
            // Arrange
            var accountId = 123;

            _mockAuthRepository.Setup(r => r.Logout(accountId))
                .ReturnsAsync(true);

            // Act
            var result = await _authController.Logout(accountId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task Logout_FailedLogout_ReturnsBadRequest()
        {
            // Arrange
            var accountId = 123;

            _mockAuthRepository.Setup(r => r.Logout(accountId))
                .ReturnsAsync(false);

            // Act
            var result = await _authController.Logout(accountId);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }
        #endregion

        #region GetNewAccessToken Tests
        [Fact]
        public async Task GetNewAccessToken_ValidToken_ReturnsOk()
        {
            // Arrange
            var tokenDto = new GetNewAccessTokenDTO
            {
                AccountId = 123,
                RefreshToken = "valid-refresh-token"
            };

            _mockAuthRepository.Setup(r => r.getNewAccessToken(It.IsAny<GetNewAccessTokenDTO>()))
                .ReturnsAsync("new-access-token");

            // Act
            var result = await _authController.GetNewAccessToken(tokenDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GetNewAccessToken_NullRequest_ReturnsBadRequest()
        {
            // Arrange
            GetNewAccessTokenDTO tokenDto = null;

            // Act
            var result = await _authController.GetNewAccessToken(tokenDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetNewAccessToken_InvalidToken_ReturnsBadRequest()
        {
            // Arrange
            var tokenDto = new GetNewAccessTokenDTO
            {
                AccountId = 123,
                RefreshToken = "invalid-token"
            };

            _mockAuthRepository.Setup(r => r.getNewAccessToken(It.IsAny<GetNewAccessTokenDTO>()))
                .ReturnsAsync((string)null);

            // Act
            var result = await _authController.GetNewAccessToken(tokenDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }
        #endregion

        #region GoogleLoginStudent Tests
        //[Fact]
        //public async Task GoogleLoginStudent_ValidIdToken_ExistingUser_ReturnsOk()
        //{
        //    // Arrange
        //    var googleLoginDto = new GoogleLoginDto
        //    {
        //        IdToken = "valid-id-token"
        //    };

        //    var googleResponse = new GoogleResponse
        //    {
        //        Email = "student@gmail.com",
        //        Name = "Test Student",
        //        AvartarURL = "https://example.com/avatar.jpg"
        //    };

        //    var loginResponse = new AuthLoginResultDTO
        //    {
        //        Status = AuthEnum.Login.Success,
        //        AuthLoginResponse = new AuthLoginResponse
        //        {
        //            AccountId = 123,
        //            Email = "student@gmail.com",
        //            Role = "Student",
        //            AccesToken = "access-token",
        //            RefreshToken = "refresh-token"
        //        }
        //    };

        //    // Set IP address in HttpContext
        //    _authController.ControllerContext.HttpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");

        //    _mockGoogleService.Setup(g => g.checkIdToken(googleLoginDto.IdToken))
        //        .ReturnsAsync(googleResponse);
        //    _mockAuthRepository.Setup(r => r.isEmailExist(googleResponse.Email))
        //        .ReturnsAsync(123);
        //    _mockAuthRepository.Setup(r => r.LoginGoogleforStudent(googleResponse.Email))
        //        .ReturnsAsync(loginResponse);

        //    // Act
        //    var result = await _authController.GoogleLoginStudent(googleLoginDto);

        //    // Assert
        //    var okResult = Assert.IsType<OkObjectResult>(result);
        //    Assert.NotNull(okResult.Value);
        //}

        //[Fact]
        //public async Task GoogleLoginStudent_ValidIdToken_NewUser_RegistersAndReturnsOk()
        //{
        //    // Arrange
        //    var googleLoginDto = new GoogleLoginDto
        //    {
        //        IdToken = "valid-id-token"
        //    };

        //    var googleResponse = new GoogleResponse
        //    {
        //        Email = "newstudent@gmail.com",
        //        Name = "New Student",
        //        AvartarURL = "https://example.com/avatar.jpg"
        //    };

        //    var loginResponse = new AuthLoginResultDTO
        //    {
        //        Status = AuthEnum.Login.Success,
        //        AuthLoginResponse = new AuthLoginResponse
        //        {
        //            AccountId = 456,
        //            Email = "newstudent@gmail.com",
        //            Role = "Student",
        //            AccesToken = "access-token",
        //            RefreshToken = "refresh-token"
        //        }
        //    };

        //    // Set IP address in HttpContext
        //    _authController.ControllerContext.HttpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");

        //    _mockGoogleService.Setup(g => g.checkIdToken(googleLoginDto.IdToken))
        //        .ReturnsAsync(googleResponse);
        //    _mockAuthRepository.Setup(r => r.isEmailExist(googleResponse.Email))
        //        .ReturnsAsync(0);
        //    _mockAuthRepository.Setup(r => r.RegisterStudent(It.IsAny<AuthRegisterStudentDTO>(), It.IsAny<string>()))
        //        .ReturnsAsync(true);
        //    _mockAuthRepository.Setup(r => r.LoginGoogleforStudent(googleResponse.Email))
        //        .ReturnsAsync(loginResponse);

        //    // Act
        //    var result = await _authController.GoogleLoginStudent(googleLoginDto);

        //    // Assert
        //    var okResult = Assert.IsType<OkObjectResult>(result);
        //    Assert.NotNull(okResult.Value);
        //    _mockAuthRepository.Verify(r => r.RegisterStudent(It.IsAny<AuthRegisterStudentDTO>(), It.IsAny<string>()), Times.Once);
        //}

        [Fact]
        public async Task GoogleLoginStudent_NullRequest_ReturnsBadRequest()
        {
            // Arrange
            GoogleLoginDto googleLoginDto = null;

            // Act
            var result = await _authController.GoogleLoginStudent(googleLoginDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GoogleLoginStudent_EmptyIdToken_ReturnsBadRequest()
        {
            // Arrange
            var googleLoginDto = new GoogleLoginDto
            {
                IdToken = ""
            };

            // Act
            var result = await _authController.GoogleLoginStudent(googleLoginDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GoogleLoginStudent_InvalidIdToken_ReturnsBadRequest()
        {
            // Arrange
            var googleLoginDto = new GoogleLoginDto
            {
                IdToken = "invalid-token"
            };

            _mockGoogleService.Setup(g => g.checkIdToken(googleLoginDto.IdToken))
                .ReturnsAsync((GoogleResponse)null);

            // Act
            var result = await _authController.GoogleLoginStudent(googleLoginDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GoogleLoginStudent_NullIpAddress_ReturnsBadRequest()
        {
            // Arrange
            var googleLoginDto = new GoogleLoginDto
            {
                IdToken = "valid-id-token"
            };

            // Không set IP address, để null
            _authController.ControllerContext.HttpContext.Connection.RemoteIpAddress = null;

            // Act
            var result = await _authController.GoogleLoginStudent(googleLoginDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }
        #endregion

        #region GoogleLoginTeacher Tests
        [Fact]
        public async Task GoogleLoginTeacher_ValidIdToken_ExistingUser_ReturnsOk()
        {
            // Arrange
            var googleLoginDto = new GoogleLoginDto
            {
                IdToken = "valid-id-token"
            };

            var googleResponse = new GoogleResponse
            {
                Email = "teacher@gmail.com",
                Name = "Test Teacher",
                AvartarURL = "https://example.com/avatar.jpg"
            };

            var loginResponse = new AuthLoginResultDTO
            {
                Status = AuthEnum.Login.Success,
                AuthLoginResponse = new AuthLoginResponse
                {
                    AccountId = 123,
                    Email = "teacher@gmail.com",
                    Role = "Teacher",
                    AccesToken = "access-token",
                    RefreshToken = "refresh-token"
                }
            };

            // Set IP address in HttpContext
            _authController.ControllerContext.HttpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");

            _mockGoogleService.Setup(g => g.checkIdToken(googleLoginDto.IdToken))
                .ReturnsAsync(googleResponse);
            _mockAuthRepository.Setup(r => r.isEmailExist(googleResponse.Email))
                .ReturnsAsync(123);
            _mockAuthRepository.Setup(r => r.LoginGoogleforTeacher(googleResponse.Email))
                .ReturnsAsync(loginResponse);

            // Act
            var result = await _authController.GoogleLoginTeacher(googleLoginDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task GoogleLoginTeacher_ValidIdToken_NewUser_RegistersAndReturnsOk()
        {
            // Arrange
            var googleLoginDto = new GoogleLoginDto
            {
                IdToken = "valid-id-token"
            };

            var googleResponse = new GoogleResponse
            {
                Email = "newteacher@gmail.com",
                Name = "New Teacher",
                AvartarURL = "https://example.com/avatar.jpg"
            };

            var loginResponse = new AuthLoginResultDTO
            {
                Status = AuthEnum.Login.Success,
                AuthLoginResponse = new AuthLoginResponse
                {
                    AccountId = 456,
                    Email = "newteacher@gmail.com",
                    Role = "Teacher",
                    AccesToken = "access-token",
                    RefreshToken = "refresh-token"
                }
            };

            // Set IP address in HttpContext
            _authController.ControllerContext.HttpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");

            _mockGoogleService.Setup(g => g.checkIdToken(googleLoginDto.IdToken))
                .ReturnsAsync(googleResponse);
            _mockAuthRepository.Setup(r => r.isEmailExist(googleResponse.Email))
                .ReturnsAsync(0);
            _mockAuthRepository.Setup(r => r.RegisterTeacher(It.IsAny<AuthRegisterTeacherDTO>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            _mockAuthRepository.Setup(r => r.LoginGoogleforTeacher(googleResponse.Email))
                .ReturnsAsync(loginResponse);

            // Act
            var result = await _authController.GoogleLoginTeacher(googleLoginDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            _mockAuthRepository.Verify(r => r.RegisterTeacher(It.IsAny<AuthRegisterTeacherDTO>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GoogleLoginTeacher_NullRequest_ReturnsBadRequest()
        {
            // Arrange
            GoogleLoginDto googleLoginDto = null;

            // Act
            var result = await _authController.GoogleLoginTeacher(googleLoginDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GoogleLoginTeacher_InvalidIdToken_ReturnsBadRequest()
        {
            // Arrange
            var googleLoginDto = new GoogleLoginDto
            {
                IdToken = "invalid-token"
            };

            _mockGoogleService.Setup(g => g.checkIdToken(googleLoginDto.IdToken))
                .ReturnsAsync((GoogleResponse)null);

            // Act
            var result = await _authController.GoogleLoginTeacher(googleLoginDto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }
        #endregion

        #region RegisterAdmin Tests
        [Fact]
        public async Task RegisterAdmin_ValidData_ReturnsOk()
        {
            // Arrange
            var registerDto = new AuthRegisterStudentDTO
            {
                Email = "admin@example.com",
                PasswordHash = "password123",
                FullName = "Admin User"
            };

            _mockAuthRepository.Setup(r => r.RegisterAccountAdmin(It.IsAny<AuthRegisterStudentDTO>()))
                .ReturnsAsync(true);

            // Act
            var result = await _authController.RegisterAdmin(registerDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task RegisterAdmin_RegistrationFailed_ReturnsInternalServerError()
        {
            // Arrange
            var registerDto = new AuthRegisterStudentDTO
            {
                Email = "admin@example.com",
                PasswordHash = "password123",
                FullName = "Admin User"
            };

            _mockAuthRepository.Setup(r => r.RegisterAccountAdmin(It.IsAny<AuthRegisterStudentDTO>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _authController.RegisterAdmin(registerDto);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }
        #endregion
    }
}