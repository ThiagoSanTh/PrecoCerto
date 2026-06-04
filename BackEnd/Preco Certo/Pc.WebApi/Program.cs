using Microsoft.EntityFrameworkCore;
using Pc.Infraestrutura;
using Pc.Repositorio.Implementacoes;
using Pc.Repositorio.Interfaces;
using Pc.Servico.Implementacoes;
using Pc.Servico.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5132");

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("MobilePolicy", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("MobilePolicy");
app.UseAuthorization();
app.MapControllers();

app.Run();
