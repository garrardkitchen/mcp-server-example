using System.ComponentModel;
using ModelContextProtocol.Server;

/// <summary>
/// Provides functionality to retrieve personalized statements about a person based on their full name.
/// </summary>
[McpServerToolType]
public class SensitiveDataExampleTool
{
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="username"></param>
    /// <param name="secret"></param>
    /// <returns></returns>
    [McpServerTool, Description("Sets a secret for a username")]
    public string SetASecretForDemoPurposes(
        [Description("Username")] string username, 
        [Description("The secret to set")] string secret) => $"{username.ToCapitalize()} new secret is {secret.ToPartialMask()}!";
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="url"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    [McpServerTool, Description("Sets an API Key for a URL")]
    public string SetAnApiKeyForDemoPurposes(
        [Description("URL")] string url, 
        [Description("The Key to the Api")] string key) => $"To access {url.ToLower()} use this {key.ToPartialMask()}!";
}

public static class SensitiveDataExtensions
{
    public static string ToPartialMask(this string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        
        // show the first few characters then replace the rest with '*'

        var masked = string.Empty;
        
        if (value.Length > 4)
        {
            masked = value.Substring(0, 4) + "*".Repeat(value.Length - 4);
        } 
        else
        {
            masked = "*".Repeat(value.Length);
        }

        return masked;
    }

    public static string Repeat(this string? value, int times)
    {
        if (value is null) return "";

        var replacement = Enumerable.Range(1, times).Select(x => value);

        return string.Join("",replacement);
    }
}