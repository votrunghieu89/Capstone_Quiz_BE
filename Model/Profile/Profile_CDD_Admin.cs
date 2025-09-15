using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model.Profile
{
    [Table("Profile_CDD_Admin")]
    public class Profile_CDD_Admin
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

        public Profile_CDD_Admin() { }
        public Profile_CDD_Admin(int profileId, int accountId, string fullName, string phoneNumber, string avartarURL)
        {
            ProfileId = profileId;
            AccountId = accountId;
            FullName = fullName;
            PhoneNumber = phoneNumber;
            AvartarURL = avartarURL;
        }
    }
}
