using Garrard.GitLab;

public static class GitLabExtentions
{
    /// <summary>
    /// Masks the values of variables in the provided collection based on their masking configuration.
    /// </summary>
    /// <param name="variables">A collection of GitLabProjectVariableDto objects to be processed. If the variable is marked as masked and has a value, the value will be partially or fully masked depending on its length.</param>
    /// <returns>A collection of GitLabProjectVariableDto objects where the variable values have been masked as configured.</returns>
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