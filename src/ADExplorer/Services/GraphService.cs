using ADExplorer.Models;
using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace ADExplorer.Services;

public class GraphService
{
    private GraphServiceClient? _client;
    public bool IsConfigured { get; private set; }

    public void Initialize(string tenantId, string clientId, string clientSecret)
    {
        try
        {
            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            _client = new GraphServiceClient(credential);
            IsConfigured = true;
        }
        catch
        {
            IsConfigured = false;
        }
    }

    public async Task<List<AdUser>> SearchUsersAsync(string query)
    {
        if (_client == null || string.IsNullOrWhiteSpace(query)) return new();

        // $search terms are wrapped in double quotes; strip any the user typed so they
        // cannot break out of the term.
        var term = query.Replace("\"", string.Empty).Trim();
        try
        {
            var users = await _client.Users.GetAsync(config =>
            {
                config.QueryParameters.Search = $"\"displayName:{term}\" OR \"userPrincipalName:{term}\"";
                config.QueryParameters.Select = new[]
                {
                    "displayName", "userPrincipalName", "mail", "jobTitle",
                    "department", "mobilePhone", "businessPhones", "accountEnabled",
                    "createdDateTime", "id"
                };
                config.QueryParameters.Top = AdService.MaxResults;
                config.Headers.Add("ConsistencyLevel", "eventual");
            });

            return users?.Value?.Select(u => new AdUser
            {
                DisplayName    = u.DisplayName ?? string.Empty,
                SamAccountName = u.UserPrincipalName?.Split('@')[0] ?? string.Empty,
                Email          = u.Mail ?? u.UserPrincipalName ?? string.Empty,
                Title          = u.JobTitle ?? string.Empty,
                Department     = u.Department ?? string.Empty,
                Phone          = u.BusinessPhones?.FirstOrDefault() ?? u.MobilePhone ?? string.Empty,
                IsEnabled      = u.AccountEnabled ?? false,
                Created        = u.CreatedDateTime?.LocalDateTime,
            }).ToList() ?? new();
        }
        catch { return new(); }
    }
}
