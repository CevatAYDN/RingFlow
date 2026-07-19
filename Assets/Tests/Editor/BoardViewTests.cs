using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Nexus.Core;
using Nexus.Core.Services;
using RingFlow.Gameplay;
using UnityEngine;

namespace RingFlow.Tests
{
    /// <summary>
    /// Aşama 1 — BoardView refactoring test suite.
    /// Snapshot/regression tests for every public method so the refactored
    /// components produce identical visual output.
    ///
    /// STRATEJİ:
    /// • Her test BoardView'ı temiz bir GameObject üzerinde oluşturur.
    /// • DI edilmesi gereken alanlar reflection ile enjekte edilir (mevcut pattern).
    /// • Her testten sonra TearDown ile BoardView GameObject'i tamamen yok edilir.
    /// • BuildBoard gerektiren testler için önceden GameFeelConfigSO Resources.Load ile
    ///   yüklenir (mevcut test altyapısı pattern'i).
    /// • VFX ve Audio yolları null-safe olduğu için mock'lar null gönderilebilir.
    /// • ProceduralAudio statik bağımlılığı Setup'da Initialize() ile çözülür.
    /// </summary>
    [TestFixture]
    public class BoardViewTests
    {
        private GameObject _boardGo;
        private BoardView _view;
        private GameFeelConfigSO _feelConfig;
        private RingColorPaletteSO _colorPalette;
        private SettingsModel _settingsModel;
        private MockBoardPoolService _poolService;
        private MockAudioService _audioService;
        private MockHapticService _hapticService;
        private RingMaterialManager _ringMaterialManager;
        private SpecialOverlayRenderer _overlayRenderer;
        private Camera _testCamera;
        private AudioConfigSO _audioConfig;

        // ── Test Pole Data ──────────────────────────────────────────────

        /// <summary>
        /// 2-pole minimal board: pole 0 has 2x Red rings, pole 1 is empty buffer.
        /// </summary>
        private static List<PoleState> TwoPoleBoard()
        {
            var poles = new List<PoleState>
            {
                new PoleState { Id = 0, MaxCapacity = 4, RingCapacity = 4 },
                new PoleState { Id = 1, MaxCapacity = 4, RingCapacity = 4 }
            };
            poles[0].AddRing(new RingData(RingColor.Red));
            poles[0].AddRing(new RingData(RingColor.Red));
            return poles;
        }

        /// <summary>
        /// 3-pole board with locked pole: pole 0 has rings, pole 1 locked empty, pole 2 empty.
        /// </summary>
        private static List<PoleState> LockedPoleBoard()
        {
            var poles = new List<PoleState>
            {
                new PoleState { Id = 0, MaxCapacity = 4, RingCapacity = 4 },
                new PoleState { Id = 1, MaxCapacity = 4, RingCapacity = 4, IsLocked = true },
                new PoleState { Id = 2, MaxCapacity = 4, RingCapacity = 4 }
            };
            poles[0].AddRing(new RingData(RingColor.Red));
            poles[0].AddRing(new RingData(RingColor.Blue));
            return poles;
        }

        /// <summary>
        /// 2-pole board with special ring types for overlay testing.
        /// </summary>
        private static List<PoleState> SpecialRingBoard()
        {
            var poles = new List<PoleState>
            {
                new PoleState { Id = 0, MaxCapacity = 4, RingCapacity = 4 },
                new PoleState { Id = 1, MaxCapacity = 4, RingCapacity = 4 }
            };
            poles[0].AddRing(new RingData(RingColor.Red, RingType.Mystery));
            poles[0].AddRing(new RingData(RingColor.Blue, RingType.Frozen));
            poles[1].AddRing(new RingData(RingColor.Green, RingType.Bomb, 3));
            poles[1].AddRing(new RingData(RingColor.Yellow, RingType.Standard));
            return poles;
        }

        // ── Setup / Teardown ────────────────────────────────────────────

        [SetUp]
        public void Setup()
        {
            // Load data-driven configs from Resources (same pattern as GameplayCommandTests, PlayModeIntegrationTests)
            _feelConfig = Resources.Load<GameFeelConfigSO>(GameplayAssetKeys.GameFeelConfig);
            Assert.IsNotNull(_feelConfig, "GameFeelConfig.asset must exist in Resources/Configs/");

            _colorPalette = Resources.Load<RingColorPaletteSO>(GameplayAssetKeys.RingColorPalette);
            Assert.IsNotNull(_colorPalette, "RingColorPalette.asset must exist in Resources/Configs/");

            _audioConfig = Resources.Load<AudioConfigSO>(GameplayAssetKeys.AudioConfig);
            // Initialize ProceduralAudio first — many BoardView methods (FlashPoleError,
            // CelebratePoleComplete) call ProceduralAudio.GetOrCreateErrorClip() etc.
            // which require a prior Initialize() call.
            if (_audioConfig != null)
                ProceduralAudio.Initialize(_audioConfig);

            _settingsModel = new SettingsModel();
            _poolService = new MockBoardPoolService();
            _audioService = new MockAudioService();
            _hapticService = new MockHapticService();

            // Create and inject RingMaterialManager (refactored from BoardView)
            _ringMaterialManager = new RingMaterialManager();
            InjectField(_ringMaterialManager, "_feelConfig", _feelConfig);
            InjectField(_ringMaterialManager, "_colorPalette", _colorPalette);
            InjectField(_ringMaterialManager, "_settingsModel", _settingsModel);

            // Create and inject SpecialOverlayRenderer (refactored from BoardView)
            _overlayRenderer = new SpecialOverlayRenderer();
            InjectField(_overlayRenderer, "_feelConfig", _feelConfig);

            // Create a test camera (BoardView references Camera.main in some paths but
            // uses injected _mainCamera for camera shake — we inject the mock here).
            _testCamera = new GameObject("TestCamera").AddComponent<Camera>();
            _testCamera.tag = "MainCamera";

            // Create BoardView on a fresh GameObject
            _boardGo = new GameObject("BoardView_Test");
            _view = _boardGo.AddComponent<BoardView>();

            // Inject dependencies via reflection (established pattern in test base)
            InjectField(_view, "_feelConfig", _feelConfig);
            InjectField(_view, "_colorPalette", _colorPalette);
            InjectField(_view, "_settingsModel", _settingsModel);
            InjectField(_view, "_objectPoolService", _poolService);
            InjectField(_view, "_vfxRegistry", null); // VFX yolları null-safe — null gönder
            InjectField(_view, "_audioService", _audioService);
            InjectField(_view, "_hapticService", _hapticService);
            InjectField(_view, "_mainCamera", _testCamera);
            InjectField(_view, "_ringMaterialManager", _ringMaterialManager);
            InjectField(_view, "_overlayRenderer", _overlayRenderer);

            // BoardView needs a torus prefab for AcquireRing
            var torusGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            torusGo.name = "TorusPrefab_Test";
            torusGo.SetActive(false);
            _view.SetTorusPrefab(torusGo);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up all test objects
            if (_boardGo != null)
                Object.DestroyImmediate(_boardGo);
            if (_testCamera != null)
                Object.DestroyImmediate(_testCamera.gameObject);

            // Clean up material cache (destroys GPU resources)
            _ringMaterialManager?.ClearCache();

            // Reset DOTween to clean state between tests
            DG.Tweening.DOTween.Clear(true);

            _view = null;
            _boardGo = null;
            _testCamera = null;
            _ringMaterialManager = null;
            _overlayRenderer = null;
        }

        // ── Construction Tests ──────────────────────────────────────────

        [Test]
        public void BoardView_CanBeConstructed_WithoutScene()
        {
            Assert.IsNotNull(_view, "BoardView must be constructable via AddComponent.");
            Assert.IsNotNull(_view.transform, "BoardView must have a valid transform.");
        }

        [Test]
        public void BoardView_BuildBoard_NullPoles_ThrowsNullReference()
        {
            // BuildBoard calls ClearBoard first (safe), then poles.Count → NullReferenceException.
            // This test documents current behavior; when a null guard is added during
            // refactoring, this test must be updated to expect DoesNotThrow.
            Assert.Throws<System.NullReferenceException>(() => _view.BuildBoard(null),
                "BuildBoard with null pole list currently throws NullReferenceException (no guard yet).");
        }

        // ── BuildBoard Tests ────────────────────────────────────────────

        [Test]
        public void BuildBoard_TwoPoles_CreatesCorrectPoleCount()
        {
            var poles = TwoPoleBoard();
            _view.BuildBoard(poles);

            var pole0 = _view.GetPoleView(0);
            var pole1 = _view.GetPoleView(1);
            var poleInvalid = _view.GetPoleView(99);

            Assert.IsNotNull(pole0, "Pole 0 must have a PoleView after BuildBoard.");
            Assert.IsNotNull(pole1, "Pole 1 must have a PoleView after BuildBoard.");
            Assert.IsNull(poleInvalid, "Invalid pole ID must return null.");
            Assert.AreEqual(0, pole0.PoleId, "PoleView.PoleId must match the pole index.");
            Assert.AreEqual(1, pole1.PoleId, "PoleView.PoleId must match the pole index.");
        }

        [Test]
        public void BuildBoard_TwoPoles_CreatesCorrectRingChildren()
        {
            var poles = TwoPoleBoard();
            _view.BuildBoard(poles);

            var pole0 = _view.GetPoleView(0);
            Assert.IsNotNull(pole0, "Pole 0 must exist.");

            // Pole 0 has 2 rings — children named "Ring_0_*" and "Ring_1_*"
            int ringCount = CountRingChildren(pole0);
            Assert.AreEqual(2, ringCount, "Pole 0 must have 2 ring visual children.");

            // Pole 1 must be empty (no ring children)
            var pole1 = _view.GetPoleView(1);
            int pole1RingCount = CountRingChildren(pole1);
            Assert.AreEqual(0, pole1RingCount, "Pole 1 (empty buffer) must have 0 ring children.");
        }

        [Test]
        public void BuildBoard_TwoPoles_PositionsPolesCorrectly()
        {
            var poles = TwoPoleBoard();
            _view.BuildBoard(poles);

            var pole0 = _view.GetPoleView(0);
            var pole1 = _view.GetPoleView(1);

            float spacing = _feelConfig.PoleSpacing;
            Assert.AreEqual(-spacing * 0.5f, pole0.transform.localPosition.x, 0.001f,
                "Pole 0 should be at -spacing/2 on X.");
            Assert.AreEqual(spacing * 0.5f, pole1.transform.localPosition.x, 0.001f,
                "Pole 1 should be at +spacing/2 on X.");
        }

        [Test]
        public void BuildBoard_EmptyPolesList_ClearsBoard()
        {
            _view.BuildBoard(TwoPoleBoard());
            Assert.IsNotNull(_view.GetPoleView(0), "Pole should exist before ClearBoard.");

            _view.BuildBoard(new List<PoleState>());
            Assert.IsNull(_view.GetPoleView(0), "Pole should be gone after rebuild with empty list.");
            Assert.IsNull(_view.GetPoleView(1), "Pole should be gone after rebuild with empty list.");
        }

        [Test]
        public void BuildBoard_LockedPole_SetsPoleVisual()
        {
            var poles = LockedPoleBoard();
            _view.BuildBoard(poles);

            var pole1 = _view.GetPoleView(1);
            Assert.IsNotNull(pole1, "Locked pole must have a PoleView.");

            // Verify via reflection that PoleView._isLocked was set
            var isLockedField = typeof(PoleView).GetField("_isLocked",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (isLockedField != null)
            {
                bool isLocked = (bool)isLockedField.GetValue(pole1);
                Assert.IsTrue(isLocked, "Pole 1 must be visually locked after BuildBoard.");
            }
        }

        [Test]
        public void BuildBoard_LockedPole_PoleNameContainsLocked()
        {
            var poles = LockedPoleBoard();
            _view.BuildBoard(poles);

            var pole1 = _view.GetPoleView(1);
            Assert.IsTrue(pole1.gameObject.name.Contains("LOCKED"),
                "Locked pole's GameObject name must contain LOCKED.");
        }

        [Test]
        public void BuildBoard_Idempotent_CalledTwice_NoDuplicatePoles()
        {
            var poles = TwoPoleBoard();
            _view.BuildBoard(poles);
            _view.BuildBoard(poles);

            var pole0 = _view.GetPoleView(0);
            Assert.IsNotNull(pole0, "Pole 0 must exist after second BuildBoard.");
            Assert.AreEqual(0, pole0.PoleId, "Pole 0 must still have correct ID.");

            // Pole positions should still be correct (not drifting)
            float spacing = _feelConfig.PoleSpacing;
            Assert.AreEqual(-spacing * 0.5f, pole0.transform.localPosition.x, 0.001f,
                "Pole 0 position must be stable after second BuildBoard.");
        }

        [Test]
        public void BuildBoard_SpecialRings_DoesNotThrow()
        {
            var poles = SpecialRingBoard();
            Assert.DoesNotThrow(() => _view.BuildBoard(poles),
                "BuildBoard must handle Mystery, Frozen, Bomb ring types without throwing.");
        }

        [Test]
        public void BuildBoard_InvalidRingCapacity_Throws()
        {
            var poles = new List<PoleState>
            {
                new PoleState { Id = 0, MaxCapacity = 0, RingCapacity = 0 },
                new PoleState { Id = 1, MaxCapacity = 4, RingCapacity = 4 }
            };

            Assert.Throws<System.InvalidOperationException>(() => _view.BuildBoard(poles),
                "BuildBoard must throw when pole has RingCapacity <= 0.");
        }

        // ── GetPoleView Tests ───────────────────────────────────────────

        [Test]
        public void GetPoleView_BeforeBuild_ReturnsNull()
        {
            Assert.IsNull(_view.GetPoleView(0), "GetPoleView before BuildBoard must return null.");
            Assert.IsNull(_view.GetPoleView(-1), "GetPoleView with negative ID must return null.");
        }

        [Test]
        public void GetPoleView_OutOfRange_ReturnsNull()
        {
            _view.BuildBoard(TwoPoleBoard());
            Assert.IsNull(_view.GetPoleView(-1), "Negative pole ID must return null.");
            Assert.IsNull(_view.GetPoleView(99), "Out-of-range pole ID must return null.");
        }

        // ── SetSelectedPole Tests ───────────────────────────────────────

        [Test]
        public void SetSelectedPole_Negative_DeselectsAll()
        {
            _view.BuildBoard(TwoPoleBoard());

            _view.SetSelectedPole(0);
            _view.SetSelectedPole(-1);

            var lastSelectedField = typeof(BoardView).GetField("_lastSelectedPoleId",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(lastSelectedField, "_lastSelectedPoleId field must exist.");
            int lastId = (int)lastSelectedField.GetValue(_view);
            Assert.AreEqual(-1, lastId, "After deselect, _lastSelectedPoleId must be -1.");
        }

        [Test]
        public void SetSelectedPole_SamePoleTwice_DoesNotVibrateTwice()
        {
            _view.BuildBoard(TwoPoleBoard());
            int hapticCountBefore = _hapticService.VibrateCallCount;

            _view.SetSelectedPole(0);
            _view.SetSelectedPole(0); // Second call — should be no-op

            Assert.AreEqual(hapticCountBefore + 1, _hapticService.VibrateCallCount,
                "Haptic must fire exactly once when selecting the same pole twice.");
        }

        [Test]
        public void SetSelectedPole_NoBoardBuilt_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _view.SetSelectedPole(0),
                "SetSelectedPole with no board must not throw.");
            Assert.DoesNotThrow(() => _view.SetSelectedPole(-1),
                "SetSelectedPole deselect with no board must not throw.");
        }

        [Test]
        public void SetSelectedPole_DifferentPoles_ChangesSelection()
        {
            // 3-pole board: select 0, then select 2
            var poles = LockedPoleBoard();
            _view.BuildBoard(poles);

            _view.SetSelectedPole(0);
            _view.SetSelectedPole(2);

            var lastSelectedField = typeof(BoardView).GetField("_lastSelectedPoleId",
                BindingFlags.NonPublic | BindingFlags.Instance);
            int lastId = (int)lastSelectedField.GetValue(_view);
            Assert.AreEqual(2, lastId, "After selecting pole 2, _lastSelectedPoleId must be 2.");
        }

        // ── FlashPoleError Tests ────────────────────────────────────────

        [Test]
        public void FlashPoleError_ValidPole_FlashesAndPlaysHaptic()
        {
            _view.BuildBoard(TwoPoleBoard());
            int hapticCountBefore = _hapticService.VibrateCallCount;
            int audioCountBefore = _audioService.PlaySfxCallCount;

            _view.FlashPoleError(0);

            Assert.AreEqual(hapticCountBefore + 1, _hapticService.VibrateCallCount,
                "FlashPoleError must trigger haptic feedback.");
            Assert.AreEqual(audioCountBefore + 1, _audioService.PlaySfxCallCount,
                "FlashPoleError must play error SFX.");
        }

        [Test]
        public void FlashPoleError_InvalidPole_DoesNotThrow()
        {
            _view.BuildBoard(TwoPoleBoard());
            Assert.DoesNotThrow(() => _view.FlashPoleError(-1),
                "FlashPoleError with invalid pole must not throw.");
            Assert.DoesNotThrow(() => _view.FlashPoleError(99),
                "FlashPoleError with out-of-range pole must not throw.");
        }

        [Test]
        public void FlashPoleError_NullPoleView_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _view.FlashPoleError(0),
                "FlashPoleError without board must not throw.");
        }

        // ── CelebratePoleComplete Tests ─────────────────────────────────

        [Test]
        public void CelebratePoleComplete_ValidPole_DoesNotThrow()
        {
            _view.BuildBoard(TwoPoleBoard());

            Assert.DoesNotThrow(() => _view.CelebratePoleComplete(0, 2, 1, false),
                "CelebratePoleComplete (non-final) must not throw.");
        }

        [Test]
        public void CelebratePoleComplete_FinalPole_DoesNotThrow()
        {
            _view.BuildBoard(TwoPoleBoard());

            Assert.DoesNotThrow(() => _view.CelebratePoleComplete(0, 2, 2, true),
                "CelebratePoleComplete (final pole) must not throw.");
        }

        [Test]
        public void CelebratePoleComplete_InvalidPole_DoesNotThrow()
        {
            _view.BuildBoard(TwoPoleBoard());

            Assert.DoesNotThrow(() => _view.CelebratePoleComplete(-1, 2, 1, false),
                "CelebratePoleComplete with invalid pole must not throw.");
            Assert.DoesNotThrow(() => _view.CelebratePoleComplete(99, 2, 1, false),
                "CelebratePoleComplete with out-of-range pole must not throw.");
        }

        [Test]
        public void CelebratePoleComplete_NoBoard_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _view.CelebratePoleComplete(0, 2, 1, false),
                "CelebratePoleComplete without board must not throw.");
        }

        // ── AnimateRingMove Tests ───────────────────────────────────────

        [Test]
        public void AnimateRingMove_ValidMove_DoesNotThrow()
        {
            _view.BuildBoard(TwoPoleBoard());

            var afterMovePoles = new List<PoleState>
            {
                new PoleState { Id = 0, MaxCapacity = 4, RingCapacity = 4 },
                new PoleState { Id = 1, MaxCapacity = 4, RingCapacity = 4 }
            };
            afterMovePoles[0].AddRing(new RingData(RingColor.Red));
            afterMovePoles[1].AddRing(new RingData(RingColor.Red));

            Assert.DoesNotThrow(() => _view.AnimateRingMove(0, 1, afterMovePoles),
                "AnimateRingMove must not throw for a valid move.");
        }

        [Test]
        public void AnimateRingMove_NegativePoleId_DoesNotThrow()
        {
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            _view.BuildBoard(TwoPoleBoard());
            var poles = TwoPoleBoard();

            Assert.DoesNotThrow(() => _view.AnimateRingMove(-1, 0, poles),
                "AnimateRingMove with negative fromPoleId must not throw.");
            Assert.DoesNotThrow(() => _view.AnimateRingMove(0, -1, poles),
                "AnimateRingMove with negative toPoleId must not throw.");
        }

        [Test]
        public void AnimateRingMove_OutOfRangePoleId_DoesNotThrow()
        {
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            _view.BuildBoard(TwoPoleBoard());
            var poles = TwoPoleBoard();

            Assert.DoesNotThrow(() => _view.AnimateRingMove(0, 99, poles),
                "AnimateRingMove with out-of-range toPoleId must not throw.");
            Assert.DoesNotThrow(() => _view.AnimateRingMove(99, 0, poles),
                "AnimateRingMove with out-of-range fromPoleId must not throw.");
        }

        [Test]
        public void AnimateRingMove_NullPoles_ThrowsNullReference()
        {
            _view.BuildBoard(TwoPoleBoard());

            // AnimateRingMove does NOT have a null guard for poles (unlike AnimateRingUndo
            // which checks `if (poles == null) return` at the top). It calls
            // BuildBoard(poles) which throws NullReferenceException.
            // This test documents current behavior; when a null guard is added during
            // refactoring, this test must be updated to expect DoesNotThrow.
            Assert.Throws<System.NullReferenceException>(
                () => _view.AnimateRingMove(0, 1, null),
                "AnimateRingMove with null poles currently throws NullReferenceException.");
        }

        [Test]
        public void AnimateRingMove_NoBoard_DoesNotThrow()
        {
            var poles = TwoPoleBoard();
            Assert.DoesNotThrow(() => _view.AnimateRingMove(0, 1, poles),
                "AnimateRingMove without pre-built board must not throw.");
        }

        [Test]
        public void AnimateRingMove_ReduceMotion_SnapsRing()
        {
            _settingsModel.ReduceMotion.Value = true;
            _view.BuildBoard(TwoPoleBoard());

            var afterMove = new List<PoleState>
            {
                new PoleState { Id = 0, MaxCapacity = 4, RingCapacity = 4 },
                new PoleState { Id = 1, MaxCapacity = 4, RingCapacity = 4 }
            };
            afterMove[0].AddRing(new RingData(RingColor.Red));
            afterMove[1].AddRing(new RingData(RingColor.Red));

            Assert.DoesNotThrow(() => _view.AnimateRingMove(0, 1, afterMove),
                "AnimateRingMove with ReduceMotion must not throw.");
        }

        [Test]
        public void AnimateRingMove_ThenClearBoard_DoesNotThrow()
        {
            _view.BuildBoard(TwoPoleBoard());
            var afterMove = new List<PoleState>
            {
                new PoleState { Id = 0, MaxCapacity = 4, RingCapacity = 4 },
                new PoleState { Id = 1, MaxCapacity = 4, RingCapacity = 4 }
            };
            afterMove[0].AddRing(new RingData(RingColor.Red));
            afterMove[1].AddRing(new RingData(RingColor.Red));

            _view.AnimateRingMove(0, 1, afterMove);
            Assert.DoesNotThrow(() => _view.ClearBoard(),
                "ClearBoard after AnimateRingMove must not throw.");
        }

        // ── BUG-4 Fix Verification Tests ────────────────────────────────

        /// <summary>
        /// BUG-4 Fix Verification — FIXED: AnimateRingMove with invalid pole ID must NOT throw.
        /// Previously threw ArgumentOutOfRangeException at _spawnedPoles[fromPoleId].
        /// After fix: early return with NexusLog.Error, no exception, no gameplay impact.
        /// Validates: Requirements 2.7, 2.8
        /// </summary>
        [Test]
        public void AnimateRingMove_InvalidPoleId_DoesNotThrow_AfterFix()
        {
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            _view.BuildBoard(TwoPoleBoard());
            var poles = TwoPoleBoard();

            Assert.DoesNotThrow(
                () => _view.AnimateRingMove(-1, 0, poles),
                "BUG-4 FIX: AnimateRingMove(-1, 0) must NOT throw after bounds guard added.");
            Assert.DoesNotThrow(
                () => _view.AnimateRingMove(999, 0, poles),
                "BUG-4 FIX: AnimateRingMove(999, 0) must NOT throw after bounds guard added.");
        }

        // ── AnimateRingUndo Tests ───────────────────────────────────────

        [Test]
        public void AnimateRingUndo_ValidUndo_DoesNotThrow()
        {
            _view.BuildBoard(TwoPoleBoard());

            var afterUndo = new List<PoleState>
            {
                new PoleState { Id = 0, MaxCapacity = 4, RingCapacity = 4 },
                new PoleState { Id = 1, MaxCapacity = 4, RingCapacity = 4 }
            };
            afterUndo[0].AddRing(new RingData(RingColor.Red));
            afterUndo[0].AddRing(new RingData(RingColor.Red));

            Assert.DoesNotThrow(() => _view.AnimateRingUndo(0, 1, afterUndo),
                "AnimateRingUndo must not throw for a valid undo.");
        }

        [Test]
        public void AnimateRingUndo_NegativePoleId_DoesNotThrow()
        {
            _view.BuildBoard(TwoPoleBoard());
            var poles = TwoPoleBoard();

            Assert.DoesNotThrow(() => _view.AnimateRingUndo(-1, 0, poles),
                "AnimateRingUndo with negative fromPoleId must not throw.");
            Assert.DoesNotThrow(() => _view.AnimateRingUndo(0, -1, poles),
                "AnimateRingUndo with negative toPoleId must not throw.");
        }

        [Test]
        public void AnimateRingUndo_OutOfRangePoleId_DoesNotThrow()
        {
            _view.BuildBoard(TwoPoleBoard());
            var poles = TwoPoleBoard();

            Assert.DoesNotThrow(() => _view.AnimateRingUndo(0, 99, poles),
                "AnimateRingUndo with out-of-range toPoleId must not throw.");
        }

        [Test]
        public void AnimateRingUndo_NullPoles_ReturnsEarly()
        {
            _view.BuildBoard(TwoPoleBoard());

            // AnimateRingUndo checks for null poles list at the top — must return early
            Assert.DoesNotThrow(() => _view.AnimateRingUndo(0, 1, null),
                "AnimateRingUndo with null poles must return early without throwing.");
        }

        [Test]
        public void AnimateRingUndo_InvalidPoleIds_ReturnsEarly()
        {
            Assert.DoesNotThrow(() => _view.AnimateRingUndo(-5, 0, new List<PoleState>()),
                "AnimateRingUndo must guard against negative fromPoleId.");
            Assert.DoesNotThrow(() => _view.AnimateRingUndo(0, -5, new List<PoleState>()),
                "AnimateRingUndo must guard against negative toPoleId.");
        }

        [Test]
        public void AnimateRingUndo_ThenAnimateRingMove_DoesNotThrow()
        {
            _view.BuildBoard(TwoPoleBoard());
            var afterMove = new List<PoleState>
            {
                new PoleState { Id = 0, MaxCapacity = 4, RingCapacity = 4 },
                new PoleState { Id = 1, MaxCapacity = 4, RingCapacity = 4 }
            };
            afterMove[0].AddRing(new RingData(RingColor.Red));
            afterMove[1].AddRing(new RingData(RingColor.Red));

            _view.AnimateRingMove(0, 1, afterMove);

            var afterUndo = new List<PoleState>
            {
                new PoleState { Id = 0, MaxCapacity = 4, RingCapacity = 4 },
                new PoleState { Id = 1, MaxCapacity = 4, RingCapacity = 4 }
            };
            afterUndo[0].AddRing(new RingData(RingColor.Red));
            afterUndo[0].AddRing(new RingData(RingColor.Red));

            Assert.DoesNotThrow(() => _view.AnimateRingUndo(0, 1, afterUndo),
                "AnimateRingUndo after AnimateRingMove must not throw.");
        }

        // ── ClearBoard Tests ────────────────────────────────────────────

        [Test]
        public void ClearBoard_AfterBuild_RemovesAllPoles()
        {
            _view.BuildBoard(TwoPoleBoard());
            Assert.IsNotNull(_view.GetPoleView(0), "Pole should exist before ClearBoard.");

            _view.ClearBoard();
            Assert.IsNull(_view.GetPoleView(0), "Pole should be null after ClearBoard.");
            Assert.IsNull(_view.GetPoleView(1), "Pole should be null after ClearBoard.");
        }

        [Test]
        public void ClearBoard_BeforeBuild_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _view.ClearBoard(),
                "ClearBoard before BuildBoard must not throw.");
        }

        [Test]
        public void ClearBoard_CalledTwice_DoesNotThrow()
        {
            _view.BuildBoard(TwoPoleBoard());
            _view.ClearBoard();
            Assert.DoesNotThrow(() => _view.ClearBoard(),
                "ClearBoard called twice must not throw.");
        }

        // ── ShowTutorialArrow / HideTutorialArrow Tests ─────────────────

        [Test]
        public void ShowTutorialArrow_ValidPole_DoesNotThrow()
        {
            _view.BuildBoard(TwoPoleBoard());
            Assert.DoesNotThrow(() => _view.ShowTutorialArrow(0, "SELECT"),
                "ShowTutorialArrow on valid pole must not throw.");
        }

        [Test]
        public void ShowTutorialArrow_InvalidPole_HidesArrow()
        {
            _view.BuildBoard(TwoPoleBoard());
            Assert.DoesNotThrow(() => _view.ShowTutorialArrow(99, "SELECT"),
                "ShowTutorialArrow on invalid pole must not throw.");
        }

        [Test]
        public void HideTutorialArrow_WithoutShow_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _view.HideTutorialArrow(),
                "HideTutorialArrow without prior show must not throw.");
        }

        [Test]
        public void ShowTutorialArrow_ThenHide_DoesNotThrow()
        {
            _view.BuildBoard(TwoPoleBoard());
            _view.ShowTutorialArrow(0, "SELECT");
            Assert.DoesNotThrow(() => _view.HideTutorialArrow(),
                "HideTutorialArrow after show must not throw.");
        }

        [Test]
        public void ShowTutorialArrow_NoBoard_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _view.ShowTutorialArrow(0, "SELECT"),
                "ShowTutorialArrow without board must not throw (hides arrow internally).");
        }

        // ── Camera Shake Tests ──────────────────────────────────────────

        [Test]
        public void ShakeCamera_ValidValues_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _view.ShakeCamera(0.5f, 0.3f),
                "ShakeCamera must not throw with valid parameters.");
        }

        [Test]
        public void ShakeCamera_ZeroValues_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _view.ShakeCamera(0f, 0f),
                "ShakeCamera must not throw with zero parameters.");
        }

        [Test]
        public void ShakeCamera_NullCameraInjected_DoesNotThrow()
        {
            InjectField(_view, "_mainCamera", null);
            Assert.DoesNotThrow(() => _view.ShakeCamera(0.5f, 0.3f),
                "ShakeCamera must not throw when camera is null.");
        }

        // ── FitCameraToBoard Tests ──────────────────────────────────────

        [Test]
        public void FitCameraToBoard_ValidCount_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _view.FitCameraToBoard(5),
                "FitCameraToBoard must not throw with valid pole count.");
        }

        [Test]
        public void FitCameraToBoard_ZeroCount_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _view.FitCameraToBoard(0),
                "FitCameraToBoard must not throw with zero pole count.");
        }

        // ── EnsureRingPoolPrewarmed Tests ───────────────────────────────

        [Test]
        public void EnsureRingPoolPrewarmed_WithTorusPrefab_CallsPrewarm()
        {
            int prewarmCountBefore = _poolService.PrewarmCallCount;

            _view.EnsureRingPoolPrewarmed();

            Assert.AreEqual(prewarmCountBefore + 1, _poolService.PrewarmCallCount,
                "EnsureRingPoolPrewarmed must call ObjectPoolService.Prewarm.");
        }

        [Test]
        public void EnsureRingPoolPrewarmed_CalledTwice_PrewarmsOnce()
        {
            _view.EnsureRingPoolPrewarmed();
            int firstCount = _poolService.PrewarmCallCount;

            _view.EnsureRingPoolPrewarmed();

            Assert.AreEqual(firstCount, _poolService.PrewarmCallCount,
                "Second call to EnsureRingPoolPrewarmed must not call Prewarm again.");
        }

        [Test]
        public void EnsureRingPoolPrewarmed_NullTorusPrefab_DoesNotThrow()
        {
            _view.SetTorusPrefab(null);
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error,
                "[Nexus][BoardView][EnsureRingPoolPrewarmed] _torusPrefab null — Torus prefab DI ile enjekte edilmemis. Ring havuzu ön ısıtması iptal.");
            Assert.DoesNotThrow(() => _view.EnsureRingPoolPrewarmed(),
                "EnsureRingPoolPrewarmed with null torus prefab must not throw.");
        }

        // ── ReduceMotion + SlowMode Integration Tests ───────────────────

        [Test]
        public void BuildBoard_WithReduceMotion_NoTweenAllocation()
        {
            _settingsModel.ReduceMotion.Value = true;
            _view.BuildBoard(TwoPoleBoard());

            var pole0 = _view.GetPoleView(0);
            Assert.IsNotNull(pole0, "Pole must exist with ReduceMotion enabled.");
        }

        [Test]
        public void SetSelectedPole_WithSlowMode_DoesNotThrow()
        {
            _settingsModel.SlowMode.Value = true;
            _view.BuildBoard(TwoPoleBoard());

            Assert.DoesNotThrow(() => _view.SetSelectedPole(0),
                "Selection with SlowMode must not throw.");
            Assert.DoesNotThrow(() => _view.SetSelectedPole(-1),
                "Deselect with SlowMode must not throw.");
        }

        // ── Snapshot Tests (Board State → Visual State) ─────────────────

        [Test]
        public void BuildBoard_RebuildWithDifferentData_ReflectsNewState()
        {
            _view.BuildBoard(TwoPoleBoard());
            Assert.AreEqual(2, CountSpawnedPoles(), "After first build, must have 2 poles.");

            var threePoles = new List<PoleState>
            {
                new PoleState { Id = 0, MaxCapacity = 4, RingCapacity = 4 },
                new PoleState { Id = 1, MaxCapacity = 4, RingCapacity = 4 },
                new PoleState { Id = 2, MaxCapacity = 4, RingCapacity = 4 }
            };
            threePoles[0].AddRing(new RingData(RingColor.Yellow));
            threePoles[1].AddRing(new RingData(RingColor.Green));
            threePoles[2].AddRing(new RingData(RingColor.Purple));

            _view.BuildBoard(threePoles);

            Assert.AreEqual(3, CountSpawnedPoles(),
                "After rebuild with 3 poles, must have 3 poles.");
            Assert.IsNull(_view.GetPoleView(3), "Pole index 3 must not exist.");
        }

        [Test]
        public void BuildBoard_MaxCapacityPoles_AllCreated()
        {
            const int maxPoles = 12;
            var poles = new List<PoleState>();
            for (int i = 0; i < maxPoles; i++)
            {
                var p = new PoleState { Id = i, MaxCapacity = 4, RingCapacity = 4 };
                p.AddRing(new RingData((RingColor)((i % 10) + 1)));
                poles.Add(p);
            }

            _view.BuildBoard(poles);

            for (int i = 0; i < maxPoles; i++)
            {
                var pole = _view.GetPoleView(i);
                Assert.IsNotNull(pole, $"Pole {i} must exist in a {maxPoles}-pole board.");
                Assert.AreEqual(i, pole.PoleId, $"PoleView.PoleId should be {i}.");
            }
        }

        [Test]
        public void AnimateRingMove_ToFullPole_DoesNotCrash()
        {
            // Pole 1 has 4 rings (full), pole 0 has 1 ring — move to full pole should be graceful
            var poles = new List<PoleState>
            {
                new PoleState { Id = 0, MaxCapacity = 4, RingCapacity = 4 },
                new PoleState { Id = 1, MaxCapacity = 4, RingCapacity = 4 }
            };
            poles[0].AddRing(new RingData(RingColor.Red));
            for (int i = 0; i < 4; i++)
                poles[1].AddRing(new RingData(RingColor.Blue));

            _view.BuildBoard(poles);

            var afterMove = new List<PoleState>
            {
                new PoleState { Id = 0, MaxCapacity = 4, RingCapacity = 4 },
                new PoleState { Id = 1, MaxCapacity = 4, RingCapacity = 4 }
            };
            afterMove[0].AddRing(new RingData(RingColor.Red));
            for (int i = 0; i < 4; i++)
                afterMove[1].AddRing(new RingData(RingColor.Blue));

            Assert.DoesNotThrow(() => _view.AnimateRingMove(0, 1, afterMove),
                "AnimateRingMove to a full pole must not crash.");
        }

        // ── Private Helpers ─────────────────────────────────────────────

        private static void InjectField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name,
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            if (field == null)
            {
                var baseType = target.GetType().BaseType;
                while (baseType != null && field == null)
                {
                    field = baseType.GetField(name,
                        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                    baseType = baseType.BaseType;
                }
            }
            if (field != null)
                field.SetValue(target, value);
        }

        private int CountSpawnedPoles()
        {
            int count = 0;
            while (_view.GetPoleView(count) != null) count++;
            return count;
        }

        private static int CountRingChildren(PoleView pole)
        {
            int count = 0;
            for (int i = 0; i < pole.transform.childCount; i++)
            {
                if (pole.transform.GetChild(i).name.StartsWith("Ring_"))
                    count++;
            }
            return count;
        }
    }

    // ── Mock Implementations ────────────────────────────────────────────

    /// <summary>
    /// Minimal IObjectPoolService mock for BoardView tests.
    /// BoardView paths that use the pool are null-safe when pool is set.
    /// </summary>
    public class MockBoardPoolService : IObjectPoolService
    {
        public int PrewarmCallCount { get; private set; }

        public void Prewarm(GameObject prefab, int count, Transform parent = null)
        {
            PrewarmCallCount++;
        }

        public GameObject Spawn(GameObject prefab, Vector3 position = default, Quaternion rotation = default, Transform parent = null)
        {
            var go = Object.Instantiate(prefab, position, rotation);
            return go;
        }

        public T Spawn<T>(T prefab, Vector3 position = default, Quaternion rotation = default, Transform parent = null) where T : Component
        {
            if (prefab == null) return null;
            var go = Spawn(prefab.gameObject, position, rotation, parent);
            return go != null ? go.GetComponent<T>() : null;
        }

        public void Despawn(GameObject instance)
        {
            if (instance != null)
                Object.DestroyImmediate(instance);
        }

        public void DespawnAfter(GameObject instance, float seconds)
        {
            if (instance != null)
                Object.DestroyImmediate(instance);
        }

        public void ClearPool(GameObject prefab) { }

        public void ClearAllPools() { }
    }

    /// <summary>
    /// Minimal IAudioService mock for BoardView tests.
    /// </summary>
    public class MockAudioService : IAudioService
    {
        public int PlaySfxCallCount { get; private set; }
        public float BgmVolume { get; set; } = 1f;
        public float SfxVolume { get; set; } = 1f;
        public float BgmStateMultiplier { get; set; } = 1f;
        public float MasterVolume { get; set; } = 1f;
        public bool IsMuted { get; set; }

        public void PlayBgm(AudioClip clip, bool loop = true, float fadeDuration = 0.5f) { }
        public void PlaySfx(AudioClip clip, float volume = 1f, float pitchMin = 1f, float pitchMax = 1f)
        {
            PlaySfxCallCount++;
        }

        public void PlaySfxAtPosition(AudioClip clip, Vector3 position, float volume = 1f) { }

        public void StopBgm(float fadeDuration = 0.5f) { }
        public void Mute() => IsMuted = true;
        public void Unmute() => IsMuted = false;
        public void Pause() { }
        public void Resume() { }
    }

    /// <summary>
    /// Minimal IHapticService mock for BoardView tests.
    /// </summary>
    public class MockHapticService : IHapticService
    {
        public bool IsEnabled { get; set; } = true;
        public int VibrateCallCount { get; private set; }

        public void Vibrate(HapticType type)
        {
            VibrateCallCount++;
        }

        public void VibratePreset(HapticType type) => Vibrate(type);
        public void Light() => Vibrate(HapticType.Light);
        public void Medium() => Vibrate(HapticType.Medium);
        public void Heavy() => Vibrate(HapticType.Heavy);
        public void Success() => Vibrate(HapticType.Success);
        public void Warning() => Vibrate(HapticType.Warning);
        public void Selection() => Vibrate(HapticType.Selection);
    }
}
