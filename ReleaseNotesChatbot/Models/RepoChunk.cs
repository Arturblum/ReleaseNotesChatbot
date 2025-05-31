using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Data;

namespace ReleaseNotesChatbot.Models;

public record RepoChunk
{
    /// <summary>A unique key for the text paragraph.</summary>
    [VectorStoreRecordKey]
    public required string Key { get; init; }

    /// <summary>A name that points at the original location of the document containing the text.</summary>
    [VectorStoreRecordData]
    public required string DocumentName { get; init; }

    /// <summary>The text of the paragraph.</summary>
    [VectorStoreRecordData]
    public required string Text { get; init; }

    /// <summary>The embedding generated from the Text.</summary>
    [VectorStoreRecordVector(1536)]
    public ReadOnlyMemory<float> TextEmbedding { get; set; }
}

sealed class TextChunkTextSearchStringMapper : ITextSearchStringMapper
{
    /// <inheritdoc />
    public string MapFromResultToString(object result)
    {
        if (result is RepoChunk dataModel)
        {
            return dataModel.Text;
        }
        throw new ArgumentException("Invalid result type.");
    }
}

sealed class TextChunkTextSearchResultMapper : ITextSearchResultMapper
{
    /// <inheritdoc />
    public TextSearchResult MapFromResultToTextSearchResult(object result)
    {
        if (result is RepoChunk dataModel)
        {
            return new (value: dataModel.Text) { Name = dataModel.Key, Link = dataModel.DocumentName };
        }
        throw new ArgumentException("Invalid result type.");
    }
}