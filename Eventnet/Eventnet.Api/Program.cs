using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using Eventnet.Api.Config;
using Eventnet.Api.Helpers.EventFilterFactories;
using Eventnet.Api.Models.Authentication.Tokens;
using Eventnet.Api.Services;
using Eventnet.Api.Services.Filters;
using Eventnet.Api.Services.SaveServices;
using Eventnet.DataAccess;
using Eventnet.DataAccess.Entities;
using Eventnet.Domain;
using Eventnet.Infrastructure;
using Eventnet.Infrastructure.PhotoServices;
using Eventnet.Infrastructure.Validators;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var jwtTokenConfig = builder.Configuration.GetSection("JWT").Get<JwtTokenConfig>();
var emailConfig = builder.Configuration.GetSection("Email").Get<EmailConfiguration>();
const string corsName = "_myAllowSpecificOrigins";
var rabbitMqConfig = builder.Configuration.GetSection("RabbitMq").Get<RabbitMqConfig>();
var photoStorageConfig = builder.Configuration.GetSection("PhotoStorage").Get<PhotoStorageConfig>();

services.AddSingleton(emailConfig);
services.AddSingleton(jwtTokenConfig);
services.AddSingleton(rabbitMqConfig);
services.AddSingleton(photoStorageConfig);
services.AddSingleton<IJwtAuthService, JwtAuthService>();
services.AddScoped<CurrentUserService>();

services.AddSingleton<IEventFilterFactory, LocationFilterFactory>();
services.AddSingleton<IEventFilterFactory, StartDateFilterFactory>();
services.AddSingleton<IEventFilterFactory, EndDateFilterFactory>();
services.AddSingleton<IEventFilterFactory, OwnerFilterFactory>();
services.AddSingleton<IEventFilterFactory, TagsFilterFactory>();
services.AddSingleton<IEventFilterMapper, EventFilterMapper>();
services.AddSingleton<IPublishEventService, PublishEventService>();
services.AddSingleton<EventSaveHandler>();
services.AddSingleton<EventsFilterService>();
services.AddMemoryCache();
services.AddSingleton<IConsumeEventService, RabbitMqConsumeEventService>();
services.AddSingleton<IEventSaveService, EventSaveService>();
services.AddSingleton<IPhotoValidator, PhotoValidator>();
services.AddScoped<IPhotoToStorageSaveService, PhotoToStorageSaveService>();
services.AddScoped<ISaveToDbService, SaveToDbService>();
services.AddSingleton<IEventValidator, EventValidator>();
services.AddSingleton<IEventCreationValidator, EventCreationValidator>();
services.AddSingleton<IRabbitMqMessageHandler, RabbitMqMessageHandler>();

services.AddScoped<IEmailService, EmailService>();
services.AddScoped<IForgotPasswordService, ForgotPasswordService>();

services.AddMemoryCache();

services.AddHttpContextAccessor();

services.AddControllers().AddJsonOptions(x =>
    x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

services.AddEndpointsApiExplorer();

services.AddDbContext<ApplicationDbContext>(opt => opt.UseNpgsql(connectionString));

services.AddAutoMapper(opt => opt.AddProfile<ApplicationMappingProfile>());

services.AddCors(options =>
{
    options.AddPolicy(corsName,
        policyBuilder =>
        {
            policyBuilder
                .WithOrigins("*")
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

services.AddIdentity<UserEntity, IdentityRole>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidAudience = jwtTokenConfig.Audience,
            ValidIssuer = jwtTokenConfig.Issuer,
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtTokenConfig.Secret))
        };
    });

services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });
    option.AddSecurityDefinition("Bearer",
        new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "Please enter a valid token",
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
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    option.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

services.AddHostedService<BackgroundConsumeEventService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(corsName);

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (context.Database.GetPendingMigrations().Any())
        context.Database.Migrate();
}

app.Run();

// ReSharper disable once UnusedType.Global Use for integration tests
// https://docs.microsoft.com/ru-ru/aspnet/core/test/integration-tests?view=aspnetcore-6.0#basic-tests-with-the-default-webapplicationfactory
// ReSharper disable once PartialTypeWithSinglePart Use for integration tests
namespace Eventnet.Api
{
    public partial class Program
    {
    }
}