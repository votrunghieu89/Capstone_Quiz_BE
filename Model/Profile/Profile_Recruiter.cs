using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model.Profile
{
    [Table("Profile_Recruiter")]
    public class Profile_Recruiter
    {
        [Key]
        [Column("ProfileId")]
        public int ProfileId { get; set; }
        [Column("AccountId")]
        public int AccountId { get; set; }
        [Column("FullName")]
        public string FullName { get; set; } = string.Empty;
        [Column("PhoneNumber")]
        public string PhoneNumber { get; set; } = string.Empty;
        [Column("AvartarURL")]
        public string AvartarURL { get; set; } = string.Empty;
        [Column("CompanyName")]
        public string CompanyName { get; set; } = string.Empty;
        [Column("CompanyLocation")]
        public string CompanyLocation { get; set; } = string.Empty;
        public Profile_Recruiter() { }
        public Profile_Recruiter(int profileId, int accountId, string fullName, string phoneNumber, string avartarURL, string companyName, string companyLocation)
        {
            ProfileId = profileId;
            AccountId = accountId;
            FullName = fullName;
            PhoneNumber = phoneNumber;
            AvartarURL = avartarURL;
            CompanyName = companyName;
            CompanyLocation = companyLocation;
        }
    }
}
