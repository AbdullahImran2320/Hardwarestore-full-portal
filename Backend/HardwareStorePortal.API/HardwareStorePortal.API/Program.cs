using HardwareStorePortal.API.Data;
using HardwareStorePortal.API.Models;
using HardwareStorePortal.API.Repositories;
using HardwareStorePortal.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var dbFolder = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
    "HardwareStorePortal");
Directory.CreateDirectory(dbFolder);
var dbPath = Path.Combine(dbFolder, "hardwarestore.db");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));
var jwtKey = builder.Configuration["Jwt:Key"]!;

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
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IBillRepository, BillRepository>();
builder.Services.AddScoped<IBillService, BillService>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "HardwareStorePortal.API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token below. Just the token itself — Swagger adds the word 'Bearer' automatically."
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    var productsNeedingCategory = db.Products
    .Where(p => p.CategoryId == null && p.LegacyCategoryText != null && p.LegacyCategoryText != "")
    .ToList();

    foreach (var product in productsNeedingCategory)
    {
        var categoryName = product.LegacyCategoryText!.Trim();

        var category = db.Categories.FirstOrDefault(c => c.Name.ToLower() == categoryName.ToLower());
        if (category == null)
        {
            category = new Category { Name = categoryName };
            db.Categories.Add(category);
            db.SaveChanges();
        }

        product.CategoryId = category.Id;
    }

    db.SaveChanges();

    if (!db.Users.Any())
    {
        db.Users.AddRange(
            new User { Username = "admin", Role = "Admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!") },
            new User { Username = "Muneeb", Role = "Admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword("muneeb786") },
            new User { Username = "Shahid", Role = "Staff", PasswordHash = BCrypt.Net.BCrypt.HashPassword("sm786") }
        );
        db.SaveChanges();
    }
}

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

// CORS + authentication
app.UseCors("AllowAngularDev");
app.UseAuthentication();
app.UseAuthorization();

// Serve the Angular frontend from wwwroot.
// IMPORTANT: HTTPS redirection is intentionally disabled because
// the installed portal runs locally on http://localhost:5000.
app.UseDefaultFiles();
app.UseStaticFiles();

// API endpoints
app.MapControllers();

// Angular client-side routes such as:
// /dashboard
// /inventory
// /customers/123
// should all fall back to index.html.
app.MapFallbackToFile("index.html");

app.Run();
