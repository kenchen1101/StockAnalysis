﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;

namespace StockAnalysis.Share
{
    public sealed class DataDictionary
    {
        public sealed class TableDataDictionary
        {
            public const string RootElementName = "Table";
            public const string NameAttributeName = "name";
            public const string RowElementName = "Row";
            public const string ColumnElementName = "Column";

            public string TableName { get; private set; }
            public AliasNameMapping RowNameMap { get; private set; }
            public AliasNameMapping ColumnNameMap { get; private set; }

            public TableDataDictionary()
                : this(string.Empty)
            {
            }

            public TableDataDictionary(string name)
            {
                TableName = name;

                RowNameMap = new AliasNameMapping();

                ColumnNameMap = new AliasNameMapping();
            }

            public TableDataDictionary(TableDataDictionary rhs)
            {
                TableName = rhs.TableName;

                RowNameMap = new AliasNameMapping(rhs.RowNameMap);

                ColumnNameMap = new AliasNameMapping(rhs.ColumnNameMap);
            }

            public void LoadFromXml(XmlElement element)
            {
                if (element.Name != RootElementName)
                {
                    throw new InvalidDataException(string.Format("root element is not expected {0}", RootElementName));
                }

                TableName = element.GetAttribute(NameAttributeName);

                foreach (var child in element.ChildNodes)
                {
                    XmlElement childElement = child as XmlElement;

                    if (childElement != null)
                    {
                        if (childElement.Name == RowElementName)
                        {
                            RowNameMap.LoadFromXml(childElement);
                        }
                        else if (childElement.Name == ColumnElementName)
                        {
                            ColumnNameMap.LoadFromXml(childElement);
                        }
                    }
                }
            }

            public XmlElement SaveToXml(XmlDocument doc)
            {
                XmlElement rootElement = doc.CreateElement(RootElementName);
                rootElement.SetAttribute(NameAttributeName, TableName);

                XmlElement rowElement = doc.CreateElement(RowElementName);
                RowNameMap.SaveToXml(doc, rowElement);

                XmlElement columnElement = doc.CreateElement(ColumnElementName);
                ColumnNameMap.SaveToXml(doc, columnElement);

                rootElement.AppendChild(rowElement);
                rootElement.AppendChild(columnElement);

                return rootElement;
            }
        }

        private const string RootElementName = "DataDictionary";
        private const string TableNamesElementName = "TableName";

        private AliasNameMapping _tableNames = new AliasNameMapping();

        private Dictionary<string, TableDataDictionary> _tableDataDictionaries = new Dictionary<string, TableDataDictionary>();

        public DataDictionary()
        {
        }

        public DataDictionary(string dataFile)
        {
            Load(dataFile);
        }

        public DataDictionary(DataDictionary rhs)
        {
            _tableNames = new AliasNameMapping(rhs._tableNames);

            foreach (var kvp in rhs._tableDataDictionaries)
            {
                _tableDataDictionaries.Add(kvp.Key, new TableDataDictionary(kvp.Value));
            }
        }

        public void Load(string dataFile)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(dataFile);

            if (doc.DocumentElement.Name != RootElementName)
            {
                throw new InvalidDataException(string.Format("Root element is not expected {0}.", RootElementName));
            }

            foreach (var node in doc.DocumentElement.ChildNodes)
            {
                XmlElement element = node as XmlElement;
                if (element == null)
                {
                    continue;
                }

                if (element.Name == TableNamesElementName)
                {
                    _tableNames.LoadFromXml(element);
                }
                else if (element.Name == TableDataDictionary.RootElementName)
                {
                    TableDataDictionary dict = new TableDataDictionary();
                    dict.LoadFromXml(element);

                    _tableDataDictionaries.Add(dict.TableName, dict);
                }
            }
        }

        public void Save(string dataFile)
        {
            XmlDocument doc = new XmlDocument();

            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "utf-8", null);

            // Create the root element
            XmlElement rootNode = doc.CreateElement(RootElementName);
            doc.InsertBefore(xmlDeclaration, doc.DocumentElement);
            doc.AppendChild(rootNode);

            XmlElement tableNamesElement = doc.CreateElement(TableNamesElementName);
            _tableNames.SaveToXml(doc, tableNamesElement);

            rootNode.AppendChild(tableNamesElement);

            foreach (var kvp in _tableDataDictionaries)
            {
                rootNode.AppendChild(kvp.Value.SaveToXml(doc));
            }

            doc.Save(dataFile);
        }

        public string GetNormalizedTableName(string tableName)
        {
            string normalizedName;
            if (!_tableNames.TryGetNormalizedNameForAlias(tableName, out normalizedName))
            {
                normalizedName = tableName;
            }

            return normalizedName;
        }

        public string GetNormalizedRowName(string tableName, string rowName)
        {
            string normalizedTableName = GetNormalizedTableName(tableName);

            string normalizedRowName = rowName;
            TableDataDictionary tableDictionary;
            if (_tableDataDictionaries.TryGetValue(normalizedTableName, out tableDictionary))
            {
                if (!tableDictionary.RowNameMap.TryGetNormalizedNameForAlias(rowName, out normalizedRowName))
                {
                    normalizedRowName = rowName;
                }
            }

            return normalizedRowName;
        }

        public IEnumerable<string> GetPossibleNormalizedTableNameByRowNameAlias(string rowName)
        {
            return _tableDataDictionaries.Where(kvp => kvp.Value.RowNameMap.ContainsAlias(rowName)).Select(kvp => kvp.Key);
        }

        public string GetNormalizedColumnName(string tableName, string columnName)
        {
            string normalizedTableName = GetNormalizedTableName(tableName);

            string normalizedColumnName = columnName;
            TableDataDictionary tableDictionary;
            if (_tableDataDictionaries.TryGetValue(normalizedTableName, out tableDictionary))
            {
                if (!tableDictionary.RowNameMap.TryGetNormalizedNameForAlias(columnName, out normalizedColumnName))
                {
                    normalizedColumnName = columnName;
                }
            }

            return normalizedColumnName;
        }

        public void AddTableName(string tableName)
        {
            if (!_tableNames.ContainsAlias(tableName))
            {
                _tableNames.Add(tableName, tableName);
            }
        }

        public void AddRowName(string tableName, string rowName)
        {
            string normalizedTableName = GetNormalizedTableName(tableName);

            TableDataDictionary tableDictionary;

            if (_tableDataDictionaries.ContainsKey(normalizedTableName))
            {
                tableDictionary = _tableDataDictionaries[normalizedTableName];
            }
            else
            {
                tableDictionary = new TableDataDictionary(normalizedTableName);
                _tableDataDictionaries.Add(normalizedTableName, tableDictionary);
            }

            tableDictionary.RowNameMap.Add(rowName, rowName);
        }

        public void AddColumnName(string tableName, string columnName)
        {
            string normalizedTableName = GetNormalizedTableName(tableName);

            TableDataDictionary tableDictionary;

            if (_tableDataDictionaries.ContainsKey(normalizedTableName))
            {
                tableDictionary = _tableDataDictionaries[normalizedTableName];
            }
            else
            {
                tableDictionary = new TableDataDictionary(normalizedTableName);
                _tableDataDictionaries.Add(normalizedTableName, tableDictionary);
            }

            tableDictionary.ColumnNameMap.Add(columnName, columnName);
        }
    }
}
