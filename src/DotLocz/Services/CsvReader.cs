
using System.Globalization;
using DotLocz.Abstractions;

namespace DotLocz.Services;

public sealed class CsvReader : ICsvReader, IDisposable
{

    private StreamReader? reader;
    private CsvHelper.CsvReader? csvReader;

    private static readonly CsvHelper.Configuration.CsvConfiguration CsvConfig =
        new(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
            Delimiter = ",",
            IgnoreBlankLines = true,
            TrimOptions = CsvHelper.Configuration.TrimOptions.Trim
        };

    public void Init(string path)
    {
        if (reader is not null || csvReader is not null)
        {
            // Make sure to dispose the previous reader and csvReader
            reader?.Dispose();
            csvReader?.Dispose();
        }

        reader = new StreamReader(path);
        csvReader = new CsvHelper.CsvReader(reader, CsvConfig);
    }

    public bool ReadRow(out string[] columns)
    {
        if (csvReader is null)
        {
            throw new InvalidOperationException("CsvReader not initialized");
        }

        columns = [];

        if (csvReader.Read())
        {
            columns = new string[csvReader.ColumnCount];
            for (int i = 0; i < csvReader.ColumnCount; i++)
            {
                columns[i] = csvReader.GetField(i) ?? string.Empty;
            }
            return true;
        }

        return false;
    }

    public void Dispose()
    {
        reader?.Dispose();
        csvReader?.Dispose();
    }

}