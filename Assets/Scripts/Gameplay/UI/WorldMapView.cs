using Nexus.Core;

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
        private void Awake()
        {
            if (transform.childCount == 0)
            {
                var titleGo = new UnityEngine.GameObject("Title", typeof(UnityEngine.RectTransform), typeof(UnityEngine.UI.Text));
                titleGo.transform.SetParent(transform, false);
                var text = titleGo.GetComponent<UnityEngine.UI.Text>();
                text.text = "WORLD MAP";
                text.fontSize = 48;
                text.alignment = UnityEngine.TextAnchor.MiddleCenter;
                text.color = UnityEngine.Color.white;
                text.font = UnityEngine.Resources.GetBuiltinResource<UnityEngine.Font>("LegacyRuntime.ttf");
                var rt = titleGo.GetComponent<UnityEngine.RectTransform>();
                rt.anchorMin = new UnityEngine.Vector2(0.1f, 0.4f);
                rt.anchorMax = new UnityEngine.Vector2(0.9f, 0.6f);
                rt.offsetMin = UnityEngine.Vector2.zero;
                rt.offsetMax = UnityEngine.Vector2.zero;

                var bodyGo = new UnityEngine.GameObject("Body", typeof(UnityEngine.RectTransform), typeof(UnityEngine.UI.Text));
                bodyGo.transform.SetParent(transform, false);
                var bodyText = bodyGo.GetComponent<UnityEngine.UI.Text>();
                bodyText.text = "World Map coming soon!";
                bodyText.fontSize = 22;
                bodyText.alignment = UnityEngine.TextAnchor.MiddleCenter;
                bodyText.color = new UnityEngine.Color(0.6f, 0.6f, 0.65f);
                bodyText.font = UnityEngine.Resources.GetBuiltinResource<UnityEngine.Font>("LegacyRuntime.ttf");
                var brt = bodyGo.GetComponent<UnityEngine.RectTransform>();
                brt.anchorMin = new UnityEngine.Vector2(0.15f, 0.25f);
                brt.anchorMax = new UnityEngine.Vector2(0.85f, 0.40f);
                brt.offsetMin = UnityEngine.Vector2.zero;
                brt.offsetMax = UnityEngine.Vector2.zero;
            }
        }
    }
}
