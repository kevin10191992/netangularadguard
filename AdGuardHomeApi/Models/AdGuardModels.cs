namespace AdGuardHomeApi.Models;

public class AdGuardHomeSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class QueryLogResponse
{
    public List<QueryLogEntry> Data { get; set; } = new();
    public string? Oldest { get; set; }
}

public class QueryLogEntry
{
    public List<DnsAnswer>? Answer { get; set; }
    public string? Question { get; set; }
    public string? Client { get; set; }
    public string? Time { get; set; }
}

public class DnsAnswer
{
    public string? Type { get; set; }
    public string? Value { get; set; }
    public int? Ttl { get; set; }
}

public class DnsQuery
{
    public string CountryCode { get; set; } = string.Empty;
    public string CountryName { get; set; } = string.Empty;
    public int QueryCount { get; set; }
}

public class GeoIpResponse
{
    public string? Status { get; set; }
    public string? CountryCode { get; set; }
    public string? Country { get; set; }
}
