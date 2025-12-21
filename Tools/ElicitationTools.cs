using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace EverythingServer.Tools;

public class ElicitationTools
{
    [McpServerTool, Description("A simple game where the user has to guess a number between 1 and 10. #elicitation")]
    public async Task<string> GuessTheNumber(
        IMcpServer server, // Get the McpServer from DI container
        CancellationToken token
    )
    {
        // First ask the user if they want to play
        var playSchema = new ElicitRequestParams.RequestSchema
        {
            Properties =
            {
                ["Answer"] = new ElicitRequestParams.BooleanSchema(),
                ["Options"] = new ElicitRequestParams.EnumSchema()
                {
                     EnumNames = new List<string> { "accept", "decline" }
                },
                ["Title"] = new ElicitRequestParams.StringSchema()
                {
                    MaxLength = 10,
                    Description = "The title of the game",
                    Type = "string"
                },
                ["Date"] = new ElicitRequestParams.StringSchema()
                {
                    Description = "Enter a date: DD/MM/YYYY",
                    Title = "Date",
                    Format = "date"
                }
            }
        };

        var playResponse = await server.ElicitAsync(new ElicitRequestParams
        {
            Message = "Do you want to play a game?",
            RequestedSchema = playSchema
        }, token);

        // Check if user wants to play
        if (playResponse.Action != "accept" || playResponse.Content?["Answer"].ValueKind != JsonValueKind.True)
        {
            return "Maybe next time!";
        }
        
        var step2Schema = new ElicitRequestParams.RequestSchema
        {
            Properties =
            {
                ["Continue"] = new ElicitRequestParams.BooleanSchema()
                    { 
                        Description = "Do you want to continue playing?"
                    },
            }
        };
        
        var step2Response = await server.ElicitAsync(new ElicitRequestParams
        {
            Message = "Do you want to play a game?",
            RequestedSchema = step2Schema
        }, token);


        // remaining implementation of GuessTheNumber method
        return "done";
    }
}