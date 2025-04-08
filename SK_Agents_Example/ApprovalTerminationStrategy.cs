using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;

/// <summary>
/// Termination strategy that terminates when all agents have approved the content.
/// </summary>
sealed class AgentApprovalTerminationStrategy : TerminationStrategy
{ 
    /// <summary>
    /// Determines if the process should stop by checking if the last messages in the history
    /// are from all agents and contain the word "approve".
    /// </summary>
    protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
    {
        // If the history contains fewer messages than the number of agents, termination is not possible.
        if (Agents == null || history.Count < Agents.Count)
        {
            return Task.FromResult(false);
        }

        // Filter messages to find those from the agents and are messages of approval.
        var agentApprovalMessages = history
            .Reverse()
            .Where(message => Agents.Any(agent => string.Equals(agent.Name, message.AuthorName, StringComparison.OrdinalIgnoreCase)) && message.Content != null && message.Role == AuthorRole.Assistant)
            .ToList();

        // Check if the last message is from the last Agent. 
        if (agentApprovalMessages.FirstOrDefault()?.AuthorName != Agents[Agents.Count - 1].Name)
        {
            return Task.FromResult(false);
        }

        // Count the number of distinct agents who approved the content in the last turn.
        var approvedMessagesCount = agentApprovalMessages
            .Take(Agents.Count)
            .Where(message => message.Content!.Contains("approve", StringComparison.OrdinalIgnoreCase))            
            .Select(message => message.AuthorName) 
            .Distinct(StringComparer.OrdinalIgnoreCase) 
            .Count(); 

        // Terminate if the number of distinct agents who approved matches the required number of agents.
        return Task.FromResult(approvedMessagesCount == Agents.Count);
    }
}