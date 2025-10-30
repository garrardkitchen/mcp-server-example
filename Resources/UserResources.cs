using System.ComponentModel;
using ModelContextProtocol.Server;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

/// <summary>
/// Provides MCP resources for retrieving user information from a database in YAML format.
/// </summary>
[McpServerResourceType]
public class UserResources
{
    private readonly ILogger<UserResources> _logger;

    public UserResources(ILogger<UserResources> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Retrieves user information from the database and returns it as YAML content.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to retrieve.</param>
    /// <returns>A task representing the asynchronous operation containing the YAML formatted user data.</returns>
    [McpServerResource(UriTemplate = "user://{userId}", Name = "user-data", MimeType = "application/x-yaml")]
    [Description("Returns user information from the database as YAML content. Specify the userId to retrieve a specific user's data.")]
    public async Task<string> GetUserAsYamlAsync(string userId)
    {
        _logger.LogInformation("GetUserAsYamlAsync called for userId: {UserId}", userId);

        try
        {
            // Simulate database retrieval (replace with actual database query)
            var user = await GetUserFromDatabaseAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", userId);
                return $"# User Not Found\nuserId: {userId}\nstatus: not_found";
            }

            // Convert user object to YAML
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var yaml = serializer.Serialize(user);

            _logger.LogInformation("Successfully retrieved user data for userId: {UserId}", userId);
            return yaml;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user data for userId: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Retrieves a list of all users from the database as YAML content.
    /// </summary>
    /// <returns>A task representing the asynchronous operation containing YAML formatted list of users.</returns>
    [McpServerResource(UriTemplate = "users://all", Name = "all-users", MimeType = "application/x-yaml")]
    [Description("Returns a list of all users from the database as YAML content")]
    public async Task<string> GetAllUsersAsYamlAsync()
    {
        _logger.LogInformation("GetAllUsersAsYamlAsync called");

        try
        {
            // Simulate database retrieval (replace with actual database query)
            var users = await GetAllUsersFromDatabaseAsync();

            // Convert users list to YAML
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var yaml = serializer.Serialize(new { users });

            _logger.LogInformation("Successfully retrieved {Count} users", users.Count);
            return yaml;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all users");
            throw;
        }
    }

    /// <summary>
    /// Simulates retrieving a user from a database.
    /// Replace this with actual database logic (e.g., Entity Framework, Dapper, etc.)
    /// </summary>
    private async Task<UserDto?> GetUserFromDatabaseAsync(string userId)
    {
        // Simulate async database call
        await Task.Delay(10);

        // Mock data - replace with actual database query
        var mockUsers = GetMockUsers();
        return mockUsers.FirstOrDefault(u => u.UserId == userId);
    }

    /// <summary>
    /// Simulates retrieving all users from a database.
    /// Replace this with actual database logic.
    /// </summary>
    private async Task<List<UserDto>> GetAllUsersFromDatabaseAsync()
    {
        // Simulate async database call
        await Task.Delay(10);

        // Mock data - replace with actual database query
        return GetMockUsers();
    }

    /// <summary>
    /// Provides mock user data for demonstration purposes.
    /// Replace this with actual database queries.
    /// </summary>
    private List<UserDto> GetMockUsers()
    {
        return new List<UserDto>
        {
            new UserDto
            {
                UserId = "user001",
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Role = "Administrator",
                Department = "Engineering",
                CreatedAt = DateTime.Parse("2024-01-15T10:30:00Z"),
                IsActive = true,
                LastLoginAt = DateTime.Parse("2025-10-29T14:22:00Z"),
                Metadata = new Dictionary<string, string>
                {
                    { "location", "New York" },
                    { "employeeId", "EMP-12345" }
                }
            },
            new UserDto
            {
                UserId = "user002",
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@example.com",
                Role = "Developer",
                Department = "Engineering",
                CreatedAt = DateTime.Parse("2024-03-20T09:15:00Z"),
                IsActive = true,
                LastLoginAt = DateTime.Parse("2025-10-30T08:45:00Z"),
                Metadata = new Dictionary<string, string>
                {
                    { "location", "San Francisco" },
                    { "employeeId", "EMP-67890" }
                }
            },
            new UserDto
            {
                UserId = "user003",
                FirstName = "Bob",
                LastName = "Johnson",
                Email = "bob.johnson@example.com",
                Role = "Manager",
                Department = "Operations",
                CreatedAt = DateTime.Parse("2023-11-10T13:00:00Z"),
                IsActive = false,
                LastLoginAt = DateTime.Parse("2025-09-15T16:30:00Z"),
                Metadata = new Dictionary<string, string>
                {
                    { "location", "Chicago" },
                    { "employeeId", "EMP-11223" }
                }
            }
        };
    }
}

/// <summary>
/// Data transfer object representing a user.
/// </summary>
public class UserDto
{
    public string UserId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}
