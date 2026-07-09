using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.UI;

namespace RingFlow.Gameplay.UI
{
    /// <summary>
    /// GDD §9 — Chest popup view.
    /// Shows accumulated Bronze/Silver/Gold/Diamond chest counts and a claim button.
    /// </summary>
    [Mediator(typeof(ChestPopupMediator))]
    public class ChestPopupView : View
    {
        public Button ClaimButton { get; private set; }
        public Button CloseButton { get; private set; }
        public Text TitleText { get; private set; }
        public Text BronzeText { get; private set; }
        public Text SilverText { get; private set; }
        public Text GoldText { get; private set; }
        public Text DiamondText { get; private set; }
        public Text TotalXpText { get; private set; }
        private GameObject _claimBtn, _closeBtn;

        private const int XpBronze = 100;
        private const int XpSilver = 250;
        private const int XpGold = 500;
        private const int XpDiamond = 1000;

        private void Awake()
        {
            BindReferencesFromChildren();
        }

        public void Localize(ILocalizationService loc)
        {
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
                var upper = btn.name.ToUpperInvariant();
                if (upper.Contains("CLAIM ALL") || upper.Contains("CLAIM")) { _claimBtn = btn.gameObject; ClaimButton = btn; }
                else if (upper.Contains("CLOSE")) { _closeBtn = btn.gameObject; CloseButton = btn; }
            }

            var texts = GetComponentsInChildren<Text>(true);
            foreach (var txt in texts)
            {
                var upper = txt.name.ToUpperInvariant();
                if (upper.Contains("TITLE") || upper.Contains("CHEST")) TitleText = txt;
                else if (upper.Contains("BRONZE")) BronzeText = txt;
                else if (upper.Contains("SILVER")) SilverText = txt;
                else if (upper.Contains("GOLD")) GoldText = txt;
                else if (upper.Contains("DIAMOND")) DiamondText = txt;
                else if (upper.Contains("TOTAL")) TotalXpText = txt;
            }
        }

        public void ShowChestCounts(int bronze, int silver, int gold, int diamond)
        {
            if (BronzeText != null) BronzeText.text = $"Bronze: x{bronze}  (+{bronze * XpBronze} XP)";
            if (SilverText != null) SilverText.text = $"Silver: x{silver}  (+{silver * XpSilver} XP)";
            if (GoldText != null) GoldText.text = $"Gold: x{gold}  (+{gold * XpGold} XP)";
            if (DiamondText != null) DiamondText.text = $"Diamond: x{diamond}  (+{diamond * XpDiamond} XP)";

            int totalXp = bronze * XpBronze + silver * XpSilver + gold * XpGold + diamond * XpDiamond;
            if (TotalXpText != null) TotalXpText.text = $"Total XP: +{totalXp}";

            bool hasAny = (bronze + silver + gold + diamond) > 0;
            if (ClaimButton != null) ClaimButton.interactable = hasAny;
        }
    }
}
