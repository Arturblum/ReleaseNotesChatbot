using System.Collections;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Embeddings;
using ReleaseNotesChatbot.Models;

namespace ReleaseNotesChatbot.DataIngestion;


#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
public class DataUploader(IVectorStore vectorStore, ITextEmbeddingGenerationService textEmbeddingGenerator)
{
    public async Task UploadToVectorStore(string collectionName, IEnumerable<RepoChunk> textChunk)
    {
        try
        {
            var collection = vectorStore.GetCollection<string, RepoChunk>(collectionName);
            await collection.CreateCollectionIfNotExistsAsync();

            foreach (var chunk in textChunk)
            {

                //chunk.Text = chunk.Text.Length > 1000 ? chunk.Text.Substring(0, 1000) : chunk.Text;
            
                Console.WriteLine($"Generating embedding for file: {chunk.DocumentName}");
                chunk.TextEmbedding = await textEmbeddingGenerator.GenerateEmbeddingAsync(chunk.Text);

                Console.WriteLine($"Upserting chunk to vector store: {chunk.Key}");
                await collection.UpsertAsync(chunk);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

    }
}

#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.