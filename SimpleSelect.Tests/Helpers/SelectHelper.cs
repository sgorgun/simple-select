using Microsoft.Data.Sqlite;
using SqlDataInsert.Tests.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace SqlDataInsert.Tests.Helpers
{
    internal class SelectHelper
    {
        public static SelectResult[] GetResults(IEnumerable<string> queries)
        {
            var results = new List<SelectResult>();
            foreach (var query in queries)
            {
                var result = GetResult(query);
                results.Add(result);
            }
            return results.ToArray();
        }

        public static SelectResult GetResult(string query)
        {
            var command = new SqliteCommand(query, SqliteHelper.Connection);
            var result = new SelectResult();
            try
            {
                var reader = command.ExecuteReader();
                result = Read(reader);
            }
            catch (Exception e)
            {
                result.ErrorMessage = e.Message;
            }
            return result;
        }

        public static void SerializeResult(SelectResult selectResult, string file)
        {
            File.WriteAllText(file, selectResult.ToString());
        }

        public static SelectResult DeserializeResult(string file)
        {
            if (!File.Exists(file))
                throw new FileNotFoundException($"The file '{file}' was not found in the 'Data' folder.");
            var lines = File.ReadAllLines(file);
            var schema = lines[0].Split(",");
            var types = lines[1].Split(",");
            var data = new List<string[]>();
            for (var i = 2; i < lines.Length; i++)
            {
                var line = lines[i];
                data.Add(line.Split(","));
            }
            return new SelectResult
            {
                Schema = schema,
                Types = types,
                Data = data.ToArray()
            };
        }

        private static SelectResult Read(SqliteDataReader reader)
        {
            var data = new List<string[]>();
            var result = new SelectResult(reader.FieldCount);
            while (reader.Read())
            {
                var rowData = new string[reader.FieldCount];
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    if (data.Count == 0)
                    {
                        result.Schema[i] = reader.GetName(i);
                        result.Types[i] = reader.GetDataTypeName(i);
                    }
                    rowData[i] = reader.GetString(i);
                }
                data.Add(rowData);
            }
            result.Data = data.ToArray();
            return result;
        }
    }
}