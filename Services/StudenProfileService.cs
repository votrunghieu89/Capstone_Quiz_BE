using Capstone.Database;
using Capstone.DTOs.StudentProfile;
using Capstone.Model;
using Capstone.RabbitMQ;
using Capstone.Repositories.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Capstone.Services
{
    // Cần có IStudentProfileRepository trong file này để code chạy
    // Tuy nhiên, dựa trên yêu cầu, tôi chỉ cung cấp phần StudentProfileService được cập nhật.

    public class StudentProfileService : IStudentProfileRepository
    {
        private readonly ILogger<StudentProfileService> _logger;
        private readonly AppDbContext _context;
        private readonly RabbitMQProducer _rabbitMQ;

        public StudentProfileService(ILogger<StudentProfileService> logger, AppDbContext context, RabbitMQProducer rabbitMQ)
        {
            _logger = logger;
            _context = context;
            _rabbitMQ = rabbitMQ;
        }

        public async Task<StudentProfileModel> getStudentProfile(int accountId)
        {
            _logger.LogInformation("getStudentProfile: Start - AccountId={AccountId}", accountId);
            try
            {
                var studentProfile = await _context.studentProfiles.Where(s => s.StudentId == accountId).FirstOrDefaultAsync();
                if (studentProfile == null)
                {
                    _logger.LogWarning("getStudentProfile: Not found - AccountId={AccountId}", accountId);
                    return null;
                }
                _logger.LogInformation("getStudentProfile: Success - AccountId={AccountId}", accountId);
                return studentProfile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "getStudentProfile: Error retrieving profile for AccountId={AccountId}", accountId);
                return null;
            }
        }

        // Đã cập nhật signature để khớp với IStudentProfileRepository (sử dụng string cho IP)
        // và thêm logic Audit Log (RabbitMQ).
        public async Task<StudentProfileResponseDTO> updateStudentProfile(StudentProfileModel studentProfile, int accountId, string ipAddress)
        {
            _logger.LogInformation("updateStudentProfile: Start - StudentId={StudentId}, AccountId={AccountId}", studentProfile?.StudentId, accountId);
            try
            {
                // Logic cũ: Lấy avatar cũ trước khi update
                var oldAvatar = await _context.studentProfiles.Where(s => s.StudentId == studentProfile.StudentId)
                                                             .Select(s => s.AvatarURL)
                                                             .FirstOrDefaultAsync();

                // Logic cũ: Update profile
                int updated = await _context.studentProfiles.Where(s => s.StudentId == studentProfile.StudentId)
                                                            .ExecuteUpdateAsync(u => u
                                                                .SetProperty(s => s.FullName, studentProfile.FullName)
                                                                .SetProperty(s => s.AvatarURL, studentProfile.AvatarURL)
                                                                .SetProperty(s => s.UpdateAt, DateTime.Now));

                if (updated <= 0)
                {
                    _logger.LogWarning("updateStudentProfile: No rows updated for StudentId={StudentId}", studentProfile.StudentId);
                    return null;
                }

                // Thêm Audit Log (RabbitMQ)
                var log = new AuditLogModel()
                {
                    AccountId = accountId,
                    Action = "Update student profile",
                    Description = $"Student profile for ID:{accountId} has been updated.",
                    Timestamp = DateTime.Now,
                    IpAddress = ipAddress
                };
                await _rabbitMQ.SendMessageAsync(Newtonsoft.Json.JsonConvert.SerializeObject(log));

                _logger.LogInformation("updateStudentProfile: Success - StudentId={StudentId}", studentProfile.StudentId);
                return new StudentProfileResponseDTO
                {
                    FullName = studentProfile.FullName,
                    AvatarURL = studentProfile.AvatarURL,
                    oldAvatar = oldAvatar
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "updateStudentProfile: Error updating StudentId={StudentId}", studentProfile?.StudentId);
                return null;
            }
        }
    }
}