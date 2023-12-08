using AutoMapper;
using BeautyHubAPI;
using BeautyHubAPI.Data;
using BeautyHubAPI.Models;
using BeautyHubAPI.Repository;
using BeautyHubAPI.Repository.IRepository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
using Amazon.S3;
using BeautyHubAPI.Helpers;
using BeautyHubAPI.IRepository;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BeautyHubAPI.Firebase;
using BeautyHubAPI.Repositories;
using ApplicationDbContext = BeautyHubAPI.Data.ApplicationDbContext;
using Amazon.S3.Model;
using System.Net;
using BeautyHubAPI.Models.Helper;
using Newtonsoft.Json;

// public partial class Program
// {
//     public static void Main(string[] args)
//     {
//         CreateHostBuilder(args).Build().Run();
//     }

//     public static IHostBuilder CreateHostBuilder(string[] args) =>
//         Host.CreateDefaultBuilder(args)
//             .ConfigureServices((hostContext, services) =>
//             {
//                 // Register other services if needed

//                 services.AddHostedService<MyBackgroundService>();
//                 // Add other background services here if needed
//             });
// }

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(option =>
{
    option.UseSqlServer(builder.Configuration.GetConnectionString("DefaultSQLConnection"));
});
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;
});
// builder.Services.AddHostedService<MyBackgroundService>();

builder.Services.AddCors();

builder.Services.AddScoped<UPIService>();
builder.Services.AddAutoMapper(typeof(MappingConfig));
builder.Services.AddScoped<ITwilioManager, TwilioManager>();
builder.Services.Configure<Aws3Services>(builder.Configuration.GetSection("Aws3Services"));
builder.Services.AddScoped<IContentRepository, ContentRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUploadRepository, UploadRepository>();
builder.Services.AddScoped<IBannerRepository, BannerRepository>();
builder.Services.AddScoped<IMembershipRecordRepository, MembershipRecordRepository>();
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);
builder.Services.AddDataProtection().PersistKeysToAWSSystemsManager("/" + "BeautyHubAPI" + "/DataProtection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultSQLConnection"));
    // options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});
builder.Services.AddSingleton<IMobileMessagingClient, MobileMessagingClient>();
builder.Services.AddHostedService<MyBackgroundService>();
builder.Services.AddScoped<MyBackgroundService>();
builder.Services.AddHostedService<EverydayMidnightService>();
builder.Services.AddScoped<EverydayMidnightService>();


//Inject EmailSettings
builder.Services.AddOptions();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("GoogleAuthentication"));
builder.Services.AddSingleton<IEmailManager, EmailManager>();



// builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
//         .AddEntityFrameworkStores<ApplicationDbContext>()
//         .AddDefaultTokenProviders();

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.Zero; // Disable automatic security stamp validation
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // c.OperationFilter<AuthResponsesOperationFilter>();
    c.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Description =
                "JWT Authorization header using the Bearer scheme. \r\n\r\n "
                + "Enter your token in the text input below.\r\n\r\n"
                + "Example: \"12345abcdef\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http, //SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            BearerFormat = "JWT"
        }
    );

    c.AddSecurityRequirement(
        new OpenApiSecurityRequirement()
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    },
                    Scheme = "oauth2",
                    Name = "Bearer",
                    In = ParameterLocation.Header,
                },
                new List<string>()
            }
        }
    );
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory,
        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
});
var key = builder.Configuration.GetValue<string>("ApiSettings:Secret");

builder.Services
    .AddAuthentication(x =>
    {
        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(x =>
    {
        x.RequireHttpsMetadata = false;
        x.SaveToken = true;
        x.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key)),
            ValidateIssuer = false,
            ValidateAudience = false
        };

        x.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userService = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                string securityStamp = context.Principal.Claims.FirstOrDefault(claim => claim.Type == "SecurityStamp")?.Value;
                var userId = context.Principal.Claims.FirstOrDefault().Value.ToString();
                var user = await userService.Users.Where(u => u.Id == userId).FirstOrDefaultAsync();
                if (user == null)
                {
                    context.Fail("Unauthorized");
                }


                if (user != null)
                {
                    if (user.SecurityStamp != securityStamp && user.PhoneNumber.Length < 11)
                    {
                        context.Fail("Unauthorized");

                        var response = new
                        {
                            isSuccess = false,
                            statusCode = HttpStatusCode.Unauthorized,
                            messages = "Access denied. You are not authorized to perform this action."
                        };

                        var jsonResponse = JsonConvert.SerializeObject(response);

                        context.Response.ContentType = "application/json";
                        context.Response.StatusCode = 401;

                        await context.Response.WriteAsync(jsonResponse);

                        return;
                    }
                }
                else
                    await Task.Delay(0);
                return;
            }

        };

    });


var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{

//}
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "BeautyHubAPI v1");
    // options.RoutePrefix = "";
});

app.UseHttpsRedirection();
app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
