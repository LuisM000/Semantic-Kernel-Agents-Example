using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

/// <summary>
/// A reducer that filters chat history based on a specified condition.
/// </summary>
sealed class ChatHistoryFilterReducer : IChatHistoryReducer
{
    private readonly Func<ChatMessageContent, bool> _filterHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatHistoryFilterReducer"/> class.
    /// </summary>
    /// <param name="filterHandler">A function to determine whether a chat message should be included in the filtered history.</param>
    public ChatHistoryFilterReducer(Func<ChatMessageContent, bool> filterHandler)
    {
        _filterHandler = filterHandler;
    }

    /// <summary>
    /// Filters the chat history asynchronously based on the provided filter handler.
    /// </summary>
    /// <param name="chatHistory">The list of chat messages to filter.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the filtered chat messages.</returns>
    public Task<IEnumerable<ChatMessageContent>?> ReduceAsync(IReadOnlyList<ChatMessageContent> chatHistory, CancellationToken cancellationToken)
    {
        var filteredChatHistory = chatHistory?.Where(_filterHandler).AsEnumerable();
        return Task.FromResult(filteredChatHistory);
    }
}