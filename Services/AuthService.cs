using Capstone.Database;
using Capstone.DTOs.Auth;
using Capstone.ENUMs;
using Capstone.Model;
using Capstone.Repositories;
using Capstone.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using StackExchange.Redis;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Capstone.Services
{
    public class AuthService : IAuthRepository
    {
        public readonly AppDbContext _context;
        public readonly string _connection;
        public readonly ILogger<AuthService> _logger;
        public readonly Token _token;
        private readonly Redis _redis;
        public AuthService(AppDbContext context, IConfiguration configuration, ILogger<AuthService> logger, Token token, Redis redis)
        {
            _context = context;
            _connection = configuration.GetConnectionString("DefaultConnection") ?? "";
            _logger = logger;
            _token = token;
            _redis = redis;
        }
        public static string GenerateIdUnique(int accountId, DateTime createAt)
        {
            string dateCode = createAt.ToString("yyyyMMdd"); // lấy ngày-tháng-năm
            return $"{accountId}{dateCode}"; // ghép thành chuỗi
        }
        public async Task<int> isEmailExist(string email)
        {
            try
            {
                var isEmail = await _context.authModels.FirstOrDefaultAsync(u => u.Email == email);
                if (isEmail != null)
                {
                    _logger.LogInformation("Email '{Email}' already exists (AccountId={AccountId}).", email, isEmail.AccountId);
                    return isEmail.AccountId;
                }
                _logger.LogInformation("Email '{Email}' does not exist in the system.", email);
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if email exists for '{Email}'.", email);
                return 0;
            }
        }
        public async Task<bool> RegisterStudent(AuthRegisterStudentDTO authRegisterDTO)
        {
            try
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        AuthModel authModel = new AuthModel
                        {
                            Email = authRegisterDTO.Email,
                            PasswordHash = Hash.HashPassword(authRegisterDTO.PasswordHash),
                            Role = AuthEnum.Role.Student.ToString(),
                            CreateAt = DateTime.Now
                        };
                        await _context.authModels.AddAsync(authModel);
                        await _context.SaveChangesAsync();

                        string uniqueId = GenerateIdUnique(authModel.AccountId, authModel.CreateAt);
                        StudentProfileModel studentProfile = new StudentProfileModel
                        {
                            StudentId = authModel.AccountId,
                            FullName = authRegisterDTO.FullName,
                            IdUnique = uniqueId,
                        };
                        await _context.studentProfiles.AddAsync(studentProfile);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        _logger.LogInformation("Student registered successfully. AccountId={AccountId}, Email={Email}", authModel.AccountId, authModel.Email);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error registering student for Email={Email}. Rolling back transaction.", authRegisterDTO?.Email);
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error registering student for Email={Email}.", authRegisterDTO?.Email);
                return false;
            }
        }
        public async Task<bool> RegisterTeacher(AuthRegisterTeacherDTO authRegisterDTO)
        {
            try
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        AuthModel authModel = new AuthModel
                        {
                            Email = authRegisterDTO.Email,
                            PasswordHash = Hash.HashPassword(authRegisterDTO.PasswordHash),
                            Role = AuthEnum.Role.Teacher.ToString(),
                            CreateAt = DateTime.Now
                        };

                        await _context.authModels.AddAsync(authModel);
                        await _context.SaveChangesAsync();
                        string uniqueId = GenerateIdUnique(authModel.AccountId, authModel.CreateAt);

                        TeacherProfileModel teacherProfile = new TeacherProfileModel
                        {
                            TeacherId = authModel.AccountId,
                            FullName = authRegisterDTO.FullName,
                            IdUnique = uniqueId,
                            OrganizationName = authRegisterDTO.OrganizationName ?? null,
                            OrganizationAddress = authRegisterDTO.OrganizationAddress ?? null,
                        };
                        await _context.teacherProfiles.AddAsync(teacherProfile);
                        await _context.SaveChangesAsync();

                        QuizzFolderModel defaultFolder = new QuizzFolderModel
                        {
                            TeacherId = authModel.AccountId,
                            FolderName = "Default",
                            ParentFolderId = null,
                            CreateAt = DateTime.Now
                        };
                        // Associate folder with teacher via separate property if needed; currently Folder model doesn't include TeacherId in schema
                        await _context.quizzFolders.AddAsync(defaultFolder);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        _logger.LogInformation("Teacher registered successfully. AccountId={AccountId}, Email={Email}", authModel.AccountId, authModel.Email);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error registering teacher for Email={Email}. Rolling back transaction.", authRegisterDTO?.Email);
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error registering teacher for Email={Email}.", authRegisterDTO?.Email);
                return false;
            }
        }
        public async Task<AuthLoginResponse> Login(AuthLoginDTO authLoginDTO)
        {
            try
            {
                AuthModel? user = await _context.authModels.FirstOrDefaultAsync(u => u.Email == authLoginDTO.Email);
                if (user == null)
                {
                    _logger.LogWarning("Login attempt failed: email not found '{Email}'.", authLoginDTO.Email);
                    return null;
                }
                bool checkPassword = Hash.VerifyPassword(authLoginDTO.Password, user.PasswordHash);
                if (!checkPassword)
                {
                    _logger.LogWarning("Login attempt failed: invalid password for Email='{Email}'.", authLoginDTO.Email);
                    return null;
                }
                var accessToken = _token.generateAccessToken(user.AccountId, user.Role, user.Email);
                var refreshToken = _token.generateRefreshToken();
                bool setRefresh = await _redis.SetStringAsync($"RefressToken_{user.AccountId}", refreshToken, TimeSpan.FromDays(7));
                AuthLoginResponse response = new AuthLoginResponse
                {
                    AccountId = user.AccountId,
                    Email = user.Email,
                    Role = user.Role,
                    AccesToken = accessToken,
                    RefreshToken = refreshToken,

                };
                bool  setActive = await _redis.SetStringAsync($"Online_{user.AccountId}", "true", TimeSpan.FromDays(7));
                _logger.LogInformation("User logged in successfully. AccountId={AccountId}, Email={Email}", user.AccountId, user.Email);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for Email={Email}.", authLoginDTO?.Email);
                return null;
            }
        }
        public async Task<bool> Logout(int accountId)
        {
            try
            {
                bool deleted = await _redis.DeleteKeyAsync($"RefressToken_{accountId}");
                bool deleteOnline = await _redis.DeleteKeyAsync($"Online_{accountId}");
                if (deleted)
                {
                    _logger.LogInformation("User logged out. AccountId={AccountId}", accountId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Logout: no refresh token found for AccountId={AccountId}", accountId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout for AccountId={AccountId}", accountId);
                return false;
            }
        }
        public async Task<bool> ChangePassword(AuthChangePasswordDTO changePasswordDTO)
        {
            try
            {
                var user = await _context.authModels.FirstOrDefaultAsync(u => u.Email == changePasswordDTO.Email);

                if (user == null)
                {
                    _logger.LogWarning("ChangePassword: email not found '{Email}'.", changePasswordDTO.Email);
                    return false;
                }
                bool checkOldPassword = Hash.VerifyPassword(changePasswordDTO.oldPassword, user.PasswordHash);
                if (!checkOldPassword)
                {
                    _logger.LogWarning("ChangePassword: invalid old password for Email='{Email}'.", changePasswordDTO.Email);
                    return false;
                }
                string newHashedPassword = Hash.HashPassword(changePasswordDTO.newPassword);
                int updated = await _context.authModels
                                     .Where(u => u.Email == changePasswordDTO.Email)
                                     .ExecuteUpdateAsync(s => s
                                         .SetProperty(u => u.PasswordHash, newHashedPassword)
                                         .SetProperty(u => u.UpdateAt, DateTime.Now)
                                     );
                if (updated > 0)
                {
                    _logger.LogInformation("Password changed successfully for Email={Email}.", changePasswordDTO.Email);
                    return true;
                }
                else
                {
                    _logger.LogWarning("ChangePassword: no records updated for Email={Email}.", changePasswordDTO.Email);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for Email={Email}.", changePasswordDTO?.Email);
                return false;
            }
        }
        public async Task<bool> verifyOTP(string email, string otp)
        {
            try
            {
                string? OTP = await _redis.GetStringAsync("OTP_" + email);
                if (OTP == null)
                {
                    _logger.LogWarning("verifyOTP: OTP expired or not found for email={email}.", email);
                    return false;
                }
                bool checkOTP = Hash.VerifyPassword(otp, OTP);

                if (!checkOTP)
                {
                    _logger.LogWarning("verifyOTP: invalid OTP for email={email}.", email);
                    return false;
                }
                _logger.LogInformation("OTP verified successfully for email={email}.", email);
                bool deleted = await _redis.DeleteKeyAsync($"OTP_{email}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying OTP for email={email}.", email);
                return false;
            }
        }
        public async Task<bool> updateNewPassword(int accountId, string newPassword)
        {
            try
            {
                string newHashedPassword = Hash.HashPassword(newPassword);
                int updated = await _context.authModels.Where(u => u.AccountId == accountId).ExecuteUpdateAsync(s => s.SetProperty(u => u.PasswordHash, newHashedPassword)
                                                                                                                      .SetProperty(u => u.UpdateAt, DateTime.Now));
                if (updated > 0)
                {
                    _logger.LogInformation("Password updated successfully for AccountId={AccountId}.", accountId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("updateNewPassword: no records updated for AccountId={AccountId}.", accountId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating password for AccountId={AccountId}.", accountId);
                return false;
            }
        }
        public async Task<string> getNewAccessToken(GetNewAccessTokenDTO tokenDTO)
        {
            try
            {
                string? refreshTokenInDb = await _redis.GetStringAsync($"RefressToken_{tokenDTO.AccountId}");
                if (refreshTokenInDb == null || refreshTokenInDb != tokenDTO.RefreshToken)
                {
                    _logger.LogWarning("getNewAccessToken: invalid or expired refresh token for AccountId={AccountId}.", tokenDTO.AccountId);
                    return null;
                }
                var user = await _context.authModels.FirstOrDefaultAsync(u => u.AccountId == tokenDTO.AccountId);
                if (user == null)
                {
                    _logger.LogWarning("getNewAccessToken: AccountId not found {AccountId}.", tokenDTO.AccountId);
                    return null;
                }
                var newAccessToken = _token.generateAccessToken(user.AccountId, user.Role, user.Email);
                _logger.LogInformation("New access token generated for AccountId={AccountId}.", tokenDTO.AccountId);
                return newAccessToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during getNewAccessToken for AccountId={AccountId}.", tokenDTO?.AccountId);
                return null;
            }
        }
        public async Task<AuthLoginResponse> LoginGoogleforStudent(string email)
        {
            try
            {
                var user = await _context.authModels.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    _logger.LogWarning("LoginGoogle: email not found '{Email}'.", email);
                    return null;
                }
                var accessToken = _token.generateAccessToken(user.AccountId, user.Role, user.Email);
                var refreshToken = _token.generateRefreshToken();
                bool setRefresh = await _redis.SetStringAsync($"RefressToken_{user.AccountId}", refreshToken, TimeSpan.FromDays(7));  
                bool setActive = await _redis.SetStringAsync($"Online_{user.AccountId}", "true", TimeSpan.FromDays(7));
                AuthLoginResponse response = new AuthLoginResponse
                {
                    AccountId = user.AccountId,
                    Email = user.Email,
                    Role = user.Role,
                    AccesToken = accessToken,
                    RefreshToken = refreshToken,
                };
                _logger.LogInformation("User logged in via Google. AccountId={AccountId}, Email={Email}", user.AccountId, user.Email);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google login for Email={Email}", email);
                return null;
            }
        }

        public async Task<AuthLoginResponse> LoginGoogleforTeacher(string email)
        {
            try
            {
                var user = await _context.authModels.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    _logger.LogWarning("LoginGoogle: email not found '{Email}'.", email);
                    return null;
                }
                var accessToken = _token.generateAccessToken(user.AccountId, user.Role, user.Email);
                var refreshToken = _token.generateRefreshToken();
                bool setRefresh = await _redis.SetStringAsync($"RefressToken_{user.AccountId}", refreshToken, TimeSpan.FromDays(7));
                bool setActive = await _redis.SetStringAsync($"Online_{user.AccountId}", "true", TimeSpan.FromDays(7));
                AuthLoginResponse response = new AuthLoginResponse
                {
                    AccountId = user.AccountId,
                    Email = user.Email,
                    Role = user.Role,
                    AccesToken = accessToken,
                    RefreshToken = refreshToken,
                };
                _logger.LogInformation("User logged in via Google. AccountId={AccountId}, Email={Email}", user.AccountId, user.Email);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google login for Email={Email}", email);
                return null;
            }
        }


        public async Task<bool> RegisterAccountAdmin(AuthModel adminAccount)
        {
            try
            {
                var newAccount = new AuthModel
                {
                    Email = adminAccount.Email,
                    PasswordHash = Hash.HashPassword(adminAccount.PasswordHash),
                    Role = AuthEnum.Role.Admin.ToString(),
                    CreateAt = DateTime.Now
                };
                await _context.authModels.AddAsync(newAccount);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Admin account registered successfully. AccountId={AccountId}, Email={Email}", newAccount.AccountId, newAccount.Email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering admin account.");
                return false;
            }
        }
    }
}
