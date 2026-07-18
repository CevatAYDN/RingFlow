using Nexus.Core;
using Nexus.Core.Services;
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
        public Button ClaimButton { get; private set; }
        public Button CloseButton { get; private set; }
        public Text TitleText { get; private set; }
        public Text BronzeText { get; private set; }
        public Text SilverText { get; private set; }
        public Text GoldText { get; private set; }
        public Text DiamondText { get; private set; }
        public Text TotalXpText { get; private set; }
        private GameObject _claimBtn, _closeBtn;
        private ILocalizationService _locService;

        private void Awake()
        {
            if (NeedsSelfBuild())
            {
                BuildUI();
                ApplyBaseStyling();
            }
            else
            {
                BindReferencesFromChildren();
                ApplyBaseStyling();
            }
        }

        private bool NeedsSelfBuild()
        {
            if (transform.childCount == 0) return true;
            BindReferencesFromChildren();
            return ClaimButton == null || CloseButton == null;
        }

        public void BuildUI()
        {
            var overlay = GetComponent<Image>();
            if (overlay != null)
            {
                overlay.color = new Color(0, 0, 0, 0.75f);
                overlay.raycastTarget = true;
            }

            var card = GameUIResources.CreatePanel("Card", transform);
            GameUIResources.SetAnchors(card.GetComponent<RectTransform>(), 0.08f, 0.15f, 0.92f, 0.85f);
            card.GetComponent<Image>().color = GameUIResources.PanelColor;
            card.GetComponent<Image>().raycastTarget = true;

            var titleGo = GameUIResources.CreateText("CHEST REWARDS", card.transform, 34, TextAnchor.MiddleCenter, GameUIResources.AccentColor);
            TitleText = titleGo.GetComponent<Text>();
            TitleText.fontStyle = FontStyle.Bold;
            TitleText.name = "Title";
            GameUIResources.SetAnchors(titleGo.GetComponent<RectTransform>(), 0.05f, 0.80f, 0.95f, 0.92f);

            // Bronze
            var bronzeGo = GameUIResources.CreateText("Bronze: x0  (+0 XP)", card.transform, 20, TextAnchor.MiddleLeft, new Color(0.8f, 0.5f, 0.2f));
            BronzeText = bronzeGo.GetComponent<Text>();
            BronzeText.name = "Bronze";
            GameUIResources.SetAnchors(bronzeGo.GetComponent<RectTransform>(), 0.08f, 0.62f, 0.92f, 0.70f);

            // Silver
            var silverGo = GameUIResources.CreateText("Silver: x0  (+0 XP)", card.transform, 20, TextAnchor.MiddleLeft, new Color(0.75f, 0.75f, 0.80f));
            SilverText = silverGo.GetComponent<Text>();
            SilverText.name = "Silver";
            GameUIResources.SetAnchors(silverGo.GetComponent<RectTransform>(), 0.08f, 0.52f, 0.92f, 0.60f);

            // Gold
            var goldGo = GameUIResources.CreateText("Gold: x0  (+0 XP)", card.transform, 20, TextAnchor.MiddleLeft, new Color(1f, 0.84f, 0f));
            GoldText = goldGo.GetComponent<Text>();
            GoldText.name = "Gold";
            GameUIResources.SetAnchors(goldGo.GetComponent<RectTransform>(), 0.08f, 0.42f, 0.92f, 0.50f);

            // Diamond
            var diamondGo = GameUIResources.CreateText("Diamond: x0  (+0 XP)", card.transform, 20, TextAnchor.MiddleLeft, new Color(0.5f, 0.8f, 1f));
            DiamondText = diamondGo.GetComponent<Text>();
            DiamondText.name = "Diamond";
            GameUIResources.SetAnchors(diamondGo.GetComponent<RectTransform>(), 0.08f, 0.32f, 0.92f, 0.40f);

            // Total XP
            var totalGo = GameUIResources.CreateText("Total XP: +0", card.transform, 22, TextAnchor.MiddleCenter, GameUIResources.AccentColor);
            TotalXpText = totalGo.GetComponent<Text>();
            TotalXpText.fontStyle = FontStyle.Bold;
            TotalXpText.name = "TotalXp";
            GameUIResources.SetAnchors(totalGo.GetComponent<RectTransform>(), 0.15f, 0.22f, 0.85f, 0.30f);

            // Claim All button
            var claimBtnGo = GameUIResources.CreateButton("CLAIM ALL", card.transform, 280, 60);
            ClaimButton = claimBtnGo.GetComponent<Button>();
            ClaimButton.name = "ClaimAll";
            _claimBtn = claimBtnGo;
            GameUIResources.SetAnchors(claimBtnGo.GetComponent<RectTransform>(), 0.15f, 0.10f, 0.85f, 0.20f);

            // Close button
            var closeBtnGo = GameUIResources.CreateButton("CLOSE", card.transform, 120, 38);
            CloseButton = closeBtnGo.GetComponent<Button>();
            CloseButton.name = "Close";
            _closeBtn = closeBtnGo;
            GameUIResources.ApplySecondaryStyle(closeBtnGo);
            var closeText = closeBtnGo.GetComponentInChildren<Text>();
            if (closeText != null) closeText.fontSize = 15;
            GameUIResources.SetAnchors(closeBtnGo.GetComponent<RectTransform>(), 0.40f, 0.02f, 0.60f, 0.09f);
        }

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
