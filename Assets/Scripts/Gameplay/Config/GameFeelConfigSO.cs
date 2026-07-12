using UnityEngine;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    [CreateAssetMenu(fileName = "GameFeelConfig", menuName = "RingFlow/Game Feel Config", order = 51)]
    public class GameFeelConfigSO : ScriptableObject
    {


        [Header("Board")]
        [Tooltip("Pole spacing on X axis (units).")]
        public float PoleSpacing = 3.5f;
        [Tooltip("Pole Y position (height).")]
        public float PoleYPosition = 2.0f;
        [Tooltip("Pole visual scale.")]
        public Vector3 PoleScale = new(0.4f, 2.5f, 0.4f);
        [Tooltip("Collider width as fraction of pole spacing (0-1).")]
        public float PoleColliderWidthFraction = 0.85f;

        [Header("Rings")]
        [Tooltip("Ring scale when Torus prefab is available.")]
        public Vector3 RingScaleTorus = new(3.0f, 0.25f, 3.0f);
        [Tooltip("Ring scale fallback (cylinder).")]
        public Vector3 RingScaleFallback = new(3.0f, 0.12f, 3.0f);
        [Tooltip("Vertical offset of the first (bottom) ring from pole top.")]
        public float RingBaseYOffset = -0.9f;
        [Tooltip("Vertical stacking distance between rings.")]
        public float RingStackSpacing = 0.4f;
        [Tooltip("Y lift for the selected top ring.")]
        public float RingSelectionLift = 0.35f;

        [Header("Animation — Move")]
        [Tooltip("Base duration for ring move jump (seconds).")]
        public float MoveDuration = 0.3f;
        [Tooltip("Jump arc power.")]
        public float MoveJumpPower = 0.8f;
        [Tooltip("Scale-pop multiplier on ring placement (1 = off).")]
        public float RingPlacePulseScale = 1.25f;
        [Tooltip("Pulse return duration.")]
        public float RingPlacePulseDuration = 0.2f;

        [Header("Animation — Selection")]
        [Tooltip("Duration for selection highlight lift.")]
        public float SelectionLiftDuration = 0.15f;
        [Tooltip("Slow-mode speed multiplier.")]
        public float SlowModeMultiplier = 2.0f;

        [Header("Camera")]
        [Tooltip("Camera position (world).")]
        public Vector3 CameraPosition = new(0f, 8f, -12f);
        [Tooltip("Camera rotation (euler).")]
        public Vector3 CameraRotation = new(30f, 0f, 0f);
        [Tooltip("Base orthographic size for 4 poles.")]
        public float CameraBaseOrtho = 10f;
        [Tooltip("Max orthographic size for 10 poles.")]
        public float CameraMaxOrtho = 16f;
        [Tooltip("Reference pole count for base ortho.")]
        public int CameraBasePoles = 4;
        [Tooltip("Reference pole count for max ortho.")]
        public int CameraMaxPoles = 10;

        [Header("Camera Shake")]
        [Tooltip("Shake intensity on error.")]
        public float ShakeErrorIntensity = 0.12f;
        [Tooltip("Shake duration on error.")]
        public float ShakeErrorDuration = 0.2f;
        [Tooltip("Shake intensity on bomb explosion.")]
        public float ShakeExplosionIntensity = 0.25f;
        [Tooltip("Shake duration on bomb explosion.")]
        public float ShakeExplosionDuration = 0.35f;

        [Header("Pole Colors")]
        [Tooltip("Pole selection highlight color.")]
        public Color SelectedTint = new(0.30f, 0.85f, 1.0f, 1.0f);
        [Tooltip("Pole error flash color.")]
        public Color ErrorTint = new(1.0f, 0.30f, 0.30f, 1.0f);
        [Tooltip("Locked pole color.")]
        public Color LockedTint = Color.black;
        [Tooltip("Error flash duration.")]
        public float ErrorFlashDuration = 0.35f;
        [Tooltip("Open pole standard color.")]
        public Color PoleColorOpen = new Color(0.20f, 0.22f, 0.25f);
        [Tooltip("Locked pole standard color.")]
        public Color PoleColorLocked = new Color(0.12f, 0.12f, 0.14f);
        [Tooltip("Pole material metallic value.")]
        public float PoleMetallic = 0.8f;
        [Tooltip("Pole material smoothness value.")]
        public float PoleSmoothness = 0.8f;

        [Header("Ring Materials")]
        [Tooltip("Ring material metallic value.")]
        public float RingMetallic = 0.1f;
        [Tooltip("Ring material smoothness value.")]
        public float RingSmoothness = 0.85f;

        [Header("Floor/Ground Visuals")]
        [Tooltip("Custom mesh to use for the floor/ground (falls back to Plane if null).")]
        public Mesh FloorMesh;
        [Tooltip("Floor/ground material color.")]
        public Color FloorColor = new Color(0.88f, 0.92f, 0.97f);
        [Tooltip("Floor material metallic value.")]
        public float FloorMetallic = 0f;
        [Tooltip("Floor material smoothness value.")]
        public float FloorSmoothness = 0.1f;

        [Header("Model Meshes")]
        [Tooltip("Custom mesh to use for the rings (falls back to Procedural Torus if null).")]
        public Mesh RingMesh;
        [Tooltip("Custom mesh to use for the pole body (falls back to Cylinder if null).")]
        public Mesh PoleBodyMesh;
        [Tooltip("Custom mesh to use for the pole cap (falls back to Sphere if null).")]
        public Mesh PoleCapMesh;

        [Header("Selection Glow")]
        [Tooltip("Glow point light color.")]
        public Color SelectionGlowColor = new Color(1f, 0.85f, 0.5f);
        [Tooltip("Glow point light intensity.")]
        public float SelectionGlowIntensity = 2f;
        [Tooltip("Glow point light range.")]
        public float SelectionGlowRange = 2.5f;
        [Tooltip("Selected ring emission color.")]
        public Color SelectionEmissionColor = new Color(0.4f, 0.3f, 0.1f);

        [Header("Tutorial Visuals")]
        [Tooltip("Tutorial arrow/cone tint color.")]
        public Color TutorialArrowColor = new Color(1f, 0.85f, 0f, 1f);
        [Tooltip("Tutorial arrow/cone scale.")]
        public Vector3 TutorialArrowScale = new Vector3(0.5f, 0.5f, 0.5f);
        [Tooltip("Tutorial arrow/cone bobbing height.")]
        public float TutorialArrowBobHeight = 0.25f;
        [Tooltip("Tutorial arrow/cone bobbing speed.")]
        public float TutorialArrowBobSpeed = 0.45f;
        [Tooltip("Tutorial arrow/cone rotation speed.")]
        public float TutorialArrowRotationSpeed = 120f;

        [Header("Rainbow Cycle")]
        [Tooltip("Hue rotation speed multiplier.")]
        public float RainbowHueSpeed = 0.25f;
        [Tooltip("Rainbow saturation.")]
        public float RainbowSaturation = 0.8f;
        [Tooltip("Rainbow value/brightness.")]
        public float RainbowValue = 0.9f;

        [Header("VFX")]
        [Tooltip("RingPop particle count.")]
        public int RingPopCount = 12;
        [Tooltip("RingPop burst duration.")]
        public float RingPopDuration = 0.5f;
        [Tooltip("RingPop auto-despawn delay.")]
        public float RingPopDespawnDelay = 0.55f;

        [Header("Merge Effect VFX")]
        [Tooltip("Merge burst particle count.")]
        public int MergeBurstCount = 16;
        [Tooltip("Merge burst duration.")]
        public float MergeBurstDuration = 0.4f;
        [Tooltip("Merge glow orb scale.")]
        public float MergeGlowOrbScale = 0.5f;

        [Header("Confetti VFX")]
        [Tooltip("Confetti piece count.")]
        public int ConfettiCount = 40;
        [Tooltip("Confetti fall duration range.")]
        public Vector2 ConfettiFallDuration = new(1.8f, 3.0f);
        [Tooltip("Confetti despawn delay.")]
        public float ConfettiDespawnDelay = 3.1f;

        [Header("Pole Completion Feedback")]
        [Tooltip("Duration of the pole success flash.")]
        public float PoleSuccessFlashDuration = 0.3f;
        [Tooltip("Pole success flash color (colorblind-safe: gold, not green).")]
        public Color PoleSuccessFlashColor = new Color(1f, 0.82f, 0f);
        [Tooltip("Bloom intensity multiplier on pole complete (1 = no change).")]
        public float BloomIntensityMultiplier = 2.5f;
        [Tooltip("Bloom pulse duration on pole complete.")]
        public float BloomPulseDuration = 0.6f;
        [Tooltip("Bloom intensity multiplier for final pole.")]
        public float FinalBloomIntensityMultiplier = 5f;
        [Tooltip("Bloom pulse duration for final pole.")]
        public float FinalBloomPulseDuration = 1.2f;
        [Tooltip("Number of completed poles threshold for medium-tier celebration.")]
        public int MediumTierThreshold = 3;
        [Tooltip("Camera micro-shake intensity on pole complete.")]
        public float CompleteShakeIntensity = 0.06f;
        [Tooltip("Camera micro-shake duration on pole complete.")]
        public float CompleteShakeDuration = 0.15f;

        [Header("Pool Sizes")]
        public int RingPoolSize = 100;
        public int RingPopPoolSize = 50;
        public int ConfettiPoolSize = 30;
        public int MergeEffectPoolSize = 20;

        [Header("Ring Mesh Compensation")]
        [Tooltip("World-space target width for ring visuals.")]
        public float RingTargetWidth = 1.5f;
        [Tooltip("World-space target height for ring visuals.")]
        public float RingTargetHeight = 0.44f;
        [Tooltip("Actual mesh height of the ring prefab (used for scale compensation).")]
        public float RingMeshHeight = 0.26f;

        [Header("Board Layout")]
        [Tooltip("Y position of the floor plane.")]
        public float FloorYPosition = -0.51f;
        [Tooltip("Scale of the floor plane.")]
        public Vector3 FloorScale = new(10f, 1f, 10f);
        [Tooltip("Capacity value that maps to full pole scale (for pole Y scaling).")]
        public int PoleScaleFullCapacity = 4;
        [Tooltip("Default capacity fallback when pole data has none.")]
        public int DefaultPoleCapacity = 4;

        [Header("Tutorial Arrow")]
        [Tooltip("Forward offset from pole center for tutorial arrow.")]
        public float TutorialForwardOffset = 0.35f;
        [Tooltip("Tutorial label Y offset above arrow base.")]
        public float TutorialLabelYOffset = 0.9f;
        [Tooltip("Tutorial label canvas scale.")]
        public Vector3 TutorialLabelCanvasScale = new(0.22f, 0.22f, 0.22f);
        [Tooltip("Tutorial label canvas size.")]
        public Vector2 TutorialLabelCanvasSize = new(2.2f, 0.7f);
        [Tooltip("Tutorial label font size.")]
        public int TutorialLabelFontSize = 36;
        [Tooltip("Tutorial label panel padding (xMin, yMin, xMax, yMax correction).")]
        public Vector2 TutorialPanelPaddingMin = new(8f, 6f);
        public Vector2 TutorialPanelPaddingMax = new(-8f, -6f);
        [Tooltip("Tutorial label outline color.")]
        public Color TutorialLabelOutlineColor = new(0f, 0f, 0f, 0.85f);
        [Tooltip("Tutorial label outline effect distance.")]
        public Vector2 TutorialLabelOutlineDistance = new(1.5f, -1.5f);
        [Tooltip("Tutorial label panel background color.")]
        public Color TutorialPanelColor = new(0f, 0f, 0f, 0.55f);
        [Tooltip("Final pole celebration ring bounce height.")]
        public float FinalPoleBounceHeight = 0.5f;
        [Tooltip("Normal pole celebration ring bounce height.")]
        public float PoleBounceHeight = 0.35f;
        [Tooltip("Tier-2 final pole confetti burst count.")]
        public int FinalPoleConfettiCount = 24;
        [Tooltip("Tier-1 medium confetti burst count.")]
        public int MediumConfettiCount = 8;

        [Header("Procedural Torus Mesh")]
        [Tooltip("Major radius of procedural torus mesh.")]
        public float TorusMajorRadius = 0.37f;
        [Tooltip("Minor radius of procedural torus mesh.")]
        public float TorusMinorRadius = 0.13f;
        [Tooltip("Radial segments of procedural torus mesh.")]
        public int TorusRadialSegments = 32;
        [Tooltip("Tubular segments of procedural torus mesh.")]
        public int TorusTubularSegments = 24;

        [Header("DOTween Capacity")]
        [Tooltip("DOTween tween pool capacity (pre-allocated at startup).")]
        public int DoTweenTweensCapacity = 1500;
        [Tooltip("DOTween sequence pool capacity (pre-allocated at startup).")]
        public int DoTweenSequencesCapacity = 200;

        [Header("UI")]
        [Tooltip("Duration for screen fade in/out transitions.")]
        public float UiScreenFadeDuration = 0.3f;

        [Header("Audio Volume Limits")]
        [Tooltip("Default SFX volume multiplier.")]
        public float DefaultSfxVolume = 1.0f;
        [Tooltip("Default SFX pitch variation min.")]
        public float DefaultSfxPitchMin = 0.92f;
        [Tooltip("Default SFX pitch variation max.")]
        public float DefaultSfxPitchMax = 1.08f;
    }
}
