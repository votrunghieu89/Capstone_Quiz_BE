using Capstone.DTOs;
using Capstone.DTOs.Admin;
using Capstone.DTOs.Quizzes;
using Capstone.DTOs.Reports.Teacher.OfflineReport;
using Capstone.DTOs.Reports.Teacher.OnlineReport;

namespace Capstone.Repositories.Filter_Search
{
    public interface ISearchRepository
    {
        public Task<AllAccountByRoleDTO> SearchAccountByEmail(string email); // tìm kiếm account trong hiển thị full tk admin dashboard
        public Task<List<AllAccountByRoleDTO>> FilterByRole(string role, int page, int pageSize); // lọc trong full tk admin dashboard
        public Task<List<ViewAllQuizDTO>> FilterByTopic(int topic, int page, int pageSize); // // lọc trong full tk admin dashboard
        public Task<List<ParticipantDTO>> SearchParticipantInGroup(string  Name, int groupId); // Tìm kiếm xem hs nào trong group
        public Task<List<SearchStudentInOfflineReportDTO>> SearchStudentInOfflineReport(string Name, int reportId); // tìm kiếm tên student trong report cả on và off
        public Task<List<ViewOnlineStudentReportEachQuizDTO>> SearchStudentInOnlineReport(string Name, int reportId);
    }
}
