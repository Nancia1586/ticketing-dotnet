using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Ticketing.Core.Data;
using Ticketing.Core.Models;
using Ticketing.BackOffice.Razor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SysAdmin", policy => policy.RequireRole("SysAdmin"));
    options.AddPolicy("Organizer", policy => policy.RequireRole("Organizer"));
    options.AddPolicy("SysAdminOrOrganizer", policy => policy.RequireRole("SysAdmin", "Organizer"));
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Categories", "SysAdmin");
    options.Conventions.AuthorizeFolder("/Venues", "SysAdmin");
    options.Conventions.AuthorizeFolder("/Events", "SysAdminOrOrganizer");
    options.Conventions.AuthorizeFolder("/Reservations", "SysAdminOrOrganizer");
    options.Conventions.AuthorizePage("/Index");
    options.Conventions.AuthorizeFolder("/"); 
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<TicketingDbContext>(options =>
    options.UseSqlServer(connectionString, b => b.MigrationsAssembly("Ticketing.BackOffice.Razor"))); 

builder.Services.AddIdentity<ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole>(options => {
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<TicketingDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddControllers()
    .AddJsonOptions(x => x.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

builder.Services.AddHttpClient<IEventService, EventApiService>(client =>
{
});


builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IVenueService, VenueService>();

builder.Services.AddRazorPages();

var app = builder.Build();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<TicketingDbContext>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    
    context.Database.Migrate();
    await DbInitializer.Initialize(context, userManager, roleManager);
}

app.Run();
