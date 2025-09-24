using Capstone.DTOs.Notification;

namespace Capstone.Repositories
{
    public interface ISupportRepository
    {
        // Lấy accountId và tên ứng viên từ CVId
        public Task<NotificationSubmitCVModel> NotificationSubmitCVModel(int cvId);
        // Lâyc accountId và CompanyName từ JDId
        public Task<NotificationEvaluateCVModel> NotificationEvaluateCVModel(int jdId);

    }
}
