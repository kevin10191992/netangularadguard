namespace AdGuardHomeApi.Models;

public class QueryLogResponse
{
    public List<QueryLogEntry> Data { get; set; } = new();
    public string? Oldest { get; set; }
}

public class QueryLogEntry
{
    public bool answer_dnssec { get; set; }
    public bool cached { get; set; }
    public string client { get; set; }
    public ClientInfo client_info { get; set; }
    public string client_proto { get; set; }
    public string elapsedMs { get; set; }
    public int filterId { get; set; }
    public Question question { get; set; }
    public string reason { get; set; }
    public string rule { get; set; }
    public List<Rule> rules { get; set; }
    public string status { get; set; }
    public string time { get; set; }
    public string upstream { get; set; }
    public List<Answer> answer { get; set; }
}

public class Question
{
    public string @class { get; set; }
    public string name { get; set; }
    public string type { get; set; }
}

public class Rule
{
    public int filter_list_id { get; set; }
    public string text { get; set; }
}

public class Answer
{
    public string type { get; set; }
    public string value { get; set; }
    public int ttl { get; set; }
}

public class ClientInfo
{
    public Whois whois { get; set; }
    public string name { get; set; }
    public string disallowed_rule { get; set; }
    public bool disallowed { get; set; }
}

public class Whois
{
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
