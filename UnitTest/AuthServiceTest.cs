using Azure.Core;
using Capstone.Database;
using Capstone.DTOs.Auth;
using Capstone.ENUMs;
using Capstone.Model;
using Capstone.RabbitMQ;
using Capstone.Security;
using Capstone.Services;
using Castle.Components.DictionaryAdapter.Xml;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using static System.Net.WebRequestMethods;

namespace Capstone.UnitTest
{
    public class AuthServiceTest
    {
        private readonly AuthService _authService;
        private readonly AppDbContext _context;
        private readonly Mock<IToken> _mockToken;
        private readonly Mock<IRedis> _mockRedis;
        private readonly Mock<IRabbitMQProducer> _mockRabbitMQ;

        public AuthServiceTest()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB per test
                .Options;
            _context = new AppDbContext(options);

            var mockLogger = new Mock<ILogger<AuthService>>();
            _mockToken = new Mock<IToken>();
            _mockRedis = new Mock<IRedis>();
            _mockRabbitMQ = new Mock<IRabbitMQProducer>();

            // Setup RabbitMQ mock để không throw exception
            _mockRabbitMQ.Setup(r => r.SendMessageAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var mockConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> 
                { 
                    { "ConnectionStrings:DefaultConnection", "Fake" } 
                })
                .Build();

            _authService = new AuthService(_context, mockConfig, mockLogger.Object, _mockToken.Object, _mockRedis.Object, _mockRabbitMQ.Object);
        }

        #region isEmailExist Tests
        [Fact]
        public async Task TestIsEmailExist_EmailExists_ReturnsAccountId()
        {
            // Arrange
            var testEmail = "existing1@example.com";
            var expectedAccountId = 123;

            var existingAccount = new AuthModel
            {
                AccountId = expectedAccountId,
                Email = testEmail,
                PasswordHash = "hashedPassword",
                Role = "Student",
                IsActive = true,
                CreateAt = DateTime.Now
            };

            await _context.authModels.AddAsync(existingAccount);
            await _context.SaveChangesAsync();

            // Act
            var result = await _authService.isEmailExist(testEmail);

            // Assert
            Assert.Equal(expectedAccountId, result);
        }

        [Fact]
        public async Task TestIsEmailExist_EmailNotExists_ReturnsZero()
        {
            // Arrange
            var testEmail = "nonexistent@example.com";

            // Act
            var result = await _authService.isEmailExist(testEmail);

            // Assert
            Assert.Equal(0, result);
        }
        #endregion

        #region RegisterStudent Tests
        [Fact]
        public async Task TestRegisterStudent_ValidData_ReturnsTrue()
        {
            try
            {
                var registerDto = new AuthRegisterStudentDTO
                {
                    Email = "student@example.com",
                    PasswordHash = "password123",
                    FullName = "Test Student"
                };

                var result = await _authService.RegisterStudent(registerDto, "127.0.0.1");
                Assert.True(result);
            }
            catch (Exception ex)
            {
                // Set breakpoint here to see the actual error
                throw;
            }
        }

        [Fact]
        public async Task TestRegisterStudent_EmailAlreadyExists_ReturnsFalse()
        {
            // Arrange
            var existingEmail = "duplicate@example.com";
            var existingAccount = new AuthModel
            {
                Email = existingEmail,
                PasswordHash = Hash.HashPassword("oldpassword"),
                Role = "Student",
                IsActive = true,
                CreateAt = DateTime.Now
            };
            await _context.authModels.AddAsync(existingAccount);
            await _context.SaveChangesAsync();

            var registerDto = new AuthRegisterStudentDTO
            {
                Email = existingEmail,
                PasswordHash = "newpassword",
                FullName = "New Student"
            };

            // Act & Assert - Should throw exception due to duplicate email
            var result = await _authService.RegisterStudent(registerDto, "127.0.0.1");
            Assert.False(result);
        }

        [Fact]
        public async Task TestRegisterStudent_NullData_ReturnsFalse()
        {
            // Arrange
            AuthRegisterStudentDTO registerDto = null;

            // Act
            var result = await _authService.RegisterStudent(registerDto, "127.0.0.1");

            // Assert
            Assert.False(result);
        }
        #endregion

        #region RegisterTeacher Tests
        [Fact]
        public async Task TestRegisterTeacher_ValidData_ReturnsTrue()
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

            // Act
            var result = await _authService.RegisterTeacher(registerDto, "127.0.0.1");

            // Assert
            Assert.True(result);

            var savedAuth = await _context.authModels.FirstOrDefaultAsync(a => a.Email == registerDto.Email);
            Assert.NotNull(savedAuth);
            Assert.Equal("Teacher", savedAuth.Role);
            Assert.True(savedAuth.IsActive);

            var savedProfile = await _context.teacherProfiles.FirstOrDefaultAsync(p => p.TeacherId == savedAuth.AccountId);
            Assert.NotNull(savedProfile);
            Assert.Equal(registerDto.FullName, savedProfile.FullName);
            Assert.Equal(registerDto.OrganizationName, savedProfile.OrganizationName);

            var defaultFolder = await _context.quizzFolders.FirstOrDefaultAsync(f => f.TeacherId == savedAuth.AccountId);
            Assert.NotNull(defaultFolder);
            Assert.Equal("Default", defaultFolder.FolderName);
        }

        [Fact]
        public async Task TestRegisterTeacher_EmailAlreadyExists_ReturnsFalse()
        {
            // Arrange
            var existingEmail = "duplicate.teacher@example.com";
            var existingAccount = new AuthModel
            {
                Email = existingEmail,
                PasswordHash = Hash.HashPassword("oldpassword"),
                Role = "Teacher",
                IsActive = true,
                CreateAt = DateTime.Now
            };
            await _context.authModels.AddAsync(existingAccount);
            await _context.SaveChangesAsync();

            var registerDto = new AuthRegisterTeacherDTO
            {
                Email = existingEmail,
                PasswordHash = "newpassword",
                FullName = "New Teacher"
            };

            // Act
            var result = await _authService.RegisterTeacher(registerDto, "127.0.0.1");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task TestRegisterTeacher_NullData_ReturnsFalse()
        {
            // Arrange
            AuthRegisterTeacherDTO registerDto = null;

            // Act
            var result = await _authService.RegisterTeacher(registerDto, "127.0.0.1");

            // Assert
            Assert.False(result);
        }
        #endregion

        #region Login Tests
        [Fact]
        public async Task TestLogin_ValidCredentials_ReturnsSuccess()
        {
            // Arrange
            var email = "user@example.com";
            var password = "password123";
            var hashedPassword = Hash.HashPassword(password);

            var user = new AuthModel
            {
                Email = email,
                PasswordHash = hashedPassword,
                Role = "Student",
                IsActive = true,
                CreateAt = DateTime.Now
            };
            await _context.authModels.AddAsync(user);
            await _context.SaveChangesAsync();

            _mockToken.Setup(t => t.generateAccessToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns("fake-access-token");
            _mockToken.Setup(t => t.generateRefreshToken())
                .Returns("fake-refresh-token");
            _mockRedis.Setup(r => r.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(true);

            var loginDto = new AuthLoginDTO(email, password);

            // Act
            var result = await _authService.Login(loginDto, "127.0.0.1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(AuthEnum.Login.Success, result.Status);
            Assert.NotNull(result.AuthLoginResponse);
            Assert.Equal(email, result.AuthLoginResponse.Email);
            Assert.Equal("fake-access-token", result.AuthLoginResponse.AccesToken);
        }

        [Fact]
        public async Task TestLogin_WrongPassword_ReturnsWrongEmailOrPassword()
        {
            // Arrange
            var email = "user@example.com";
            var correctPassword = "correctpassword";
            var wrongPassword = "wrongpassword";

            var user = new AuthModel
            {
                Email = email,
                PasswordHash = Hash.HashPassword(correctPassword),
                Role = "Student",
                IsActive = true,
                CreateAt = DateTime.Now
            };
            await _context.authModels.AddAsync(user);
            await _context.SaveChangesAsync();

            var loginDto = new AuthLoginDTO(email, wrongPassword);

            // Act
            var result = await _authService.Login(loginDto, "127.0.0.1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(AuthEnum.Login.WrongEmailOrPassword, result.Status);
            Assert.Null(result.AuthLoginResponse);
        }

        [Fact]
        public async Task TestLogin_EmailNotExists_ReturnsWrongEmailOrPassword()
        {
            // Arrange
            var loginDto = new AuthLoginDTO("nonexistent@example.com", "password");

            // Act
            var result = await _authService.Login(loginDto, "127.0.0.1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(AuthEnum.Login.WrongEmailOrPassword, result.Status);
            Assert.Null(result.AuthLoginResponse);
        }

        [Fact]
        public async Task TestLogin_AccountBanned_ReturnsAccountHasBanned()
        {
            // Arrange
            var email = "banned@example.com";
            var password = "password123";

            var user = new AuthModel
            {
                Email = email,
                PasswordHash = Hash.HashPassword(password),
                Role = "Student",
                IsActive = false, // Account is banned
                CreateAt = DateTime.Now
            };
            await _context.authModels.AddAsync(user);
            await _context.SaveChangesAsync();

            var loginDto = new AuthLoginDTO(email, password);

            // Act
            var result = await _authService.Login(loginDto, "127.0.0.1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(AuthEnum.Login.AccountHasBanned, result.Status);
            Assert.Null(result.AuthLoginResponse);
        }
        #endregion

        #region ChangePassword Tests
        [Fact]
        public async Task TestChangePassword_ValidOldPassword_ReturnsTrue()
        {
            // Arrange
            var email = "user@example.com";
            var oldPassword = "oldpassword";
            var newPassword = "newpassword";

            var user = new AuthModel
            {
                Email = email,
                PasswordHash = Hash.HashPassword(oldPassword),
                Role = "Student",
                IsActive = true,
                CreateAt = DateTime.Now
            };
            await _context.authModels.AddAsync(user);
            await _context.SaveChangesAsync();

            var changePasswordDto = new AuthChangePasswordDTO(email, oldPassword, newPassword);

            // Act
            var result = await _authService.ChangePassword(changePasswordDto);

            // Assert
            Assert.True(result);

            var updatedUser = await _context.authModels.FirstOrDefaultAsync(u => u.Email == email);
            Assert.True(Hash.VerifyPassword(newPassword, updatedUser.PasswordHash));
        }

        [Fact]
        public async Task TestChangePassword_WrongOldPassword_ReturnsFalse()
        {
            // Arrange
            var email = "user@example.com";
            var oldPassword = "correctoldpassword";
            var wrongOldPassword = "wrongoldpassword";
            var newPassword = "newpassword";

            var user = new AuthModel
            {
                Email = email,
                PasswordHash = Hash.HashPassword(oldPassword),
                Role = "Student",
                IsActive = true,
                CreateAt = DateTime.Now
            };
            await _context.authModels.AddAsync(user);
            await _context.SaveChangesAsync();

            var changePasswordDto = new AuthChangePasswordDTO(email, wrongOldPassword, newPassword);

            // Act
            var result = await _authService.ChangePassword(changePasswordDto);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task TestChangePassword_EmailNotExists_ReturnsFalse()
        {
            // Arrange
            var changePasswordDto = new AuthChangePasswordDTO("nonexistent@example.com", "oldpass", "newpass");

            // Act
            var result = await _authService.ChangePassword(changePasswordDto);

            // Assert
            Assert.False(result);
        }
        #endregion

        #region Logout Tests
        [Fact]
        public async Task TestLogout_ValidAccountId_ReturnsTrue()
        {
            // Arrange
            var accountId = 123;
            _mockRedis.Setup(r => r.DeleteKeyAsync($"RefressToken_{accountId}"))
                .ReturnsAsync(true);
            _mockRedis.Setup(r => r.DeleteKeyAsync($"Online_{accountId}"))
                .ReturnsAsync(true);

            // Act
            var result = await _authService.Logout(accountId);

            // Assert
            Assert.True(result);
            _mockRedis.Verify(r => r.DeleteKeyAsync($"RefressToken_{accountId}"), Times.Once);
        }

        [Fact]
        public async Task TestLogout_NoRefreshToken_ReturnsFalse()
        {
            // Arrange
            var accountId = 999;
            _mockRedis.Setup(r => r.DeleteKeyAsync($"RefressToken_{accountId}"))
                .ReturnsAsync(false);

            // Act
            var result = await _authService.Logout(accountId);

            // Assert
            Assert.False(result);
        }
        #endregion

        #region verifyOTP Tests
        [Fact]
        public async Task TestVerifyOTP_ValidOTP_ReturnsTrue()
        {
            // Arrange
            var email = "user@example.com";
            var otp = "123456";
            var hashedOtp = Hash.HashPassword(otp);

            _mockRedis.Setup(r => r.GetStringAsync($"OTP_{email}"))
                .ReturnsAsync(hashedOtp);
            _mockRedis.Setup(r => r.DeleteKeyAsync($"OTP_{email}"))
                .ReturnsAsync(true);

            // Act
            var result = await _authService.verifyOTP(email, otp);

            // Assert
            Assert.True(result);
            _mockRedis.Verify(r => r.DeleteKeyAsync($"OTP_{email}"), Times.Once);
        }

        [Fact]
        public async Task TestVerifyOTP_WrongOTP_ReturnsFalse()
        {
            // Arrange
            var email = "user@example.com";
            var correctOtp = "123456";
            var wrongOtp = "654321";
            var hashedOtp = Hash.HashPassword(correctOtp);

            _mockRedis.Setup(r => r.GetStringAsync($"OTP_{email}"))
                .ReturnsAsync(hashedOtp);

            // Act
            var result = await _authService.verifyOTP(email, wrongOtp);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task TestVerifyOTP_EmailNotExists_ReturnsFalse()
        {
            // Arrange
            var email = "nonexistent@example.com";
            _mockRedis.Setup(r => r.GetStringAsync($"OTP_{email}"))
                .ReturnsAsync((string)null);

            // Act
            var result = await _authService.verifyOTP(email, "123456");

            // Assert
            Assert.False(result);
        }
        #endregion

        #region updateNewPassword Tests
        [Fact]
        public async Task TestUpdateNewPassword_ValidAccountId_ReturnsTrue()
        {
            // Arrange
            var accountId = 123;
            var newPassword = "newpassword123";

            var user = new AuthModel
            {
                AccountId = accountId,
                Email = "user@example.com",
                PasswordHash = Hash.HashPassword("oldpassword"),
                Role = "Student",
                IsActive = true,
                CreateAt = DateTime.Now
            };
            await _context.authModels.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _authService.updateNewPassword(accountId, newPassword);

            // Assert
            Assert.True(result);

            var updatedUser = await _context.authModels.FirstOrDefaultAsync(u => u.AccountId == accountId);
            Assert.True(Hash.VerifyPassword(newPassword, updatedUser.PasswordHash));
        }

        [Fact]
        public async Task TestUpdateNewPassword_AccountNotExists_ReturnsFalse()
        {
            // Arrange
            var accountId = 999;
            var newPassword = "newpassword123";

            // Act
            var result = await _authService.updateNewPassword(accountId, newPassword);

            // Assert
            Assert.False(result);
        }
        #endregion

        #region getNewAccessToken Tests
        [Fact]
        public async Task TestGetNewAccessToken_ValidRefreshToken_ReturnsNewToken()
        {
            // Arrange
            var accountId = 123;
            var refreshToken = "valid-refresh-token";

            var user = new AuthModel
            {
                AccountId = accountId,
                Email = "user@example.com",
                PasswordHash = "hashedpassword",
                Role = "Student",
                IsActive = true,
                CreateAt = DateTime.Now
            };
            await _context.authModels.AddAsync(user);
            await _context.SaveChangesAsync();

            _mockRedis.Setup(r => r.GetStringAsync($"RefressToken_{accountId}"))
                .ReturnsAsync(refreshToken);
            _mockToken.Setup(t => t.generateAccessToken(accountId, user.Role, user.Email))
                .Returns("new-access-token");

            var tokenDto = new GetNewAccessTokenDTO { AccountId = accountId, RefreshToken = refreshToken };

            // Act
            var result = await _authService.getNewAccessToken(tokenDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("new-access-token", result);
        }

        [Fact]
        public async Task TestGetNewAccessToken_ExpiredRefreshToken_ReturnsNull()
        {
            // Arrange
            var accountId = 123;
            var refreshToken = "expired-token";

            _mockRedis.Setup(r => r.GetStringAsync($"RefressToken_{accountId}"))
                .ReturnsAsync((string)null);

            var tokenDto = new GetNewAccessTokenDTO { AccountId = accountId, RefreshToken = refreshToken };

            // Act
            var result = await _authService.getNewAccessToken(tokenDto);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task TestGetNewAccessToken_InvalidRefreshToken_ReturnsNull()
        {
            // Arrange
            var accountId = 123;
            var validRefreshToken = "valid-token";
            var invalidRefreshToken = "invalid-token";

            _mockRedis.Setup(r => r.GetStringAsync($"RefressToken_{accountId}"))
                .ReturnsAsync(validRefreshToken);

            var tokenDto = new GetNewAccessTokenDTO { AccountId = accountId, RefreshToken = invalidRefreshToken };

            // Act
            var result = await _authService.getNewAccessToken(tokenDto);

            // Assert
            Assert.Null(result);
        }
        #endregion

        #region LoginGoogleforStudent Tests
        [Fact]
        public async Task TestLoginGoogleforStudent_ValidEmail_ReturnsSuccess()
        {
            // Arrange
            var email = "student@gmail.com";

            var user = new AuthModel
            {
                Email = email,
                PasswordHash = "hashedpassword",
                Role = "Student",
                IsActive = true,
                CreateAt = DateTime.Now
            };
            await _context.authModels.AddAsync(user);
            await _context.SaveChangesAsync();

            _mockToken.Setup(t => t.generateAccessToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns("fake-access-token");
            _mockToken.Setup(t => t.generateRefreshToken())
                .Returns("fake-refresh-token");
            _mockRedis.Setup(r => r.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(true);

            // Act
            var result = await _authService.LoginGoogleforStudent(email);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(AuthEnum.Login.Success, result.Status);
            Assert.NotNull(result.AuthLoginResponse);
            Assert.Equal(email, result.AuthLoginResponse.Email);
        }

        [Fact]
        public async Task TestLoginGoogleforStudent_EmailNotRegistered_ReturnsWrongEmailOrPassword()
        {
            // Arrange
            var email = "unregistered@gmail.com";

            // Act
            var result = await _authService.LoginGoogleforStudent(email);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(AuthEnum.Login.WrongEmailOrPassword, result.Status);
            Assert.Null(result.AuthLoginResponse);
        }
        #endregion

        #region LoginGoogleforTeacher Tests
        [Fact]
        public async Task TestLoginGoogleforTeacher_ValidEmail_ReturnsSuccess()
        {
            // Arrange
            var email = "teacher@gmail.com";

            var user = new AuthModel
            {
                Email = email,
                PasswordHash = "hashedpassword",
                Role = "Teacher",
                IsActive = true,
                CreateAt = DateTime.Now
            };
            await _context.authModels.AddAsync(user);
            await _context.SaveChangesAsync();

            _mockToken.Setup(t => t.generateAccessToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns("fake-access-token");
            _mockToken.Setup(t => t.generateRefreshToken())
                .Returns("fake-refresh-token");
            _mockRedis.Setup(r => r.SetStringAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(true);

            // Act
            var result = await _authService.LoginGoogleforTeacher(email);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(AuthEnum.Login.Success, result.Status);
            Assert.NotNull(result.AuthLoginResponse);
            Assert.Equal(email, result.AuthLoginResponse.Email);
        }

        [Fact]
        public async Task TestLoginGoogleforTeacher_EmailNotRegistered_ReturnsWrongEmailOrPassword()
        {
            // Arrange
            var email = "unregistered@gmail.com";

            // Act
            var result = await _authService.LoginGoogleforTeacher(email);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(AuthEnum.Login.WrongEmailOrPassword, result.Status);
            Assert.Null(result.AuthLoginResponse);
        }
        #endregion
    }
}
