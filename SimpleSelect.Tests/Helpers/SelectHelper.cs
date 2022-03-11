﻿using Microsoft.Data.Sqlite;
using AutocodeDB.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace AutocodeDB.Helpers
{
    internal class SelectHelper
    {
        private static readonly Regex SelectDistinctFrom_Regex = new Regex(@"\s*SELECT\s+(DISTINCT\s+)*((\w+(\.\w+)*(\s+AS\s+((\w+)|('\w+')))*)|(\*))(\s*[\S\s][^;]*\s+FROM\s+\w+)");
        private static readonly Regex SelectFromAggregate_Regex = new Regex(@"\s*SELECT\s+((COUNT)|(AVG)|(SUM)|(MIN)|(MAX))(\s+DISTINCT)*\s*\(\s*((\w+)|(\*))\s*\)(\s+AS\s+((\w+)|('\w+')*)|(\*))*(\s*[\S\s][^;]*\s+FROM\s+\w+)");
        private static readonly Regex InnerJoin_Regex = new Regex(@"\s*INNER\s+JOIN\s+([\s\w]+ON){1}\s+([\s\.\w]*[^=]){1}\s*=");
        private static readonly Regex OrderBy_Regex = new Regex(@"ORDER\s+BY\s+\w+");
        private static readonly Regex GroupBy_Regex = new Regex(@"GROUP\s+BY\s+\w+");

        public static bool ContainsSelectDistinctFrom(string query) => SelectDistinctFrom_Regex.IsMatch(query);
        public static bool ContainsSelectFromAggregate(string query) => SelectFromAggregate_Regex.IsMatch(query);
        public static bool ContainsInnerJoin(string query) => InnerJoin_Regex.IsMatch(query);
        public static bool ContainsOrderBy(string query) => OrderBy_Regex.IsMatch(query);
        public static bool ContainsGroupBy(string query) => GroupBy_Regex.IsMatch(query);

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