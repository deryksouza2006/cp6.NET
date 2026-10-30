using System.Text;
using CP6_CatalogoJogos.OpenApi;
using CP6_CatalogoJogos.Repositories;
using CP6_CatalogoJogos.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32
    || string.IsNullOrWhiteSpace(jwtIssuer) || string.IsNullOrWhiteSpace(jwtAudience)
    || builder.Configuration.GetValue<int>("Jwt:ExpirationSeconds", 3600) <= 0)
{
    throw new InvalidOperationException("Configure Jwt:Key (mínimo 32 bytes), Issuer, Audience e ExpirationSeconds positivo.");
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<IJogoRepository, JogoRepository>();
builder.Services.AddScoped<JogoService>();
builder.Services.AddScoped<TokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
            // Sem a tolerância padrão de cinco minutos: expirou, retorna 401.
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CP6 - Catálogo de Jogos",
        Version = "v1",
        Description = "API acadêmica com CRUD em memória, JWT e relatórios LINQ. Login: Mauricio / 123456."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Faça login e informe apenas o conteúdo do token, sem o prefixo Bearer."
    });
    options.OperationFilter<BearerSecurityOperationFilter>();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options => options.RouteTemplate = "openapi/{documentName}.json");
    app.MapScalarApiReference(options => options
        .WithTitle("CP6 - Catálogo de Jogos")
        .WithOpenApiRoutePattern("/openapi/{documentName}.json"));
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
