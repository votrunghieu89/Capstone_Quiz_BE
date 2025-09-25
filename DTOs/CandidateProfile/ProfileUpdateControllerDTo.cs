namespace Capstone.DTOs.CandidateProfile
{
    public class ProfileUpdateControllerDTo
    {
        public string? FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; } = string.Empty;
        public string? oldAvatarURL { get; set; } = string.Empty;
        public string? AvatarURL { get; set; } = string.Empty;

        public ProfileUpdateControllerDTo() { }
        public ProfileUpdateControllerDTo(string fullName, string phoneNumber, string oldAvatarURL,
            string avatarURL)
        {
            FullName = fullName;
            PhoneNumber = phoneNumber;
            this.oldAvatarURL = oldAvatarURL;
            AvatarURL = avatarURL;
        }
    }
}
