namespace DotLocz.Abstractions;

public interface ICsvReader
{
    void Init(string path);
    bool ReadRow(out string[] columns);
}
