using BLL.IServices;
using BLL.Services;
using DAL.IRepositories;
using DAL.Repositories;
using DAL.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartFoodAPI.Common;
using SmartFoodAPI.Middlewares;
using System.Text;
using System.Text.Json.Serialization;
using PayOS;
using System.IdentityModel.Tokens.Jwt;
using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var _logger = loggerFactory.CreateLogger<Program>();

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRestaurantRepository, RestaurantRepository>();
builder.Services.AddScoped<IRestaurantService, RestaurantService>();
builder.Services.AddHttpClient<IImageService, AnhMoeImageService>();
builder.Services.AddScoped<ISellerRepository, SellerRepository>();
builder.Services.AddScoped<IMenuItemRepository, MenuItemRepository>();
builder.Services.AddScoped<IMenuItemService, MenuItemService>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IAreaRepository, AreaRepository>();
builder.Services.AddScoped<IAreaService, AreaService>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISellerService, SellerService>();
builder.Services.AddHttpClient(); // For ZaloPay API calls
builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.AddSingleton<PayOSClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new PayOSClient(
        clientId: config["PayOS:ClientId"],
        apiKey: config["PayOS:ApiKey"],
        checksumKey: config["PayOS:ChecksumKey"]
    );
});

// Configure ZaloPay settings
builder.Services.Configure<ZaloPaySettings>(builder.Configuration.GetSection("ZaloPay"));

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SmartFoodAPI",
        Version = "v1",
        Description = "API documentation with JWT authentication"
    });

    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "Enter 'Bearer {your JWT token}'",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    c.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { jwtSecurityScheme, Array.Empty<string>() } });
});

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddDbContext<SmartFoodContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        // Get the logger from the DI container
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

        // Log all validation errors
        foreach (var (key, value) in context.ModelState)
        {
            if (value.Errors.Any())
            {
                var errors = string.Join(", ", value.Errors.Select(e => e.ErrorMessage));
                logger.LogError("Model validation error for key '{Key}': {Errors}", key, errors);
            }
        }

        var firstError = context.ModelState.Values.SelectMany(v => v.Errors)
            .FirstOrDefault()?.ErrorMessage ?? "Invalid input";
        var error = ErrorResponse.FromStatus(400, firstError);
        return new BadRequestObjectResult(error);
    };
});

// Clear default claim type mappings to prevent 'sub' -> 'nameidentifier' mapping
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// JWT & Google OAuth
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                var error = ErrorResponse.FromStatus(401, "Authentication required");
                return context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(error));
            },
            OnForbidden = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                var error = ErrorResponse.FromStatus(403, "Not authorized");
                return context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(error));
            }
        };
    })
    .AddCookie("ExternalCookie", options =>
    {
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddGoogle(options =>
    {
        options.SignInScheme = "ExternalCookie";
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
        options.CallbackPath = "/api/Auth/google-response";
        options.SaveTokens = true;
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.Events = new Microsoft.AspNetCore.Authentication.OAuth.OAuthEvents
        {
            OnRemoteFailure = context =>
            {
                context.Response.Redirect($"{builder.Configuration["Frontend:BaseUrl"]}/auth/failed?error=oauth_failed");
                context.HandleResponse();
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ConfigureHttpsDefaults(co =>
    {
        co.AllowAnyClientCertificate();
    });
});

builder.Services.AddAuthorization();

// Build app
var app = builder.Build();

// Configure pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartFoodAPI v1");
});

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseSession();

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/Auth/google-response") &&
        !context.Request.Query.ContainsKey("state") &&
        !context.Request.Query.ContainsKey("code"))
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogWarning("[GoogleOAuth] Duplicate empty callback intercepted by middleware.");
        context.Response.StatusCode = 200;
        await context.Response.WriteAsJsonAsync(new { message = "Duplicate Google callback ignored (no state or code).", handled = true });
        return;
    }
    await next();
});

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ErrorHandlingMiddleware>();


app.MapControllers();

// Fallback & Controllers
app.MapFallback(context =>
{
    context.Response.StatusCode = 404;
    return context.Response.WriteAsync("Endpoint not found");
});

app.Run();
