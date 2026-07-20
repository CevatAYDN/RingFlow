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
            BindReferencesFromChildren();
        }

        /// <summary>
        /// Satisfies IAuthoredView. UI hierarchy is now loaded from prefab;
        /// this method is kept for interface compliance (Editor UI Studio tooling).
        /// </summary>
        public void BuildUI() { }

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
