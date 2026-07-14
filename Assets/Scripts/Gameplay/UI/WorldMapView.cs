using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.UI;

namespace RingFlow.Gameplay.UI
{
    /// <summary>
    /// Placeholder view for WorldMap screen. Currently stubbed — the GDD specifies
    /// a world-overview map but the feature is scheduled for a future milestone.
    /// When implemented, replace this with a full authored prefab.
    /// </summary>
    [Mediator(typeof(WorldMapMediator))]
    public class WorldMapView : View
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
            if (_titleText != null) GameUIResources.LocalizeText(_titleText.gameObject, "worldmap_title", loc);
            if (_bodyText != null) GameUIResources.LocalizeText(_bodyText.gameObject, "worldmap_body", loc);
        }

        private void BuildUI()
        {
            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(transform, false);
            _titleText = titleGo.GetComponent<Text>();
            _titleText.text = "WORLD MAP";
            _titleText.fontSize = 48;
            _titleText.alignment = TextAnchor.MiddleCenter;
            _titleText.color = Color.white;
            _titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var rt = titleGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 0.4f);
            rt.anchorMax = new Vector2(0.9f, 0.6f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bodyGo = new GameObject("Body", typeof(RectTransform), typeof(Text));
            bodyGo.transform.SetParent(transform, false);
            _bodyText = bodyGo.GetComponent<Text>();
            _bodyText.text = "World Map coming soon!";
            _bodyText.fontSize = 22;
            _bodyText.alignment = TextAnchor.MiddleCenter;
            _bodyText.color = new Color(0.6f, 0.6f, 0.65f);
            _bodyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var brt = bodyGo.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.15f, 0.25f);
            brt.anchorMax = new Vector2(0.85f, 0.40f);
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
