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
            BindReferencesFromChildren();
        }

        public void Localize(ILocalizationService loc)
        {
            if (loc == null) return;
            if (_titleText != null) GameUIResources.LocalizeText(_titleText.gameObject, "worldmap_title", loc);
            if (_bodyText != null) GameUIResources.LocalizeText(_bodyText.gameObject, "worldmap_body", loc);
        }

        /// <summary>
        /// Satisfies IAuthoredView. UI hierarchy is now loaded from prefab;
        /// this method is kept for interface compliance (Editor UI Studio tooling).
        /// </summary>
        public void BuildUI() { }

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
