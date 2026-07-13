using UnityEngine;
using System.Collections.Generic;

namespace RingFlow.Gameplay.Localization
{
    [System.Serializable]
    public struct LanguageEntry
    {
        public string Code;
        public string DisplayName;
        public bool IsRTL;
    }

    [CreateAssetMenu(fileName = "LocalizationConfig", menuName = "RingFlow/Localization Config", order = 62)]
    public class LocalizationConfigSO : ScriptableObject
    {
        public List<LanguageEntry> Languages = new()
        {
            new() { Code = "en", DisplayName = "English",    IsRTL = false },
            new() { Code = "tr", DisplayName = "Türkçe",     IsRTL = false },
            new() { Code = "id", DisplayName = "Bahasa Indonesia", IsRTL = false },
            new() { Code = "es", DisplayName = "Español",    IsRTL = false },
            new() { Code = "fr", DisplayName = "Français",   IsRTL = false },
            new() { Code = "de", DisplayName = "Deutsch",    IsRTL = false },
            new() { Code = "pt", DisplayName = "Português",  IsRTL = false },
            new() { Code = "it", DisplayName = "Italiano",   IsRTL = false },
            new() { Code = "ar", DisplayName = "العربية",    IsRTL = true  },
            new() { Code = "hi", DisplayName = "हिन्दी",     IsRTL = false },
            new() { Code = "ru", DisplayName = "Русский",    IsRTL = false },
            new() { Code = "ja", DisplayName = "日本語",     IsRTL = false },
            new() { Code = "zh", DisplayName = "中文",       IsRTL = false },
            new() { Code = "ko", DisplayName = "한국어",     IsRTL = false },
            new() { Code = "vi", DisplayName = "Tiếng Việt", IsRTL = false }
        };
    }
}
