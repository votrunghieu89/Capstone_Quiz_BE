using Capstone.Database;
using Capstone.DTOs.CandidateProfile;
using Capstone.Model;
using Capstone.Model.Profile;
using Capstone.Repositories.Profile;
using Microsoft.EntityFrameworkCore;

namespace Capstone.Services
{
    public class ProfileCandidateService : ICandidatePofileRepository
    {
        private readonly ILogger<ProfileCandidateService> _logger;
        private readonly AppDbContext _context;

        public ProfileCandidateService(ILogger<ProfileCandidateService> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }
        public async Task<bool> checkConnection()
        {
            try
            {
                bool canConnect = await _dbContext.Database.CanConnectAsync();
                if (canConnect)
                {
                    _logger.LogInformation("Database connection successful.");
                    return true;
                }
                else
                {
                    _logger.LogError("Database connection failed.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in checkConnection");
                return false;
            }
        }
        public async Task<bool> deleteCV(int CVId)
        {
            try
            {
                int isDeleted = await _context.cVsModels.Where(cv => cv.CVId == CVId).ExecuteDeleteAsync();
                if (isDeleted > 0)
                {
                    _logger.LogInformation("deleteCV successful for CVId: {CVId}", CVId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("No CV found to delete for CVId: {CVId}", CVId);
                    return false;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in deleteCV for CVId: {CVId}", CVId);
                return false;
            }
        }

        public Task<List<CVsModel>> getListCVByAccountID(int accountId)
        {
            throw new NotImplementedException();
        }

        public Task<ProfileCandidateResDTO> getProfileCandidateByAccountId(int accountId)
        {
            throw new NotImplementedException();
        }

        public  async Task<ProfileCandidateUpdateDTO> UpdateProfileCandidate(ProfileCandidate profileCandidate)
        {
            try
            {
                // Lấy avatar cũ
                var oldAvatarURL = await _context.profileCandidates
                    .Where(pc => pc.AccountId == profileCandidate.AccountId)
                    .Select(pc => pc.AvatarURL)
                    .FirstOrDefaultAsync();
                if (oldAvatarURL == null)
                {
                    _logger.LogWarning("No profile candidate found for AccountId: {AccountId}", profileCandidate.AccountId);
                    return null;
                }
                _logger.LogDebug("Start updating profile for AccountId: {AccountId}", profileCandidate.AccountId);

                // Update dữ liệu
                int isUpdated = await _context.profileCandidates
                    .Where(pc => pc.AccountId == profileCandidate.AccountId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(pc => pc.FullName, profileCandidate.FullName)
                        .SetProperty(pc => pc.PhoneNumber, profileCandidate.PhoneNumber)
                        .SetProperty(pc => pc.AvatarURL, profileCandidate.AvatarURL)
                        .SetProperty(pc => pc.UpdatedAt, DateTime.Now));

                if (isUpdated <= 0)
                {
                    _logger.LogWarning("No profile candidate found to update for AccountId: {AccountId}", profileCandidate.AccountId);
                    return null;
                }
                var newResult = new ProfileCandidateUpdateDTO()
                {
                    FullName = profileCandidate.FullName,
                    PhoneNumber = profileCandidate.PhoneNumber,
                    AvatarURL = profileCandidate.AvatarURL,
                    oldAvatarURL = oldAvatarURL
                };
                _logger.LogInformation("UpdateProfileCandidate successful for AccountId: {AccountId}", profileCandidate.AccountId);
                return newResult;
            }
            catch( Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in UpdateProfileCandidate");
                return null;
            }
        }

        public async Task<bool> uploadCV(CVsModel cVModel)
        {
            try
            {
                await _context.cVsModels.AddAsync(cVModel);
                int isAdded = await _context.SaveChangesAsync();
                if (isAdded > 0)
                {
                    _logger.LogInformation("uploadCV successful for CVId: {CVId}", cVModel.CVId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("uploadCV failed for CVId: {CVId}", cVModel.CVId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in uploadCV");
                return false;
            }
        }
    }
}
