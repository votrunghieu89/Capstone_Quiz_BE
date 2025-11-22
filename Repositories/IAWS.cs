namespace Capstone.Repositories
{
    public interface IAWS
    {
        public Task<string> UploadProfileImageToS3(IFormFile file);
        public Task<string> UploadQuizImageToS3(IFormFile file);
        public Task<bool> DeleteImage(string key);
        public Task<string> ReadImage(string key);
    }
}
