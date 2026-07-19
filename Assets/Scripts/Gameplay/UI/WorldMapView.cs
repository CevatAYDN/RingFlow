using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.UI;

namespace RingFlow.Gameplay.UI
{
    /// <summary>
    /// Base world map view used for world navigation and progression overview.
    /// The screen intentionally stays lightweight and readable for mobile.
    /// </summary>
    [Mediator(typeof(WorldMapMediator))]
    public class WorldMapView : View, IAuthoredView
    {
        public Button BackButton { get; private set; }
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
            if (_titleText != null) GameUIResources.LocalizeText(_titleText.gameObject, "worldmap_title", loc);
            if (_bodyText != null) GameUIResources.LocalizeText(_bodyText.gameObject, "worldmap_body", loc);
        }

        public void BuildUI()
        {
            var card = GameUIResources.CreatePanel("Card", transform);
            GameUIResources.SetAnchors(card.GetComponent<RectTransform>(), 0.08f, 0.18f, 0.92f, 0.82f);
            card.GetComponent<Image>().color = GameUIResources.PanelColor;
            card.GetComponent<Image>().raycastTarget = true;

            var titleGo = GameUIResources.CreateText("WORLD MAP", card.transform, 40, TextAnchor.MiddleCenter, GameUIResources.TextColor);
            _titleText = titleGo.GetComponent<Text>();
            _titleText.fontStyle = FontStyle.Bold;
            GameUIResources.SetAnchors(titleGo.GetComponent<RectTransform>(), 0.08f, 0.68f, 0.92f, 0.84f);

            var bodyGo = GameUIResources.CreateText("Explore the kingdoms ahead.", card.transform, 22, TextAnchor.MiddleCenter, GameUIResources.MutedText);
            _bodyText = bodyGo.GetComponent<Text>();
            GameUIResources.SetAnchors(bodyGo.GetComponent<RectTransform>(), 0.10f, 0.30f, 0.90f, 0.52f);

            // Back button
            var backBtnGo = GameUIResources.CreateIconButton("", transform, 36f);
            backBtnGo.name = "Btn_BACK";
            BackButton = backBtnGo.GetComponent<Button>();
            GameUIResources.SetAnchors(backBtnGo.GetComponent<RectTransform>(), 0.04f, 0.90f, 0.15f, 0.98f);
            backBtnGo.GetComponent<Image>().color = GameUIResources.SurfaceColor;
            var backIconGo = new GameObject("IconImage", typeof(RectTransform), typeof(Image));
            backIconGo.transform.SetParent(backBtnGo.transform, false);
            var backIcon = backIconGo.GetComponent<Image>();
            backIcon.sprite = GameUIResources.GetSprite("back_arrow");
            backIcon.preserveAspect = true;
            GameUIResources.SetAnchors(backIconGo.GetComponent<RectTransform>(), 0.15f, 0.15f, 0.85f, 0.85f);
        }

        private void BindReferencesFromChildren()
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                GameUIResources.AddButtonEffects(btn);
                var upperName = btn.name.ToUpperInvariant();
                if (upperName.Contains("BACK")) BackButton = btn;
            }
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
