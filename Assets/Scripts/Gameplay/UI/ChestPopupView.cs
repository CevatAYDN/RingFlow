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
            // ── Dimmed background overlay ─────────────────────────────
            var overlay = GetComponent<Image>();
            overlay.color = new Color(0, 0, 0, 0.80f);

            // ── Centered card ─────────────────────────────────────────
            var card = GameUIResources.CreatePanel("Card", transform);
            GameUIResources.SetAnchors(card.GetComponent<RectTransform>(), 0.15f, 0.20f, 0.85f, 0.80f);
            card.GetComponent<Image>().color = GameUIResources.PanelColor;

            // ── Title ────────────────────────────────────────────────
            var title = GameUIResources.CreateText("CHESTS", transform, 36, TextAnchor.MiddleCenter, GameUIResources.AccentColor);
            TitleText = title.GetComponent<Text>();
            TitleText.fontStyle = FontStyle.Bold;
            GameUIResources.SetAnchors(title.GetComponent<RectTransform>(), 0.2f, 0.72f, 0.8f, 0.78f);

            // ── Chest rows ───────────────────────────────────────────
            // Bronze
            var bronze = GameUIResources.CreateText("Bronze: 0", transform, 22, TextAnchor.MiddleLeft, new Color(0.8f, 0.5f, 0.2f));
            GameUIResources.SetAnchors(bronze.GetComponent<RectTransform>(), 0.25f, 0.62f, 0.75f, 0.68f);
            BronzeText = bronze.GetComponent<Text>();

            // Silver
            var silver = GameUIResources.CreateText("Silver: 0", transform, 22, TextAnchor.MiddleLeft, new Color(0.75f, 0.75f, 0.80f));
            GameUIResources.SetAnchors(silver.GetComponent<RectTransform>(), 0.25f, 0.55f, 0.75f, 0.61f);
            SilverText = silver.GetComponent<Text>();

            // Gold
            var gold = GameUIResources.CreateText("Gold: 0", transform, 22, TextAnchor.MiddleLeft, new Color(1.0f, 0.84f, 0.0f));
            GameUIResources.SetAnchors(gold.GetComponent<RectTransform>(), 0.25f, 0.48f, 0.75f, 0.54f);
            GoldText = gold.GetComponent<Text>();

            // Diamond
            var diamond = GameUIResources.CreateText("Diamond: 0", transform, 22, TextAnchor.MiddleLeft, new Color(0.0f, 0.8f, 1.0f));
            GameUIResources.SetAnchors(diamond.GetComponent<RectTransform>(), 0.25f, 0.41f, 0.75f, 0.47f);
            DiamondText = diamond.GetComponent<Text>();

            // ── Total XP line ────────────────────────────────────────
            var total = GameUIResources.CreateText("Total XP: 0", transform, 24, TextAnchor.MiddleCenter, GameUIResources.SuccessColor);
            GameUIResources.SetAnchors(total.GetComponent<RectTransform>(), 0.2f, 0.34f, 0.8f, 0.40f);
            TotalXpText = total.GetComponent<Text>();

            // ── Claim All button (primary) ───────────────────────────
            _claimBtn = GameUIResources.CreateButton("CLAIM ALL", transform, 280, 56);
            GameUIResources.SetAnchors(_claimBtn.GetComponent<RectTransform>(), 0.30f, 0.26f, 0.70f, 0.32f);
            ClaimButton = _claimBtn.GetComponent<Button>();

            // ── Close button (secondary) ─────────────────────────────
            _closeBtn = GameUIResources.CreateButton("CLOSE", transform, 200, 44);
            GameUIResources.SetAnchors(_closeBtn.GetComponent<RectTransform>(), 0.36f, 0.20f, 0.64f, 0.24f);
            GameUIResources.ApplySecondaryStyle(_closeBtn);
            CloseButton = _closeBtn.GetComponent<Button>();
        }

        public void Localize(ILocalizationService loc)
        {
            if (loc == null) return;
            GameUIResources.LocalizeText(TitleText.gameObject, "chest_title", loc);
            GameUIResources.LocalizeButtonText(_claimBtn, "chest_claim_all", loc);
            GameUIResources.LocalizeButtonText(_closeBtn, "settings_close", loc);
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
