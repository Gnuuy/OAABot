namespace Infrastructure;

public interface IDataPathProvider
{
    string DataRoot { get; }
}

public sealed class DataPathProvider : IDataPathProvider
{
    public string DataRoot { get; }

    public DataPathProvider()
    {
        DataRoot = ResolveDataRoot();
        Directory.CreateDirectory(DataRoot);
    }

    private static string ResolveDataRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && directory.GetFiles("*.sln").Length == 0)
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException(
                "Could not locate the solution root (no .sln file found in any parent directory) to resolve the Data directory.");
        }

        return Path.Combine(directory.FullName, "Data");
    }
}
