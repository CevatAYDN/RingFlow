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
            var card = GameUIResources.CreatePanel("Card", transform);
            GameUIResources.SetAnchors(card.GetComponent<RectTransform>(), 0.08f, 0.16f, 0.92f, 0.84f);
            card.GetComponent<Image>().color = GameUIResources.PanelColor;
            card.GetComponent<Image>().raycastTarget = true;

            var titleGo = GameUIResources.CreateText("HOW TO PLAY", card.transform, 40, TextAnchor.MiddleCenter, GameUIResources.TextColor);
            _titleText = titleGo.GetComponent<Text>();
            _titleText.fontStyle = FontStyle.Bold;
            GameUIResources.SetAnchors(titleGo.GetComponent<RectTransform>(), 0.08f, 0.72f, 0.92f, 0.86f);

            var bodyGo = GameUIResources.CreateText("Sort the rings by color to clear each pole. Tap a ring, then tap a pole to move it.", card.transform, 21, TextAnchor.MiddleCenter, GameUIResources.MutedText);
            _bodyText = bodyGo.GetComponent<Text>();
            GameUIResources.SetAnchors(bodyGo.GetComponent<RectTransform>(), 0.10f, 0.18f, 0.90f, 0.54f);
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
