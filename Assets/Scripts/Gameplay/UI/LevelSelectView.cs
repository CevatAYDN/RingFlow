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
            TitleText = GetComponentInChildren<Text>(true);
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                if (button.name.Contains("BACK"))
                {
                    _backBtn = button.gameObject;
                    BackButton = button;
                }
                else if (button.name.Contains("Level"))
                {
                    LevelButtons.Add(button);
                }
            }
        }

        public void Localize(ILocalizationService loc)
        {
            if (TitleText != null) GameUIResources.LocalizeText(TitleText.gameObject, "menu_select_level", loc);
            GameUIResources.LocalizeButtonText(_backBtn, "menu_back", loc);
        }
    }
}
