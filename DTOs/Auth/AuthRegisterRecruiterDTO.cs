namespace Capstone.DTOs.Auth
{
    public class AuthRegisterRecruiterDTO
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyLocation { get; set; } = string.Empty;
        public AuthRegisterRecruiterDTO() { }

        public AuthRegisterRecruiterDTO(string fullName, string email, string password, string companyName, string companyLocation)
        {
            FullName = fullName;
            Email = email;
            Password = password;
            CompanyName = companyName;
            CompanyLocation = companyLocation;
        }
    }
}
