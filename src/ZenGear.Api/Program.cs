using System.Text;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using ZenGear.Api.Middleware;
using ZenGear.Application;
using ZenGear.Domain.Common;
using ZenGear.Infrastructure;
using ZenGear.Infrastructure.Identity;
using ZenGear.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ===== LAYER SERVICES =====

// Add Application layer services
builder.Services.AddApplicationServices();

// Add Infrastructure layer services (Database, Identity, Repositories)
builder.Services.AddInfrastructureServices(builder.Configuration);

// ===== EXCEPTION HANDLING =====

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ===== AUTHENTICATION & AUTHORIZATION =====

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey not configured.");

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
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// ===== API VERSIONING =====

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'V";
    options.SubstituteApiVersionInUrl = true;
});

// ===== CONTROLLERS & OPENAPI =====

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Configure Swagger with JWT Bearer authentication
builder.Services.AddSwaggerGen(options =>
{
    // Add JWT Bearer security definition
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Enter your token in the text input below."
    });

    // Add security requirement (Func<OpenApiDocument, OpenApiSecurityRequirement> in v10+)
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document),
            new List<string>()
        }
    });

    // API information
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ZenGear API",
        Version = "v1",
        Description = "E-commerce platform for computer parts and gaming gear",
        Contact = new OpenApiContact
        {
            Name = "ZenGear Support",
            Email = "support@zengear.com"
        }
    });
});

// ===== CORS =====

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
            builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? ["http://localhost:3000"])
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

// ===== DATABASE INITIALIZATION =====

// Seed database with initial data (roles, admin user)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<long>>>();
        var externalIdGenerator = services.GetRequiredService<IExternalIdGenerator>();

        await ApplicationDbContextSeed.SeedAsync(
            context,
            userManager,
            roleManager,
            externalIdGenerator);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// ===== HTTP PIPELINE =====

// Exception handling (must be first)
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ZenGear API v1");
    });
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();



