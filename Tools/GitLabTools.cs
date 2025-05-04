using System.ComponentModel;
using ModelContextProtocol.Server;
using Garrard.GitLab;

[McpServerToolType]
public class GitLabTools
{
    private readonly IConfiguration _configurationManager;
    private readonly ILogger<GitLabTools> _logger;
    
    public GitLabTools(IConfiguration configurationManager, ILogger<GitLabTools> logger)
    {
        _configurationManager = configurationManager;
        _logger = logger;
    }

    // Prompts:
    // 1 - table example: I would like to get a list of gitlab groups based on a search pattern and the result to be put in a  markdown table including their (1) name, (2) have web_url as a url link with the word 'click me' and (3) parent_id and (4) a suitable emoji to indicate if has_subgroups is true. if a group has the parent id that equals the group Id, then group those beneath it. include the group id in brackets after the group name
    // 2 - tree example: create a tree structure nesting the groups by parent id and group id
    [McpServerTool, Description("Returns a list of Groups in GitLab")]
    public async Task<IEnumerable<GitLabGroupDto>> SearchGroupsAsync(string pattern) {

        _logger.LogInformation("SearchGroupsAsync called with pattern: {Pattern}", pattern); // Log the method call
        
        try {
            var token = _configurationManager["GitLab:Token"] ?? throw new Exception("Missing GitLab:Token");
            var domain = _configurationManager["GitLab:Domain"] ?? throw new Exception("Missing GitLab:Domain");
            var groups = await GroupOperations.SearchGroups(pattern, token, domain);

            if (groups.IsSuccess)
            {
                return groups.Value.ToList();
            }
            return new List<GitLabGroupDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in SearchGroupsAsync with pattern: {Pattern}", pattern);
            
            // if you want to return an empty list in case of an error, uncomment the line below and remove the throw statement
            // return new List<GitLabGroupDto>();

            throw;
        }
    }

}