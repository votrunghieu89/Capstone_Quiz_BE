using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.Model
{
    [Table("QuizzFavourite")]
    public class QuizzFavouriteModel
    {
        [Key]
        [Column("FavouriteId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int FavouriteId { get; set; }

        [Column("StudentId")]
        public int StudentId { get; set; }

        [Column("QuizId")]
        public int QuizId { get; set; }

        [Column("CreateAt")]
        [Required]
        public DateTime CreateAt { get; set; } = DateTime.Now;
    }
}