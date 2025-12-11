using Microsoft.EntityFrameworkCore;
using Ticketing.Core.Data;
using Ticketing.BackOffice.Razor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Récupération de la chaîne de connexion
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Ajout du DbContext pour l'injection de dépendances
builder.Services.AddDbContext<TicketingDbContext>(options =>
    options.UseSqlServer(connectionString, b => b.MigrationsAssembly("Ticketing.BackOffice.Razor"))); 


// Register Repository for Data Access (Used by API Controller)
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddHttpContextAccessor();

// Register API Controllers with JSON Options for handling cycles
builder.Services.AddControllers()
    .AddJsonOptions(x => x.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

// Register HTTP Client for IEventService (Used by Razor Pages to call API)
builder.Services.AddHttpClient<IEventService, EventApiService>(client =>
{
    var baseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7281";
    client.BaseAddress = new Uri(baseUrl);
});


builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
var defaultCulture = new System.Globalization.CultureInfo("en-US");
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(defaultCulture),
    SupportedCultures = new List<System.Globalization.CultureInfo> { defaultCulture },
    SupportedUICultures = new List<System.Globalization.CultureInfo> { defaultCulture }
};
app.UseRequestLocalization(localizationOptions);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

// Seed Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<TicketingDbContext>();
    // context.Database.Migrate(); // Auto-migrate
    DbInitializer.Initialize(context);
}

app.Run();
