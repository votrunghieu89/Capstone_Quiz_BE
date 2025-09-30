using Capstone.Database;
using Capstone.DTOs.TeacherProfile;
using Capstone.Model;
using Capstone.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Capstone.Services
{
    public class TeacherProfileService : ITeacherProfileRepository
    {
        private readonly ILogger<TeacherProfileService> _logger;
        private readonly AppDbContext _context;
        public TeacherProfileService(ILogger<TeacherProfileService> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<TeacherProfileModel> getTeacherProfile(int accountId)
        {
            _logger.LogInformation("getTeacherProfile: Start - AccountId={AccountId}", accountId);
            try
            {
                var teacherProfile = await _context.teacherProfiles.Where(t => t.TeacherId == accountId).FirstOrDefaultAsync();
                if (teacherProfile == null)
                {
                    _logger.LogWarning("getTeacherProfile: Not found - AccountId={AccountId}", accountId);
                    return null;
                }
                _logger.LogInformation("getTeacherProfile: Success - AccountId={AccountId}", accountId);
                return teacherProfile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "getTeacherProfile: Error retrieving profile for AccountId={AccountId}", accountId);
                return null;
            }
        }

        public async Task<TeacherProfileResponseDTO> updateTeacherProfile(TeacherProfileModel teacherProfile)
        {
            _logger.LogInformation("updateTeacherProfile: Start - TeacherId={TeacherId}", teacherProfile?.TeacherId);
            try
            {
                var oldAvatar = await _context.teacherProfiles.Where(t => t.TeacherId == teacherProfile.TeacherId)
                                                              .Select(t => t.AvatarURL)
                                                              .FirstOrDefaultAsync();

                int updated = await _context.teacherProfiles.Where(t => t.TeacherId == teacherProfile.TeacherId)
                                                            .ExecuteUpdateAsync(u => u
                                                                .SetProperty(t => t.FullName, teacherProfile.FullName)
                                                                .SetProperty(t => t.AvatarURL, teacherProfile.AvatarURL)
                                                                .SetProperty(t => t.PhoneNumber, teacherProfile.PhoneNumber)
                                                                .SetProperty(t => t.OrganizationName, teacherProfile.OrganizationName)
                                                                .SetProperty(t => t.OrganizationAddress, teacherProfile.OrganizationAddress)
                                                                .SetProperty(t => t.UpdateAt, DateTime.Now));

                if (updated <= 0)
                {
                    _logger.LogWarning("updateTeacherProfile: No rows updated for TeacherId={TeacherId}", teacherProfile.TeacherId);
                    return null;
                }

                _logger.LogInformation("updateTeacherProfile: Success - TeacherId={TeacherId}, Phone={Phone}, Org={Org}", teacherProfile.TeacherId, teacherProfile.PhoneNumber, teacherProfile.OrganizationName);
                return new TeacherProfileResponseDTO
                {
                    FullName = teacherProfile.FullName,
                    AvatarURL = teacherProfile.AvatarURL,
                    oldAvatar = oldAvatar
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "updateTeacherProfile: Error updating TeacherId={TeacherId}", teacherProfile?.TeacherId);
                return null;
            }
        }
    }
}
