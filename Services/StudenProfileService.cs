using Capstone.Database;
using Capstone.DTOs.StudentProfile;
using Capstone.Model;
using Capstone.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Capstone.Services
{
    public class StudenProfileService : IStudentProfileRepository
    {
        private readonly ILogger<StudenProfileService> _logger;
        private readonly AppDbContext _context;
        public StudenProfileService(ILogger<StudenProfileService> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<StudentProfileModel> getStudentProfile(int accountId)
        {
            try
            {
                var studentProfile = await _context.studentProfiles.Where(p => p.StudentId == accountId).FirstOrDefaultAsync();
                if (studentProfile != null)
                {
                    return studentProfile;
                }
                else
                {
                    _logger.LogError("Student profile not found for accountId: {AccountId}", accountId);
                    return null;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in getStudentProfile");
                return null;
            }
        }

        public async Task<StudentProfileResponseDTO> updateStudentProfile(StudentProfileModel studentProfile)
        {
            try
            {
                var oldAvatar = await _context.studentProfiles.Where(p => p.StudentId == studentProfile.StudentId)
                                                             .Select(p => p.AvatarURL)
                                                             .FirstOrDefaultAsync();


                int isUpdate = await _context.studentProfiles.Where(p => p.StudentId == studentProfile.StudentId)
                                                             .ExecuteUpdateAsync(u => u
                                                                                .SetProperty(p => p.FullName, studentProfile.FullName)
                                                                                .SetProperty(p => p.AvatarURL, studentProfile.AvatarURL)
                                                                                .SetProperty(p=> p.UpdateAt, DateTime.Now));
                if (isUpdate > 0)
                {
                    return new StudentProfileResponseDTO
                    {
                        FullName = studentProfile.FullName,
                        AvatarURL = studentProfile.AvatarURL,
                        oldAvatar = oldAvatar
                    };
                }
                else
                {
                    _logger.LogError("Failed to update student profile for StudentId: {StudentId}", studentProfile.StudentId);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in updateStudentProfile");
                return null;
            }
        }
    }
}
