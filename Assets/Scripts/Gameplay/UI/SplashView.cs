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

        private void Awake()
        {
            Debug.Log("[SplashView] Awake called");
            BindReferencesFromChildren();
        }

        protected override void OnEnable()
        {
            NexusLog.Info("SplashView", nameof(OnEnable), "", "OnEnable called");
            base.OnEnable();
        }

        public void Localize(ILocalizationService loc)
        {
            if (LogoText != null) GameUIResources.LocalizeText(LogoText.gameObject, "game_title", loc);
            if (TaglineText != null) GameUIResources.LocalizeText(TaglineText.gameObject, "game_loading", loc);
        }

        private void BindReferencesFromChildren()
        {
            var texts = GetComponentsInChildren<Text>(true);
            foreach (var txt in texts)
            {
                if (txt.fontSize == 64 || txt.name.Contains("Logo")) LogoText = txt;
                else if (txt.fontSize == 18 || txt.name.Contains("Tag") || txt.name.Contains("Loading")) TaglineText = txt;
                else if (txt.fontSize == 14 || txt.name.Contains("Progress")) ProgressText = txt;
            }
        }
    }
}
