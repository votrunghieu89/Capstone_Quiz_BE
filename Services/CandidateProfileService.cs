using Capstone.Database;
using Capstone.DTOs.CandidateProfile;
using Capstone.Model;
using Capstone.Model.Profile;
using Capstone.Repositories.Profile;
using Microsoft.EntityFrameworkCore;

namespace Capstone.Services
{
    public class CandidateProfileService : ICandidatePofileRepository
    {
        private readonly ILogger<CandidateProfileService> _logger;
        private readonly AppDbContext _context;

        public CandidateProfileService(ILogger<CandidateProfileService> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }
        public async Task<bool> checkConnection()
        {
            try
            {
                bool canConnect = await _context.Database.CanConnectAsync();
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
        public async Task<CVsModel> deleteCV(int CVId)
        {
            try
            {
                var deletedCV = await _context.cVsModels.Where(cv => cv.CVId == CVId).FirstOrDefaultAsync();
                int isDeleted = await _context.cVsModels.Where(cv => cv.CVId == CVId).ExecuteDeleteAsync();
                
                Console.WriteLine("fo"+isDeleted);
                if (isDeleted > 0)
                {
                    _logger.LogInformation("deleteCV successful for CVId: {CVId}", CVId);
                    return deletedCV;
                }
                else
                {
                    _logger.LogWarning("No CV found to delete for CVId: {CVId}", CVId);
                    return null;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in deleteCV for CVId: {CVId}", CVId);
                return null;
            }
        }

        public async Task<List<CVsModel>> getListCVByAccountID(int accountId)
        {
            try
            {
                
                var cvs = await (from p in _context.profileCandidates
                                 where p.AccountId == accountId
                                 join cv in _context.cVsModels on p.PCAId equals cv.PCAId
                                 select cv)
                                 .OrderByDescending(c => c.CreatedAt)
                                 .ToListAsync();

                if (cvs == null || cvs.Count == 0)
                {
                    _logger.LogInformation("No CVs found for AccountId: {AccountId}", accountId);
                    return new List<CVsModel>();
                }

                _logger.LogInformation("Retrieved {Count} CV(s) for AccountId: {AccountId}", cvs.Count, accountId);
                return cvs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in getListCVByAccountID for AccountId: {AccountId}", accountId);
                return new List<CVsModel>();
            }
        }

        public async Task<ProfileCandidateResDTO> getProfileCandidateByAccountId(int accountId)
        {
            try
            {
                // Join auth (email) with profile candidate (profile data). Profile may be null.
                var dto = await(from a in _context.authModels
                                where a.AccountId == accountId
                                join p in _context.profileCandidates on a.AccountId equals p.AccountId into pg
                                from p in pg.DefaultIfEmpty()
                                select new ProfileCandidateResDTO
                                {
                                   
                                    Email = a.Email ?? string.Empty,
                                    FullName = p != null ? p.FullName : string.Empty,
                                    PhoneNumber = p != null ? p.PhoneNumber : string.Empty,
                                    AvatarURL = p != null ? p.AvatarURL : string.Empty,
                                }).FirstOrDefaultAsync();

                if (dto == null)
                {
                    _logger.LogInformation("No account/profile found for AccountId: {AccountId}", accountId);
                }
                else
                {
                    _logger.LogInformation("Retrieved profile for AccountId: {AccountId}", accountId);
                }

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in getProfileCandidateByAccountId for AccountId: {AccountId}", accountId);
                return null;
            }
        }

        public  async Task<ProfileCandidateUpdateDTO> UpdateProfileCandidate(ProfileCandidateModel profileCandidate)
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

        public async Task<int> getPACIDbyAccountId(int accountId)
        {
            try
            {
                var pcaId = await _context.profileCandidates
                    .Where(pc => pc.AccountId == accountId)
                    .Select(pc => pc.PCAId)
                    .FirstOrDefaultAsync();
                if (pcaId == 0)
                {
                    _logger.LogWarning("No ProfileCandidate found for AccountId: {AccountId}", accountId);
                }
                else
                {
                    _logger.LogInformation("Retrieved PCAId {PCAId} for AccountId: {AccountId}", pcaId, accountId);
                }
                return pcaId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in getPACIDbyAccountId for AccountId: {AccountId}", accountId);
                return 0;
            }
        }
    }
}
