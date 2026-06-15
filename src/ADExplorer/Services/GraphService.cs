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
        if (_client == null) return new();
        try
        {
            var users = await _client.Users.GetAsync(config =>
            {
                config.QueryParameters.Search = $"\"displayName:{query}\" OR \"userPrincipalName:{query}\"";
                config.QueryParameters.Select = new[]
                {
                    "displayName", "userPrincipalName", "mail", "jobTitle",
                    "department", "manager", "mobilePhone", "accountEnabled",
                    "createdDateTime", "id"
                };
                config.Headers.Add("ConsistencyLevel", "eventual");
            });

            return users?.Value?.Select(u => new AdUser
            {
                DisplayName    = u.DisplayName ?? string.Empty,
                SamAccountName = u.UserPrincipalName?.Split('@')[0] ?? string.Empty,
                Email          = u.Mail ?? u.UserPrincipalName ?? string.Empty,
                Title          = u.JobTitle ?? string.Empty,
                Department     = u.Department ?? string.Empty,
                Phone          = u.MobilePhone ?? string.Empty,
                IsEnabled      = u.AccountEnabled ?? false,
                Created        = u.CreatedDateTime?.DateTime,
            }).ToList() ?? new();
        }
        catch { return new(); }
    }
}
