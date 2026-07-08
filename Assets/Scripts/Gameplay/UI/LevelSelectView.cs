using System.Collections.Generic;
using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.UI;

namespace RingFlow.Gameplay.UI
{
    /// <summary>
    /// Level select screen. Renders 8 level buttons (within a single world) plus a Back button.
    /// The Mediator updates the unlock state (text, color, interactable) when it binds,
    /// using <see cref="IProgressionService.MaxUnlockedLevel"/>.
    /// </summary>
    [Mediator(typeof(LevelSelectMediator))]
    public class LevelSelectView : View
    {
        public const int ButtonsPerWorld = 8;

        public List<Button> LevelButtons { get; } = new();
        public Button BackButton { get; private set; }
        public Text TitleText { get; private set; }
        private GameObject _backBtn, _levelBtns;

        private void Awake()
        {
            // Title
            var titleGo = GameUIResources.CreateText("SELECT LEVEL", transform, 36, TextAnchor.UpperCenter, GameUIResources.AccentColor);
            TitleText = titleGo.GetComponent<Text>();
            GameUIResources.SetAnchors(titleGo.GetComponent<RectTransform>(), 0.1f, 0.82f, 0.9f, 0.92f);

            // Level grid
            float startY = 0.72f;
            float spacing = 0.08f;
            for (int i = 1; i <= ButtonsPerWorld; i++)
            {
                var btn = GameUIResources.CreateButton($"Level {i}", transform, 200, 50);
                var rect = btn.GetComponent<RectTransform>();
                float yPos = startY - (i - 1) * spacing;
                GameUIResources.SetAnchors(rect, 0.38f, yPos - 0.03f, 0.62f, yPos + 0.03f);
                var button = btn.GetComponent<Button>();
                LevelButtons.Add(button);
            }

            // Back button
            _backBtn = GameUIResources.CreateButton("BACK", transform, 200, 50);
            GameUIResources.SetAnchors(_backBtn.GetComponent<RectTransform>(), 0.38f, 0.06f, 0.62f, 0.12f);
            BackButton = _backBtn.GetComponent<Button>();
        }

        public void Localize(ILocalizationService loc)
        {
            GameUIResources.LocalizeText(TitleText.gameObject, "menu_select_level", loc);
            GameUIResources.LocalizeButtonText(_backBtn, "menu_back", loc);
        }
    }
}
