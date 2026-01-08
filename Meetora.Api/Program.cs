using Meetora.Api.Data;
using Meetora.Api.Auth;
using Meetora.Api.Public;
using Meetora.Api.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<AuthOptions>()
    .Bind(builder.Configuration.GetSection(AuthOptions.SectionName))
    .Validate(o => !string.IsNullOrWhiteSpace(o.JwtSigningKey), "Auth:JwtSigningKey is required")
    .Validate(o => !string.IsNullOrWhiteSpace(o.GoogleClientId), "Auth:GoogleClientId is required")
    .Validate(o => !string.IsNullOrWhiteSpace(o.PublicTokenPepper), "Auth:PublicTokenPepper is required")
    .ValidateOnStart();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>, JwtBearerOptionsSetup>();
builder.Services.AddDataProtection();
builder.Services.AddSingleton<IPublicTokenService>(sp =>
{
    var auth = sp.GetRequiredService<IOptions<AuthOptions>>().Value;
    var dp = sp.GetRequiredService<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>();
    return new PublicTokenService(auth.PublicTokenPepper, dp);
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddAuthorization();

builder.Services.AddControllers();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseMiddleware<ApiExceptionMiddleware>();

await using (var scope = app.Services.CreateAsyncScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var canConnect = await dbContext.Database.CanConnectAsync();
        app.Logger.LogInformation("Database connection check: {CanConnect}", canConnect);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Database connection check failed.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    if (int.TryParse(app.Configuration["ASPNETCORE_HTTPS_PORT"], out var httpsPort) && httpsPort > 0)
    {
        app.UseHttpsRedirection();
    }
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
