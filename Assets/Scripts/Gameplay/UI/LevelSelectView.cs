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
        public List<Button> LevelButtons { get; } = new();
        public Button BackButton { get; private set; }
        public Text TitleText { get; private set; }
        private GameObject _backBtn, _levelBtns;

        private void Awake()
        {
            TitleText = GetComponentInChildren<Text>(true);
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                if (button.name.ToUpperInvariant().Contains("BACK"))
                {
                    _backBtn = button.gameObject;
                    BackButton = button;
                }
                else if (button.name.ToUpperInvariant().Contains("LEVEL"))
                {
                    LevelButtons.Add(button);
                }
            }

            ApplyBaseStyling();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            NexusLog.Info("LevelSelectView", nameof(Awake), "",
                $"LevelSelectView bound. LevelButtons={LevelButtons.Count}, BackButton={BackButton != null}, TitleText={TitleText != null}.");
            if (BackButton == null)
                NexusLog.Warn("LevelSelectView", nameof(Awake), "",
                    "BackButton not found — player cannot navigate back from level select.");
            if (LevelButtons.Count == 0)
                NexusLog.Warn("LevelSelectView", nameof(Awake), "",
                    "No level buttons found — level select screen will be empty.");
#endif
        }

        private void ApplyBaseStyling()
        {
            if (_backBtn != null) GameUIResources.ApplySecondaryStyle(_backBtn);
        }

        public void Localize(ILocalizationService loc)
        {
            if (loc == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                NexusLog.Warn("LevelSelectView", nameof(Localize), "", "Localize called with null ILocalizationService.");
#endif
                return;
            }
            if (TitleText != null) GameUIResources.LocalizeText(TitleText.gameObject, "menu_select_level", loc);
            GameUIResources.LocalizeButtonText(_backBtn, "menu_back", loc);
        }
    }
}
