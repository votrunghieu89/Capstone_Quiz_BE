using Capstone.DTOs.StudentProfile;
using Capstone.Model;

namespace Capstone.Repositories.Profiles
{
    public interface IStudentProfileRepository
    {
        Task<StudentProfileModel> getStudentProfile(int accountId);
        Task<StudentProfileResponseDTO> updateStudentProfile(StudentProfileModel studentProfileDTO, int accountId, string IpAddress);
    }
}
