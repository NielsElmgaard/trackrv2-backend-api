using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
        options.AddPolicy("ReactApp", policy =>
        {
            policy.WithOrigins("http://localhost:5173") // Vite dev server
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    builder.Host.UseSerilog();

    var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

    // ONLY use the internal Docker routing if physically inside a container
    var isDocker =
        Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") ==
        "true";

    string connectionString;
    if (isDocker)
    {
        // Inside Docker container network
        connectionString =
            $"Host=db;Port=5432;Database=trackrv2_db;Username=postgres;Password={dbPassword};";
    }
    else
    {
        // Running locally via IDE or Terminal on Windows
        connectionString =
            $"Host=127.0.0.1;Port=5432;Database=trackrv2_db;Username=postgres;Password={dbPassword};";
    }

    // EFC
    builder.Services.AddDbContext<TrackrContext>(options =>
        options.UseNpgsql(connectionString));

    // Add services to the container.

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy =
                JsonNamingPolicy.CamelCase;
        });
    builder.Services.AddMemoryCache();
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
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
                Id = JwtBearerDefaults.AuthenticationScheme,
                Type = ReferenceType.SecurityScheme
            }
        };
        options.AddSecurityDefinition("Bearer", jwtSecurityScheme);
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            { jwtSecurityScheme, Array.Empty<string>() }
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
            ValidateIssuerSigningKey = true
        };
    });



    // Error Handling
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        options.AddPolicy("UserOnly", policy => policy.RequireRole("User").RequireAssertion(context => !context.User.IsInRole("Admin")));
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
        app.UseSwagger();
        app.MapOpenApi();
        app.UseSwaggerUI();
    }

    app.UseExceptionHandler();
    app.UseCors("ReactApp");

    // app.UseHttpsRedirection(); // Uncomment in production

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