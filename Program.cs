using Capstone.Database;
using Capstone.Model;
using Capstone.Repositories;
using Capstone.Security;
using Capstone.Services;
using Capstone.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Text;

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


// Đăng ký các dịch vụ cần thiết
builder.Services.AddScoped<Token>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<IAuthRepository, AuthService>();
builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);
builder.Services.AddSingleton<Redis>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
