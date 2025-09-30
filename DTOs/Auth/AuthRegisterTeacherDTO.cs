namespace Capstone.DTOs.Auth
{
    public class AuthRegisterTeacherDTO
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public string OrganizationAddress { get; set; } = string.Empty;
        public AuthRegisterTeacherDTO() { }

        public AuthRegisterTeacherDTO(string fullName, string email, string passwordHash, string organizationName, string organizationAddress)
        {
            FullName = fullName;
            Email = email;
            PasswordHash = passwordHash;
            OrganizationName = organizationName;
            OrganizationAddress = organizationAddress;
           
        }
    }
}
