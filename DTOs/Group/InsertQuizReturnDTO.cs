namespace Capstone.DTOs.Group
{
    public class InsertQuizReturnDTO
    {
        public int TeacherId { get; set; }
        public List<int> StudentIds { get; set; } = new List<int>();
        public string GroupName { get; set; } 
        
    }
}
