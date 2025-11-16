using System.ComponentModel;
using ModelContextProtocol.Server;
using Garrard.GitLab;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;

/// <summary>
/// Provides tools and utilities to interact with the GitLab API, including operations
/// such as searching for groups, retrieving projects in groups, and getting project variables.
/// </summary>
[McpServerToolType]
public class GitLabTools
{
    private readonly IConfiguration _configurationManager;
    private readonly ILogger<GitLabTools> _logger;
    private readonly string _token;
    private readonly string _domain;

    /// <summary>
    /// Provides tools and utilities to interact with the GitLab API, including operations on groups, projects, and variables.
    /// </summary>
    public GitLabTools(IConfiguration configurationManager, ILogger<GitLabTools> logger)
    {
        _configurationManager = configurationManager;
        _logger = logger;
        _token = _configurationManager.ToGitLabToken();
        _domain = _configurationManager.ToGitLabDomain();
    }

    /// <summary>
    /// Searches for GitLab groups that match the provided pattern.
    /// </summary>
    /// <param name="pattern">The search pattern used to locate groups in GitLab.</param>
    /// <returns>A task representing the asynchronous operation. The result contains a collection of GitLabGroupDto objects matching the search pattern.</returns>
    // Prompts:
    // 1 - table example: I would like to get a list of gitlab groups based on a search pattern and the result to be put in a  markdown table including their (1) name, (2) have web_url as a url link with the word 'click me' and (3) parent_id and (4) a suitable emoji to indicate if has_subgroups is true. if a group has the parent id that equals the group Id, then group those beneath it. include the group id in brackets after the group name
    // 2 - tree example: create a tree structure nesting the groups by parent id and group id
    [McpServerTool, Description("Returns a list of Groups in GitLab")]
    public async Task<IEnumerable<GitLabGroupDto>> SearchGroupsAsync(string pattern) {

        _logger.LogInformation("SearchGroupsAsync called with pattern: {Pattern}", pattern); // Log the method call

        try {
            var groups = await GroupOperations.SearchGroups(pattern, _token, _domain);
            if (groups.IsSuccess)
            {
                return groups.Value.ToList();
            }
            return new List<GitLabGroupDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in SearchGroupsAsync with pattern: {Pattern}", pattern);
            throw;
        }
    }

    /// <summary>
    /// Retrieves a list of projects within a GitLab group that matches the provided group pattern.
    /// </summary>
    /// <param name="groupPattern">The search pattern used to locate the group in GitLab.</param>
    /// <returns>A task representing the asynchronous operation. The result contains a collection of GitLabProjectInfoDto objects representing the projects within the group.</returns>
    [McpServerTool, Description("Returns a list of Projects in GitLab group")]
    public async Task<IEnumerable<GitLabProjectInfoDto>> GetProjectsInGroupAsync(string groupPattern) {

        _logger.LogInformation("GetProjectsInGroupAsync called with pattern: {Pattern}", groupPattern); // Log the method call
        
        try {
            var projects = await ProjectOperations.GetProjectsInGroup(groupPattern, _token, _domain);

            if (projects.IsSuccess)
            {
                return projects.Value.ToList();
            }
            return new List<GitLabProjectInfoDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in GetProjectsInGroupAsync with pattern: {Pattern}", groupPattern);
            throw;
        }
    }

    /// <summary>
    /// Retrieves a list of variables for the specified GitLab project.
    /// </summary>
    /// <param name="projectId">The identifier of the GitLab project for which the variables need to be retrieved.</param>
    /// <returns>A task representing the asynchronous operation. The result contains a collection of GitLabProjectVariableDto objects associated with the specified GitLab project.</returns>
    [McpServerTool, Description("Returns a list of variables in GitLab project")]
    public async Task<IEnumerable<GitLabProjectVariableDto>> GetVariablesInProjectAsync(string projectId) {

        _logger.LogInformation("GetVariablesInProjectAsync called with pattern: {Pattern}", projectId); // Log the method call
        
        try {
            var projects = await ProjectOperations.GetProjectVariables(projectId, _token, _domain);

            if (projects.IsSuccess)
            {
                return projects.Value.ToMasked();
            }
            return new List<GitLabProjectVariableDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in GetVariablesInProjectAsync with pattern: {Pattern}", projectId);
            throw;
        }
    }

    #region Constants
    private const string BudgetFileName = "budget.tf";
    private const string VariablesFileName = "variable.tf";
    private const string OutputFileName = "output.tf";
    private const string NewBranchName = "feat/platform-engineering/add-budget";
    private const string CommitMessage = "feat:Added Azure consumption budget";
    private const string MergeRequestTitle = "feat: Added Azure consumption budget";
    private const int BudgetThreshold = 80;
    private const int DefaultBudgetAmount = 100;
    private const string DefaultNotificationEmail = "garrard.kitchen@fujitsu.com";
    private const string TimeGrain = "Monthly";
    #endregion

    /// <summary>
    /// Checks if a GitLab project has an Azure consumption budget resource defined and adds it if it doesn't.
    /// </summary>
    /// <param name="projectId">The ID of the GitLab project to check.</param>
    /// <param name="branchName">The branch to pull from initially.</param>
    /// <returns>A URL to the created merge request or a message indicating the budget already exists.</returns>
    [McpServerTool, Description("Checks for and adds an Azure consumption budget to a GitLab project")]
    public async Task<string> AddAzureConsumptionBudgetAsync(string projectId, string branchName)
    {
        ValidateInputParameters(projectId, branchName);
        
        _logger.LogInformation("AddAzureConsumptionBudgetAsync called with project ID: {ProjectId} and branch: {BranchName}", 
            projectId, branchName);

        try
        {
            var cloneUrl = await GetProjectCloneUrlAsync(projectId);
            var tempDir = CreateTempDirectory();

            try
            {
                await ProcessRepositoryAsync(tempDir, cloneUrl, branchName, projectId);
                return await CreateMergeRequestAsync(projectId, branchName);
            }
            finally
            {
                CleanupTempDirectory(tempDir);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in AddAzureConsumptionBudgetAsync with project ID: {ProjectId}", projectId);
            throw;
        }
    }

    #region Private Helper Methods

    private static void ValidateInputParameters(string projectId, string branchName)
    {
        if (string.IsNullOrWhiteSpace(projectId))
            throw new ArgumentException("Project ID cannot be null or empty", nameof(projectId));
        
        if (string.IsNullOrWhiteSpace(branchName))
            throw new ArgumentException("Branch name cannot be null or empty", nameof(branchName));
    }

    private async Task<string> GetProjectCloneUrlAsync(string projectId)
    {
        using var httpClient = CreateHttpClientWithAuth();
        var projectUrl = BuildProjectApiUrl(projectId);
        
        var response = await httpClient.GetAsync(projectUrl);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorResponse = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to get project details: {errorResponse}");
        }

        var projectInfoJson = await response.Content.ReadAsStringAsync();
        return ExtractAndFormatCloneUrl(projectInfoJson);
    }

    private HttpClient CreateHttpClientWithAuth()
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", _token);
        return httpClient;
    }

    private string BuildProjectApiUrl(string projectId)
    {
        var normalizedDomain = _domain.TrimStart('h', 't', 'p', 's', ':', '/').TrimStart('/');
        return $"https://{normalizedDomain}/api/v4/projects/{Uri.EscapeDataString(projectId)}";
    }

    private string ExtractAndFormatCloneUrl(string projectInfoJson)
    {
        var httpUrlMatch = Regex.Match(projectInfoJson, "\"http_url_to_repo\":\"(.*?)\"");
        
        if (!httpUrlMatch.Success)
            throw new InvalidOperationException("Could not find HTTPS clone URL for the project.");

        var cloneUrl = httpUrlMatch.Groups[1].Value.Replace("\\", "");
        
        // Ensure HTTPS
        if (!cloneUrl.StartsWith("https://"))
        {
            cloneUrl = Regex.Replace(cloneUrl, "^http://", "https://");
        }

        // Add authentication
        var uriBuilder = new UriBuilder(cloneUrl)
        {
            UserName = "oauth2",
            Password = _token
        };

        return uriBuilder.Uri.ToString();
    }

    private static string CreateTempDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"gitlab-budget-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }

    private async Task ProcessRepositoryAsync(string tempDir, string cloneUrl, string branchName, string projectId)
    {
        await CloneAndCheckoutRepositoryAsync(tempDir, cloneUrl, branchName);
        
        if (await CheckForConsumptionBudget(tempDir))
        {
            _logger.LogInformation("Azure consumption budget already exists in project");
            throw new InvalidOperationException("Azure consumption budget resource already exists in the project.");
        }

        await CreateFeatureBranchAsync(tempDir);
        await CreateBudgetFilesAsync(tempDir);
        await CommitAndPushChangesAsync(tempDir);
    }

    private async Task CloneAndCheckoutRepositoryAsync(string tempDir, string cloneUrl, string branchName)
    {
        _logger.LogInformation("Cloning repository to {TempDir}", tempDir);
        await RunGitCommand(tempDir, $"clone {cloneUrl} .");
        
        _logger.LogInformation("Checking out branch {BranchName}", branchName);
        await RunGitCommand(tempDir, $"checkout {branchName}");
        await RunGitCommand(tempDir, "pull");
    }

    private async Task CreateFeatureBranchAsync(string tempDir)
    {
        _logger.LogInformation("Creating new branch '{NewBranchName}'", NewBranchName);
        await RunGitCommand(tempDir, $"checkout -b {NewBranchName}");
    }

    private async Task CreateBudgetFilesAsync(string tempDir)
    {
        await CreateBudgetTerraformFileAsync(tempDir);
        await UpdateVariablesFileAsync(tempDir);
        await UpdateOutputFileAsync(tempDir);
    }

    private async Task CreateBudgetTerraformFileAsync(string tempDir)
    {
        _logger.LogInformation("Creating {BudgetFileName} file", BudgetFileName);
        
        var (startDate, endDate) = CalculateBudgetDateRange();
        var budgetContent = GenerateBudgetTerraformContent(startDate, endDate);
        
        await File.WriteAllTextAsync(Path.Combine(tempDir, BudgetFileName), budgetContent);
    }

    private static (string startDate, string endDate) CalculateBudgetDateRange()
    {
        var today = DateTime.UtcNow.Date;
        var nextYear = today.AddYears(1);
        
        var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
        var startDate = firstDayOfMonth.ToString("yyyy-MM-dd") + "T00:00:00Z";
        
        var lastDayOfNextYearMonth = new DateTime(nextYear.Year, nextYear.Month, 
            DateTime.DaysInMonth(nextYear.Year, nextYear.Month));
        var endDate = lastDayOfNextYearMonth.ToString("yyyy-MM-dd") + "T00:00:00Z";
        
        return (startDate, endDate);
    }

    private static string GenerateBudgetTerraformContent(string startDate, string endDate)
    {
        return $@"resource ""azurerm_consumption_budget_subscription"" ""this"" {{
  name            = ""budget""
  subscription_id = ""/subscriptions/${{var.ARM_SUBSCRIPTION_ID}}""
  amount          = var.budget_amount
  time_grain      = ""{TimeGrain}""
  time_period {{
    start_date = ""{startDate}""
    end_date   = ""{endDate}""
  }}

  notification {{
    enabled        = true
    threshold      = {BudgetThreshold}
    operator       = ""GreaterThan""
    contact_emails = [var.budget_notification_email]
  }}
}}";
    }

    private async Task UpdateVariablesFileAsync(string tempDir)
    {
        _logger.LogInformation("Updating {VariablesFileName} file", VariablesFileName);
        
        var variablesTfPath = Path.Combine(tempDir, VariablesFileName);
        var variablesContent = GenerateVariablesContent();
        
        await AppendOrCreateFileAsync(variablesTfPath, variablesContent);
    }

    private static string GenerateVariablesContent()
    {
        return $@"variable ""budget_amount"" {{
  description = ""The allocated budget for this subscription""
  type        = number
  default     = {DefaultBudgetAmount}
}}

variable ""budget_notification_email"" {{
  description = ""The address to use to notify when the budget hits the threshold and beyond""
  type        = string
  default     = ""{DefaultNotificationEmail}""
}}
";
    }

    private async Task UpdateOutputFileAsync(string tempDir)
    {
        _logger.LogInformation("Updating {OutputFileName} file", OutputFileName);
        
        var outputTfPath = Path.Combine(tempDir, OutputFileName);
        const string outputContent = @"output ""budget_name"" {
  value = azurerm_consumption_budget_subscription.this.name
}
";
        
        await AppendOrCreateFileAsync(outputTfPath, outputContent);
    }

    private static async Task AppendOrCreateFileAsync(string filePath, string content)
    {
        if (File.Exists(filePath))
        {
            var existingContent = await File.ReadAllTextAsync(filePath);
            await File.WriteAllTextAsync(filePath, existingContent + Environment.NewLine + content);
        }
        else
        {
            await File.WriteAllTextAsync(filePath, content);
        }
    }

    private async Task CommitAndPushChangesAsync(string tempDir)
    {
        _logger.LogInformation("Committing changes");
        await RunGitCommand(tempDir, $"add {BudgetFileName} {VariablesFileName} {OutputFileName}");
        await RunGitCommand(tempDir, $"commit -m \"{CommitMessage}\"");
        
        _logger.LogInformation("Pushing changes to remote");
        await RunGitCommand(tempDir, $"push --set-upstream origin {NewBranchName}");
    }

    private async Task<string> CreateMergeRequestAsync(string projectId, string branchName)
    {
        _logger.LogInformation("Creating merge request");
        
        using var httpClient = CreateHttpClientWithAuth();
        var createMrUrl = BuildMergeRequestApiUrl(projectId);
        var content = CreateMergeRequestContent(branchName);
        
        var response = await httpClient.PostAsync(createMrUrl, content);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorResponse = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to create merge request: {errorResponse}");
        }

        var mrJsonResponse = await response.Content.ReadAsStringAsync();
        return ExtractMergeRequestUrl(mrJsonResponse);
    }

    private string BuildMergeRequestApiUrl(string projectId)
    {
        var normalizedDomain = _domain.TrimStart('h', 't', 'p', 's', ':', '/').TrimStart('/');
        return $"https://{normalizedDomain}/api/v4/projects/{projectId}/merge_requests";
    }

    private StringContent CreateMergeRequestContent(string branchName)
    {
        var requestBody = $@"{{
            ""source_branch"": ""{NewBranchName}"",
            ""target_branch"": ""{branchName}"",
            ""title"": ""{MergeRequestTitle}""
        }}";
        
        return new StringContent(requestBody, Encoding.UTF8, "application/json");
    }

    private string ExtractMergeRequestUrl(string mrJsonResponse)
    {
        var match = Regex.Match(mrJsonResponse, "\"web_url\":\"(.*?)\"");
        
        if (match.Success)
        {
            var mrUrl = match.Groups[1].Value.Replace("\\", "");
            _logger.LogInformation("Merge request created successfully: {MrUrl}", mrUrl);
            return mrUrl;
        }
        
        _logger.LogWarning("Could not extract merge request URL from response");
        return "Merge request created but could not extract URL from response.";
    }

    private void CleanupTempDirectory(string tempDir)
    {
        try
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
                _logger.LogDebug("Successfully cleaned up temporary directory: {TempDir}", tempDir);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean up temporary directory: {TempDir}", tempDir);
        }
    }

    #endregion
    
    /// <summary>
    /// Checks if a consumption budget resource exists in the repository.
    /// </summary>
    /// <param name="repoDir">The directory containing the repository.</param>
    /// <returns>True if a consumption budget exists, false otherwise.</returns>
    private async Task<bool> CheckForConsumptionBudget(string repoDir)
    {
        // Look for any .tf file containing azurerm_consumption_budget_subscription
        foreach (var file in Directory.GetFiles(repoDir, "*.tf", SearchOption.AllDirectories))
        {
            var content = await File.ReadAllTextAsync(file);
            if (content.Contains("azurerm_consumption_budget_subscription"))
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Runs a Git command in the specified directory.
    /// </summary>
    /// <param name="workingDir">The working directory for the command.</param>
    /// <param name="args">The Git command arguments.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task RunGitCommand(string workingDir, string args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = args,
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        using var process = new Process { StartInfo = startInfo };
        process.Start();
        
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        
        await process.WaitForExitAsync();
        
        if (process.ExitCode != 0)
        {
            _logger.LogError("Git command failed: {Command}, Error: {Error}", args, error);
            throw new Exception($"Git command failed: {args}, Error: {error}");
        }
        
        _logger.LogDebug("Git command succeeded: {Command}, Output: {Output}", args, output);
    }
}