namespace Capstone.DTOs.CandidateProfile
{
    public class ProfileCandidateUpdateDTO
    {
        public string FullName { get; set; } = string.Empty;    
        public string PhoneNumber { get; set; } = string.Empty;
        public string oldAvatarURL { get; set; } = string.Empty;
        public string AvatarURL { get; set; } = string.Empty;
    
        public ProfileCandidateUpdateDTO() { }
        public ProfileCandidateUpdateDTO(string fullName, string phoneNumber, string oldAvatarURL,
            string avatarURL)
        {
            FullName = fullName;
            PhoneNumber = phoneNumber;
            this.oldAvatarURL = oldAvatarURL;
            AvatarURL = avatarURL;
        }
    }
}
