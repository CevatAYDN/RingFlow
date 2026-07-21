using Nexus.Core;
using Nexus.Core.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RingFlow.Gameplay.UI
{
    [Mediator(typeof(SplashMediator))]
    public class SplashView : View, IAuthoredView
    {
        [SerializeField] private TextMeshProUGUI _logoText;
        public TextMeshProUGUI LogoText => _logoText;
        [SerializeField] private TextMeshProUGUI _taglineText;
        public TextMeshProUGUI TaglineText => _taglineText;
        [SerializeField] private TextMeshProUGUI _progressText;
        public TextMeshProUGUI ProgressText => _progressText;
        [SerializeField] private Image _progressBar;
        public Image ProgressBar => _progressBar;
        [SerializeField] private CanvasGroup _cardGroup;
        public CanvasGroup CardGroup => _cardGroup;

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
            if (_logoText == null || _taglineText == null || _progressText == null)
            {
                var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var txt in texts)
                {
                    if (txt.name.Contains("Logo")) _logoText = txt;
                    else if (txt.name.Contains("Tag")) _taglineText = txt;
                    else if (txt.name.Contains("Progress")) _progressText = txt;
                }
            }
            if (_progressBar == null)
            {
                var bars = GetComponentsInChildren<Image>(true);
                foreach (var img in bars)
                {
                    if (img.name.Contains("ProgressBarFill")) _progressBar = img;
                }
            }

            if (_cardGroup == null)
            {
                var group = GetComponent<CanvasGroup>();
                if (group == null) group = GetComponentInChildren<CanvasGroup>(true);
                _cardGroup = group;
            }
        }
    }
}
