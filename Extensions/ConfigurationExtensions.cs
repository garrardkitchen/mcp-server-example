public static class ConfigurationExtensions
{
    /// <summary>
    /// Retrieves the GitLab token from the application's configuration.
    /// If the token is not found, an exception is thrown.
    /// </summary>
    /// <param name="configuration">The application configuration instance where the GitLab token is defined.</param>
    /// <returns>The GitLab token as a string.</returns>
    /// <exception cref="Exception">Thrown when the GitLab token is missing in the configuration.</exception>
    public static string ToGitLabToken(this IConfiguration configuration)
    {
        return configuration["GitLab:Token"] ?? throw new Exception("Missing GitLab:Token");
    }

    /// <summary>
    /// Retrieves the GitLab domain from the application's configuration.
    /// If the domain is not found, an exception is thrown.
    /// </summary>
    /// <param name="configuration">The application configuration instance where the GitLab domain is defined.</param>
    /// <returns>The GitLab domain as a string.</returns>
    /// <exception cref="Exception">Thrown when the GitLab domain is missing in the configuration.</exception>
    public static string ToGitLabDomain(this IConfiguration configuration)
    {
        return configuration["GitLab:Domain"] ?? throw new Exception("Missing GitLab:Domain");
    }
}