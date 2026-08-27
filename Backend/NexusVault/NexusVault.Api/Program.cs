using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NexusVault.Application.Interfaces;
using NexusVault.Application.Services;
using NexusVault.Infrastructure;
using NexusVault.Infrastructure.AiService;
using NexusVault.Infrastructure.Identity;
using NexusVault.Infrastructure.Jobs;
using NexusVault.Infrastructure.Persistence;
using NexusVault.Infrastructure.Persistence.Repositories;
using NexusVault.Infrastructure.TextExtraction;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("ConnectionStrings:Postgres is not configured.");

builder.Services.AddDbContext<NexusVaultDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.UseVector()));

builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IChunkSearchRepository, ChunkSearchRepository>();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<NexusVaultDbContext>();

builder.Services.AddScoped<ITokenService, JwtTokenService>();

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

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
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "NexusVault",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "NexusVault",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30) 
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Every business endpoint (documents, search, ask, invitations) should
    // require this policy -- it's what stops a narrow tenant-selection
    // token (which has no tenant_id claim at all) from being usable
    // anywhere except /api/auth/select-tenant. Retrofitted onto the
    // existing controllers in the next stage.
    options.AddPolicy("RequireTenantContext", policy =>
        policy.RequireAuthenticatedUser().RequireClaim("tenant_id"));
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentTenant, CurrentTenant>();
builder.Services.AddScoped<IAuthService, AuthService>();

var storageRoot = builder.Configuration["Storage:LocalRootPath"] ?? "/data/nexusvault-files";
builder.Services.AddSingleton<IFileStorage>(new LocalFileStorage(storageRoot));

builder.Services.AddScoped<ITextExtractor, PdfTextExtractor>();
builder.Services.AddScoped<ITextExtractor, DocxTextExtractor>();
builder.Services.AddScoped<TextExtractorResolver>();

var aiServiceBaseUrl = builder.Configuration["AiService:BaseUrl"]
    ?? throw new InvalidOperationException("AiService:BaseUrl is not configured.");
var aiServiceTimeoutSeconds = builder.Configuration.GetValue("AiService:TimeoutSeconds", 120);

builder.Services.AddHttpClient<IChunkingService, HttpChunkingService>(client =>
{
    client.BaseAddress = new Uri(aiServiceBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(aiServiceTimeoutSeconds);
});

builder.Services.AddHttpClient<IEmbeddingService, HttpEmbeddingService>(client =>
{
    client.BaseAddress = new Uri(aiServiceBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(aiServiceTimeoutSeconds);
});

builder.Services.AddHttpClient<IRerankingService, HttpRerankingService>(client =>
{
    client.BaseAddress = new Uri(aiServiceBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(aiServiceTimeoutSeconds);
});

builder.Services.AddHttpClient<IRagSynthesisService, HttpRagSynthesisService>(client =>
{
    client.BaseAddress = new Uri(aiServiceBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(aiServiceTimeoutSeconds);
});

builder.Services.AddScoped<DocumentIngestionService>();
builder.Services.AddScoped<SearchService>();
builder.Services.AddScoped<AskService>();

// --- Background jobs (Hangfire, backed by Postgres -- no separate broker) ----
builder.Services.AddHangfire(config => config
    .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));
builder.Services.AddHangfireServer();

builder.Services.AddScoped<IIngestionJobScheduler, HangfireIngestionJobScheduler>();
builder.Services.AddScoped<ProcessDocumentVersionJob>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHangfireDashboard("/hangfire");
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
