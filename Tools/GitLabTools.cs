using System.ComponentModel;
using ModelContextProtocol.Server;
using Garrard.GitLab;

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
}