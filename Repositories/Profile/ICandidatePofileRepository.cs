using Capstone.DTOs.CandidateProfile;
using Capstone.Model;
using Capstone.Model.Profile;
using StackExchange.Redis;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Capstone.Repositories.Profile
{
    public interface ICandidatePofileRepository
    {
        //Update Profile
        //Upload, Delete CV
        // Lấy danh sách CV theo id người đăng
        // Theo dõi trạng thái CV khi apply JD

        //Thêm JD yêu thích(candidate, )
        // Xóa JD yêu thích ( candidate, )
        // Lấy danh sách JD yêu thích ( candidate, )

        // Thêm công ty yêu thích ( candidate)
        // Xoá công ty yêu thích ( candidate)
        // Lấy danh sách công ty yêu thích ( candidate)
        public Task<bool> checkConnection();
        public Task<ProfileCandidateResDTO> getProfileCandidateByAccountId(int accountId);
        public Task<ProfileCandidateUpdateDTO> UpdateProfileCandidate(ProfileCandidateModel profileCandidate);
        public Task<bool> uploadCV(CVsModel cVModel);
        public Task<CVsModel> deleteCV(int CVId);
        public Task<List<CVsModel>> getListCVByAccountID(int accountId);
        public Task<int> getPACIDbyAccountId(int accountId);



    }
}
