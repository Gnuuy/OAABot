namespace Application.Interfaces;

public interface IFileOperations
{
    public Task WriteToJsonFileAsync<T>(string fileName, T content);
    public Task<T> ReadFromJsonFileAsync<T>(string fileName);
}