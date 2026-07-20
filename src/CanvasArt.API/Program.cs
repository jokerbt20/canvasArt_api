using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using AutoMapper;
using CanvasArt.API.Filters;
using CanvasArt.API.Mapping;
using CanvasArt.API.Middleware;
using CanvasArt.API.Repository;
using CanvasArt.API.Services;
using CanvasArt.API.Services.Interfaces;
using CanvasArt.API.Settings;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    var config = builder.Configuration;

    // ----- Options / Settings -----
    builder.Services.Configure<JwtSettings>(config.GetSection(JwtSettings.SectionName));
    builder.Services.Configure<ImageSettings>(config.GetSection(ImageSettings.SectionName));
    builder.Services.Configure<UploadsSettings>(config.GetSection(UploadsSettings.SectionName));
    builder.Services.Configure<EmailSettings>(config.GetSection(EmailSettings.SectionName));

    // ----- AutoMapper & FluentValidation -----
    var appAssembly = Assembly.GetExecutingAssembly();
    builder.Services.AddAutoMapper(cfg => cfg.AddMaps(appAssembly));
    builder.Services.AddValidatorsFromAssembly(appAssembly, includeInternalTypes: true);

    // ----- Persistence -----
    builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();

    // ----- Repositories -----
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IRoleRepository, RoleRepository>();
    builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
    builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
    builder.Services.AddScoped<ITagRepository, TagRepository>();
    builder.Services.AddScoped<IPaintingRepository, PaintingRepository>();
    builder.Services.AddScoped<IFrameRepository, FrameRepository>();
    builder.Services.AddScoped<IPromotionRepository, PromotionRepository>();
    builder.Services.AddScoped<IOrderRepository, OrderRepository>();
    builder.Services.AddScoped<ISlideRepository, SlideRepository>();
    builder.Services.AddScoped<ISettingRepository, SettingRepository>();
    builder.Services.AddScoped<ITestimonialRepository, TestimonialRepository>();
    builder.Services.AddScoped<IContactMessageRepository, ContactMessageRepository>();

    // ----- Cross-cutting services -----
    builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
    builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
    builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
    builder.Services.AddSingleton<IImageService, ImageService>();
    builder.Services.AddSingleton<IEmailService, EmailService>();

    // ----- Pricing engine (per-request cache of active promotions) -----
    builder.Services.AddScoped<IPromotionEvaluator, PromotionEvaluator>();
    builder.Services.AddScoped<CartPricer>();

    // ----- Application services -----
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<ICategoryService, CategoryService>();
    builder.Services.AddScoped<ITagService, TagService>();
    builder.Services.AddScoped<IPaintingService, PaintingService>();
    builder.Services.AddScoped<IFrameService, FrameService>();
    builder.Services.AddScoped<IFramePreviewService, FramePreviewService>();
    builder.Services.AddScoped<IPromotionService, PromotionService>();
    builder.Services.AddScoped<ICartService, CartService>();
    builder.Services.AddScoped<IOrderService, OrderService>();
    builder.Services.AddScoped<ICmsService, CmsService>();
    builder.Services.AddScoped<IContactService, ContactService>();

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

    // ----- MVC -----
    builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>())
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

    builder.Services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);
    builder.Services.Configure<FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = 15 * 1024 * 1024;
    });

    // ----- Authentication (JWT) -----
    var jwtSettings = config.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
    builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(15)
            };
        });

    builder.Services.AddAuthorization();

    // ----- CORS -----
    var corsOrigins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
    builder.Services.AddCors(options => options.AddPolicy("Default", policy =>
    {
        if (corsOrigins.Length > 0)
            policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        else
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    }));

    // ----- Swagger -----
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "CanvasArt API",
            Version = "v1",
            Description = "REST API for the CanvasArt online art gallery and painting store."
        });

        var scheme = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Description = "Enter the JWT access token (without the 'Bearer ' prefix).",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        };
        options.AddSecurityDefinition("Bearer", scheme);
        options.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() });
    });

    var app = builder.Build();

    // ----- Pipeline -----
    app.UseMiddleware<GlobalExceptionMiddleware>();
    app.UseSerilogRequestLogging();

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "CanvasArt API v1");
        options.DocumentTitle = "CanvasArt API";
    });

    app.UseCors("Default");

    // Serve generated public images (originals, thumbnails, frames) from the public storage root only.
    var uploadsSettings = config.GetSection(UploadsSettings.SectionName).Get<UploadsSettings>() ?? new UploadsSettings();
    var publicMediaRoot = Path.IsPathRooted(uploadsSettings.PhysicalRoot)
        ? Path.GetFullPath(uploadsSettings.PhysicalRoot)
        : Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, uploadsSettings.PhysicalRoot));
    Directory.CreateDirectory(publicMediaRoot);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(publicMediaRoot),
        RequestPath = "/uploads"
    });

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    Log.Information("CanvasArt API starting up");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "CanvasArt API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>Exposed so integration tests can reference the entry-point assembly.</summary>
public partial class Program;
