using System;
using MarketConnect.Data;
using MarketConnect.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using Elastic.Clients.Elasticsearch;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? Environment.GetEnvironmentVariable("DEFAULT_CONNECTION");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Database connection string is not configured. Set ConnectionStrings:DefaultConnection or DEFAULT_CONNECTION environment variable.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
    }));

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<MarketConnect.Services.Models.JwtSettings>() ?? new MarketConnect.Services.Models.JwtSettings();
builder.Services.AddSingleton(jwtSettings);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAdminMfaService, AdminMfaService>();
builder.Services.AddScoped<IModerationWorkflowGuard, ModerationWorkflowGuard>();
builder.Services.AddScoped<IModerationAppealService, ModerationAppealService>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IMerchantStoreService, MerchantStoreService>();
builder.Services.AddScoped<IContentModerationService, ContentModerationService>();
builder.Services.AddScoped<IMultiMerchantCartService, MultiMerchantCartService>();
builder.Services.AddScoped<IReviewAbuseService, ReviewAbuseService>();
builder.Services.AddScoped<IAdService, AdService>();
builder.Services.AddScoped<IMobileVendorService, MobileVendorService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

var esUrl = builder.Configuration["Elasticsearch:Url"] ?? "http://localhost:9200";
var esSettings = new ElasticsearchClientSettings(new Uri(esUrl));
var esClient = new ElasticsearchClient(esSettings);
builder.Services.AddSingleton(esClient);
builder.Services.AddScoped<IProductCompareService, ProductCompareService>();
builder.Services.AddScoped<IMultiMarketProductService, MultiMarketProductService>();

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSection["Secret"] ?? "DayLaKeyBiMatMacDinhCuaMarketConnect2026";
var jwtIssuer = jwtSection["Issuer"] ?? "MarketConnect";
var jwtAudience = jwtSection["Audience"] ?? "MarketConnectUser";

builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => {
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateLifetime = true
    };
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(12);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options => options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
    c.EnableAnnotations();
    c.DocInclusionPredicate((docName, apiDesc) => {
        var relativePath = apiDesc.RelativePath ?? "";
        return relativePath.Contains("api/", StringComparison.OrdinalIgnoreCase);
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        try
        {
            dbContext.Database.Migrate();
        }
        catch { }

        try
        {
            dbContext.Database.ExecuteSqlRaw(@"
                ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""AccessFailedCount"" integer NOT NULL DEFAULT 0;
                ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""LockoutEnd"" timestamp with time zone NULL;
                ALTER TABLE ""Stores"" ADD COLUMN IF NOT EXISTS ""IdentityInfo"" text NULL;
            ");
        }
        catch { }
        await SeedData.InitializeAsync(dbContext);
        Console.WriteLine("--> Database initialization and seeding completed successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"--> Error during database init: {ex.Message}");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MarketConnect API V1");
        c.RoutePrefix = "swagger";
    });
}

app.UseStaticFiles();
app.UseCors(options => options
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();
app.MapRazorPages();

app.MapGet("/_endpoints", (EndpointDataSource ds) =>
    string.Join("\n", ds.Endpoints.Select(e => e.DisplayName)));

app.Run();