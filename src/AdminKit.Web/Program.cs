using System.Globalization;
using AdminKit.Web.Services;

// Fixed culture so money, dates and the seeded numbers render the same on any
// machine that clones this repo.
var culture = new CultureInfo("en-GB");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

// Stands in for a database. Singleton so edits survive between requests.
builder.Services.AddSingleton<IOrderStore, InMemoryOrderStore>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
