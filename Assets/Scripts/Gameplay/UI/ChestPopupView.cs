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

        private void Awake()
        {
            if (NeedsSelfBuild())
            {
                BuildUI();
            }
            else
            {
                BindReferencesFromChildren();
            }
        }

        private bool NeedsSelfBuild()
        {
            if (transform.childCount == 0) return true;
            BindReferencesFromChildren();
            return ClaimButton == null || CloseButton == null;
        }

        private void BuildUI()
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

        public void ShowChestCounts(int bronze, int silver, int gold, int diamond, 
            int xpBronze = 100, int xpSilver = 250, int xpGold = 500, int xpDiamond = 1000)
        {
            if (BronzeText != null) BronzeText.text = $"Bronze: x{bronze}  (+{bronze * xpBronze} XP)";
            if (SilverText != null) SilverText.text = $"Silver: x{silver}  (+{silver * xpSilver} XP)";
            if (GoldText != null) GoldText.text = $"Gold: x{gold}  (+{gold * xpGold} XP)";
            if (DiamondText != null) DiamondText.text = $"Diamond: x{diamond}  (+{diamond * xpDiamond} XP)";

            int totalXp = bronze * xpBronze + silver * xpSilver + gold * xpGold + diamond * xpDiamond;
            if (TotalXpText != null) TotalXpText.text = $"Total XP: +{totalXp}";

            bool hasAny = (bronze + silver + gold + diamond) > 0;
            if (ClaimButton != null) ClaimButton.interactable = hasAny;
        }
    }
}
