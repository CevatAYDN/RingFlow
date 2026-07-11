using Nexus.Core;

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
        private void Awake()
        {
            if (transform.childCount == 0)
            {
                var titleGo = new UnityEngine.GameObject("Title", typeof(UnityEngine.RectTransform), typeof(UnityEngine.UI.Text));
                titleGo.transform.SetParent(transform, false);
                var text = titleGo.GetComponent<UnityEngine.UI.Text>();
                text.text = "HOW TO PLAY";
                text.fontSize = 42;
                text.alignment = UnityEngine.TextAnchor.MiddleCenter;
                text.color = UnityEngine.Color.white;
                text.font = UnityEngine.Resources.GetBuiltinResource<UnityEngine.Font>("LegacyRuntime.ttf");
                var rt = titleGo.GetComponent<UnityEngine.RectTransform>();
                rt.anchorMin = new UnityEngine.Vector2(0.1f, 0.4f);
                rt.anchorMax = new UnityEngine.Vector2(0.9f, 0.55f);
                rt.offsetMin = UnityEngine.Vector2.zero;
                rt.offsetMax = UnityEngine.Vector2.zero;

                var bodyGo = new UnityEngine.GameObject("Body", typeof(UnityEngine.RectTransform), typeof(UnityEngine.UI.Text));
                bodyGo.transform.SetParent(transform, false);
                var bodyText = bodyGo.GetComponent<UnityEngine.UI.Text>();
                bodyText.text = "Sort the rings by color to clear each pole!\n\nTap to select, tap again to move.";
                bodyText.fontSize = 20;
                bodyText.alignment = UnityEngine.TextAnchor.MiddleCenter;
                bodyText.color = new UnityEngine.Color(0.6f, 0.6f, 0.65f);
                bodyText.font = UnityEngine.Resources.GetBuiltinResource<UnityEngine.Font>("LegacyRuntime.ttf");
                var brt = bodyGo.GetComponent<UnityEngine.RectTransform>();
                brt.anchorMin = new UnityEngine.Vector2(0.1f, 0.15f);
                brt.anchorMax = new UnityEngine.Vector2(0.9f, 0.38f);
                brt.offsetMin = UnityEngine.Vector2.zero;
                brt.offsetMax = UnityEngine.Vector2.zero;
            }
        }
    }
}
