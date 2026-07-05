using System.Text.Json;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using trackrv2_efc;
using trackrv2_efc.Entities;
using trackrv2_efc.Middleware;
using trackrv2_web_api.Services.Auth;
using trackrv2_web_api.Services.TrackerEntryService;
using trackrv2_web_api.Services.TrackerService;
using trackrv2_web_api.Services.User;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate:
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starter TrackrV2 WebApi...");

    if (File.Exists(".env"))
    {
        DotNetEnv.Env.Load(".env");
    }
    else if (File.Exists("../.env"))
    {
        DotNetEnv.Env.Load("../.env");
    }
    else if (File.Exists("../../.env"))
    {
        DotNetEnv.Env.Load("../../.env");
    }
    else if (File.Exists("../../../.env"))
    {
        DotNetEnv.Env.Load("../../../.env");
    }

    var builder = WebApplication.CreateBuilder(args);

    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(8080);
    });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("CorsPolicy", policy =>
        {
            policy.WithOrigins("http://localhost:5173"  // Vite dev server
            , "https://nielselmgaard.github.io" // github pages
            )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    builder.Host.UseSerilog();

    var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

    var supabaseDbPassword = Environment.GetEnvironmentVariable("SUPABASE_DB_PASSWORD");

    var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");

    if (string.IsNullOrEmpty(connectionString))
    {
        var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
        if (isDocker)
        {
            connectionString = $"Host=aws-0-eu-west-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.fzqputdxlzlnvslgdmdj;Password={supabaseDbPassword};Pooling=true;";
        }
        else
        {
            //Direct connection for local development (no Docker)
            connectionString = $"Host=aws-0-eu-west-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.fzqputdxlzlnvslgdmdj;Password={supabaseDbPassword};Pooling=true;";
        }
    }

    // EFC
    builder.Services.AddDbContext<TrackrContext>(options =>
        options.UseNpgsql(connectionString));

    builder.Services.AddDataProtection()
        .PersistKeysToDbContext<TrackrContext>();

    // Add services to the container.

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy =
                JsonNamingPolicy.CamelCase;
        });
    builder.Services.AddMemoryCache();
    // builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
    // builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimitPolicies"));
    builder.Services.AddInMemoryRateLimiting();
    builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
    builder.Services.Configure<IpRateLimitOptions>(options =>
    {
        options.EnableEndpointRateLimiting = true;
        options.StackBlockedRequests = false;
        options.HttpStatusCode = 429;
        options.RealIpHeader = "X-Forwarded-For"; // to get real IP address
        options.ClientIdHeader = "X-ClientId"; // to identify clients
        options.QuotaExceededResponse = new QuotaExceededResponse
        {
            ContentType = "application/problem+json",
            Content = "{{\n  \"title\": \"For mange anmodninger\",\n  \"status\": 429,\n  \"detail\": \"For mange anmodninger på samme tid. Prøv igen om lidt.\"\n}}"
        };
        options.GeneralRules = [
    new RateLimitRule {
        Endpoint = "POST:/api/v1/auth/login", // login endpoint
        Period = "1m",
        Limit = 5 // 5 request per minute
    },
    new RateLimitRule {
        Endpoint = "POST:/api/v1/users", // register user endpoint
        Period = "1m",
        Limit = 3 // 3 request per minute
    },

    new RateLimitRule {
        Endpoint = "*", // rate limit for all other endpoints
        Period = "10s",
        Limit = 20 // 20 requests per 10 seconds
    }
];
    });
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    // builder.Services.AddOpenApi();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "TrackrV2 API",
            Version = "v1"
        });
        var jwtSecurityScheme = new OpenApiSecurityScheme
        {
            BearerFormat = "JWT",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = JwtBearerDefaults.AuthenticationScheme,
            Description = "Enter your JWT Access Token",
            Reference = new OpenApiReference
            {
                Id = "Bearer",
                Type = ReferenceType.SecurityScheme
            }
        };
        options.AddSecurityDefinition("Bearer", jwtSecurityScheme);
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
                Array.Empty<string>()
            }
        });
    });


    // Web API
    builder.Services.AddScoped<ITrackerService, TrackerService>();
    builder.Services.AddScoped<ITrackerEntryService, TrackerEntryService>();
    builder.Services.AddScoped<ILoginService, LoginService>();
    builder.Services.AddScoped<IUserService, UserService>();

    // JWT
    builder.Services
        .AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
    builder.Services.AddScoped<IJwtService, JwtService>();

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment(); // TRUE in production
        options.SaveToken = true;
        var jwtKey = builder.Configuration["JwtConfig:Key"]
                     ?? Environment.GetEnvironmentVariable("JwtConfig__Key");

        var rawIssuers = builder.Configuration["JwtConfig:Issuer"] ?? "http://localhost:8080";
        var rawAudiences = builder.Configuration["JwtConfig:Audience"] ?? "http://localhost:5173";

        var allowedIssuers = rawIssuers.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var allowedAudiences = rawAudiences.Split(',', StringSplitOptions.RemoveEmptyEntries);

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!)),
            ValidateIssuer = true,
            ValidIssuers = allowedIssuers,

            ValidateAudience = true,
            ValidAudiences = allowedAudiences,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "name",
            RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
        };
    });



    // Error Handling
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));

        options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));
    });
    var app = builder.Build();


    // Database migration at startup
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context =
                services.GetRequiredService<TrackrContext>();

            context.Database.Migrate();

            Log.Information("Databasen er klar og opdateret.");
        }
        catch (Exception ex)
        {
            Log.Error(ex,
                "Der skete en fejl under oprettelse/opdatering af databasen.");
        }
    }


    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        // app.UseHttpsRedirection(); // Uncomment in local production

    }
    app.UseExceptionHandler();
    //app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TrackrV2 API v1");
        c.RoutePrefix = "swagger";
    });
    app.UseRouting();
    app.UseCors("CorsPolicy");
    app.UseIpRateLimiting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Applikationen crashede uventet under opstart!");
}
finally
{
    Log.Information("Applikationen lukker ned...");
    Log.CloseAndFlush();
}