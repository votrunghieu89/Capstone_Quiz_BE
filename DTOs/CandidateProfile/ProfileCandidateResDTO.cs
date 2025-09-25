using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.DTOs.CandidateProfile
{
    public class ProfileCandidateResDTO
    {
     
        public string? FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; } = string.Empty;
        public string? AvatarURL { get; set; } = string.Empty;
        public string? Email { get; set; } = string.Empty;
       
    }
}
