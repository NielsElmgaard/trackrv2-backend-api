using System.Text;
using System.Text.Json;
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
            connectionString = $"Host=db;Port=5432;Database=trackrv2_db;Username=postgres;Password={dbPassword};";
        }
        else
        {
            // Din direkte forbindelse til lokal brug
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
        options.RequireHttpsMetadata = true; // change to TRUE in production
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = builder.Configuration["JwtConfig:Issuer"],
            ValidAudience = builder.Configuration["JwtConfig:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    builder.Configuration["JwtConfig:Key"]!)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
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


    // Database
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

    //app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TrackrV2 API v1");
        c.RoutePrefix = "swagger";
    });

    app.UseExceptionHandler();
    app.UseCors("CorsPolicy");

    app.UseRouting();
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