namespace Capstone.DTOs.Auth
{
    public class AuthRegisterRecruiterDTO
    {
      
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyAddress { get; set; } = string.Empty;
        public AuthRegisterRecruiterDTO() { }

        public AuthRegisterRecruiterDTO( string email, string password, string companyName, string companyLocation)
        {
          
            Email = email;
            Password = password;
            CompanyName = companyName;
            CompanyAddress = companyLocation;
        }
    }
}
