using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.UI;

namespace RingFlow.Gameplay.UI
{
    /// <summary>
    /// Placeholder view for Onboarding screen (GDD §14 — first-launch tutorial overlay).
    /// When implemented, this will walk new players through core mechanics.
    /// Currently auto-advances to LevelSelect.
    /// </summary>
    [Mediator(typeof(OnboardingMediator))]
    public class OnboardingView : View
    {
        private Text _titleText;
        private Text _bodyText;

        private void Awake()
        {
            if (transform.childCount == 0)
            {
                BuildUI();
            }
            else
            {
                BindReferencesFromChildren();
            }
        }

        public void Localize(ILocalizationService loc)
        {
            if (loc == null) return;
            if (_titleText != null) GameUIResources.LocalizeText(_titleText.gameObject, "onboarding_title", loc);
            if (_bodyText != null) GameUIResources.LocalizeText(_bodyText.gameObject, "onboarding_body", loc);
        }

        private void BuildUI()
        {
            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(transform, false);
            _titleText = titleGo.GetComponent<Text>();
            _titleText.text = "HOW TO PLAY";
            _titleText.fontSize = 42;
            _titleText.alignment = TextAnchor.MiddleCenter;
            _titleText.color = Color.white;
            _titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var rt = titleGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 0.4f);
            rt.anchorMax = new Vector2(0.9f, 0.55f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bodyGo = new GameObject("Body", typeof(RectTransform), typeof(Text));
            bodyGo.transform.SetParent(transform, false);
            _bodyText = bodyGo.GetComponent<Text>();
            _bodyText.text = "Sort the rings by color to clear each pole!\n\nTap to select, tap again to move.";
            _bodyText.fontSize = 20;
            _bodyText.alignment = TextAnchor.MiddleCenter;
            _bodyText.color = new Color(0.6f, 0.6f, 0.65f);
            _bodyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var brt = bodyGo.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.1f, 0.15f);
            brt.anchorMax = new Vector2(0.9f, 0.38f);
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;
        }

        private void BindReferencesFromChildren()
        {
            var texts = GetComponentsInChildren<Text>(true);
            foreach (var txt in texts)
            {
                var upper = txt.name.ToUpperInvariant();
                if (upper.Contains("TITLE")) _titleText = txt;
                else if (upper.Contains("BODY")) _bodyText = txt;
            }
        }
    }
}
