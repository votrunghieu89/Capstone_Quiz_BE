using Capstone.Database;
using Capstone.DTOs.Auth;
using Capstone.Model.Others;
using Capstone.Model.Profile;
using Capstone.Repositories;
using Capstone.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
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
        // Kiểm tra kết nối cơ sở dữ liệu
        public async Task<bool> checkConnection()
        {
            try
            {
                bool isConn = await _context.Database.CanConnectAsync();
                return isConn;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking database connection");
                return false;
            }
        }
        public async Task<int> isEmailExist(string email)
        {
            try
            {
                var isEmail = await _context.authModels.FirstOrDefaultAsync(u => u.Email == email);
                if (isEmail != null)
                {
                    // Email tồn tại
                    _logger.LogInformation("Email already exists");
                    return isEmail.AccountId;
                }
                // Email chưa tồn tại
                _logger.LogInformation("Email does not exist");
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if email exists");
                return 0;
            }
        }
        public async Task<bool> RegisterCandidate(AuthRegisterDTO authRegisterDTO)
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
                            Password = Hash.HashPassword(authRegisterDTO.Password),
                            Role = "Candidate"
                        };

                        await _context.authModels.AddAsync(authModel);
                        await _context.SaveChangesAsync();

                        ProfileCandidateModel profile_CDD_Admin = new ProfileCandidateModel
                        {
                            AccountId = authModel.AccountId,
                            FullName = authRegisterDTO.FullName

                        };
                        await _context.profileCandidates.AddAsync(profile_CDD_Admin);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        _logger.LogInformation("Candidate registered successfully with AccountId {AccountId}", authModel.AccountId);
                        return true;
                    }
                    catch (Exception)
                    {
                        _logger.LogError("Error registering candidate, rolling back transaction");
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user");
                return false;
            }
        }
        public async Task<bool> RegisterRecruiter(AuthRegisterRecruiterDTO authRegisterDTO)
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
                            Password = Hash.HashPassword(authRegisterDTO.Password),
                            Role = "Recruiter"
                        };

                        await _context.authModels.AddAsync(authModel);
                        await _context.SaveChangesAsync();

                        ProfileCompanyModel profile_Recruiter = new ProfileCompanyModel
                        {
                            AccountId = authModel.AccountId,
                            CompanyName = authRegisterDTO.CompanyName,
                            CompanyAddress = authRegisterDTO.CompanyAddress
                        };
                        await _context.profileCompanies.AddAsync(profile_Recruiter);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        _logger.LogInformation("Recruiter registered successfully with AccountId {AccountId}", authModel.AccountId);
                        return true;
                    }
                    catch (Exception)
                    {
                        _logger.LogError("Error registering Recruiter, rolling back transaction");
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user");
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
                    _logger.LogWarning("Login failed for email");
                    return null;
                }
                bool checkPassword = Hash.VerifyPassword(authLoginDTO.Password, user.Password);
                if (!checkPassword)
                {
                    _logger.LogWarning("Invalid password for email ");
                    return null;
                }
                var accessToken = _token.generateAccessToken(user.AccountId, user.Role, user.Email);
                var refreshToken = _token.generateRefreshToken();
                bool setRefresh = await _redis.SetStringAsync($"RT_{user.AccountId}", refreshToken, TimeSpan.FromDays(7));
                AuthLoginResponse response = new AuthLoginResponse
                {
                    AccountId = user.AccountId,
                    Email = user.Email,
                    Role = user.Role,
                    AccesToken = accessToken,
                    RefreshToken = refreshToken,

                };
                bool  setActive = await _redis.SetStringAsync($"Online_{user.AccountId}", "true", TimeSpan.FromDays(7));
                _logger.LogInformation("User {Email} logged in successfully", authLoginDTO.Email);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return null;
            }
        }
        public async Task<bool> Logout(int accountId)
        {
            bool deleted = await _redis.DeleteKeyAsync($"RT_{accountId}");
            bool deleteOnline = await _redis.DeleteKeyAsync($"Online_{accountId}");
            if (deleted)
            {
                _logger.LogInformation("User with AccountId {AccountId} logged out successfully", accountId);
                return true;
            }
            else
            {
                _logger.LogWarning("No refresh token found to delete for AccountId {AccountId}", accountId);
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
                    _logger.LogWarning("Email not found");
                    return false;
                }
                bool checkOldPassword = Hash.VerifyPassword(changePasswordDTO.oldPassword, user.Password);
                if (!checkOldPassword)
                {
                    _logger.LogWarning("Invalid old password");
                    return false;
                }
                string newHashedPassword = Hash.HashPassword(changePasswordDTO.newPassword);
                int updated = await _context.authModels
                                     .Where(u => u.Email == changePasswordDTO.Email)
                                     .ExecuteUpdateAsync(s => s
                                         .SetProperty(u => u.Password, newHashedPassword)
                                         .SetProperty(u => u.UpdatedAt, DateTime.Now)
                                     );
                _logger.LogInformation("Password changed successfully");
                if (updated > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for email {Email}", changePasswordDTO.Email);
                return false;
            }
        }
        public async Task<bool> verifyOTP(int accountId, string otp)
        {
            string? OTP = await _redis.GetStringAsync("OTP_" + accountId);
            if (OTP == null)
            {
                _logger.LogWarning("OTP expired or not found  ");
                return false;
            }
            bool checkOTP = Hash.VerifyPassword(otp, OTP);

            if (!checkOTP)
            {
                _logger.LogWarning("Invalid OTP for AccountId ");
                return false;
            }
            _logger.LogInformation("OTP verified successfully }");
            bool deleted = await _redis.DeleteKeyAsync($"OTP_{accountId}");
            return true;
        }
        public async Task<bool> updateNewPassword(int accountId, string newPassword)
        {
            try
            {
                string newHashedPassword = Hash.HashPassword(newPassword);
                int updated = await _context.authModels.Where(u => u.AccountId == accountId).ExecuteUpdateAsync(s => s.SetProperty(u => u.Password, newHashedPassword)
                                                                                                                      .SetProperty(u => u.UpdatedAt, DateTime.Now));
                if (updated > 0)
                {
                    _logger.LogInformation("Password updated successfully for AccountId {AccountId}", accountId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("No records updated for AccountId {AccountId}", accountId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking database connection");
                return false;
            }
        }
        public async Task<string> getNewAccessToken(GetNewATDTO tokenDTO)
        {
            try
            {
                string? refreshTokenInDb = await _redis.GetStringAsync($"RT_{tokenDTO.AccountId}");
                if (refreshTokenInDb == null || refreshTokenInDb != tokenDTO.RefreshToken)
                {
                    _logger.LogWarning("Invalid or expired refresh token for AccountId");
                    return null;
                }
                var user = await _context.authModels.FirstOrDefaultAsync(u => u.AccountId == tokenDTO.AccountId);
                if (user == null)
                {
                    _logger.LogWarning("AccountId not found");
                    return null;
                }
                var newAccessToken = _token.generateAccessToken(user.AccountId, user.Role, user.Email);
                return newAccessToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during getNewAccessToken");
                return null;
            }
        }
        public async Task<AuthLoginResponse> LoginGoogle(string email)
        {
            try
            {
                var user = await _context.authModels.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    _logger.LogWarning("Email not found");
                    return null;
                }
                var accessToken = _token.generateAccessToken(user.AccountId, user.Role, user.Email);
                var refreshToken = _token.generateRefreshToken();
                bool setRefresh = await _redis.SetStringAsync($"RT_{user.AccountId}", refreshToken, TimeSpan.FromDays(7));
                bool setActive = await _redis.SetStringAsync($"Online_{user.AccountId}", "true", TimeSpan.FromDays(7));
                AuthLoginResponse response = new AuthLoginResponse
                {
                    AccountId = user.AccountId,
                    Email = user.Email,
                    Role = user.Role,
                    AccesToken = accessToken,
                    RefreshToken = refreshToken,
                };
                _logger.LogInformation("User with email {Email} logged in successfully via Google", email);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google login for email {Email}", email);
                return null;
            }
        }
    }
}
