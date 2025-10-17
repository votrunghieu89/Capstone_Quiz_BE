using Capstone.Database;
using Capstone.Model;
using Capstone.Notification;
using Capstone.Repositories;
using Capstone.Repositories.Admin;
using Capstone.Repositories.Groups;
using Capstone.Repositories.Profiles;
using Capstone.Repositories.Favourite;
using Capstone.Repositories.Quizzes;
using Capstone.Security;
using Capstone.Services;
using Capstone.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Text;
using Capstone.Repositories.Histories;
using Capstone.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "Integration System API", Version = "v1" });
    // Định nghĩa Security Scheme cho Bearer Token (JWT)
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid JWT token prefixed with 'Bearer ' (e.g., 'Bearer eyJhbGciOi...')",
        Name = "Authorization",
        Type = SecuritySchemeType.Http, // Sử dụng HTTP authentication
        BearerFormat = "JWT",           // Định dạng là JWT
        Scheme = "Bearer"               // Scheme là Bearer
    });
    // Yêu cầu Security Scheme này cho các endpoint cần bảo vệ
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer" // Phải khớp với tên trong AddSecurityDefinition
                }
            },
            new string[]{} // Không cần scopes cụ thể
        }
    });
});

builder.Services.AddSignalR();

// 2. Cấu hình Authentication với JWT
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
//
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<GoogleSetting>(
    builder.Configuration.GetSection("GoogleAuth"));

// Đọc cấu hình Redis từ appsettings.json
var redisSettings = new RedisSetting();
builder.Configuration.GetSection("Redis").Bind(redisSettings);

// Tạo cấu hình Redis
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

// Đăng ký các dịch vụ cần thiết
builder.Services.AddScoped<Token>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<GoogleService>();
builder.Services.AddScoped<IAuthRepository, AuthService>();
builder.Services.AddScoped<IStudentProfileRepository, StudenProfileService>();
builder.Services.AddScoped<ITeacherProfileRepository, TeacherProfileService>();
builder.Services.AddScoped<IQuizRepository, QuizService>();
builder.Services.AddScoped<IGroupRepository, GroupService>();
builder.Services.AddScoped<IAdminRepository,AdminService>();
builder.Services.AddScoped<ITeacherReportRepository, TeacherReportService>();
builder.Services.AddScoped<IStudentReportRepository, StudentReportService>();
builder.Services.AddScoped<IOnlineQuizRepository, OnlineQuizService>();
builder.Services.AddScoped<IOfflineQuizRepository, OfflineQuizService>();
builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);
builder.Services.AddSingleton<Redis>();
builder.Services.AddSingleton<IUserIdProvider, QueryStringUserIdProvider>();
builder.Services.AddScoped<ConnectionService>();
builder.Services.AddScoped<IFavouriteRepository , FavouriteService>();
var app = builder.Build();

// Swagger chỉ nên bật khi dev
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Static files (phục vụ ảnh, css, js, …) nên đặt TRƯỚC routing
//app.UseStaticFiles(new StaticFileOptions
//{
//    FileProvider = new PhysicalFileProvider(@"E:\Capstone\Capstone\ProfileImage"),
//    RequestPath = "/ProfileImage"
//});
//app.UseStaticFiles(new StaticFileOptions
//{
//    FileProvider = new PhysicalFileProvider(@"E:\Capstone\Capstone\QuizImage"),
//    RequestPath = "/QuizImage"
//});
app.UseRouting();

app.UseCors("AllowFrontend");

// Authentication trước Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<QuizHub>("/QuizHub");

app.Run();
