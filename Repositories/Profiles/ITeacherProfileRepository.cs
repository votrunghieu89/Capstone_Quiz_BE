using Capstone.DTOs.TeacherProfile;
using Capstone.Model;

namespace Capstone.Repositories.Profiles
{
    public interface ITeacherProfileRepository
    {
        Task<TeacherProfileModel> getTeacherProfile(int accountId);
        Task<TeacherProfileResponseDTO> updateTeacherProfile(TeacherProfileModel teacherProfile);
    }
}
