using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace AI.Agent.ProductRepository;

public static class ProductRepositoryAgent
{
    public static Func<IServiceProvider, string, AIAgent> CreateAgentDelegate()
    {
        return (sp, key) =>
        {
            var searchFunctions = sp.GetRequiredService<ISearchLimitedStockFunctions>();
            var chatClient = sp.GetRequiredService<IChatClient>();

            var aiAgent = chatClient.CreateAIAgent(
                    name: key,
                    instructions: "You are a useful agent that helps business to check repository and decide if a product might be out of order so new set of product order should be placed.",
                    description: "An AI agent that manages products in repository by searching limited stock items and placing orders as needed.",
                    tools: [AIFunctionFactory.Create(searchFunctions.SearchAsync)]
                )
                .AsBuilder()
                .UseOpenTelemetry(configure: c => c.EnableSensitiveData = true)
                .Build();

            return aiAgent;
        };
    }
}

public interface ISearchLimitedStockFunctions
{
    [Description("Searches for products with limited stock (below a threshold) in the repository")]
    Task<string> SearchAsync(
        [Description("Maximum stock level to consider as 'limited'. Products with stock at or below this threshold will be returned.")]
        int stockThreshold = 2,
        CancellationToken cancellationToken = default);
}

public class SearchLimitedStockFunctions : ISearchLimitedStockFunctions
{
    public async Task<string> SearchAsync(int stockThreshold = 2, CancellationToken cancellationToken = default)
    {
        //TODO: Implement the logic to search for products with stock less than or equal to stockThreshold
        
        return "Monitor item is limited";
    }
}


