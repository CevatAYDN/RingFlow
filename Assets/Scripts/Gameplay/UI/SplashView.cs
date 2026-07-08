using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.UI;

namespace RingFlow.Gameplay.UI
{
    [Mediator(typeof(SplashMediator))]
    public class SplashView : View
    {
        public Text LogoText { get; private set; }
        public Text TaglineText { get; private set; }
        public Text ProgressText { get; private set; }

        protected virtual void Awake()
        {
            var overlay = GetComponent<Image>();
            overlay.color = new Color(0.06f, 0.07f, 0.10f, 1f);

            var logo = GameUIResources.CreateText("RING FLOW", transform, 64, TextAnchor.MiddleCenter, GameUIResources.AccentColor);
            logo.GetComponent<Text>().fontStyle = FontStyle.Bold;
            GameUIResources.SetAnchors(logo.GetComponent<RectTransform>(), 0.1f, 0.45f, 0.9f, 0.60f);
            LogoText = logo.GetComponent<Text>();

            var tag = GameUIResources.CreateText("Loading...", transform, 18, TextAnchor.MiddleCenter, GameUIResources.MutedText);
            GameUIResources.SetAnchors(tag.GetComponent<RectTransform>(), 0.1f, 0.38f, 0.9f, 0.44f);
            TaglineText = tag.GetComponent<Text>();

            var prog = GameUIResources.CreateText("", transform, 14, TextAnchor.MiddleCenter, GameUIResources.MutedText);
            GameUIResources.SetAnchors(prog.GetComponent<RectTransform>(), 0.1f, 0.30f, 0.9f, 0.36f);
            ProgressText = prog.GetComponent<Text>();
        }

        public void Localize(ILocalizationService loc)
        {
            GameUIResources.LocalizeText(LogoText.gameObject, "game_title", loc);
            GameUIResources.LocalizeText(TaglineText.gameObject, "game_loading", loc);
        }
    }
}
