using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.Agents.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.DependencyInjection;

// Prompts for the agents
const string StyleReviewerName = "Revisor_Estilo";
const string StyleReviewerInstructions =
        $"""
        Tu tarea es revisar una entrada de blog generada por IA y asegurarte que nadie se da cuenta de que es un texto generado por IA.
        Para ello usa tus Tools que te darán información sobre cómo se escriben las entradas de blog y compáralas con el texto generado.
        Debe parecer que el texto está escrito con el mismo tono que el resto de entradas de blog y con las mismas expresiones como, saludo, despedida y forma de escribir.
        Sé muy exhaustivo con tu revisión y exigente con tu aprobación.
        Si todo es correcto, simplemente escribe 'approve'.
        Si no es correcto, escribe 'Sugerencia de {StyleReviewerName}: reject' y añade tus comentarios indicando el motivo de rechazo. Sé claro y conciso.
        """;

const string FormatReviewerName = "Revisor_Formato";
const string FormatReviewerInstructions =
        $"""
         Tu tarea es revisar el formato de una entrada para un blog. 
         Para ello, revisa el texto de la entrada de blog y asegúrate que está en HTML simple. No debe tener html, head, body, etc. sino directamente html simple con los tags de formato.
         No debe contener Markdown, sino html simple.
         Sé muy exhaustivo con tu revisión y exigente con tu aprobación.
         Si todo es correcto, simplemente escribe 'approve'.
         Si no es correcto, escribe 'Sugerencia de {FormatReviewerName}: reject' y añade tus comentarios indicando el motivo de rechazo. Sé claro y conciso, no vuelvas a escribir el texto de la entrada como html, sino da las instrucciones indicando cómo corregir la entrada para que sea correcta.
        """;

const string BlogWriterName = "BlogWriter";
const string BlogWriterInstructions =
        """
        Tu misión es crear una entrada para un blog en base al tema que te indique el usuario. 
        Estás totalmente concentrado en el objetivo.
        No pierdas el tiempo con charlas superficiales, sólo escribe el texto de la entrada.
        Nunca debes aprobar ni rechazar el texto, sólo escribir el texto de la entrada.
        Considera las sugerencias de los revisores si existen, para refinar el texto de la entrada.
        Cuando ajustes el texto en función de las sugerencias de los revisores, asegúrate de tener las sugerencias previas de los revisores.
        """;

// Azure OpenAI configuration
const string AzureOpenAIEndpoint = "https://xxxx.azure.com/";
const string AzureOpenAIKey = "xxxx";

var builder = Kernel.CreateBuilder();
builder.Services
.AddHttpClient()
.AddAzureOpenAIChatCompletion("gpt-4o-simple", AzureOpenAIEndpoint, AzureOpenAIKey);
var kernel = builder.Build();

// Define the agents:

// StyleReviewer: Reviews the style of the blog entry and suggests improvements:
//   - Contains BlogInfoPlugin to get information about the blog style and tone.
//   - Uses ChatHistoryFilterReducer to only include messages from the BlogWriter and itself in the chat history.
var agentReviewerKernel = kernel.Clone();
agentReviewerKernel.ImportPluginFromObject(new BlogInfoPlugin(kernel.GetRequiredService<IHttpClientFactory>()));
var agentStyleReviewer = new ChatCompletionAgent()
    {
        Instructions = StyleReviewerInstructions,
        Name = StyleReviewerName,
        Arguments = new KernelArguments(new OpenAIPromptExecutionSettings() 
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
            MaxTokens = 4000,
        }),
        Kernel = agentReviewerKernel,
        HistoryReducer = new ChatHistoryFilterReducer(message =>
            message.AuthorName == StyleReviewerName || message.AuthorName == BlogWriterName)
    };

// FormatReviewer: Reviews the format of the blog entry and suggests improvements. 
//   - Uses ChatHistoryFilterReducer to only include messages from the BlogWriter and itself in the chat history.
var agentFormatReviewer = new ChatCompletionAgent()
    {
        Instructions = FormatReviewerInstructions,
        Name = FormatReviewerName,
        Kernel = kernel.Clone(),
        Arguments = new KernelArguments(new OpenAIPromptExecutionSettings() 
        {
            MaxTokens = 4000 
        }),
        HistoryReducer = new ChatHistoryFilterReducer(message =>
            message.AuthorName == FormatReviewerName || message.AuthorName == BlogWriterName)
    };

// BlogWriter: Writes the blog entry based on the user's input and refines it based on the reviewers' suggestions.
//   - The ChatHistoryFilterReducer is used to filter messages in the chat history.
//     Specifically, the messages must meet one of the following criteria:
//       1. The message has a Role of Assistant and its AuthorName matches one of the following: BlogWriterName, StyleReviewerName, or FormatReviewerName.
//       2. The message has a Role of User.
//     This filtering ensures that only relevant messages are considered during the chat processing.        
var agentWriter = new ChatCompletionAgent()
    {
        Instructions = BlogWriterInstructions,
        Name = BlogWriterName,
        Kernel = kernel.Clone(),  
        Arguments = new KernelArguments(new OpenAIPromptExecutionSettings() { MaxTokens = 4000 }),
        HistoryReducer = new ChatHistoryFilterReducer(message =>
            !string.IsNullOrWhiteSpace(message.Content) &&
            (
                (message.Role == AuthorRole.Assistant &&
                 (message.AuthorName == BlogWriterName ||
                  message.AuthorName == StyleReviewerName ||
                  message.AuthorName == FormatReviewerName)) ||
                message.Role == AuthorRole.User
            ))
    };

// Create a chat for agent interaction.
//   - Uses AgentApprovalTerminationStrategy to ensure that the chat ends correctly based on the reviewers' approvals or rejections.
var chat = new AgentGroupChat(agentWriter, agentStyleReviewer, agentFormatReviewer)
{
    ExecutionSettings = new AgentGroupChatSettings()
    {
        TerminationStrategy = new AgentApprovalTerminationStrategy()
        {
            Agents = [agentStyleReviewer, agentFormatReviewer],
            MaximumIterations = 20,
        },
        SelectionStrategy = new SequentialSelectionStrategy(),
    }
};

// Invoke chat and display messages

ChatMessageContent input = new(AuthorRole.User, "Fluent Assertion en c#. Añade ejemplos de código.");
chat.AddChatMessage(input);

WriteAgentChatMessage(input);

await foreach (ChatMessageContent response in chat.InvokeAsync())
{
    WriteAgentChatMessage(response);
}

if (chat.IsComplete)
{
    Console.WriteLine("[CHAT COMPLETADO CON ÉXITO]");    
}

/// <summary>
/// Common method to write formatted agent chat content to the console.
/// </summary>
static void WriteAgentChatMessage(ChatMessageContent message)
{
    // Include ChatMessageContent.AuthorName in output, if present.
    string authorExpression = message.Role == AuthorRole.User ? string.Empty : $" - {message.AuthorName ?? "*"}";
    // Include TextContent (via ChatMessageContent.Content), if present.
    string contentExpression = string.IsNullOrWhiteSpace(message.Content) ? string.Empty : message.Content;
    bool isCode = message.Metadata?.ContainsKey(OpenAIAssistantAgent.CodeInterpreterMetadataKey) ?? false;
    string codeMarker = isCode ? "\n  [CODE]\n" : " ";
    Console.WriteLine($"\n# {message.Role}{authorExpression}:{codeMarker}{contentExpression}");
    // Provide visibility for inner content (that isn't TextContent).
    foreach (KernelContent item in message.Items)
    {
        if (item is AnnotationContent annotation)
        {
            Console.WriteLine($"  [{item.GetType().Name}] {annotation.Quote}: File #{annotation.FileId}");
        }
        else if (item is FileReferenceContent fileReference)
        {
            Console.WriteLine($"  [{item.GetType().Name}] File #{fileReference.FileId}");
        }
        else if (item is ImageContent image)
        {
            Console.WriteLine($"  [{item.GetType().Name}] {image.Uri?.ToString() ?? image.DataUri ?? $"{image.Data?.Length} bytes"}");
        }
        else if (item is FunctionCallContent functionCall)
        {
            Console.WriteLine($"  [{item.GetType().Name}] {functionCall.Id}");
        }
        else if (item is FunctionResultContent functionResult)
        {
            Console.WriteLine($"  [{item.GetType().Name}] {functionResult.CallId} - {JsonSerializer.Serialize(functionResult.Result) ?? "*"}");
        }
    }
}