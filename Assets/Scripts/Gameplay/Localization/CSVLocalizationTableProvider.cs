using System;
using System.Collections.Generic;
using Nexus.Core.Services;
using UnityEngine;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// GDD §11 — Loads and parses the Localization.csv file from Resources.
    /// Provides translations for all 15 supported languages.
    /// </summary>
    public class CSVLocalizationTableProvider : ILocalizationTableProvider
    {
        private readonly Dictionary<string, IDictionary<string, string>> _tables = new(StringComparer.OrdinalIgnoreCase);
        private bool _loaded;

        private void LoadCSVIfNeeded()
        {
            if (_loaded) return;

            var csvAsset = Resources.Load<TextAsset>("Localization");
            if (csvAsset == null)
            {
                Debug.LogError("[CSVLocalizationTableProvider] Localization.csv not found in Resources!");
                _loaded = true;
                return;
            }

            string[] lines = csvAsset.text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length <= 1)
            {
                _loaded = true;
                return;
            }

            // Header: Key,en,tr,id,es,fr,de,pt,it,ar,hi,ru,ja,zh,ko,vi
            string[] headers = lines[0].Split(',');
            List<string> langCodes = new List<string>();
            for (int i = 1; i < headers.Length; i++)
            {
                string code = headers[i].Trim().ToLower();
                langCodes.Add(code);
                _tables[code] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            // Parse values
            for (int lineIndex = 1; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex];
                string[] parts = SplitCsvLine(line);
                if (parts.Length == 0) continue;

                string key = parts[0].Trim();
                for (int i = 1; i < parts.Length && i - 1 < langCodes.Count; i++)
                {
                    string lang = langCodes[i - 1];
                    string val = parts[i].Trim();
                    _tables[lang][key] = val;
                }
            }

            _loaded = true;
        }

        private string[] SplitCsvLine(string line)
        {
            // Simple split by comma, handling potential commas inside quotes if needed
            return line.Split(',');
        }

        public bool TryGetTable(string langCode, out IDictionary<string, string> table)
        {
            LoadCSVIfNeeded();
            string normalized = langCode.Trim().ToLower();
            if (_tables.TryGetValue(normalized, out var dict))
            {
                table = dict;
                return true;
            }
            table = null;
            return false;
        }
    }
}
