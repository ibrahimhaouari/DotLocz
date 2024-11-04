namespace DotLocz;

public interface ICsvReader
{
    void Init(string path);
    string[]? ReadRow();
}
