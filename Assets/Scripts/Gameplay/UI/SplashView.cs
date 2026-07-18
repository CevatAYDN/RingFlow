using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.UI;

namespace RingFlow.Gameplay.UI
{
    [Mediator(typeof(SplashMediator))]
    public class SplashView : View, IAuthoredView
    {
        public Text LogoText { get; private set; }
        public Text TaglineText { get; private set; }
        public Text ProgressText { get; private set; }
        public Image ProgressBar { get; private set; }
        public CanvasGroup CardGroup { get; private set; }

        private void Awake()
        {
            if (transform.childCount == 0 || NeedsSelfBuild())
                BuildUI();
            else
                BindReferencesFromChildren();
        }

        private bool NeedsSelfBuild()
        {
            BindReferencesFromChildren();
            return LogoText == null;
        }

        public void BuildUI()
        {
            // Backdrop
            var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
            backdrop.transform.SetParent(transform, false);
            var backdropRect = backdrop.GetComponent<RectTransform>();
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = Vector2.zero;
            backdropRect.offsetMax = Vector2.zero;
            backdrop.GetComponent<Image>().color = GameUIResources.BgDark;
            backdrop.GetComponent<Image>().raycastTarget = false;

            // Card container
            var cardGo = GameUIResources.CreatePanel("Card", transform);
            cardGo.GetComponent<Image>().color = Color.clear;
            GameUIResources.SetAnchors(cardGo.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f);
            CardGroup = cardGo.GetComponent<CanvasGroup>();

            // Logo text - large centered
            var logoGo = GameUIResources.CreateDisplayText("RING FLOW", cardGo.transform, 72, GameUIResources.TextOnDark);
            logoGo.name = "LogoText";
            var logoText = logoGo.GetComponent<Text>();
            logoText.fontStyle = FontStyle.Bold;
            LogoText = logoText;
            GameUIResources.SetAnchors(logoGo.GetComponent<RectTransform>(), 0.12f, 0.62f, 0.88f, 0.78f);

            // Tagline
            var tagGo = GameUIResources.CreateText("", cardGo.transform, 18, TextAnchor.MiddleCenter, GameUIResources.MutedTextDark);
            tagGo.name = "TaglineText";
            TaglineText = tagGo.GetComponent<Text>();
            GameUIResources.SetAnchors(tagGo.GetComponent<RectTransform>(), 0.15f, 0.54f, 0.85f, 0.60f);

            // Loading bar background
            var barBg = new GameObject("ProgressBarBg", typeof(RectTransform), typeof(Image));
            barBg.transform.SetParent(cardGo.transform, false);
            var barBgRect = barBg.GetComponent<RectTransform>();
            barBg.GetComponent<Image>().color = new Color(0.20f, 0.22f, 0.28f);
            GameUIResources.SetAnchors(barBgRect, 0.30f, 0.32f, 0.70f, 0.36f);
            barBg.GetComponent<Image>().sprite = GameUIResources.GetRoundedSprite();
            barBg.GetComponent<Image>().type = Image.Type.Sliced;

            // Loading bar fill
            var barFill = new GameObject("ProgressBarFill", typeof(RectTransform), typeof(Image));
            barFill.transform.SetParent(barBg.transform, false);
            var barFillRect = barFill.GetComponent<RectTransform>();
            barFillRect.anchorMin = Vector2.zero;
            barFillRect.anchorMax = new Vector2(0f, 1f);
            barFillRect.offsetMin = Vector2.zero;
            barFillRect.offsetMax = Vector2.zero;
            barFill.GetComponent<Image>().color = GameUIResources.AccentColor;
            barFill.GetComponent<Image>().sprite = GameUIResources.GetRoundedSprite();
            barFill.GetComponent<Image>().type = Image.Type.Sliced;
            ProgressBar = barFill.GetComponent<Image>();

            // Loading text
            var progGo = GameUIResources.CreateText("", cardGo.transform, 14, TextAnchor.MiddleCenter, GameUIResources.MutedTextDark);
            progGo.name = "ProgressText";
            ProgressText = progGo.GetComponent<Text>();
            GameUIResources.SetAnchors(progGo.GetComponent<RectTransform>(), 0.20f, 0.26f, 0.80f, 0.30f);

            // Version label
            var verGo = GameUIResources.CreateText("v1.0", cardGo.transform, 12, TextAnchor.LowerCenter, GameUIResources.MutedTextDark);
            verGo.name = "VersionLabel";
            GameUIResources.SetAnchors(verGo.GetComponent<RectTransform>(), 0.30f, 0.04f, 0.70f, 0.08f);
        }

        public void Localize(ILocalizationService loc)
        {
            if (loc == null) return;
            if (LogoText != null) GameUIResources.LocalizeText(LogoText.gameObject, "game_title", loc);
            if (TaglineText != null) GameUIResources.LocalizeText(TaglineText.gameObject, "game_tagline", loc);
        }

        /// <summary>Update loading progress (0..1).</summary>
        public void SetProgress(float progress)
        {
            if (ProgressBar != null)
            {
                var anchoredMax = ProgressBar.GetComponent<RectTransform>().anchorMax;
                anchoredMax.x = Mathf.Clamp01(progress);
                ProgressBar.GetComponent<RectTransform>().anchorMax = anchoredMax;
            }
            if (ProgressText != null)
                ProgressText.text = $"{(int)(progress * 100)}%";
        }

        private void BindReferencesFromChildren()
        {
            var texts = GetComponentsInChildren<Text>(true);
            foreach (var txt in texts)
            {
                if (txt.name.Contains("Logo")) LogoText = txt;
                else if (txt.name.Contains("Tag")) TaglineText = txt;
                else if (txt.name.Contains("Progress")) ProgressText = txt;
            }
            var bars = GetComponentsInChildren<Image>(true);
            foreach (var img in bars)
            {
                if (img.name.Contains("ProgressBarFill")) ProgressBar = img;
            }
        }
    }
}
