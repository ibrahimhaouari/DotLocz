
using System.Globalization;
using DotLocz.Abstractions;

namespace DotLocz.Services;

public sealed class CsvReader : ICsvReader, IDisposable
{

    private CsvHelper.CsvReader? csvReader;

    public void Init(string path)
    {
        var reader = new StreamReader(path);
        csvReader = new CsvHelper.CsvReader(reader, CultureInfo.InvariantCulture);
    }

    public string[]? ReadRow()
    {
        if (csvReader is null)
        {
            throw new InvalidOperationException("CsvReader not initialized");
        }

        if (csvReader.Read())
        {
            var columns = new string[csvReader.ColumnCount];
            for (int i = 0; i < csvReader.ColumnCount; i++)
            {
                columns[i] = csvReader.GetField(i) ?? string.Empty;
            }
            return columns;
        }

        return null;
    }

    public void Dispose()
    {
        csvReader?.Dispose();
    }

}