using Capstone.Model;


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

        // Profile / Account related
        public DbSet<StudentProfileModel> studentProfiles { get; set; }
        public DbSet<TeacherProfileModel> teacherProfiles { get; set; }

        // Groups
        public DbSet<GroupModel> groups { get; set; }
        public DbSet<StudentGroupModel> studentGroups { get; set; }

        // Topics and folders
        public DbSet<TopicModel> topics { get; set; }
        public DbSet<QuizzFolderModel> quizzFolders { get; set; }

        // Quizzes and related
        public DbSet<QuizModel> quizzes { get; set; }
        public DbSet<QuizzGroupModel> quizzGroups { get; set; }

        // Questions / Options
        public DbSet<QuestionModel> questions { get; set; }
        public DbSet<OptionModel> options { get; set; }

        // Results
        public DbSet<OfflineResultModel> offlineResults { get; set; }
        public DbSet<OnlineResultModel> onlineResults { get; set; }
        public DbSet<OfflineWrongAnswerModel> offlineWrongAnswers { get; set; }
        public DbSet<OnlineWrongAnswerModel> onlineWrongAnswers { get; set; }

        // Favourites / Stats
        public DbSet<QuizzFavouriteModel> quizzFavourites { get; set; }
        public DbSet<QuestionStatsModel> questionStats { get; set; }

        // Reports
        public DbSet<OfflineReportsModel> offlinereports { get; set; }
        public DbSet<OnlineReportModel> onlinereports { get; set; }

        // Notification
        public DbSet<NotificationModel> notifications { get; set; }
    }
}
