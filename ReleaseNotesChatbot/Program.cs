using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Embeddings;
using ReleaseNotesChatbot.DataIngestion;
using ReleaseNotesChatbot.Models;


#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true)
    .Build();

var modelName = configuration["ModelName"] ?? throw new ApplicationException("ModelName not found");
var embedding = configuration["EmbeddingModel"] ?? throw new ApplicationException("ModelName not found");
var endpoint = configuration["Endpoint"] ?? throw new ApplicationException("Endpoint not found");
var apiKey = configuration["ApiKey"] ?? throw new ApplicationException("ApiKey not found");

var builder = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(modelName, endpoint, apiKey)
    .AddAzureOpenAITextEmbeddingGeneration(embedding, endpoint, apiKey)
    .AddInMemoryVectorStore();

var kernel = builder.Build();

var repoChunks = new List<RepoChunk>();
var filesContent = GitHelper.GetAllCodeFromLatestCommit("");

foreach (var file in filesContent)
{
    var repoChunk = new RepoChunk()
    {
        Key = $"{file.Key}_{Guid.NewGuid()}",
        DocumentName = file.Key,
        Text = file.Value
    };
    repoChunks.Add(repoChunk);
}

var vectorStore = kernel.GetRequiredService<IVectorStore>();
var textEmbeddingGenerator = kernel.GetRequiredService<ITextEmbeddingGenerationService>();

foreach (var file in filesContent)
{
    var dataUploader = new DataUploader(vectorStore, textEmbeddingGenerator);
    await dataUploader.UploadToVectorStore("loveStory", repoChunks);//todo:replace "loveStory" 
}

var collection = vectorStore.GetCollection<string, RepoChunk>("loveStory");//todo:replace "loveStory" with a more descriptive name if needed

var stringMapper = new TextChunkTextSearchStringMapper();
var resultMapper = new TextChunkTextSearchResultMapper();
// todo: update not to use obsolete way
var textSearch = new VectorStoreTextSearch<RepoChunk>(collection, textEmbeddingGenerator, stringMapper, resultMapper);

var searchPlugin = textSearch.CreateWithGetSearchResults("LoveStorySearchPlugin");//todo: replace "LoveStorySearchPlugin" with a more descriptive name if needed
kernel.Plugins.Add(searchPlugin);

var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

AzureOpenAIPromptExecutionSettings openAiPromptExecutionSettings = new()
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

var history = new ChatHistory();

history.AddSystemMessage("You are a RAG‐enabled assistant. For every query:\n" +
                         "1. Always invoke the “LoveStorySearchPlugin” to retrieve relevant text chunks.\n" +
                         "2. Base your answer on those chunks whenever possible.\n" +
                         "3. Cite each fact with its source in the form (DocumentName, paragraph #).\n" +
                         "Keep answers concise and grounded in the retrieved material.");

do
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write("Me > ");
    Console.ResetColor();

    var userInput = Console.ReadLine();
    if (userInput == "exit")
    {
        break;
    }

    history.AddUserMessage(userInput!);

    var streamingResponse =
        chatCompletionService.GetStreamingChatMessageContentsAsync(
            history,
            openAiPromptExecutionSettings,
            kernel);

    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("Agent > ");
    Console.ResetColor();

    var fullResponse = "";
    await foreach (var chunk in streamingResponse)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write(chunk.Content);
        Console.ResetColor();
        fullResponse += chunk.Content;
    }
    Console.WriteLine();

    history.AddMessage(AuthorRole.Assistant, fullResponse);


} while (true);
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.