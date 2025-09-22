using Capstone.DTOs.Dashboard;

namespace Capstone.Repositories.Dashboard
{
    public interface IDashboardAccountRepository
    {
        // // nhớ phần trang
        // lấy số tài khoản đã tạo theo tháng // hiển thị Email, ngày tạo, Role, 
        // Toàn bộ tài khoản đã tạo 
        // Số tài khoản Candidate đã tạo
        // Số tài khoản Recruiter đã tạo
        // Số tài khoản Candidate đã tạo theo tháng
        // Số tài khoản Recruiter đã tạo theo tháng
        // Xoá tài khoản
        public Task<bool> checkConnection();
        public Task<int> GetTotalAccountsCreated();
        public Task<int> GetTotalCandidateAccountsCreated();
        public Task<int> GetTotalRecruiterAccountsCreated(); 
        public Task<int> GetAccountsCreatedInMonth(int month, int year); // Tổng số tài khoản tạo trong tháng
        public Task<int> GetCandidateAccountsCreatedInMonth(int month, int year); // Số tài khoản Candidate đã tạo theo tháng
        public Task<int> GetRecruiterAccountsCreatedInMonth(int month, int year); // Số tài khoản Recruiter đã tạo theo tháng
        public Task<List<DashboardAccountDTO>> GetAllAccounts(int pageNumber, int pageSize);
        public Task<bool> DeleteAccount(int accountId);
    }
}
