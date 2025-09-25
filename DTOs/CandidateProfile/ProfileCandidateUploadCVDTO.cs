namespace Capstone.DTOs.CandidateProfile
{
    public class ProfileCandidateUploadCVDTO
    {
        public int AccountId { get; set; }
        public IFormFile? FormFile { get; set; } = null!;
        public ProfileCandidateUploadCVDTO() { }
        public ProfileCandidateUploadCVDTO(int accountId, IFormFile formFile)
        {
            AccountId = accountId;
            FormFile = formFile;
        }
    }
}
