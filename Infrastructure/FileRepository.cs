using System.Text.Json;
using Application.Interfaces;

namespace Infrastructure;

public class FileRepository : IFileOperations
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    // Deserialization only: tolerates hand-edited data files using lowercase/camelCase keys
    // (natural for a human editing JSON by hand) against C#'s PascalCase property names, without
    // changing how the app's own auto-generated files serialize.
    private static readonly JsonSerializerOptions DeserializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IDataPathProvider _pathProvider;

    public FileRepository(IDataPathProvider pathProvider)
    {
        _pathProvider = pathProvider;
    }

    public async Task WriteToJsonFileAsync<T>(string fileName, T content)
    {
        ValidateFileName(fileName);

        if (content is null)
        {
            throw new ArgumentNullException(nameof(content), "The content cannot be null.");
        }

        string actualPath = ResolvePath(fileName, ".json");
        string tempPath = ResolvePath($"{fileName}.{Guid.NewGuid():N}", ".tmp");

        Directory.CreateDirectory(Path.GetDirectoryName(actualPath)!);

        try
        {
            await using (FileStream fileStream = new FileStream(
                tempPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                useAsync: true
            ))
            {
                await JsonSerializer.SerializeAsync(fileStream, content, SerializerOptions);

                await fileStream.FlushAsync();

                fileStream.Flush(flushToDisk: true);
            }

            File.Move(tempPath, actualPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    public async Task<T> ReadFromJsonFileAsync<T>(string fileName)
    {
        ValidateFileName(fileName);

        string actualPath = ResolvePath(fileName, ".json");

        if (!File.Exists(actualPath))
        {
            throw new FileNotFoundException(
                $"The JSON file '{fileName}.json' does not exist.",
                actualPath);
        }

        await using FileStream fileStream = new(
            actualPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);

        T? content = await JsonSerializer.DeserializeAsync<T>(fileStream, DeserializerOptions);

        if (content is null)
        {
            throw new JsonException(
                $"The file '{fileName}.json' did not contain valid data.");
        }

        return content;
    }

    private static void ValidateFileName(string fileName)
    {
        if (fileName is null)
        {
            throw new ArgumentNullException(nameof(fileName), "The file name cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException(
                "The file name cannot be empty or contain only whitespace.", nameof(fileName));
        }
    }

    // fileName may contain forward-slash separated segments (e.g. "leaderboards/leaderboard")
    // to place the file in a subdirectory under the Data root.
    private string ResolvePath(string relativeFileName, string extension)
    {
        var segments = relativeFileName.Split('/', StringSplitOptions.RemoveEmptyEntries);
        segments[^1] += extension;

        var path = _pathProvider.DataRoot;
        foreach (var segment in segments)
        {
            path = Path.Combine(path, segment);
        }

        return path;
    }
}
