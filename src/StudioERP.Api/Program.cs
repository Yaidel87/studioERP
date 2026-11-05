using Microsoft.EntityFrameworkCore;
using StudioERP.Infrastructure.Data;
using StudioERP.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.OpenApi; // Namespace unificado de OpenAPI en .NET 10

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("StudioERPWeb", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5150",
                "https://localhost:7222",
                "http://localhost:5046",
                "https://localhost:7139")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

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
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);

                if (!Guid.TryParse(userIdValue, out var userId))
                {
                    context.Fail("El token no contiene un identificador de usuario válido.");
                    return;
                }

                await using var scope = context.HttpContext.RequestServices.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<StudioDbContext>();
                var user = await dbContext.Usuarios
                    .AsNoTracking()
                    .Where(user => user.Id == userId)
                    .Select(user => new
                    {
                        user.Activo,
                        user.IsDeleted,
                        RoleName = user.Rol.Nombre
                    })
                    .FirstOrDefaultAsync(
                        context.HttpContext.RequestAborted);

                if (user == null || !user.Activo || user.IsDeleted)
                {
                    context.Fail("La cuenta está inactiva o fue eliminada.");
                    return;
                }

                if (context.Principal?.Identity is ClaimsIdentity identity)
                {
                    foreach (var roleClaim in identity.FindAll(identity.RoleClaimType).ToList())
                        identity.RemoveClaim(roleClaim);

                    identity.AddClaim(new Claim(identity.RoleClaimType, user.RoleName));
                }
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddControllers(options =>
{
    // Permitir subir archivos de hasta 10 MB
    options.MaxModelBindingCollectionSize = 1000;
});

builder.Services.AddEndpointsApiExplorer();

// 6. Swagger configurado para .NET 10
builder.Services.AddSwaggerGen(options =>
{
    // Evita el choque de nombres entre clases anidadas (ej. CategoriaRequest)
    options.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);

    // SOLUCIÓN AL ERROR 500: Resuelve conflictos si hay dos endpoints con la misma ruta y método
    options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

    // Definición del esquema de seguridad JWT
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Introduce el token JWT (sin la palabra 'Bearer ' delante)."
    });

    // Requerimiento de seguridad (nueva API de Microsoft.OpenApi en .NET 10)
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
app.UseCors("StudioERPWeb");

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