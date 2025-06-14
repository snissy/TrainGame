using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class CsvFileWriter
{
    private readonly string fileName;
    private readonly List<string> rows = new();
    
    public CsvFileWriter(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            throw new System.ArgumentException("File name cannot be null or empty.", nameof(fileName));

        // Ensure .csv extension
        this.fileName = fileName.EndsWith(".csv") ? fileName : fileName + ".csv";
    }
    
    public void WriteHeader(params string[] headers)
    {
        rows.Clear();
        rows.Add(JoinValues(headers));
    }
    
    public void AppendRow(params string[] values)
    {
        rows.Add(JoinValues(values));
    }
    
    public string Save(string directoryPath = null)
    {
        if (string.IsNullOrEmpty(directoryPath))
        {
            directoryPath = Application.persistentDataPath;
        }

        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string fullPath = Path.Combine(directoryPath, fileName);
        File.WriteAllLines(fullPath, rows);

        Debug.Log($"CSV file saved to: {fullPath}");
        return fullPath;
    }

    // Helper to join values with comma, escaping if necessary
    private string JoinValues(string[] values)
    {
        var csvFields = new List<string>(values.Length);
        foreach (var field in values)
        {
            if (field.Contains(",") || field.Contains("\""))
            {
                // Escape quotes by doubling them
                string escaped = field.Replace("\"", "\"\"");
                csvFields.Add($"\"{escaped}\"");
            }
            else
            {
                csvFields.Add(field);
            }
        }
        return string.Join(",", csvFields);
    }
}
