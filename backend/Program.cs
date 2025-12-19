using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Minio;
using RusalProject.Provider.Database;
using RusalProject.Provider.Redis;
using RusalProject.Services.Auth;
using RusalProject.Services.Email;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Отключаем автоматическую обработку ошибок валидации
        // Будем обрабатывать вручную в контроллерах
        options.SuppressModelStateInvalidFilter = true;
    });

// Swagger Configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Rusal Project API",
        Version = "v1",
        Description = "JWT Authentication API для проекта Rusal"
    });

    // JWT Authorization в Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Введите JWT токен в формате: Bearer {твой токен}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Database Configuration (PostgreSQL)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Redis Configuration
var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IRedisService>(sp => new RedisService(redisConnection));

// MinIO Configuration
var minioEndpoint = builder.Configuration["MinIO:Endpoint"] ?? "localhost:9000";
var minioAccessKey = builder.Configuration["MinIO:AccessKey"] ?? "minioadmin";
var minioSecretKey = builder.Configuration["MinIO:SecretKey"] ?? "minioadmin123";
var useSSL = builder.Configuration.GetValue<bool>("MinIO:UseSSL", false);

var minioClient = new MinioClient()
    .WithEndpoint(minioEndpoint)
    .WithCredentials(minioAccessKey, minioSecretKey);

if (useSSL)
{
    minioClient.WithSSL();
}

builder.Services.AddSingleton<IMinioClient>(minioClient.Build());

// Auth Services
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// JWT Authentication Configuration
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] 
    ?? throw new InvalidOperationException("JWT SecretKey is not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "RusalProject";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "RusalProject-Client";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() 
            ?? new[] { "http://localhost:3000" };
        
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Apply migrations automatically & warm-up connections
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Apply migrations
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
        Console.WriteLine("Database migrations applied successfully.");
        
        // Warm-up database connection (prevents cold start delay)
        var userCount = context.Users.Count();
        Console.WriteLine($"Database warm-up completed. Users count: {userCount}");
        
        // Warm-up Redis connection
        var redisService = services.GetRequiredService<IRedisService>();
        redisService.SetAsync("warmup:check", "ready", TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
        var warmupCheck = redisService.GetAsync("warmup:check").GetAwaiter().GetResult();
        redisService.DeleteAsync("warmup:check").GetAwaiter().GetResult();
        Console.WriteLine($"Redis warm-up completed. Check: {warmupCheck}");
        
        // Warm-up MinIO - ensure default bucket exists
        var minioClient = services.GetRequiredService<IMinioClient>();
        var defaultBucket = builder.Configuration["MinIO:DefaultBucket"] ?? "documents";
        try
        {
            var bucketExistsArgs = new Minio.DataModel.Args.BucketExistsArgs()
                .WithBucket(defaultBucket);
            var exists = minioClient.BucketExistsAsync(bucketExistsArgs).GetAwaiter().GetResult();
            if (!exists)
            {
                var makeBucketArgs = new Minio.DataModel.Args.MakeBucketArgs()
                    .WithBucket(defaultBucket);
                minioClient.MakeBucketAsync(makeBucketArgs).GetAwaiter().GetResult();
                Console.WriteLine($"MinIO bucket '{defaultBucket}' created.");
            }
            else
            {
                Console.WriteLine($"MinIO bucket '{defaultBucket}' already exists.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MinIO warm-up error: {ex.Message}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred during startup: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Rusal Project API v1");
        c.RoutePrefix = "swagger"; // Swagger будет доступен на /swagger
    });
}

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
