using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model.Profile
{
    [Table("ProfileCandidate")]
    public class ProfileCandidateModel
    {
        [Key]
        [Column("PCAId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PCAId { get; set; }

        [Column("AccountId")]
        public int AccountId { get; set; }

        [Column("FullName")]
        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Column("PhoneNumber")]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Column("AvatarURL")]
        [MaxLength(255)]
        public string AvatarURL { get; set; } = string.Empty;

        [Column("CreatedAt")]
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("UpdatedAt")]
        public DateTime? UpdatedAt { get; set; } = DateTime.Now;

        public ProfileCandidateModel()
        {
           
        }

        public ProfileCandidateModel(int accountId, string fullName, string phoneNumber,
            string avatarURL, DateTime createAt, DateTime updateAt)
        {
            AccountId = accountId;
            FullName = fullName ?? string.Empty;
            PhoneNumber = phoneNumber ?? string.Empty;
            AvatarURL = avatarURL ?? string.Empty;
            CreatedAt = createAt;
            UpdatedAt = updateAt;
        }
    }
}
