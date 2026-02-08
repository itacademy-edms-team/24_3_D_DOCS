using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using RusalProject.Provider.Database;
using RusalProject.Provider.Redis;
using RusalProject.Services.Agent;
using RusalProject.Services.Auth;
using RusalProject.Services.Document;
using RusalProject.Services.Attachment;
using RusalProject.Services.Email;
using RusalProject.Services.Markdown;
using RusalProject.Services.Pdf;
using RusalProject.Services.Profile;
using RusalProject.Services.Storage;
using RusalProject.Services.TitlePage;
using RusalProject.Services.Chat;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel request timeout for long-running operations
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(30);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(30);
});

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Настройка JSON сериализации для enum (без учета регистра)
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        // Устанавливаем camelCase для совместимости с фронтендом
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    })
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
{
    options.UseNpgsql(connectionString);
    options.ConfigureWarnings(warnings =>
        warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
});

// Redis Configuration
var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IRedisService>(sp => new RedisService(redisConnection));

// Auth Services
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Storage Services
builder.Services.AddSingleton<IMinioService, MinioService>();

// Document Services
builder.Services.AddScoped<IDocumentService, DocumentService>();
// Attachment Services
builder.Services.AddScoped<IAttachmentService, AttachmentService>();

// Profile Services
builder.Services.AddScoped<IProfileService, ProfileService>();

// TitlePage Services
builder.Services.AddScoped<ITitlePageService, TitlePageService>();

// PDF Services
builder.Services.AddScoped<IPdfGeneratorService, PdfGeneratorService>();

// Markdown Services
builder.Services.AddScoped<IMarkdownParserService, MarkdownParserService>();

// Agent Services
builder.Services.AddScoped<IAgentLogService, AgentLogService>();
builder.Services.AddScoped<IDocumentAgent, DocumentAgent>();
builder.Services.AddScoped<IAgentService, AgentService>();

// Chat Services
builder.Services.AddScoped<IChatService, ChatService>();

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
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Apply migrations
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        // Wait for database to be ready (retry logic)
        var maxRetries = 10;
        var retryDelay = TimeSpan.FromSeconds(2);
        var dbReady = false;
        
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                if (context.Database.CanConnect())
                {
                    dbReady = true;
                    logger.LogInformation("Database connection established.");
                    break;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Database connection attempt {i + 1}/{maxRetries} failed: {ex.Message}");
                if (i < maxRetries - 1)
                {
                    Task.Delay(retryDelay).GetAwaiter().GetResult();
                }
            }
        }
        
        if (!dbReady)
        {
            throw new InvalidOperationException("Failed to connect to database after multiple retries. Please check database configuration and ensure it is running.");
        }
        
        var pendingMigrations = context.Database.GetPendingMigrations().ToList();
        var appliedMigrations = context.Database.GetAppliedMigrations().ToList();
        
        logger.LogInformation($"Applied migrations: {string.Join(", ", appliedMigrations)}");
        
        if (pendingMigrations.Any())
        {
            logger.LogInformation($"Applying pending migrations: {string.Join(", ", pendingMigrations)}");
            context.Database.Migrate();
            logger.LogInformation("Database migrations applied successfully.");
        }
        else
        {
            logger.LogInformation("No pending migrations. Database is up to date.");
        }
        
        // Verify migrations were applied by checking if Users table exists
        try
        {
            var userCount = context.Users.Count();
            logger.LogInformation($"Database warm-up completed. Users count: {userCount}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Database migrations appear to have failed. Users table is not accessible: {ex.Message}", ex);
        }
        
        // Warm-up Redis connection
        try
        {
            var redisService = services.GetRequiredService<IRedisService>();
            redisService.SetAsync("warmup:check", "ready", TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
            var warmupCheck = redisService.GetAsync("warmup:check").GetAwaiter().GetResult();
            redisService.DeleteAsync("warmup:check").GetAwaiter().GetResult();
            logger.LogInformation($"Redis warm-up completed. Check: {warmupCheck}");
        }
        catch (Exception ex)
        {
            logger.LogWarning($"Redis warm-up failed: {ex.Message}. Continuing anyway...");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "CRITICAL: Failed to initialize database. Application will not start.");
        throw; // Re-throw to prevent application from starting with broken database
    }
}

// Configure the HTTP request pipeline.
// CORS must be before Authentication/Authorization
app.UseCors("AllowFrontend");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Rusal Project API v1");
        c.RoutePrefix = "swagger"; // Swagger будет доступен на /swagger
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
