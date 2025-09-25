using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model.Profile
{
    [Table("ProfileCompany")]
    public class ProfileCompanyModel
    {
        [Key]
        [Column("PCId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PCId { get; set; }

        [Column("AccountId")]
        public int AccountId { get; set; }

        [Column("PhoneNumber")]
        [MaxLength(15)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Column("AvatarURL")]
        [MaxLength(255)]
        public string AvatarURL { get; set; } = string.Empty;

        [Column("CompanyName")]
        [Required]
        [MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [Column("CompanyAddress")]
        [Required]
        [MaxLength(200)]
        public string CompanyAddress { get; set; } = string.Empty;

 
        [Column("ComnpanyIntroduction")]
        [Required]
        public string CompanyIntroduction { get; set; } = string.Empty;

        [Column("CompanyLink")]
        [MaxLength(255)]
        public string CompanyLink { get; set; } = string.Empty;

        [Column("CreatedAt")]
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("UpdatedAt")]
        public DateTime? UpdatedAt { get; set; } = DateTime.Now;

        public ProfileCompanyModel()
        {
          
        }

        public ProfileCompanyModel(int accountId, string phoneNumber, string avatarURL, 
            string companyName, string companyAddress, string companyIntroduction,
            string companyLink, DateTime createAt, DateTime updateAt)
        {
            AccountId = accountId;
            PhoneNumber = phoneNumber ?? string.Empty;
            AvatarURL = avatarURL ?? string.Empty;
            CompanyName = companyName ?? string.Empty;
            CompanyAddress = companyAddress ?? string.Empty;
            CompanyIntroduction = companyIntroduction ?? string.Empty;
            CompanyLink = companyLink ?? string.Empty;
            CreatedAt = createAt;
            UpdatedAt = updateAt;
        }
    }
}
