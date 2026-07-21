using Nexus.Core;
using Nexus.Core.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RingFlow.Gameplay.UI
{
    /// <summary>
    /// GDD §9 — Chest popup view.
    /// Shows accumulated Bronze/Silver/Gold/Diamond chest counts and a claim button.
    /// Self-building when the prefab lacks authored children.
    /// </summary>
    [Mediator(typeof(ChestPopupMediator))]
    public class ChestPopupView : View, IAuthoredView
    {
        [SerializeField] private Button _claimButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _bronzeText;
        [SerializeField] private TextMeshProUGUI _silverText;
        [SerializeField] private TextMeshProUGUI _goldText;
        [SerializeField] private TextMeshProUGUI _diamondText;
        [SerializeField] private TextMeshProUGUI _totalXpText;
        public Button ClaimButton => _claimButton;
        public Button CloseButton => _closeButton;
        public TextMeshProUGUI TitleText => _titleText;
        public TextMeshProUGUI BronzeText => _bronzeText;
        public TextMeshProUGUI SilverText => _silverText;
        public TextMeshProUGUI GoldText => _goldText;
        public TextMeshProUGUI DiamondText => _diamondText;
        public TextMeshProUGUI TotalXpText => _totalXpText;
        private GameObject _claimBtn, _closeBtn;
        private ILocalizationService _locService;

        private void Awake()
        {
            BindReferencesFromChildren();
            ApplyBaseStyling();
        }

        /// <summary>
        /// Satisfies IAuthoredView. UI hierarchy is now loaded from prefab;
        /// this method is kept for interface compliance (Editor UI Studio tooling).
        /// </summary>
        public void BuildUI() { }

        public void Localize(ILocalizationService loc)
        {
            _locService = loc;
            if (loc == null) return;
            if (TitleText != null) GameUIResources.LocalizeText(TitleText.gameObject, "chest_title", loc);
            GameUIResources.LocalizeButtonText(_claimBtn, "chest_claim_all", loc);
            GameUIResources.LocalizeButtonText(_closeBtn, "settings_close", loc);
        }

        private void BindReferencesFromChildren()
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                GameUIResources.AddButtonEffects(btn);
                var upper = btn.name.ToUpperInvariant();
                if (upper.Contains("CLAIM ALL") || upper.Contains("CLAIM")) { _claimBtn = btn.gameObject; _claimButton = btn; }
                else if (upper.Contains("CLOSE")) { _closeBtn = btn.gameObject; _closeButton = btn; }
            }

            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var txt in texts)
            {
                var upper = txt.name.ToUpperInvariant();
                if (upper.Contains("TITLE") || upper.Contains("CHEST")) _titleText = txt;
                else if (upper.Contains("BRONZE")) _bronzeText = txt;
                else if (upper.Contains("SILVER")) _silverText = txt;
                else if (upper.Contains("GOLD")) _goldText = txt;
                else if (upper.Contains("DIAMOND")) _diamondText = txt;
                else if (upper.Contains("TOTAL")) _totalXpText = txt;
            }
        }

        private void ApplyBaseStyling()
        {
            if (_claimBtn != null) GameUIResources.ApplyPrimaryStyle(_claimBtn);
            if (_closeBtn != null) GameUIResources.ApplySecondaryStyle(_closeBtn);
        }

        public void ShowChestCounts(int bronze, int silver, int gold, int diamond, 
            int xpBronze = 100, int xpSilver = 250, int xpGold = 500, int xpDiamond = 1000)
        {
            int totalXp = bronze * xpBronze + silver * xpSilver + gold * xpGold + diamond * xpDiamond;

            if (BronzeText != null)
            {
                string fmt = GetLocalizedFormat("chest_bronze_format", "Bronze: x{0}  (+{1} XP)");
                BronzeText.text = string.Format(fmt, bronze, bronze * xpBronze);
            }
            if (SilverText != null)
            {
                string fmt = GetLocalizedFormat("chest_silver_format", "Silver: x{0}  (+{1} XP)");
                SilverText.text = string.Format(fmt, silver, silver * xpSilver);
            }
            if (GoldText != null)
            {
                string fmt = GetLocalizedFormat("chest_gold_format", "Gold: x{0}  (+{1} XP)");
                GoldText.text = string.Format(fmt, gold, gold * xpGold);
            }
            if (DiamondText != null)
            {
                string fmt = GetLocalizedFormat("chest_diamond_format", "Diamond: x{0}  (+{1} XP)");
                DiamondText.text = string.Format(fmt, diamond, diamond * xpDiamond);
            }
            if (TotalXpText != null)
            {
                string fmt = GetLocalizedFormat("chest_total_xp_format", "Total XP: +{0}");
                TotalXpText.text = string.Format(fmt, totalXp);
            }

            bool hasAny = (bronze + silver + gold + diamond) > 0;
            if (ClaimButton != null) ClaimButton.interactable = hasAny;
        }

        private string GetLocalizedFormat(string key, string fallback)
        {
            return _locService != null ? _locService.GetString(key, fallback) : fallback;
        }
    }
}
