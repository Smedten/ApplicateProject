using Applicate.Domain;
using Applicate.Domain.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --- 1. ENGINE SETUP (Samme som i CLI) ---
builder.Services.AddSingleton<ResourceService>();
builder.Services.AddScoped<ActionService>();
builder.Services.AddScoped<QueryExecutor>();
builder.Services.AddDbContext<AppDbContext>();
builder.Services.AddScoped<DataService>();
builder.Services.AddScoped<ExportService>();

// Tilføj controllere
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});
// JWT SETUP
var key = "super_hemmelig_nøgle_der_er_mindst_32_tegn_lang"; // Skal matche AuthController!
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "Applicate",
            ValidAudience = "ApplicateUsers",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
        };
    });

// VIGTIGT: Vi skal kunne tilgå brugeren nede i vores Services (Domain layer)
builder.Services.AddHttpContextAccessor();


var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var resourceService = scope.ServiceProvider.GetRequiredService<ResourceService>();

    // Vi antager at 'specs' mappen ligger ved siden af API'ets .exe fil
    var specsPath = Path.Combine(AppContext.BaseDirectory, "specs"); 

    // Opret mappen hvis den mangler, for at undgå crash
    if (!Directory.Exists(specsPath)) Directory.CreateDirectory(specsPath);

    await resourceService.LoadFromDirectoryAsync(specsPath);
}

// --- 3. STANDARD API SETUP ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication(); // "Hvem er du?"
app.UseAuthorization();  // "Må du det?"
app.UseCors("AllowAll");
app.MapControllers();
app.Run();