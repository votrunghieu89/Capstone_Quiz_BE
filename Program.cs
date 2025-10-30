using Capstone.Database;
using Capstone.Model;
using Capstone.RabbitMQ;
using Capstone.Repositories;
using Capstone.Repositories.Admin;
using Capstone.Repositories.Favourite;
using Capstone.Repositories.Folder;
using Capstone.Repositories.Groups;
using Capstone.Repositories.Histories;
using Capstone.Repositories.Profiles;
using Capstone.Repositories.Quizzes;
using Capstone.Security;
using Capstone.Services;
using Capstone.Settings;
using Capstone.SignalR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//builder.Services.AddRateLimiter(options =>
//{
//    // bạn có thể cấu hình sẵn policy nếu muốn
//    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
//});
builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "Integration System API", Version = "v1" });
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid JWT token prefixed with 'Bearer ' (e.g., 'Bearer eyJhbGciOi...')",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });
});

builder.Services.AddSignalR();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<GoogleSetting>(
    builder.Configuration.GetSection("GoogleAuth"));

var redisSettings = new RedisSetting();
builder.Configuration.GetSection("Redis").Bind(redisSettings);

var redisOptions = new ConfigurationOptions
{
    EndPoints = { $"{redisSettings.Host}:{redisSettings.Port}" },
    Password = string.IsNullOrEmpty(redisSettings.Password) ? null : redisSettings.Password,
    DefaultDatabase = redisSettings.DefaultDatabase,
    AbortOnConnectFail = false
};
var redisConnection = ConnectionMultiplexer.Connect(redisOptions);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost:3000", "http://127.0.0.1:3000", "http://localhost:3001")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// ✅ Đăng ký đầy đủ services (kết hợp các nhánh)
builder.Services.AddScoped<IEmailService,EmailService>();
builder.Services.AddScoped<IGoogleService,GoogleService>();
builder.Services.AddScoped<Capstone.Database.IRedis, Redis>();
builder.Services.AddScoped<IToken, Token>();
builder.Services.AddScoped<IAuthRepository, AuthService>();
builder.Services.AddScoped<IStudentProfileRepository, StudentProfileService>();
builder.Services.AddScoped<ITopicRepository, TopicService>();
builder.Services.AddScoped<ITeacherProfileRepository, TeacherProfileService>();
builder.Services.AddScoped<IQuizRepository, QuizService>();
builder.Services.AddScoped<IGroupRepository, GroupService>();
builder.Services.AddScoped<IAdminRepository, AdminService>();
builder.Services.AddScoped<ITeacherReportRepository, TeacherReportService>();
builder.Services.AddScoped<IStudentReportRepository, StudentReportService>();
builder.Services.AddScoped<IOnlineQuizRepository, OnlineQuizService>();
builder.Services.AddScoped<INotificationRepository, NotificationService>();
builder.Services.AddScoped<IOfflineQuizRepository, OfflineQuizService>();
builder.Services.AddScoped<ITeacherFolder, TeacherFolderService>();
builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);
builder.Services.AddSingleton<Redis>();
builder.Services.AddScoped<ConnectionService>();
builder.Services.AddScoped<IFavouriteRepository, FavouriteService>();

builder.Services.AddScoped<IAuditLogRepository, AuditLogService>();
builder.Services.Configure<RabbitMQModel>(builder.Configuration.GetSection("RabbitMQSettings"));
builder.Services.AddScoped<MongoDbContext>();

builder.Services.AddSingleton<RabbitMQProducer>();
builder.Services.AddScoped<IRabbitMQProducer, RabbitMQProducer>();
builder.Services.AddHostedService<AuditLogConsumer>();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 🖼 Static files nên đặt sớm — trước routing
var profilePath = Path.Combine(builder.Environment.ContentRootPath, "ProfileImage");
Directory.CreateDirectory(profilePath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(profilePath),
    RequestPath = "/ProfileImage"
});

var quizPath = Path.Combine(builder.Environment.ContentRootPath, "QuizImage");
Directory.CreateDirectory(quizPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(quizPath),
    RequestPath = "/QuizImage"
});

// 🧭 Routing phải đến trước các middleware phụ thuộc vào endpoint (như RateLimiter)
app.UseRouting();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

//// 🚦 Rate Limiter nên đặt SAU UseRouting và TRƯỚC MapControllers / MapHub
//app.UseRateLimiter(new RateLimiterOptions
//{
//    OnRejected = async (context, token) =>
//    {
//        Console.WriteLine($"Rate limit triggered for {context.HttpContext.Connection.RemoteIpAddress}");
//        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
//        await context.HttpContext.Response.WriteAsync("Rate limit: quá nhiều yêu cầu!", token);
//    },
//    GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
//        RateLimitPartition.GetFixedWindowLimiter(
//            partitionKey: context.User?.Identity?.Name
//                ?? context.Connection.RemoteIpAddress?.ToString()
//                ?? "anonymous",
//            factory: _ => new FixedWindowRateLimiterOptions
//            {
//                PermitLimit = 10,
//                Window = TimeSpan.FromMinutes(1)
//            }))
//});

app.MapControllers();
app.MapHub<QuizHub>("/QuizHub");
app.MapHub<NotificationHub>("/NotificationHub");
app.MapHub<AuditlogHub>("/AuditHub");

app.Run();
