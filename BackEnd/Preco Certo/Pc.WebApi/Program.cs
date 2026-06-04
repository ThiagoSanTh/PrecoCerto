using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Pc.Infraestrutura;
using Pc.Repositorio.Implementacoes;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Implementacoes;
using Pc.Servico.Interfaces;
using Pc.WebApi.Authorization;
using Pc.WebApi.Configuration;
using Pc.WebApi.Middleware;
using Pc.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.WebHost.UseUrls("http://0.0.0.0:5132");
}

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:8081", "http://localhost:19006"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("MobilePolicy", policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
var jwtKey = jwtSettings.Key;

if (string.IsNullOrWhiteSpace(jwtKey))
{
    if (builder.Environment.IsDevelopment())
        jwtKey = "PrecoCertoDevKeyMinimo32Caracteres!!";
    else
        throw new InvalidOperationException("Jwt:Key deve ser configurada via variáveis de ambiente em produção.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PoliticasAutorizacao.Cliente, p => p.RequireRole("Cliente"));
    options.AddPolicy(PoliticasAutorizacao.Lojista, p => p.RequireRole("Lojista"));
    options.AddPolicy(PoliticasAutorizacao.Admin, p => p.RequireRole("Admin"));
    options.AddPolicy(PoliticasAutorizacao.LojistaOuAdmin, p =>
        p.RequireRole("Lojista", "Admin"));
    options.AddPolicy(PoliticasAutorizacao.QualquerAutenticado, p =>
        p.RequireAuthenticatedUser());
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 10;
        opt.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("busca", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 60;
        opt.QueueLimit = 0;
    });
});

// Repositórios — Catálogo e Estabelecimentos
builder.Services.AddScoped<IProdutoRepositorio, ProdutoRepositorio>();
builder.Services.AddScoped<ILojaRepositorio, LojaRepositorio>();
builder.Services.AddScoped<IOfertaRepositorio, OfertaRepositorio>();

// Repositórios — Usuários
builder.Services.AddScoped<IClienteRepositorio, ClienteRepositorio>();
builder.Services.AddScoped<ILojistaRepositorio, LojistaRepositorio>();
builder.Services.AddScoped<IAdminRepositorio, AdminRepositorio>();

// Repositórios — Interações
builder.Services.AddScoped<IFavoritoRepositorio, FavoritoRepositorio>();
builder.Services.AddScoped<IHistoricoPesquisaRepositorio, HistoricoPesquisaRepositorio>();
builder.Services.AddScoped<IAvaliacaoRepositorio, AvaliacaoRepositorio>();
builder.Services.AddScoped<IPreferenciaClienteRepositorio, PreferenciaClienteRepositorio>();

// Serviços — Segurança
builder.Services.AddSingleton<ISenhaServico, SenhaServico>();
builder.Services.AddScoped<IJwtTokenServico, JwtTokenServico>();

// Serviços — Catálogo e Estabelecimentos
builder.Services.AddScoped<IProdutoServico, ProdutoServico>();
builder.Services.AddScoped<ILojaServico, LojaServico>();
builder.Services.AddScoped<IOfertaServico, OfertaServico>();

// Serviços — Usuários
builder.Services.AddScoped<IClienteServico, ClienteServico>();
builder.Services.AddScoped<ILojistaServico, LojistaServico>();
builder.Services.AddScoped<IAdminServico, AdminServico>();

// Serviços — Interações
builder.Services.AddScoped<IFavoritoServico, FavoritoServico>();
builder.Services.AddScoped<IHistoricoPesquisaServico, HistoricoPesquisaServico>();
builder.Services.AddScoped<IAvaliacaoServico, AvaliacaoServico>();
builder.Services.AddScoped<IPreferenciaClienteServico, PreferenciaClienteServico>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
    app.UseHsts();
}

app.UseCors("MobilePolicy");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
