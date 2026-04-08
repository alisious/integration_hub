using IntegrationHub.Api.Config;
using IntegrationHub.Api.Middleware;
using IntegrationHub.Api.Services;
using IntegrationHub.Application.ANPRS;
using IntegrationHub.Application.RequestValidators.ZW;
using IntegrationHub.Application.ZW;
using IntegrationHub.Common.Config;
using IntegrationHub.Common.Interfaces;
using IntegrationHub.Common.Providers;
using IntegrationHub.Common.RequestValidation;
using IntegrationHub.Domain.Contracts.ZW;
using IntegrationHub.Infrastructure.Audit;
using IntegrationHub.Infrastructure.Audit.Http;
using IntegrationHub.Infrastructure.Cepik;
using IntegrationHub.Infrastructure.Sql;
using IntegrationHub.PIESP.Data;
using IntegrationHub.PIESP.Services;
using IntegrationHub.Sources.ANPRS.Config;
using IntegrationHub.Sources.ANPRS.Extensions;
using IntegrationHub.Sources.CEP.Config;
using IntegrationHub.Sources.CEP.Services;
using IntegrationHub.Sources.CEP.Udostepnianie.Services;
using IntegrationHub.Sources.CEP.UpKi.Extensions;

using IntegrationHub.Sources.KSIP.Config;
using IntegrationHub.Sources.KSIP.Services;
using IntegrationHub.Sources.KSIP.Extensions;
using IntegrationHub.Sources.ZW.Extensions;
using IntegrationHub.SRP.Config;
using IntegrationHub.SRP.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore; // Add this using directive for 'UseSqlServer'
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Swashbuckle.AspNetCore.Filters;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Security;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Trentum.Horkos;



// Serilog � bootstrap logger, �eby logowa� od samego pocz�tku
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

// Tryb CI/offline: uruchom minimalną aplikację tylko po to, aby wystawić swagger.json.
// Użycie (env var): SwaggerGenOnly=true
var swaggerGenOnly = builder.Configuration.GetValue<bool>("SwaggerGenOnly");
if (swaggerGenOnly)
{
    builder.Services.AddControllers();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Integration Hub API",
            Version = "v1",
        });
        options.EnableAnnotations();
        options.ExampleFilters();

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "Wprowadź JWT w formacie 'Bearer {token}'",
            Name = "Authorization",
            Type = SecuritySchemeType.ApiKey
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    // examples (nie wymagają uruchamiania zewnętrznych serwisów)
    builder.Services.AddSwaggerExamplesFromAssemblyOf<
        IntegrationHub.Api.Swagger.Examples.SRP.SearchPerson200Example>();
    builder.Services.AddSwaggerExamplesFromAssemblyOf<
        IntegrationHub.Api.Swagger.Examples.PIESP.Login401Example>();
    builder.Services.AddSwaggerExamplesFromAssemblyOf<
        IntegrationHub.Api.Swagger.Examples.ANPRS.Code400InvalidParameterExample>();

    var swaggerApp = builder.Build();
    swaggerApp.UseSwagger();
    swaggerApp.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Integration Hub API V1");
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    });

    swaggerApp.MapControllers();
    swaggerApp.Run();
    return;
}

// KONFIGURACJA SERILOG � musi by� przed builder.Build() U�ywaj konfiguracji Seriloga z appsettings.*.json
builder.Logging.ClearProviders(); // usu� domy�lnego ConsoleLoggera itp.
builder.Host.UseSerilog((ctx, services, cfg) =>
cfg.ReadFrom.Configuration(ctx.Configuration)
       .ReadFrom.Services(services));

//Konfiguracja CORS � PROD + localhost (dev)
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("PIESP", p =>
        p
        // zezw�l na originy z appsettings (PROD)
        .WithOrigins(allowedOrigins)
        // i dodatkowo zezw�l na localhost (dev)
        .SetIsOriginAllowed(origin =>
        {
            if (Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.IsLoopback) return true;
            return allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
        })
        .AllowAnyHeader()
        .AllowAnyMethod()
        // .AllowCredentials() // w��cz TYLKO je�li faktycznie u�ywacie cookies/credentials
        .SetPreflightMaxAge(TimeSpan.FromHours(1))
    );
});



// Add services to the container.
// ====== DB CONTEXT ======
builder.Services.AddDbContext<PiespDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("IntegrationHubDB")));

//Rejestracja ClientCertificateProvider
builder.Services.AddSingleton<IClientCertificateProvider, ClientCertificateProvider>();

var integrationHubDbConnectionString = builder.Configuration.GetConnectionString("IntegrationHubDB")
             ?? throw new InvalidOperationException("Missing ConnectionStrings:IntegrationHubDB.");
var horkosDbConnectionString = builder.Configuration.GetConnectionString("HorkosDB")
             ?? throw new InvalidOperationException("Missing ConnectionStrings:HorkosDB.");

builder.Services.AddHorkosDapper(horkosDbConnectionString);
builder.Services.Configure<FileExportOptions>(builder.Configuration.GetSection(FileExportOptions.SectionName));
builder.Services.AddScoped<IExportFileService, ExportFileService>();
builder.Services.AddIntegrationHubSqlInfrastructure(integrationHubDbConnectionString);



//===========Audyt handlers=====================

builder.Services.AddSingleton<IRequestContext, RequestContext>();
builder.Services.AddSingleton<SqlAuditSink>();
builder.Services.AddSingleton<IAuditSink>(sp => sp.GetRequiredService<SqlAuditSink>());
builder.Services.AddHostedService<SqlAuditSink.Worker>();
builder.Services.AddSingleton<ISourceCallAuditor, SourceCallAuditor>();
builder.Services.AddTransient<Func<string, SourceAuditHandler>>(provider => sourceName =>
{
    return new SourceAuditHandler(
        sourceName,
        provider.GetRequiredService<ISourceCallAuditor>(),
        provider.GetRequiredService<IConfiguration>(),
        provider.GetRequiredService<ILogger<SourceAuditHandler>>());
});
//builder.Services.AddTransient<SrpHttpLoggingHandler>();


/**************************************************************/
// ====== SRP CLIENT ======
// Rejestracja konfiguracji SRP
builder.Services.Configure<SrpConfig>(builder.Configuration.GetSection("ExternalServices:SRP"));
var srpConfig = builder.Configuration.GetSection("ExternalServices:SRP").Get<SrpConfig>();


builder.Services.AddHttpClient("SrpServiceClient", c =>
{
    // Ca�kowity timeout HttpClient wy��czamy � kontrolujemy czas przez pipeline (Attempt/Total)
    c.Timeout = Timeout.InfiniteTimeSpan;
    // Spr�buj HTTP/2 (wenn dost�pny), z fallbackiem w d� � lepsze mno�enie strumieni
    c.DefaultRequestVersion = HttpVersion.Version20;
    c.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
})
// 1) Handler audytu �r�de� � wsp�lny mechanizm dla SRP
.AddHttpMessageHandler(sp =>
{
    var factory = sp.GetRequiredService<Func<string, SourceAuditHandler>>();
    return factory("SRP");
})

// Handler gniazd � klucz do wydajno�ci r�wnoleg�ych wywo�a�
.ConfigurePrimaryHttpMessageHandler(sp =>
{

    // Uwaga: NIE �adujemy certyfikatu w trybie testowym
    if (srpConfig?.TestMode == true)
    {
        
        Log.Warning("SRP dzia�a w TRYBIE TESTOWYM. Nie u�ywam certyfikatu klienta.");
        return new HttpClientHandler();

    }


    var config = sp.GetRequiredService<IOptions<SrpConfig>>().Value;
    var certProvider = sp.GetRequiredService<IClientCertificateProvider>();
    var clientCert = certProvider.GetClientCertificate(config);

    // ZLATA ZASADA: MaxConnectionsPerServer >= 2 * maxParallel (zapas)
    var maxConn = Math.Max(16, (config.HttpMaxConnectionsPerServer ?? 0)); // opcjonalnie z appsettings
    if (maxConn <= 0) maxConn = 32; // domy�lnie pod bulk

    var h = new SocketsHttpHandler
    {
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        MaxConnectionsPerServer = maxConn,
        PooledConnectionLifetime = TimeSpan.FromMinutes(5), // rotacja po��cze� (DNS/zdrowie)
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
        ConnectTimeout = TimeSpan.FromSeconds(5),
        // W .NET 8 dost�pne: utrzymanie po��cze� H2 przy d�u�szej bezczynno�ci
        KeepAlivePingDelay = TimeSpan.FromSeconds(30),
        KeepAlivePingTimeout = TimeSpan.FromSeconds(15),
        KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always
    };

    h.SslOptions = new SslClientAuthenticationOptions
    {
        ClientCertificates = new X509CertificateCollection { clientCert },
        RemoteCertificateValidationCallback = config.TrustServerCertificate
            ? new RemoteCertificateValidationCallback((_, _, _, _) => true)
            : null
    };

    return h;
})

// Polly v8 � gotowy �standard� z dopracowaniem czas�w i prog�w
.AddStandardResilienceHandler(opt =>
{
    // 1) Timeout pojedynczej pr�by (wa�ne przy retrach)
    opt.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);

    // 2) ��czny limit czasu ca�ego ��dania (przez wszystkie retry)
    opt.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);

    // 3) Retry � exponential + jitter (domy�lnie na 5xx/408/transport; 429 te� jest sensowny na odczytach)
    opt.Retry.MaxRetryAttempts = 3;

    // 4) Circuit Breaker � �eby nie �m�czy� us�ugi gdy ewidentnie ma k�opot
    opt.CircuitBreaker.FailureRatio = 0.2;                // 20% pora�ek w oknie => przerwa
    opt.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
    opt.CircuitBreaker.MinimumThroughput = 20;            // minimalna liczba pr�b do oceny
    opt.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
});


/**************************************************************/
// ====== CEP CLIENT ======
// Rejestracja konfiguracji CEP
builder.Services.Configure<CEPConfig>(builder.Configuration.GetSection("ExternalServices:CEP"));
var cepConfig = builder.Configuration.GetSection("ExternalServices:CEP").Get<CEPConfig>();






// ====== DEPENDENCY INJECTION ======



builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<DutyService>();
builder.Services.AddScoped<SupervisorService>();
builder.Services.AddScoped<IntegrationHub.PIESP.Services.IDictService, IntegrationHub.PIESP.Services.DictService>();

builder.Services.AddSingleton<IRequestValidator<WPMRequest>, WPMRequestValidator>();

builder.Services.AddTransient<ISrpSoapInvoker, SrpSoapInvoker>();
builder.Services.AddTransient<ICepSoapInvoker, CepSoapInvoker>();

builder.Services.AddScoped<IANPRSDictionaryFacade, ANPRSDictionaryFacade>();
builder.Services.AddScoped<IANPRSReportsFacade, ANPRSReportsFacade>();
builder.Services.AddScoped<IANPRSSourceFacade, ANPRSSourceFacade>();
builder.Services.AddScoped<IANPRSReportWriter, ANPRSReportWriter>();
builder.Services.AddScoped<IZWSourceFacade, ZWSourceFacade>();

if (srpConfig!.TestMode)
{
    
    // Je�li TestMode, zarejestruj PeselService jako PeselServiceTest
    builder.Services.AddTransient<IPeselService, PeselServiceTest>();
    builder.Services.AddTransient<IRdoService, RdoServiceTest>();
    Log.Warning("SRP dzia�a w trybie testowym.");
}
else
{
   builder.Services.AddTransient<IRdoService, RdoService>();
   builder.Services.AddTransient<IPeselService, PeselService>();
   Log.Information("SRP w trybie produkcyjnym.");
}

if (cepConfig!.TestMode)
{
    builder.Services.AddScoped<ICEPSlownikiService, CEPSlownikiServiceTest>();
    builder.Services.AddScoped<ICEPUdostepnianieService, CEPUdostepnianieServiceTest>();
    Log.Warning("CEP dzia�a w trybie testowym.");
}
else
{
    builder.Services.AddScoped<ICEPSlownikiService, CEPSlownikiService>();
    builder.Services.AddScoped<ICEPUdostepnianieService, CEPUdostepnianieService>();
    //TODO: Zamieni� na UpKiService w trybie produkcyjnym
    Log.Information("CEP dzia�a w trybie produkcyjnym.");
}

/**************************************************************/
// ====== KSIP CLIENT ======
// Rejestracja konfiguracji KSIP
builder.Services.AddKSIP(builder.Configuration, out var ksipLogMessage);
Log.Information(ksipLogMessage);

/**************************************************************/
// ====== CEPiK UpKi CLIENT ======
builder.Services.AddCEPiKUpKi(builder.Configuration, out var cepikLogMessage);
Log.Information(cepikLogMessage);


// ====== ANPRS CLIENT ======
builder.Services.AddANPRS(builder.Configuration, out var anprsLogMessage);
Log.Information(anprsLogMessage);


builder.Services.AddZWSource(builder.Configuration, out var zwLogMessage);
Log.Information(zwLogMessage);



// ====== CONTROLLERS ======
builder.Services.AddControllers();

// ====== AUTHENTICATION (JWT + revocation via jti) ======
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]);
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        o.Events = new JwtBearerEvents
        {
            OnTokenValidated = async ctx =>
            {
                var auth = ctx.HttpContext.RequestServices.GetRequiredService<AuthService>();

                // 1) logout pojedynczego tokenu (JTI blacklist)
                var jti = ctx.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                if (string.IsNullOrEmpty(jti) || await auth.IsTokenRevokedAsync(jti))
                { ctx.Fail("Token revoked or missing jti."); return; }

                // 2) wersjonowanie tokenu (force-logout / zmiana r�l)
                var uidStr = ctx.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var verStr = ctx.Principal?.FindFirst("ver")?.Value;
                if (!Guid.TryParse(uidStr, out var uid) || !int.TryParse(verStr, out var ver))
                { ctx.Fail("Missing user id or token version."); return; }

                if (await auth.IsTokenVersionStaleAsync(uid, ver))
                { ctx.Fail("Token version is stale."); return; }
            }
        };
    });



// ====== SWAGGER ======
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{

    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Integration Hub API",
        Version = "v1",
        Description = @"System API dla aplikacji patrolowej �andarmerii Wojskowej.

    W bazie danych znajduj� si�:

    - **U�ytkownicy**: 'kpr. Jan Kowalski' (badge: 1111, PIN: 1111), 'mjr Tomasz Nowak' (badge: 2222, PIN: 2222), z przypisanymi rolami takimi jak `User` i `Supervisor`.
    - **PIN-y**: przechowywane jako hashe, logowanie wymaga numeru odznaki i PIN-u.
    - **S�u�by**: przypisane do u�ytkownika 1111, typy: 'Patrol pieszy', 'Patrol zapobiegawczy', 'Kontrola ruchu', 'Zabezpieczenie wydarzenia', w dniach od 18 do 21.06.2025 r.
    - **Kody bezpiecze�stwa**: generowane na 10 minut w celu resetu PIN-u.
    - **Role**: `User`, `Supervisor`, `PowerUser`.

    **Autoryzacja**: Wymagany token JWT przesy�any w nag��wku:
    **Autoryzacja JWT � Jak korzysta� w Swaggerze:**

    1. Wywo�aj endpoint POST /piesp/auth/login z tre�ci�:

       badgeNumber: 1111
       pin: 1111

       Odpowied� zawiera token JWT (w polu ''token'').

    2. Kliknij przycisk Authorize (k��dka w prawym g�rnym rogu).
    3. Wklej token w formacie:

       Bearer [wklej_token_tutaj]

    4. Kliknij Authorize, a nast�pnie Close.
    5. Teraz mo�esz wywo�ywa� wszystkie zabezpieczone endpointy.

    Token JWT jest wa�ny 1 dzie�. Po jego wyga�ni�ciu zaloguj si� ponownie, aby uzyska� nowy token.

    Zalecane testowe dane logowania:

    - Badge: 1111, PIN: 1111 (Dow�dca patrolu)
    - Badge: 2222, PIN: 2222 (Oficer dy�urny)"
    });
    options.MapType<IFormFile>(() => new OpenApiSchema 
    { 
        Type = "string", 
        Format = "binary" 
    });
    options.MapType<IFormFileCollection>(() => new OpenApiSchema
    {
        Type = "array",
        Items = new OpenApiSchema { Type = "string", Format = "binary" }
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Wprowad� JWT w formacie 'Bearer {token}'",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    options.EnableAnnotations();// �eby dzia�a� [SwaggerOperation] itd.
    options.ExampleFilters();// �eby dzia�a�y [SwaggerResponseExample]
});

// Wskazanie assembly, w kt�rym s� klasy przyk�ad�w wynik�w dzia�ania metod konroler�w:
builder.Services.AddSwaggerExamplesFromAssemblyOf<
    IntegrationHub.Api.Swagger.Examples.SRP.SearchPerson200Example>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<
    IntegrationHub.Api.Swagger.Examples.PIESP.Login401Example>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<
    IntegrationHub.Api.Swagger.Examples.ANPRS.Code400InvalidParameterExample>();

// ====== BUILD ======
var app = builder.Build();
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    // opcjonalnie dodaj znane proxy/sieci:
    KnownProxies = { IPAddress.Parse("20.100.30.4") }
});

app.Lifetime.ApplicationStarted.Register(() =>
{
    _ = Task.Run(async () =>
    {
        try
        {
            

            using var scope = app.Services.CreateScope();
            var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var factory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("SrpServiceClient");

            var rdoUrl = cfg["ExternalServices:SRP:RdoShareServiceUrl"];
            if (string.IsNullOrWhiteSpace(rdoUrl)) return;

            var warmupEnabled = cfg.GetValue<bool?>("ExternalServices:SRP:WarmUpEnabled") ?? false;
            if (!warmupEnabled) return; // <-- wy��cza warm-up


            // r�wnoleg�o�� ~ po�owa MaxConnectionsPerServer (bezpiecznie dla dziesi�tek user�w)
            var maxConns = cfg.GetValue<int?>("ExternalServices:SRP:HttpMaxConnectionsPerServer") ?? 32;
            var parallelism = Math.Clamp(maxConns / 2, 4, 32);
            var attempts = 2; // po 2 lekkie strza�y

            using var sem = new SemaphoreSlim(parallelism);
            var tasks = new List<Task>();

            for (int i = 0; i < attempts; i++)
            {
                await sem.WaitAsync();
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                        foreach (var candidate in new[] { $"{rdoUrl}?wsdl", rdoUrl })
                        {
                            try
                            {
                                using var req = new HttpRequestMessage(HttpMethod.Get, candidate)
                                {
                                    Version = HttpVersion.Version20,
                                    VersionPolicy = HttpVersionPolicy.RequestVersionOrLower
                                };
                                using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                                break; // handshake/ALPN/H2 i po��czenia w puli s� gotowe
                            }
                            catch { /* spr�buj kolejny candidate */ }
                        }
                    }
                    finally { sem.Release(); }
                }));
            }

            await Task.WhenAll(tasks);
        }
        catch { /* warm-up nie mo�e blokowa� startu */ }
    });
});




app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Integration Hub API V1");
    //c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
});

app.UseCors("PIESP");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
// ====== MIDDLEWARE ======
app.UseMiddleware<ErrorLoggingMiddleware>();
app.UseMiddleware<ApiAuditMiddleware>();
app.UseSerilogRequestLogging(); // przed MapControllers
app.MapControllers();

// ====== RUN APPLICATION ======
app.Run();




