using System.Text.Json;
using AdGuardHomeApi.Models;

namespace AdGuardHomeApi.Services;

public interface IGeoIpService
{
    Task<Dictionary<string, (string CountryCode, string CountryName)>> GetCountriesForIpsAsync(List<string> ips);
}

public class GeoIpService : IGeoIpService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly Dictionary<string, (string CountryCode, string CountryName)> _cache = new();

    public GeoIpService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("http://ip-api.com/");
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<Dictionary<string, (string CountryCode, string CountryName)>> GetCountriesForIpsAsync(List<string> ips)
    {
        var results = new Dictionary<string, (string CountryCode, string CountryName)>();

        // ip-api.com has a rate limit of 45 requests per minute
        // Use batch endpoint for efficiency (up to 100 IPs per request)
        var uncachedIps = ips.Where(ip => !_cache.ContainsKey(ip)).ToList();
        
        // Add cached results first
        foreach (var ip in ips.Where(ip => _cache.ContainsKey(ip)))
        {
            results[ip] = _cache[ip];
        }

        // Batch requests (100 IPs max per batch for ip-api.com)
        foreach (var batch in uncachedIps.Chunk(100))
        {
            try
            {
                var batchRequest = batch.Select(ip => new { query = ip }).ToList();
                var json = JsonSerializer.Serialize(batchRequest);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("batch?fields=status,countryCode,country,query", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var batchResults = JsonSerializer.Deserialize<List<BatchGeoIpResponse>>(responseContent, _jsonOptions);
                    
                    if (batchResults != null)
                    {
                        foreach (var result in batchResults)
                        {
                            if (result.Status == "success" && 
                                !string.IsNullOrEmpty(result.CountryCode) &&
                                !string.IsNullOrEmpty(result.Query))
                            {
                                var countryData = (result.CountryCode, result.Country ?? result.CountryCode);
                                _cache[result.Query] = countryData;
                                results[result.Query] = countryData;
                            }
                        }
                    }
                }
                
                // Respect rate limit - wait a bit between batches
                if (uncachedIps.Count > 100)
                {
                    await Task.Delay(1500);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching GeoIP data: {ex.Message}");
            }
        }

        return results;
    }
}

public class BatchGeoIpResponse
{
    public string? Status { get; set; }
    public string? CountryCode { get; set; }
    public string? Country { get; set; }
    public string? Query { get; set; }
}
