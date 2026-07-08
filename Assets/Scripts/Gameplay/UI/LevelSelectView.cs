using System.Collections.Generic;
using Nexus.Core;
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

        protected virtual void Awake()
        {
            // Title
            var titleGo = GameUIResources.CreateText("SELECT LEVEL", transform, 36, TextAnchor.UpperCenter, GameUIResources.AccentColor);
            var titleRect = titleGo.GetComponent<RectTransform>();
            GameUIResources.SetAnchors(titleRect, 0.1f, 0.82f, 0.9f, 0.92f);

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
            var backBtn = GameUIResources.CreateButton("BACK", transform, 200, 50);
            var backRect = backBtn.GetComponent<RectTransform>();
            GameUIResources.SetAnchors(backRect, 0.38f, 0.06f, 0.62f, 0.12f);
            BackButton = backBtn.GetComponent<Button>();
        }
    }
}
