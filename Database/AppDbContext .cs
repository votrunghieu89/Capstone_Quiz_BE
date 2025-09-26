using Capstone.Model;
using Capstone.Model.Others;
using Capstone.Model.Profile;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Capstone.Database
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
          : base(options)
        {
        }

        public DbSet<AuthModel> authModels { get; set; }
        
   


        public DbSet<ProfileCandidateModel> profileCandidates { get; set; }
        public DbSet<ProfileCompanyModel> profileCompanies { get; set; }

        // CVs
        public DbSet<CVsModel> cVsModels { get; set; }
        public DbSet<CVExtractionModel> cVExtractionModels { get; set; }

        // JDs and related
        public DbSet<JDsModel> jDsModel { get; set; }
        public DbSet<JDDetailModel> jDDetailModels { get; set; }
        public DbSet<PositionModel> positionsModel { get; set; }
        public DbSet<JDPositionModel> jDPositionsModel { get; set; }

        // Favourites
        public DbSet<CVFavouriteModel> cVFavouriteModels { get; set; }
        public DbSet<CompanyFavouriteModel> companyFavourites { get; set; }
        public DbSet<JDFavouriteModel> jDFavourites { get; set; }

        // CV <-> JD
        public DbSet<CV_JD_ApplyModel> cV_JD_Applies { get; set; }
        public DbSet<CV_JD_ScoreModel> cV_JD_Scores { get; set; }

        // Notifications
        public DbSet<NotificationsModel> notificationsModels{ get; set; }

    }
}
