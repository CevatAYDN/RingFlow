using Nexus.Core;
using UnityEngine;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// Aşama 1 — BoardView'dan ayrıştırılan özel ring overlay renderer'ı.
    ///
    /// Sorumluluk: Özel ring tiplerinin (Mystery, Frozen, Bomb vb.) üzerinde
    /// gösterilecek ASCII metin overlay'lerini oluşturur ve RainbowCycle
    /// bileşenini Rainbow ring'lere ekler.
    ///
    /// Bu sınıf:
    /// • Herhangi bir MonoBehaviour olay döngüsüne bağımlı değildir (pure POCO).
    /// • GDD § V1: Emoji yerine ASCII sembolleri kullanır (tüm cihazlarda çalışır).
    /// • BoardView'daki AddSpecialOverlay çağrısının tam karşılığıdır.
    /// </summary>
    public sealed class SpecialOverlayRenderer
    {
        // ── DI dependencies ─────────────────────────────────────────────
        [Inject] private GameFeelConfigSO _feelConfig;

        /// <summary>
        /// Adds a special overlay and/or RainbowCycle component to the given
        /// ring GameObject based on the ring's special type.
        ///
        /// For non-Standard ring types:
        ///   • Rainbow rings get a RainbowCycle child component.
        ///   • All others get an ASCII TextMesh overlay (GDD § V1).
        ///   • Bomb rings display their countdown value.
        ///
        /// Safe to call multiple times; RecycleRing in BoardView handles cleanup.
        /// </summary>
        public void AddSpecialOverlay(GameObject ringObj, RingData ringData)
        {
            if (ringData.Type == RingType.Standard) return;

            // ── RainbowCycle (always attached regardless of text overlay) ──
            if (ringData.Type == RingType.Rainbow)
            {
                // Only create if not already present
                if (ringObj.transform.Find("RainbowCycle") == null)
                {
                    var cycle = new GameObject("RainbowCycle");
                    cycle.transform.SetParent(ringObj.transform, false);
                    cycle.AddComponent<RainbowCycle>().Initialize(_feelConfig);
                }
            }

            // ── ASCII TextMesh overlay ──
            var (text, color) = ringData.Type switch
            {
                RingType.Mystery => ("?", Color.yellow),
                RingType.Frozen => ("*", Color.cyan),
                RingType.Locked => ("L", new Color(1f, 0.84f, 0f)),
                RingType.Key => ("K", new Color(1f, 0.84f, 0f)),
                RingType.Stone => ("S", Color.gray),
                RingType.Glass => ("G", new Color(1f, 1f, 1f, 0.5f)),
                RingType.Bomb => (ringData.AdditionalData.ToString(), Color.red),
                RingType.Chain => ("C", Color.white),
                RingType.Magnet => ("M", Color.magenta),
                RingType.Paint => ("P", Color.green),
                _ => (string.Empty, Color.white)
            };

            if (string.IsNullOrEmpty(text)) return;

            // Find or create the overlay TextMesh child
            var existing = ringObj.transform.Find("SpecialOverlay");
            GameObject overlayGo;
            TextMesh textMesh;

            if (existing != null)
            {
                overlayGo = existing.gameObject;
                textMesh = overlayGo.GetComponent<TextMesh>();
            }
            else
            {
                overlayGo = new GameObject("SpecialOverlay");
                overlayGo.transform.SetParent(ringObj.transform, false);

                textMesh = overlayGo.AddComponent<TextMesh>();
                textMesh.fontSize = 72;
                textMesh.anchor = TextAnchor.MiddleCenter;
                textMesh.alignment = TextAlignment.Center;
                textMesh.fontStyle = FontStyle.Bold;

                // Position overlay above the ring surface and tilt it to face the camera
                overlayGo.transform.localPosition = new Vector3(0f, 0.22f, 0f);
                float tilt = _feelConfig != null ? _feelConfig.CameraRotation.x : 30f;
                overlayGo.transform.localRotation = Quaternion.Euler(tilt, 0f, 0f);
                overlayGo.transform.localScale = new Vector3(0.06f, 0.06f, 0.06f);
            }

            textMesh.text = text;
            textMesh.color = color;
            overlayGo.SetActive(true);

            // Share the font's material across all overlays so they batch.
            var mr = overlayGo.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
        }
    }
}
