namespace Capstone.DTOs.Reports.Teacher
{
    public class DetailOfQuestionDTO
    {
        public string QuestionContent { get; set; }
        public int Time {get; set; }
        public List<OptionsDTO> options { get; set; }  = new List<OptionsDTO>();
    }
    public class OptionsDTO
    {
        public int OptionId { get; set; }
        public string OptionContent { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }
}
