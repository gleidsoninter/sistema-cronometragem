using AutoMapper;
using DevNationCrono.API.Configuration;
using DevNationCrono.API.Data;
using DevNationCrono.API.Hubs;
using DevNationCrono.API.Middlewares;
using DevNationCrono.API.Repositories.Implementations;
using DevNationCrono.API.Repositories.Interfaces;
using DevNationCrono.API.Services.Background;
using DevNationCrono.API.Services.Implementations;
using DevNationCrono.API.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

builder.Services.Configure<PagamentoSettings>(
    builder.Configuration.GetSection("PagamentoSettings"));

var pagamentoSettings = builder.Configuration
    .GetSection("PagamentoSettings")
    .Get<PagamentoSettings>();

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

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
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.Secret)
            ),
            ClockSkew = TimeSpan.Zero // Remove os 5 min de tolerância padrão
        };

        // Eventos para debug
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Token inválido: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine($"Token válido para: {context.Principal?.Identity?.Name}");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Políticas de acesso
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("OrganizadorOnly", policy => policy.RequireRole("Organizador", "Admin"));
    options.AddPolicy("PilotoOnly", policy => policy.RequireRole("Piloto"));
    options.AddPolicy("ColetorOnly", policy => policy.RequireRole("Coletor"));
    options.AddPolicy("PilotoOrOrganizador", policy =>
        policy.RequireRole("Piloto", "Organizador", "Admin"));
});

// Add services to the container.

var isTestEnvironment = builder.Environment.IsEnvironment("Testing") ||
                        Environment.GetEnvironmentVariable("ASPNETCORE_TESTING") == "true";


// ===== ENTITY FRAMEWORK =====
if (!isTestEnvironment)
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseMySql(
            connectionString,
            ServerVersion.AutoDetect(connectionString),
            mySqlOptions =>
            {
                mySqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
            });
    });
}

builder.Services.AddAutoMapper(typeof(AutoMapperProfile));
builder.Services.AddScoped<IPilotoRepository, PilotoRepository>();
builder.Services.AddScoped<IModalidadeRepository, ModalidadeRepository>();
builder.Services.AddScoped<IEventoRepository, EventoRepository>();
builder.Services.AddScoped<ICategoriaRepository, CategoriaRepository>();
builder.Services.AddScoped<IEtapaRepository, EtapaRepository>();
builder.Services.AddScoped<IInscricaoRepository, InscricaoRepository>();
builder.Services.AddScoped<IPagamentoRepository, PagamentoRepository>();
builder.Services.AddScoped<ITempoRepository, TempoRepository>();
builder.Services.AddScoped<IDispositivoColetorRepository, DispositivoColetorRepository>();

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPilotoService, PilotoService>();
builder.Services.AddScoped<IModalidadeService, ModalidadeService>();
builder.Services.AddScoped<IEventoService, EventoService>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IEtapaService, EtapaService>();
builder.Services.AddScoped<IInscricaoService, InscricaoService>();
builder.Services.AddScoped<ICronometragemService, CronometragemService>();
builder.Services.AddScoped<IResultadoEnduroService, ResultadoEnduroService>();
builder.Services.AddScoped<IResultadoCircuitoService, ResultadoCircuitoService>();
builder.Services.AddScoped<INotificacaoTempoRealService, NotificacaoTempoRealService>();
builder.Services.AddScoped<IExportacaoService, ExportacaoService>();



if (pagamentoSettings?.GatewayAtivo == "Asaas")
{
    builder.Services.AddHttpClient<IPagamentoService, AsaasService>();
}
else
{
    builder.Services.AddHttpClient<IPagamentoService, MercadoPagoService>();
}

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "CronometragemCache_";
});

builder.Services.AddMemoryCache();

builder.Services.AddHostedService<VerificacaoPagamentosJob>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",  // React dev
                "http://localhost:5173",  // Vite dev
                "https://seudominio.com"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Necessário para SignalR
    });
});

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressModelStateInvalidFilter = false;
    });
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen(options =>
//{
//    // Informações da API
//    options.SwaggerDoc("v1", new OpenApiInfo
//    {
//        Version = "v1",
//        Title = "Sistema de Cronometragem API",
//        Description = "API completa para cronometragem de eventos de Motocross, Enduro e Velocross",
//        Contact = new OpenApiContact
//        {
//            Name = "Suporte Técnico",
//            Email = "suporte@cronometragem.com.br",
//            Url = new Uri("https://cronometragem.com.br/suporte")
//        },
//        License = new OpenApiLicense
//        {
//            Name = "Uso Interno",
//            Url = new Uri("https://cronometragem.com.br/licenca")
//        }
//    });

//    // Autenticação JWT no Swagger
//    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//    {
//        Name = "Authorization",
//        Type = SecuritySchemeType.Http,
//        Scheme = "Bearer",
//        BearerFormat = "JWT",
//        In = ParameterLocation.Header,
//        Description = "Insira o token JWT no formato: Bearer {seu_token}"
//    });

//    options.AddSecurityRequirement(new OpenApiSecurityRequirement
//    {
//        {
//            new OpenApiSecurityScheme
//            {
//                Reference = new OpenApiReference
//                {
//                    Type = ReferenceType.SecurityScheme,
//                    Id = "Bearer"
//                }
//            },
//            Array.Empty<string>()
//        }
//    });

//    // Incluir comentários XML
//    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
//    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
//    if (File.Exists(xmlPath))
//    {
//        options.IncludeXmlComments(xmlPath);
//    }

//    // Agrupar por tags
//    options.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });
//    options.DocInclusionPredicate((name, api) => true);

//    // Ordenar endpoints
//    options.OrderActionsBy(api => api.RelativePath);
//}); 
builder.Services.AddSwaggerConfiguration();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true; // Retorna versões suportadas no header
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(), // /api/v1/pilotos
        new HeaderApiVersionReader("X-Api-Version"), // Header: X-Api-Version: 1.0
        new QueryStringApiVersionReader("api-version") // ?api-version=1.0
    );
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger(options =>
    {
        options.RouteTemplate = "api/docs/{documentName}/swagger.json";
    });

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/api/docs/v1/swagger.json", "Cronometragem API v1");
        options.RoutePrefix = "api/docs";
        options.DocumentTitle = "Cronometragem API - Documentação";
        options.DefaultModelsExpandDepth(2);
        options.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Model);
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
        options.EnableFilter();
        options.EnableDeepLinking();
        options.DisplayRequestDuration();
    });
}
app.UseCors("CorsPolicy");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<CronometragemHub>("/hubs/cronometragem");

app.Run();

public partial class Program { }