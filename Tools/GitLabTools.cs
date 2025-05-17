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
    
    /// <summary>
    /// Checks if a GitLab project has an Azure consumption budget resource defined and adds it if it doesn't.
    /// </summary>
    /// <param name="projectId">The ID of the GitLab project to check.</param>
    /// <param name="branchName">The branch to pull from initially.</param>
    /// <returns>A URL to the created merge request or a message indicating the budget already exists.</returns>
    [McpServerTool, Description("Checks for and adds an Azure consumption budget to a GitLab project")]
    public async Task<string> AddAzureConsumptionBudgetAsync(string projectId, string branchName)
    {
        _logger.LogInformation("AddAzureConsumptionBudgetAsync called with project ID: {ProjectId} and branch: {BranchName}", projectId, branchName);
        try
        {
            // Get project details using GitLab REST API
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", _token);
            // Always prefix with https:// for all HTTP calls
            var projectUrl = $"https://{_domain.TrimStart('h', 't', 'p', 's', ':', '/').TrimStart('/')}/api/v4/projects/{Uri.EscapeDataString(projectId)}";
            var response = await httpClient.GetAsync(projectUrl);
            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to get project details: {errorResponse}");
            }
            var projectInfoJson = await response.Content.ReadAsStringAsync();
            // Extract HTTP clone URL (force HTTPS, never use SSH)
            var httpUrlMatch = Regex.Match(projectInfoJson, "\"http_url_to_repo\":\"(.*?)\"");
            string cloneUrl = httpUrlMatch.Success ? httpUrlMatch.Groups[1].Value.Replace("\\", "") : string.Empty;
            // Always use HTTPS for cloning
            if (!string.IsNullOrEmpty(cloneUrl))
            {
                if (!cloneUrl.StartsWith("https://"))
                {
                    // Replace http:// with https:// if needed
                    cloneUrl = Regex.Replace(cloneUrl, "^http://", "https://");
                }
                // Add token to the URL for authentication if using HTTPS
                var uriBuilder = new UriBuilder(cloneUrl);
                uriBuilder.UserName = "oauth2";
                uriBuilder.Password = _token;
                cloneUrl = uriBuilder.Uri.ToString();
            }
            else
            {
                throw new Exception("Could not find HTTPS clone URL for the project.");
            }
            // Create a temporary directory for the clone
            string tempDir = Path.Combine(Path.GetTempPath(), $"gitlab-budget-{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);
            try
            {
                // Step 1: Clone the repository using HTTPS only
                _logger.LogInformation("Cloning repository to {TempDir}", tempDir);
                await RunGitCommand(tempDir, $"clone {cloneUrl} .");
                // Step 2: Check out the user-specified branch
                _logger.LogInformation("Checking out branch {BranchName}", branchName);
                await RunGitCommand(tempDir, $"checkout {branchName}");
                await RunGitCommand(tempDir, "pull");
                
                // Check if the repository already has an Azure consumption budget defined
                bool hasBudget = await CheckForConsumptionBudget(tempDir);
                if (hasBudget)
                {
                    _logger.LogInformation("Azure consumption budget already exists in project");
                    return "Azure consumption budget resource already exists in the project.";
                }
                
                var newBranchName = "feat/platform-engineering/add-budget";
                // Step 3: Create a new branch
                _logger.LogInformation("Creating new branch '{NewBranchName}'", newBranchName);
                await RunGitCommand(tempDir, $"checkout -b {newBranchName}");
                
                // Step 4: Add the budget.tf file
                _logger.LogInformation("Creating budget.tf file");
                // Calculate today's date and a year from today in ISO 8601 format
                var today = DateTime.UtcNow.Date;
                var nextYear = today.AddYears(1);
                // Set startDate to the first day of the current month in ISO 8601 format
                var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
                string startDate = firstDayOfMonth.ToString("yyyy-MM-dd") + "T00:00:00Z";
                // Set endDate to the last day of the month a year from today in ISO 8601 format
                var lastDayOfNextYearMonth = new DateTime(nextYear.Year, nextYear.Month, DateTime.DaysInMonth(nextYear.Year, nextYear.Month));
                string endDate = lastDayOfNextYearMonth.ToString("yyyy-MM-dd") + "T00:00:00Z";
                string budgetTfContent = $@"resource ""azurerm_consumption_budget_subscription"" ""this"" {{
  name            = ""budget""
  subscription_id = ""/subscriptions/${{var.ARM_SUBSCRIPTION_ID}}""
  amount          = var.budget_amount
  time_grain      = ""Monthly""
  time_period {{
    start_date = ""{startDate}""
    end_date   = ""{endDate}""
  }}

  notification {{
    enabled        = true
    threshold      = 80
    operator       = ""GreaterThan""
    contact_emails = [var.budget_notification_email]
  }}
}}";
                await File.WriteAllTextAsync(Path.Combine(tempDir, "budget.tf"), budgetTfContent);
                
                // Step 5: Update variable.tf file
                _logger.LogInformation("Updating variable.tf file");
                string variablesTfPath = Path.Combine(tempDir, "variable.tf");
                string variablesContent = @"variable ""budget_amount"" {
  description = ""The allocated budget for this subscription""
  type        = number
  default     = 100
}

variable ""budget_notification_email"" {
  description = ""The address to use to notify when the budet hits the threshold and beyond""
  type        = string
  default     = ""garrard.kitchen@fujitsu.com""
}
";
                if (File.Exists(variablesTfPath))
                {
                    string existingContent = await File.ReadAllTextAsync(variablesTfPath);
                    await File.WriteAllTextAsync(variablesTfPath, existingContent + Environment.NewLine + variablesContent);
                }
                else
                {
                    await File.WriteAllTextAsync(variablesTfPath, variablesContent);
                }
                
                // Step 6: Update output.tf file
                _logger.LogInformation("Updating output.tf file");
                string outputTfPath = Path.Combine(tempDir, "output.tf");
                string outputContent = @"output ""budget_name"" {
  value = azurerm_consumption_budget_subscription.this.name
}
";
                if (File.Exists(outputTfPath))
                {
                    string existingContent = await File.ReadAllTextAsync(outputTfPath);
                    await File.WriteAllTextAsync(outputTfPath, existingContent + Environment.NewLine + outputContent);
                }
                else
                {
                    await File.WriteAllTextAsync(outputTfPath, outputContent);
                }
                
                // Step 7: Commit changes
                _logger.LogInformation("Committing changes");
                await RunGitCommand(tempDir, "add budget.tf variable.tf output.tf");
                await RunGitCommand(tempDir, "commit -m \"feat:Added Azure consumption budget\"");
                
                // Step 8: Push changes
                _logger.LogInformation("Pushing changes to remote");
                await RunGitCommand(tempDir, $"push --set-upstream origin {newBranchName}");
                
                // Step 9: Create merge request
                _logger.LogInformation("Creating merge request");
                // Always prefix with https:// for all HTTP calls
                var createMrUrl = $"https://{_domain.TrimStart('h', 't', 'p', 's', ':', '/').TrimStart('/')}/api/v4/projects/{projectId}/merge_requests";
                using var httpClientMr = new HttpClient();
                httpClientMr.DefaultRequestHeaders.Add("PRIVATE-TOKEN", _token);
                var content = new StringContent(
                    $"{{\"source_branch\":\"{newBranchName}\",\"target_branch\":\"{branchName}\",\"title\":\"feat: Added Azure consumption budget\"}}",
                    Encoding.UTF8,
                    "application/json");
                var responseMr = await httpClientMr.PostAsync(createMrUrl, content);
                if (!responseMr.IsSuccessStatusCode)
                {
                    var errorResponseMr = await responseMr.Content.ReadAsStringAsync();
                    throw new Exception($"Failed to create merge request: {errorResponseMr}");
                }
                var mrJsonResponse = await responseMr.Content.ReadAsStringAsync();
                // Extract web_url from the JSON response
                var match = Regex.Match(mrJsonResponse, "\"web_url\":\"(.*?)\"");
                if (match.Success)
                {
                    string mrUrl = match.Groups[1].Value.Replace("\\", "");
                    _logger.LogInformation("Merge request created successfully: {MrUrl}", mrUrl);
                    return mrUrl;
                }
                
                _logger.LogWarning("Could not extract merge request URL from response");
                return "Merge request created but could not extract URL from response.";
            }
            finally
            {
                // Clean up temp directory
                try
                {
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to clean up temporary directory: {TempDir}", tempDir);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in AddAzureConsumptionBudgetAsync with project ID: {ProjectId}", projectId);
            throw;
        }
    }
    
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