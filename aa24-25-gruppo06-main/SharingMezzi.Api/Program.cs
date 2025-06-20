using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using SharingMezzi.Core.Interfaces.Repositories;
using SharingMezzi.Core.Interfaces.Services;
using SharingMezzi.Core.Services;
using SharingMezzi.Core.Entities;
using SharingMezzi.Infrastructure.Database;
using SharingMezzi.Infrastructure.Database.Repositories;
using SharingMezzi.Infrastructure.Mqtt;
using SharingMezzi.Infrastructure.Services;
using SharingMezzi.IoT.Services;
using SharingMezzi.Api.Hubs;

var builder = WebApplication.CreateBuilder(args);

// ===== DATABASE =====
builder.Services.AddDbContext<SharingMezziContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ===== REPOSITORIES =====
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IMezzoRepository, MezzoRepository>();

// ===== BUSINESS SERVICES =====
builder.Services.AddScoped<ICorsaService, CorsaService>();
builder.Services.AddScoped<IMezzoService, MezzoService>();
builder.Services.AddScoped<IParcheggioService, ParcheggioService>();

// ===== AUTHENTICATION SERVICES =====
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// ===== MQTT ACTUATOR SERVICE =====
builder.Services.AddScoped<SharingMezzi.Core.Services.IMqttActuatorService, SharingMezzi.Infrastructure.Services.MqttActuatorService>();

// ===== JWT AUTHENTICATION =====
var jwtSection = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSection["SecretKey"] ?? throw new ArgumentNullException("Jwt:SecretKey non configurato");

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
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };    // SignalR JWT configuration + Cookie support
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            
            // Anche supporto per cookie se non c'è header Authorization
            if (string.IsNullOrEmpty(context.Token))
            {
                var tokenFromCookie = context.Request.Cookies["AuthToken"];
                if (!string.IsNullOrEmpty(tokenFromCookie))
                {
                    context.Token = tokenFromCookie;
                }
            }
            
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// ===== MQTT SERVICES =====
builder.Services.AddSingleton<IMqttService>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    return new MqttService(
        config["Mqtt:Server"] ?? "localhost",
        config.GetValue<int>("Mqtt:Port", 1883),
        config["Mqtt:ClientId"] ?? "SharingMezziApi"
    );
});

// ===== IOT SERVICES =====
builder.Services.AddSingleton<ConnectedIoTClientsService>();
builder.Services.AddSingleton<SharingMezziBroker>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<SharingMezziBroker>());
builder.Services.AddHostedService<IoTBackgroundService>();
builder.Services.AddHostedService<MqttBackgroundService>();

// ===== BATTERY EMULATION SERVICE =====
builder.Services.AddSingleton<BatteryEmulatorService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<BatteryEmulatorService>());

// ===== SIGNALR HUBS =====
builder.Services.AddSignalR();

// ===== API CONTROLLERS =====
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ===== HTTP CLIENT =====
builder.Services.AddHttpClient();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "SharingMezzi API", 
        Version = "v1.0",
        Description = @"
**API completa per sistema sharing mezzi con IoT/MQTT e autenticazione JWT**

### Funzionalità principali:
- **Autenticazione JWT** - Sistema sicuro di login/registrazione
- **Gestione Mezzi** - Bici muscolari, elettriche e monopattini
- **Gestione Parcheggi** - Monitoraggio slot in tempo reale
- **Sistema Corse** - Inizio/fine corsa con calcolo tariffe
- **Gestione Crediti** - Ricariche e punti eco
- **SignalR Real-time** - Notifiche live per utenti e admin
- **MQTT/IoT** - Integrazione sensori e attuatori
- **Dashboard Admin** - Pannello amministrazione completo

### Come usare l'API:
1. **Registrazione**: `POST /api/auth/register` 
2. **Login**: `POST /api/auth/login` per ottenere il token JWT
3. **Autorizzazione**: Clicca il pulsante 'Authorize' e inserisci: `Bearer {il_tuo_token}`
4. **Test**: Ora puoi testare tutti gli endpoint protetti!

### Credenziali di test:
- **Admin**: admin@test.com / admin123
- **Utente**: mario@test.com / user123
",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "SharingMezzi Team",
            Email = "support@sharingmezzi.com"
        }
    });
    
    // JWT Authentication configuration
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = @"
Autenticazione JWT usando l'header Authorization.
Inserisci 'Bearer' seguito da uno spazio e dal token.
Esempio: 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement()
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "Bearer",
                Name = "Authorization",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            },
            new List<string>()
        }
    });

    // Organizza i controller in gruppi
    c.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });
    c.DocInclusionPredicate((name, api) => true);
});

// ===== CORS =====
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
    
    // NUOVO: Policy per Admin Dashboard
    options.AddPolicy("AdminDashboard", policy =>
    {
        policy.WithOrigins("http://localhost:5000", "https://localhost:5001")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseStaticFiles(new StaticFileOptions
{
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/octet-stream"
});
// ===== DATABASE INITIALIZATION =====
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SharingMezziContext>();
    await context.Database.EnsureCreatedAsync();
    await SeedTestData(context);
}

// ===== PIPELINE =====
// Abilita Swagger in tutte le modalità per testing
app.UseSwagger();
app.UseSwaggerUI(c => 
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SharingMezzi API v1");
    c.RoutePrefix = "swagger"; // Swagger disponibile su /swagger
    c.DocumentTitle = "SharingMezzi API - Swagger UI";
    c.DefaultModelsExpandDepth(-1); // Nasconde i modelli di default
    c.DisplayRequestDuration();
});

if (app.Environment.IsDevelopment())
{
    // ===== MIDDLEWARE PER DEBUGGING ROUTING =====
    app.Use(async (context, next) =>
    {
        Console.WriteLine($"[ROUTING] Request: {context.Request.Method} {context.Request.Path}");
        
        // Log del token Authorization header per debug
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            Console.WriteLine("[AUTH] User has Authorization header");
        }
        else
        {
            Console.WriteLine("[AUTH] No Authorization header found");
        }
        
        await next();
        
        Console.WriteLine($"[ROUTING] Response: {context.Response.StatusCode}");
    });
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

// ===== CONTROLLERS =====
app.MapControllers();

// ===== ADMIN DASHBOARD ROUTES =====
app.MapFallbackToFile("/admin/{*path:nonfile}", "/admin/index.html");

// ===== ROUTING PERSONALIZZATO =====
// Route di default per la root
app.MapGet("/", context =>
{
    context.Response.Redirect("/admin/dashboard.html");
    return Task.CompletedTask;
});

// Admin dashboard redirect  
app.MapGet("/admin", context =>
{
    context.Response.Redirect("/admin/dashboard.html");
    return Task.CompletedTask;
});

// ===== SIGNALR HUBS =====
app.MapHub<MezziHub>("/hubs/mezzi");
app.MapHub<CorseHub>("/hubs/corse");
app.MapHub<ParcheggiHub>("/hubs/parcheggi");
app.MapHub<IoTHub>("/hubs/iot");

// ===== STARTUP INFO =====
Console.WriteLine("╔═════════════════════════════════════════════════════════════╗");
Console.WriteLine("║                   SHARING MEZZI API                         ║");
Console.WriteLine("╠═════════════════════════════════════════════════════════════╣");
Console.WriteLine($"║ API URL: http://localhost:5000                              ║");
Console.WriteLine($"║ Swagger: http://localhost:5000/swagger                      ║");
Console.WriteLine($"║ MQTT Broker: localhost:1883                                 ║");
Console.WriteLine($"║ SignalR Hubs: /hubs/[mezzi|corse|parcheggi|iot]             ║");
Console.WriteLine("║ Admin Dashboard: http://localhost:5000/admin                ║");
Console.WriteLine("║ Login Admin: admin@test.com / admin123                      ║");
Console.WriteLine("╠═════════════════════════════════════════════════════════════╣");
Console.WriteLine("║ Servizi attivi:                                             ║");
Console.WriteLine("║    • MQTT Broker per IoT                                    ║");
Console.WriteLine("║    • Client IoT simulati per tutti i mezzi                  ║");
Console.WriteLine("║    • SignalR per notifiche real-time                        ║");
Console.WriteLine("║    • Database SQLite con dati di test                       ║");
Console.WriteLine("║    • Swagger UI abilitato per testing                       ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");

app.Run();

// ===== SEED DATA =====
static async Task SeedTestData(SharingMezziContext context)
{
    if (!context.Utenti.Any())
    {        
        var utenti = new[]
        {
            new Utente { 
                Email = "admin@test.com", 
                Nome = "Admin", 
                Cognome = "System", 
                Password = HashPassword("admin123"), 
                Ruolo = RuoloUtente.Amministratore,
                DataRegistrazione = DateTime.UtcNow
            },
            new Utente { 
                Email = "mario@test.com", 
                Nome = "Mario", 
                Cognome = "Rossi", 
                Password = HashPassword("user123"), 
                Ruolo = RuoloUtente.Utente,
                Telefono = "3331234567",
                DataRegistrazione = DateTime.UtcNow
            },
            new Utente { 
                Email = "lucia@test.com", 
                Nome = "Lucia", 
                Cognome = "Verdi", 
                Password = HashPassword("user123"), 
                Ruolo = RuoloUtente.Utente,
                Telefono = "3337654321",
                DataRegistrazione = DateTime.UtcNow
            }
        };
        await context.Utenti.AddRangeAsync(utenti);
        
        var parcheggi = new[]
        {
            new Parcheggio { Nome = "Centro Storico", Indirizzo = "Piazza Castello 1", Capienza = 25, PostiLiberi = 20, PostiOccupati = 5 },
            new Parcheggio { Nome = "Politecnico", Indirizzo = "Corso Duca Abruzzi 24", Capienza = 40, PostiLiberi = 30, PostiOccupati = 10 },
            new Parcheggio { Nome = "Porta Nuova", Indirizzo = "Piazza Carlo Felice 1", Capienza = 30, PostiLiberi = 25, PostiOccupati = 5 }
        };
        await context.Parcheggi.AddRangeAsync(parcheggi);
        await context.SaveChangesAsync();
          var mezzi = new[]
        {
            new Mezzo { Modello = "City Bike Classic", Tipo = TipoMezzo.BiciMuscolare, IsElettrico = false, TariffaPerMinuto = 0.15m, TariffaFissa = 1.00m, ParcheggioId = 1, Stato = StatoMezzo.Disponibile },
            new Mezzo { Modello = "E-Bike Urban Pro", Tipo = TipoMezzo.BiciElettrica, IsElettrico = true, LivelloBatteria = 95, TariffaPerMinuto = 0.25m, TariffaFissa = 1.00m, ParcheggioId = 1, Stato = StatoMezzo.Disponibile },
            new Mezzo { Modello = "E-Bike Mountain", Tipo = TipoMezzo.BiciElettrica, IsElettrico = true, LivelloBatteria = 78, TariffaPerMinuto = 0.30m, TariffaFissa = 1.00m, ParcheggioId = 2, Stato = StatoMezzo.Disponibile },
            new Mezzo { Modello = "Urban Scooter X1", Tipo = TipoMezzo.Monopattino, IsElettrico = true, LivelloBatteria = 82, TariffaPerMinuto = 0.35m, TariffaFissa = 1.00m, ParcheggioId = 2, Stato = StatoMezzo.Disponibile },
            new Mezzo { Modello = "Eco Scooter Lite", Tipo = TipoMezzo.Monopattino, IsElettrico = true, LivelloBatteria = 67, TariffaPerMinuto = 0.30m, TariffaFissa = 1.00m, ParcheggioId = 3, Stato = StatoMezzo.Disponibile },
            new Mezzo { Modello = "City Bike Sport", Tipo = TipoMezzo.BiciMuscolare, IsElettrico = false, TariffaPerMinuto = 0.18m, TariffaFissa = 1.00m, ParcheggioId = 3, Stato = StatoMezzo.Disponibile }
        };
        await context.Mezzi.AddRangeAsync(mezzi);
        await context.SaveChangesAsync();
          Console.WriteLine("Database seeded with test data");
        Console.WriteLine($"   • {utenti.Length} utenti");
        Console.WriteLine($"   • {parcheggi.Length} parcheggi");
        Console.WriteLine($"   • {mezzi.Length} mezzi");
    }
}

// ===== HELPER FUNCTIONS =====
static string HashPassword(string password)
{
    using var sha256 = System.Security.Cryptography.SHA256.Create();
    var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
    return Convert.ToBase64String(hashedBytes);
}