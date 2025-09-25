namespace Capstone.DTOs.CandidateProfile
{
    public class UpdateProfileCandidateControllerDTO
    {
        public int accoutnId { get; set; }
        public string? fullName { get; set; } = string.Empty;
        public string? phoneNumber { get; set; } = string.Empty;
       
        public IFormFile? FormFile { get; set; } = null!;

        public UpdateProfileCandidateControllerDTO() { }
        public UpdateProfileCandidateControllerDTO(int accountId, string fullName, string phoneNumber,
            string avatarURL, IFormFile formFile)
        {
            this.accoutnId = accountId;
            this.fullName = fullName;
            this.phoneNumber = phoneNumber;
            this.FormFile = formFile;
        }
    }
}
