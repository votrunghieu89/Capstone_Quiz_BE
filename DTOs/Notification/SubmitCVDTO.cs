using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone.DTOs.Notification
{
    public class SubmitCVDTO
    {
        public int CVId { get; set; }
        public int JDId { get; set; }

        public SubmitCVDTO() { }
        public SubmitCVDTO(int cvId, int jdId)
        {
            CVId = cvId;
            JDId = jdId;
        }
    }
}
