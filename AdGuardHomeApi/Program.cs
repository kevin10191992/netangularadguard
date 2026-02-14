using AdGuardHomeApi.Models;
using AdGuardHomeApi.Services;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);
Env.Load();

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddHttpClient<IAdGuardHomeService, AdGuardHomeService>();
builder.Services.AddHttpClient<IGeoIpService, GeoIpService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy => 
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowAngularApp");

app.MapGet("/", async (IAdGuardHomeService adGuardService, IGeoIpService geoIpService) =>
{
    // Get unique IPs from AdGuard Home query log
    var ips = await adGuardService.GetUniqueIpsFromQueryLogAsync();
    
    if (ips.Count == 0)
    {
        return Results.Ok(new List<DnsQuery>());
    }
    
    // Resolve IPs to countries
    var countryData = await geoIpService.GetCountriesForIpsAsync(ips);
    
    // Aggregate by country
    var countryQueries = countryData.Values
        .GroupBy(c => c.CountryCode)
        .Select(g => new DnsQuery
        {
            CountryCode = g.Key,
            CountryName = g.First().CountryName,
            QueryCount = g.Count()
        })
        .OrderByDescending(q => q.QueryCount)
        .ToList();
    
    return Results.Ok(countryQueries);
})
.WithName("GetDnsTrafficByCountry");

await app.RunAsync();
