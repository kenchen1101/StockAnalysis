﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data.SqlClient;

using CommandLine;
using StockAnalysis.Share;

namespace ImportTable
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = new Options();
            var parser = new CommandLine.Parser(with => with.HelpWriter = Console.Error);

            if (parser.ParseArgumentsStrict(args, options, () => { Environment.Exit(-2); }))
            {
                options.BoundaryCheck();

                Run(options);
            }
        }

        private static void Run(Options options)
        {
            options.Print(Console.Out);

            string tableName = Path.GetFileNameWithoutExtension(options.CsvFile);
            Csv csv = Csv.Load(options.CsvFile, Encoding.UTF8, options.Separator);

            using (SqlConnection connection = new SqlConnection(ImportTable.Properties.Settings.Default.stockConnectionString))
            {
                connection.Open();

                // drop table
                string cmdstring = BuildDropTableSql(tableName);
                SqlCommand cmd = new SqlCommand(cmdstring, connection);
                cmd.ExecuteNonQuery();

                // create table
                cmdstring = BuildCreateTableSql(tableName, csv.Header);
                cmd = new SqlCommand(cmdstring, connection);
                cmd.ExecuteNonQuery();

                // create index if necessary
                cmdstring = BuildCreateIndexSql(tableName, "Index1", csv.Header);
                if (!string.IsNullOrEmpty(cmdstring))
                {
                    cmd = new SqlCommand(cmdstring, connection);
                    cmd.ExecuteNonQuery();
                }

                // insert values
                for (int i = 0; i < csv.RowCount; ++i)
                {
                    cmdstring = BuildInsertRowSql(tableName, csv[i]);
                    cmd = new SqlCommand(cmdstring, connection);
                    cmd.ExecuteNonQuery();

                    if (i % 100 == 0)
                    {
                        Console.Write(".");
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine("Done.");
        }

        private static string BuildInsertRowSql(string tableName, string[] row)
        {
            // INSERT INTO [dbo].[table] ( "a", "b", "c" )
            StringBuilder builder = new StringBuilder();

            builder.AppendFormat("INSERT INTO [dbo].[{0}] VALUES (", tableName);

            for (int i = 0; i < row.Length; ++i)
            {
                if (i > 0)
                {
                    builder.Append(",");
                }

                builder.AppendFormat("N'{0}'", row[i]);
            }

            builder.Append(")");

            return builder.ToString();
        }

        private static string BuildDropTableSql(string tableName)
        {
            // IF EXISTS ( SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '[table_name]')DROP TABLE [table_name]
            return string.Format("IF EXISTS ( SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'{0}' ) DROP TABLE [dbo].[{0}]", tableName);
        }

        private static string BuildCreateIndexSql(string tableName, string indexName, string[] columns)
        {
            string[] primaryKeys = GetPrimaryKeys(columns);

            if (primaryKeys != null && primaryKeys.Length > 1)
            {
                //CREATE INDEX [CodeIndex] ON [dbo].[Table] ([Code])

                return string.Format("CREATE INDEX [{0}] ON [dbo].[{1}] ([{2}])", indexName, tableName, primaryKeys[0]);
            }

            return string.Empty;
        }

        private static string BuildCreateTableSql(string tableName, string[] columns)
        {
            //CREATE TABLE [dbo].[Table]
            //(
            //    [code] NVARCHAR(50) NOT NULL , 
            //    [column] NCHAR(50) NOT NULL, 
            //    PRIMARY KEY ([code], [column])
            //)

            StringBuilder builder = new StringBuilder();

            builder.AppendFormat("CREATE TABLE [dbo].[{0}] (", tableName);

            for (int i = 0; i < columns.Length; ++i)
            {
                if (i != 0)
                {
                    builder.Append(",");
                }

                builder.AppendFormat("[{0}] NVARCHAR(50)", columns[i]);

                builder.Append(IsColumnNullable(columns[i]) ? "NULL" : "NOT NULL");
            }

            string[] primaryKeys = GetPrimaryKeys(columns);
            if (primaryKeys != null && primaryKeys.Length > 0)
            {
                builder.Append(", PRIMARY KEY (");

                for (int j = 0; j < primaryKeys.Length; ++j)
                {
                    if (j > 0)
                    {
                        builder.Append(",");
                    }

                    builder.AppendFormat("[{0}]", primaryKeys[j]);
                }

                builder.Append(")");
            }

            builder.Append(")");

            return builder.ToString();
        }

        private static bool IsColumnNullable(string column)
        {
            if (column.ToLower() == "code"
                || column.ToLower() == "periodorcolumn")
            {
                return false;
            }

            return true;
        }

        private static string[] GetPrimaryKeys(string[] columns)
        {
            List<string> primaryKeys = new List<string>();

            columns = columns.Select(s => s.ToLower()).ToArray();

            if (Array.IndexOf(columns, "code") >= 0)
            {
                primaryKeys.Add("code");
            }
            
            if (Array.IndexOf(columns, "periodorcolumn") >= 0)
            {
                primaryKeys.Add("periodorcolumn");
            }

            return primaryKeys.ToArray();
        }
    }
}
