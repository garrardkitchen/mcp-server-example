public static class ConfigurationExtensions
{
    public static string ToGitLabToken(this IConfiguration configuration)
    {
        return configuration["GitLab:Token"] ?? throw new Exception("Missing GitLab:Token");
    }
    public static string ToGitLabDomain(this IConfiguration configuration)
    {
        return configuration["GitLab:Domain"] ?? throw new Exception("Missing GitLab:Domain");
    }
}