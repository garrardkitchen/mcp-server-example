using System.ComponentModel;
using ModelContextProtocol.Server;

[McpServerToolType]
public class WhoIsTool
{
    [McpServerTool, Description("Looks up a persons name and returns information on them")]
    public string WhoIs(string fullname) => $"{fullname} is a wonderful person!";

}