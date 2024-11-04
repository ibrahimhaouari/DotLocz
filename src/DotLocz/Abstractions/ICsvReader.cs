namespace DotLocz.Abstractions;

public interface ICsvReader
{
    void Init(string path);
    string[]? ReadRow();
}
