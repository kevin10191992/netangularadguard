using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AdGuardHomeApi.Models;
using Microsoft.Extensions.Options;

namespace AdGuardHomeApi.Services;

public interface IAdGuardHomeService
{
    Task<List<string>> GetUniqueIpsFromQueryLogAsync();
}

public class AdGuardHomeService : IAdGuardHomeService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public AdGuardHomeService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        string _baseUrl = Environment.GetEnvironmentVariable("ADGUARD_BASE_URL") ?? "";
        string Username = Environment.GetEnvironmentVariable("ADGUARD_USERNAME") ?? "";
        string Password = Environment.GetEnvironmentVariable("ADGUARD_PASSWORD") ?? "";


        // Configure Basic Auth
        var credentials = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{Username}:{Password}"));
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);
        _httpClient.BaseAddress = new Uri(_baseUrl);
    }

    public async Task<List<string>> GetUniqueIpsFromQueryLogAsync()
    {
        var uniqueIps = new HashSet<string>();

        try
        {
            var response = await _httpClient.GetAsync("/control/querylog");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var queryLog = JsonSerializer.Deserialize<QueryLogResponse>(content, _jsonOptions);

            if (queryLog?.Data != null)
            {
                foreach (var entry in queryLog.Data)
                {
                    if (entry.answer != null)
                    {
                        foreach (var answer in entry.answer)
                        {
                            // Only process A (IPv4) and AAAA (IPv6) records
                            if ((answer.type == "A" || answer.type == "AAAA") &&
                                !string.IsNullOrEmpty(answer.value) &&
                                !IsPrivateIp(answer.value))
                            {
                                uniqueIps.Add(answer.value);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching AdGuard Home query log: {ex.Message}");
        }

        return uniqueIps.ToList();
    }

    private static bool IsPrivateIp(string ip)
    {
        // Filter out private/local IPs that won't have geo data
        if (ip.StartsWith("10.") || ip.StartsWith("192.168.") ||
            ip.StartsWith("172.16.") || ip.StartsWith("172.17.") ||
            ip.StartsWith("172.18.") || ip.StartsWith("172.19.") ||
            ip.StartsWith("172.2") || ip.StartsWith("172.3") ||
            ip.StartsWith("127.") || ip.StartsWith("0.") ||
            ip == "::1" || ip.StartsWith("fe80:") || ip.StartsWith("fc") ||
            ip.StartsWith("fd"))
        {
            return true;
        }
        return false;
    }
}
