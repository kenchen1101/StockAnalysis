﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace StockAnalysis.Share
{
    public sealed class AliasNameMapping
    {
        private const string MapElementName = "Map";
        private const string NameAttributeName = "name";
        private const string AliasesAttributeName = "aliases";
        private const string AliasSeparator = "|";

        private Dictionary<string, List<string>> _normalizedNameToAliasesMap = new Dictionary<string, List<string>>();

        private Dictionary<string, string> _aliasToNormalizedNameMap = new Dictionary<string, string>();

        public AliasNameMapping()
        {
        }

        public AliasNameMapping(AliasNameMapping rhs)
            : this(rhs.GetAliasToNormalizedNameMap())
        {

        }

        public AliasNameMapping(IDictionary<string, string> aliasToNormalizedNameMap)
        {
            if (aliasToNormalizedNameMap == null)
            {
                throw new ArgumentNullException("aliasToNormalizedNameMap");
            }

            foreach (var kvp in aliasToNormalizedNameMap)
            {
                string normalizedName = kvp.Value;
                string alias = kvp.Key;

                Add(alias, normalizedName);
            }
        }

        public void LoadFromXml(XmlElement parentElement)
        {
            foreach (var mapNode in parentElement.ChildNodes)
            {
                XmlElement mapElement = mapNode as XmlElement;
                if (mapElement == null || mapElement.Name != MapElementName)
                {
                    continue;
                }

                string name = mapElement.GetAttribute(NameAttributeName);
                string aliasesString = mapElement.GetAttribute(AliasesAttributeName);

                string[] aliases = aliasesString.Split(new string[] { AliasSeparator }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var alias in aliases)
                {
                    if (ContainsAlias(alias))
                    {
                        string existingName = GetNormalizedNameForAlias(alias);
                        if (existingName != name)
                        {
                            // same alias, different names
                            throw new InvalidOperationException(
                                string.Format(
                                    "Alias [{0}] has different normalized name: [{1}] and [{2}]",
                                    name,
                                    existingName));
                        }
                        else
                        {
                            // duplicated <alias, name>
                            continue;
                        }
                    }
                    else
                    {
                        Add(alias, name);
                    }
                }
            }
        }

        public void SaveToXml(XmlDocument doc, XmlElement parentElement)
        {
            var sortedNames = GetAllNormalizedNames().OrderBy(s => s);
            if (sortedNames.Count() > 0)
            {
                foreach (var name in sortedNames)
                {
                    string aliases = string.Join(AliasSeparator, GetAliasesForNormalizedName(name));

                    XmlElement mapElement = doc.CreateElement(MapElementName);
                    mapElement.SetAttribute(AliasesAttributeName, aliases);
                    mapElement.SetAttribute(NameAttributeName, name);

                    parentElement.AppendChild(mapElement);
                }
            }
        }

        public IDictionary<string, string> GetAliasToNormalizedNameMap()
        {
            return _aliasToNormalizedNameMap;
        }

        public void Add(string alias, string normalizedName)
        {
            // check duplicate data
            if (_aliasToNormalizedNameMap.ContainsKey(alias) && _aliasToNormalizedNameMap[alias] == normalizedName)
            {
                return;
            }

            _aliasToNormalizedNameMap.Add(alias, normalizedName);

            if (_normalizedNameToAliasesMap.ContainsKey(normalizedName))
            {
                _normalizedNameToAliasesMap[normalizedName].Add(alias);
            }
            else
            {
                List<string> aliases = new List<string>();
                aliases.Add(alias);

                _normalizedNameToAliasesMap.Add(normalizedName, aliases);
            }
        }

        public bool ContainsNormalizedName(string normalizedName)
        {
            return _normalizedNameToAliasesMap.ContainsKey(normalizedName);
        }

        public bool ContainsAlias(string alias)
        {
            return _aliasToNormalizedNameMap.ContainsKey(alias);
        }

        public bool TryGetNormalizedNameForAlias(string alias, out string normalizedName)
        {
            return _aliasToNormalizedNameMap.TryGetValue(alias, out normalizedName);
        }

        public string GetNormalizedNameForAlias(string alias)
        {
            return _aliasToNormalizedNameMap[alias];
        }

        public bool TryGetAliasesForNormalizedName(string normalizedName, out List<string> aliases)
        {
            return _normalizedNameToAliasesMap.TryGetValue(normalizedName, out aliases);
        }

        public IEnumerable<string> GetAliasesForNormalizedName(string normalizedName)
        {
            return _normalizedNameToAliasesMap[normalizedName];
        }

        public IEnumerable<string> GetAllAliases()
        {
            return _aliasToNormalizedNameMap.Keys;
        }

        public IEnumerable<string> GetAllNormalizedNames()
        {
            return _normalizedNameToAliasesMap.Keys;
        }
    }
}
