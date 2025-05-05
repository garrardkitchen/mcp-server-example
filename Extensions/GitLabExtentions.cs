using Garrard.GitLab;

public static class GitLabExtentions
{
    public static IEnumerable<GitLabProjectVariableDto> ToMasked(this IEnumerable<GitLabProjectVariableDto>? variables)
    {
        if (variables == null || !variables.Any())
        {
            return new List<GitLabProjectVariableDto>();
        }
        
        var modifiedVars = new List<GitLabProjectVariableDto>();
        foreach (var variable in variables.ToList())
        {
            if (variable.Masked && !string.IsNullOrEmpty(variable.Value) && variable.Value.Length > 4)
            {
                variable.Value = new string('*', variable.Value.Length - 4) + variable.Value.Substring(variable.Value.Length - 4);
            }
            else if (variable.Masked && !string.IsNullOrEmpty(variable.Value))
            {
                variable.Value = new string('*', variable.Value.Length);
            }
            modifiedVars.Add(variable);
        }

        return modifiedVars;
    }
}