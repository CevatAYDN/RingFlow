using System;
using System.Collections.Generic;
using Nexus.Core;
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

        [Inject] private Services.IAssetService _assetService;

        private void LoadCSVIfNeeded()
        {
            if (_loaded) return;

            if (_assetService == null)
                throw new InvalidOperationException("[CSVLocalizationTableProvider] IAssetService is required.");

            var task = _assetService.LoadAsync<TextAsset>(GameplayAssetKeys.Localization);
            var csvAsset = task.GetAwaiter().GetResult();
            if (csvAsset == null)
                throw new InvalidOperationException($"[CSVLocalizationTableProvider] Localization.csv not found at '{GameplayAssetKeys.Localization}'.");

            string[] lines = csvAsset.text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length <= 1)
            {
                NexusLog.Warn("CSVLocalizationTableProvider", nameof(LoadCSVIfNeeded), "",
                    $"Localization asset has only {lines.Length} lines — no usable content.");
                _loaded = true;
                return;
            }

            // Header: Key,en,tr,id,es,fr,de,pt,it,ar,hi,ru,ja,zh,ko,vi
            string[] headers = SplitCsvLine(lines[0]);
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
            var result = new List<string>();
            bool inQuotes = false;
            int start = 0;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (line[i] == ',' && !inQuotes)
                {
                    result.Add(UnescapeCsvField(line.Substring(start, i - start)));
                    start = i + 1;
                }
            }
            result.Add(UnescapeCsvField(line.Substring(start)));
            return result.ToArray();
        }

        private static string UnescapeCsvField(string field)
        {
            field = field.Trim();
            if (field.Length >= 2 && field[0] == '"' && field[^1] == '"')
            {
                field = field.Substring(1, field.Length - 2);
                field = field.Replace("\"\"", "\"");
            }
            return field;
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
