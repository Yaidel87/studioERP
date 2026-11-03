using Microsoft.EntityFrameworkCore;
using StudioERP.Infrastructure.Data;
using StudioERP.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using Microsoft.OpenApi; // <-- El nuevo namespace de .NET 10

var builder = WebApplication.CreateBuilder(args);

// 1. Base de Datos PostgreSQL
builder.Services.AddDbContext<StudioDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Inyección de HttpContext (para auditoría)
builder.Services.AddHttpContextAccessor();

// 3. Servicio de Autenticación
builder.Services.AddScoped<AuthService>();

// 4. Configuración de Seguridad (JWT)
var jwtKey = builder.Configuration["Jwt:Key"] ?? "ClaveSecretaSuperLargaParaElERPStudio1234567890!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "StudioERP",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "StudioERP",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            
            // VITAL PARA QUE FUNCIONE [Authorize(Roles = "Admin")]
            RoleClaimType = ClaimTypes.Role 
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers(options =>
{
    // Permitir subir archivos de hasta 10 MB
    options.MaxModelBindingCollectionSize = 1000;
});
builder.Services.AddEndpointsApiExplorer();

// 6. Swagger configurado para .NET 10
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresa el token JWT. Ejemplo: eyJhbGciOiJIUzI1NiIs..."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Permite servir las imágenes de wwwroot/uploads
app.UseStaticFiles(); 

app.UseHttpsRedirection();

// 7. Middleware de Seguridad
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Inicializar datos base (Roles y Especialidades)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        StudioERP.Infrastructure.Data.DbInitializer.InitializeAsync(services).GetAwaiter().GetResult();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al inicializar la base de datos.");
    }
}


app.Run();