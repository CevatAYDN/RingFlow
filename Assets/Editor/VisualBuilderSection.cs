using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RingFlow.Gameplay;
using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Editor
{
    public sealed class VisualBuilderSection : EditorSection
    {
        private static Shader s_cachedShader;
        private static Material s_cachedDefaultMaterial;

        private GeneratorSection _generator;

        public override string DisplayName => "Sahne Görsel Tahta Oluşturucu";
        public override string PrefKey => EditorPrefsKeys.FoldBuilder;

        private List<MoveRecord> _previewMoves = new();
        private int _currentPreviewIndex = -1;
        private BoardState _initialPreviewBoard;
        private int[] _initialPreviewPortalTargets;
        private bool _solvedSuccessfully;
        private string _solveStatusMsg = "";

        public VisualBuilderSection(GeneratorSection generator) { _generator = generator; }

        public override void OnGUI()
        {
            DrawFoldoutHeader();
            if (!IsFoldedOut) return;

            RingFlowEditorUtils.BeginSectionBox("Sahne Kurulum Kontrolleri", "Aktif veya üretilmiş seviyenin sahne tahtasını kurun ya da temizleyin.");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Sahneyi Kur (Build Board)", GUILayout.Height(32)))
                    BuildInScene();

                if (GUILayout.Button("Sahneyi Temizle (Clear Board)", GUILayout.Height(32)))
                {
                    ClearScene();
                    _previewMoves.Clear();
                    _currentPreviewIndex = -1;
                    _solveStatusMsg = "";
                }
            }

            RingFlowEditorUtils.EndSectionBox();

            RingFlowEditorUtils.BeginSectionBox("Çözücü Önizleme (AI Solver Preview)", "Sahnedeki güncel tahta durumuna göre yapay zeka hamle önizlemelerini takip edin.");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Sahne Tahtasını Çöz", GUILayout.Height(26)))
                {
                    var board = ReadBoardFromScene(out var portalTargets);
                    if (board.PoleCount == 0)
                    {
                        _solveStatusMsg = "Sahne üzerinde tahta bulunamadı. Önce sahneyi kurun.";
                        _solvedSuccessfully = false;
                    }
                    else
                    {
                        var result = LevelSolver.Solve(board, board.MaxCapacity, portalTargets: portalTargets);
                        _solvedSuccessfully = result.IsSolvable;
                        if (result.IsSolvable && result.Moves != null && result.Moves.Count > 0)
                        {
                            _initialPreviewBoard = board;
                            _initialPreviewPortalTargets = portalTargets;
                            _previewMoves = result.Moves;
                            _currentPreviewIndex = -1;
                            _solveStatusMsg = $"Çözülebilir! Hamle sayısı: {result.MoveCount}";
                        }
                        else
                        {
                            _previewMoves.Clear();
                            _currentPreviewIndex = -1;
                            _solveStatusMsg = result.IsSolvable ? "Tahta zaten çözülmüş durumda!" : "Çözülemez tahta!";
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(_solveStatusMsg))
            {
                var style = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = _solvedSuccessfully ? EditorPaths.EditorColors.Success : EditorPaths.EditorColors.Error },
                    fontStyle = FontStyle.Bold
                };
                EditorGUILayout.LabelField(_solveStatusMsg, style);
            }

            if (_previewMoves.Count > 0)
            {
                EditorGUILayout.Space(4f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(_currentPreviewIndex < 0))
                    {
                        if (GUILayout.Button("<< Önceki Hamle", GUILayout.Height(24)))
                        {
                            _currentPreviewIndex--;
                            RebuildPreviewStep();
                        }
                    }

                    EditorGUILayout.LabelField($"Adım: {_currentPreviewIndex + 1} / {_previewMoves.Count}", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(100f));

                    using (new EditorGUI.DisabledScope(_currentPreviewIndex >= _previewMoves.Count - 1))
                    {
                        if (GUILayout.Button("Sonraki Hamle >>", GUILayout.Height(24)))
                        {
                            _currentPreviewIndex++;
                            RebuildPreviewStep();
                        }
                    }
                }

                if (_currentPreviewIndex >= 0 && _currentPreviewIndex < _previewMoves.Count)
                {
                    var currentMove = _previewMoves[_currentPreviewIndex];
                    EditorGUILayout.HelpBox($"Mevcut Adım: Direk {currentMove.FromPoleId}'den Direk {currentMove.ToPoleId}'ye taşı.", MessageType.Info);
                }
                else if (_currentPreviewIndex == -1)
                {
                    EditorGUILayout.HelpBox("Başlangıç tahta durumu. Başlamak için 'Sonraki Hamle >>' butonuna tıklayın.", MessageType.None);
                }

                if (GUILayout.Button("Önizlemeyi Sıfırla", GUILayout.Height(20)))
                {
                    _currentPreviewIndex = -1;
                    RebuildPreviewStep();
                }
            }

            RingFlowEditorUtils.EndSectionBox();
        }

        public void BuildFromDashboard()
        {
            BuildInScene();
        }

        private static Shader ResolveShader()
        {
            if (s_cachedShader != null) return s_cachedShader;
            s_cachedShader = Shader.Find("Universal Render Pipeline/Lit")
                          ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                          ?? Shader.Find("Standard");
            return s_cachedShader;
        }

        private static Material GetDefaultMaterial() => s_cachedDefaultMaterial ??= new Material(ResolveShader());

        private void BuildInScene()
        {
            List<PoleState> polesToBuild = null;
            if (Application.isPlaying)
            {
                var context = NexusRuntime.CurrentContext;
                var model = context?.TryResolve<GameplayModel>();
                if (model != null && model.Poles.Count > 0)
                    polesToBuild = model.Poles;
            }

            if (polesToBuild == null && _generator.GeneratedLevel == null)
            {
                EditorUtility.DisplayDialog("Hata",
                    "Lütfen önce bir seviye üretin VEYA aktif oyundan yüklemek için PlayMode'a girin!", "Tamam");
                return;
            }

            int poleCount = polesToBuild != null ? polesToBuild.Count : _generator.GeneratedLevel.Poles.Count;
            NexusLog.Info("RingFlowEditor", nameof(BuildInScene), "", $"Building visual board with {poleCount} poles.");

            var database = new RingFlow.Gameplay.Services.ResourcesAssetService()
                    .LoadAsync<GameConfigDatabaseSO>(EditorPaths.GameConfigDatabaseKey)
                    .GetAwaiter().GetResult();
            if (database == null)
            {
                EditorUtility.DisplayDialog("Veritabanı Bulunamadı",
                    $"GameConfigDatabaseSO '{EditorPaths.GameConfigDatabaseKey}' path'inde bulunamadı.",
                    "Tamam");
                return;
            }

            int levelIndex = _generator.GeneratedLevel != null
                ? _generator.GeneratedLevel.LevelIndex
                : _generator.GeneratedLevel == null ? 0 : _generator.GeneratedLevel.LevelIndex;
            if (levelIndex <= 0)
            {
                throw new System.InvalidOperationException("[VisualBuilderSection] GeneratedLevel is required to resolve capacity.");
            }

            int resolvedMaxCapacity = database.GetMaxCapacityForLevel(levelIndex);
            var board = new BoardState { PoleCount = poleCount, MaxCapacity = resolvedMaxCapacity };
            var portalTargets = CreateEmptyPortalTargets(poleCount);

            for (int p = 0; p < poleCount; p++)
            {
                bool isLocked;
                int portalTarget;
                List<RingData> rings;
                if (polesToBuild != null)
                {
                    isLocked = polesToBuild[p].IsLocked;
                    portalTarget = polesToBuild[p].PortalPartnerId;
                    rings = polesToBuild[p].Rings;
                }
                else
                {
                    isLocked = _generator.GeneratedLevel.Poles[p].IsLocked;
                    portalTarget = _generator.GeneratedLevel.Poles[p].PortalTargetId;
                    rings = _generator.GeneratedLevel.Poles[p].Rings;
                }
                board.SetPoleLocked(p, isLocked);
                portalTargets[p] = portalTarget;
                board.SetRingCount(p, rings.Count);
                for (int r = 0; r < rings.Count; r++)
                {
                    board.SetRingColor(p, r, rings[r].Color);
                    board.SetRingType(p, r, rings[r].Type);
                    board.SetRingAdditional(p, r, rings[r].AdditionalData);
                }
            }

            BuildBoardStateInScene(board, portalTargets);

            var boardRoot = GameObject.Find(EditorPaths.VisualBoardName);
            if (boardRoot == null)
            {
                throw new System.InvalidOperationException($"[VisualBuilderSection] Visual board root '{EditorPaths.VisualBoardName}' was not created.");
            }

            var selectedRoot = boardRoot;
            EditorApplication.delayCall += () =>
            {
                if (selectedRoot == null) return;
                Selection.activeGameObject = selectedRoot;
                SceneView.FrameLastActiveSceneView();
            };
            NexusLog.Info("RingFlowEditor", nameof(BuildInScene), "", "Visual board built successfully.");
        }

        private void RebuildPreviewStep()
        {
            if (_previewMoves.Count == 0) return;
            var currentBoard = _initialPreviewBoard;

            for (int i = 0; i <= _currentPreviewIndex; i++)
            {
                var mv = _previewMoves[i];
                if (currentBoard.CanPopRing(mv.FromPoleId) && currentBoard.GetRingCount(mv.ToPoleId) < currentBoard.MaxCapacity)
                {
                    int landingIndex = currentBoard.GetRingCount(mv.ToPoleId);
                    var ring = currentBoard.PopRing(mv.FromPoleId);
                    currentBoard.AddRing(mv.ToPoleId, ring);
                    ApplyPortalTeleportForPreview(ref currentBoard, mv.ToPoleId, landingIndex, _initialPreviewPortalTargets);
                }
            }

            BuildBoardStateInScene(currentBoard, _initialPreviewPortalTargets);
        }

        private BoardState ReadBoardFromScene(out int[] portalTargets)
        {
            var boardRoot = GameObject.Find(EditorPaths.VisualBoardName);
                        if (boardRoot == null)
            {
                throw new System.InvalidOperationException($"[VisualBuilderSection] Visual board root '{EditorPaths.VisualBoardName}' was not found in the scene. Build the board first.");
            }

            var f = new RingFlow.Gameplay.Services.ResourcesAssetService()
                    .LoadAsync<GameFeelConfigSO>(EditorPaths.GameFeelConfigKey)
                    .GetAwaiter().GetResult();
            if (f == null)
            {
                throw new System.InvalidOperationException($"[VisualBuilderSection] GameFeelConfigSO '{EditorPaths.GameFeelConfigKey}' not found.");
            }

            var polesList = new List<Transform>();
            foreach (Transform child in boardRoot.transform)
            {
                if (child.name.StartsWith("Pole_"))
                    polesList.Add(child);
            }

            int resolvedMaxCapacity = f.PoleSpacing > 0f
                ? _generator.GeneratedLevel != null
                    ? _generator.GeneratedLevel.Poles[0].RingCapacity
                    : throw new System.InvalidOperationException("[VisualBuilderSection] Cannot infer max capacity from scene without generated level data.")
                : throw new System.InvalidOperationException("[VisualBuilderSection] GameFeelConfigSO has invalid PoleSpacing.");

            var board = new BoardState { PoleCount = polesList.Count, MaxCapacity = resolvedMaxCapacity };
            portalTargets = CreateEmptyPortalTargets(polesList.Count);

            for (int p = 0; p < polesList.Count; p++)
            {
                var pTrans = polesList[p];
                var poleMeta = pTrans.GetComponent<EditorPoleMetadata>();
                bool isLocked = poleMeta != null ? poleMeta.IsLocked : pTrans.name.Contains("[LOCKED]");
                board.SetPoleLocked(p, isLocked);
                if (poleMeta != null)
                    portalTargets[p] = poleMeta.PortalTargetId;

                var ringsList = new List<Transform>();
                foreach (Transform child in pTrans)
                {
                    if (child.name.StartsWith("Ring_"))
                        ringsList.Add(child);
                }

                board.SetRingCount(p, ringsList.Count);
                for (int r = 0; r < ringsList.Count; r++)
                {
                    var rTrans = ringsList[r];
                    var ringMeta = rTrans.GetComponent<EditorRingMetadata>();
                    RingColor color = RingColor.None;
                    RingType type = RingType.Standard;
                    int additional = 0;

                    if (ringMeta != null)
                    {
                        color = ringMeta.Color;
                        type = ringMeta.Type;
                        additional = ringMeta.AdditionalData;
                    }
                    else
                    {
                        string[] parts = rTrans.name.Split('_');
                        if (parts.Length >= 4)
                        {
                            System.Enum.TryParse(parts[2], out color);
                            System.Enum.TryParse(parts[3], out type);
                            if (parts.Length >= 5)
                                int.TryParse(parts[4], out additional);
                        }
                    }
                    board.SetRingColor(p, r, color);
                    board.SetRingType(p, r, type);
                    board.SetRingAdditional(p, r, additional);
                }

                if (ringsList.Count > 0)
                {
                    var topRingType = board.GetRingType(p, ringsList.Count - 1);
                    board.SetTopRingFrozen(p, topRingType == RingType.Frozen);
                }
            }

            return board;
        }

        private void BuildBoardStateInScene(BoardState board, int[] portalTargets = null)
        {
            ClearScene();

            var boardRoot = new GameObject(EditorPaths.VisualBoardName);
            Undo.RegisterCreatedObjectUndo(boardRoot, "Build Visual Board");

            var torusModel = AssetDatabase.LoadAssetAtPath<GameObject>(EditorPaths.TorusPrefabPath);
            var f = new RingFlow.Gameplay.Services.ResourcesAssetService()
                    .LoadAsync<GameFeelConfigSO>(EditorPaths.GameFeelConfigKey)
                    .GetAwaiter().GetResult();
            var palette = new RingFlow.Gameplay.Services.ResourcesAssetService()
                    .LoadAsync<RingColorPaletteSO>(EditorPaths.RingColorPaletteKey)
                    .GetAwaiter().GetResult();
            if (f == null || palette == null)
            {
                EditorUtility.DisplayDialog("Görsel Konfig Eksik",
                    "GameFeelConfigSO veya RingColorPaletteSO bulunamadı. Visual Builder çalıştırılamıyor.",
                    "Tamam");
                return;
            }

            for (int p = 0; p < board.PoleCount; p++)
            {
                bool isLocked = board.IsPoleLocked(p);
                var rings = new List<RingData>();
                int count = board.GetRingCount(p);
                for (int r = 0; r < count; r++)
                {
                    rings.Add(new RingData(
                        board.GetRingColor(p, r),
                        board.GetRingType(p, r),
                        board.GetRingAdditional(p, r)
                    ));
                }

                CreatePole(boardRoot.transform, p, board.PoleCount, isLocked, board.MaxCapacity, rings, torusModel, f, palette, GetPortalTarget(portalTargets, p));
            }
        }

        private static void CreatePole(Transform parent, int index, int totalCount, bool isLocked, int capacity, List<RingData> rings, GameObject torusModel, GameFeelConfigSO f, RingColorPaletteSO palette, int portalTarget)
        {
            var poleObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            poleObj.name = $"Pole_{index}" + (isLocked ? " [LOCKED]" : "");
            poleObj.transform.SetParent(parent);
            
            float poleScaleYDefault = f != null ? f.PoleScale.y : 2.5f;
            float ringStackSpacingDefault = f != null ? f.RingStackSpacing : 0.176f;
            
            float worldBaseFromBottom = 0.22f;
            float worldSpacing = ringStackSpacingDefault * poleScaleYDefault;
            float totalHeight = worldBaseFromBottom + (capacity * worldSpacing) + 0.15f;
            
            Vector3 poleScale = f != null ? f.PoleScale : new Vector3(0.4f, 2.5f, 0.4f);
            poleScale.y = totalHeight / 2.0f;
            poleObj.transform.localScale = poleScale;
            
            Vector3 basePos = BoardView.GetPolePosition(index, totalCount, f);
            float poleY = basePos.y + (totalHeight / 2.0f);
            poleObj.transform.position = new Vector3(basePos.x, poleY, basePos.z);

            var capacityLabel = new GameObject("CapacityLabel");
            capacityLabel.transform.SetParent(poleObj.transform);
            capacityLabel.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            var text = capacityLabel.AddComponent<TextMesh>();
            text.text = $"Cap: {capacity}";
            text.characterSize = 0.08f;
            text.fontSize = 64;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.white;

            if (portalTarget >= 0)
            {
                var portalLabel = new GameObject("PortalLabel");
                portalLabel.transform.SetParent(poleObj.transform);
                portalLabel.transform.localPosition = new Vector3(0f, 1.25f, 0f);
                var portalText = portalLabel.AddComponent<TextMesh>();
                portalText.text = $"Portal → {portalTarget}";
                portalText.characterSize = 0.07f;
                portalText.fontSize = 48;
                portalText.anchor = TextAnchor.MiddleCenter;
                portalText.alignment = TextAlignment.Center;
                portalText.color = Color.cyan;
            }

            var poleView = poleObj.AddComponent<PoleView>();
            poleView.PoleId = index;

            var poleMeta = poleObj.AddComponent<EditorPoleMetadata>();
            poleMeta.PoleId = index;
            poleMeta.Capacity = capacity;
            poleMeta.IsLocked = isLocked;
            poleMeta.PortalTargetId = portalTarget;

            var capsule = poleObj.GetComponent<CapsuleCollider>();
            if (capsule != null)
                Object.DestroyImmediate(capsule);

            var box = poleObj.AddComponent<BoxCollider>();
            float currentSpacing = f != null ? f.PoleSpacing : 3.5f;
            float colWidth = f != null ? (currentSpacing * f.PoleColliderWidthFraction) : 2.125f;
            float targetWorldWidth = colWidth;
            float targetWorldHeight = totalHeight + 1.5f;
            float targetWorldDepth = totalCount <= 5 ? 4.0f : 2.0f;
            box.size = new Vector3(targetWorldWidth / poleScale.x, targetWorldHeight / poleScale.y, targetWorldDepth / poleScale.z);
            float centerWorldY = (targetWorldHeight / 2.0f) - 0.5f;
            box.center = new Vector3(0f, (centerWorldY - (totalHeight / 2.0f)) / poleScale.y, 0f);

            var renderer = poleObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                var shader = ResolveShader();
                Material poleMat;
                if (isLocked)
                {
                    var darkColor = new Color(0.12f, 0.12f, 0.14f);
                    poleMat = new Material(shader) { color = darkColor, name = "PoleMat_Locked", hideFlags = HideFlags.DontSave };
                    if (poleMat.HasProperty("_BaseColor"))
                        poleMat.SetColor("_BaseColor", darkColor);
                    poleMat.SetFloat("_Metallic", 0.9f);
                    poleMat.SetFloat("_Smoothness", 0.9f);
                }
                else
                {
                    var slateColor = new Color(0.20f, 0.22f, 0.25f);
                    poleMat = new Material(shader) { color = slateColor, name = "PoleMat_Open", hideFlags = HideFlags.DontSave };
                    if (poleMat.HasProperty("_BaseColor"))
                        poleMat.SetColor("_BaseColor", slateColor);
                    poleMat.SetFloat("_Metallic", 0.8f);
                    poleMat.SetFloat("_Smoothness", 0.8f);
                }
                renderer.sharedMaterial = poleMat;
            }

            var shaderForRings = ResolveShader();
            for (int r = 0; r < rings.Count; r++)
                CreateRing(poleObj.transform, r, rings[r], torusModel, shaderForRings, f, palette);
        }

        private static void CreateRing(Transform parent, int index, RingData ringData, GameObject torusModel, Shader shader, GameFeelConfigSO f, RingColorPaletteSO palette)
        {
            GameObject ringObj;
            float poleScaleYDefault = f != null ? f.PoleScale.y : 2.5f;
            float ringStackSpacingDefault = f != null ? f.RingStackSpacing : 0.176f;
            
            float worldBaseFromBottom = 0.22f;
            float worldSpacing = ringStackSpacingDefault * poleScaleYDefault;
            float targetWorldY = worldBaseFromBottom + (index * worldSpacing);
            float targetY = (targetWorldY / parent.localScale.y) - 1.0f;

            if (torusModel != null)
            {
                ringObj = Object.Instantiate(torusModel);
                ringObj.transform.SetParent(parent);
                ringObj.transform.localPosition = new Vector3(0f, targetY, 0f);
                ringObj.transform.localRotation = Quaternion.identity;
                
                float localX = f != null ? f.RingScaleTorus.x : 3.0f;
                float localY = (f != null ? f.RingScaleTorus.y : 0.25f) / parent.localScale.y;
                float localZ = f != null ? f.RingScaleTorus.z : 3.0f;
                ringObj.transform.localScale = new Vector3(localX, localY, localZ);
            }
            else
            {
                ringObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ringObj.transform.SetParent(parent);
                ringObj.transform.localPosition = new Vector3(0f, targetY, 0f);
                
                float scaleX = f != null ? f.RingScaleFallback.x : 0.8f;
                float scaleY = (f != null ? f.RingScaleFallback.y : 0.12f) / parent.localScale.y;
                float scaleZ = f != null ? f.RingScaleFallback.x : 0.8f;
                ringObj.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
            }

            ringObj.name = ringData.AdditionalData > 0
                ? $"Ring_{index}_{ringData.Color}_{ringData.Type}_{ringData.AdditionalData}"
                : $"Ring_{index}_{ringData.Color}_{ringData.Type}";

            var ringMeta = ringObj.AddComponent<EditorRingMetadata>();
            ringMeta.RingIndex = index;
            ringMeta.Color = ringData.Color;
            ringMeta.Type = ringData.Type;
            ringMeta.AdditionalData = ringData.AdditionalData;

            var ringRenderer = ringObj.GetComponentInChildren<Renderer>();
            if (ringRenderer != null)
            {
                var mat = new Material(shader) { hideFlags = HideFlags.DontSave };
                Color baseColor = palette != null ? palette.GetColor(ringData.Color, RingColorPaletteSO.ColorBlindMode.Off) : Color.grey;
                mat.color = baseColor;
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", baseColor);
                mat.SetFloat("_Metallic", 0.1f);
                mat.SetFloat("_Smoothness", 0.85f);
                
                switch (ringData.Type)
                {
                    case RingType.Frozen:
                        mat.color = Color.Lerp(baseColor, Color.cyan, 0.5f);
                        mat.SetFloat("_Metallic", 0.1f); mat.SetFloat("_Smoothness", 0.9f); break;
                    case RingType.Key: case RingType.Locked:
                        mat.color = new Color(1f, 0.84f, 0f);
                        mat.SetFloat("_Metallic", 0.8f); mat.SetFloat("_Smoothness", 0.6f); break;
                    case RingType.Stone:
                        mat.color = new Color(0.4f, 0.38f, 0.35f);
                        mat.SetFloat("_Metallic", 0f); mat.SetFloat("_Smoothness", 0.1f); break;
                    case RingType.Glass:
                        mat.color = new Color(1f, 1f, 1f, 0.25f);
                        mat.SetFloat("_Metallic", 0f); mat.SetFloat("_Smoothness", 0.95f); break;
                    case RingType.Ghost:
                        mat.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.15f);
                        mat.SetFloat("_Metallic", 0.3f); mat.SetFloat("_Smoothness", 0.3f); break;
                }
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", mat.color);

                ringRenderer.sharedMaterial = mat;
            }

            var col = ringObj.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
        }


        private static int[] CreateEmptyPortalTargets(int poleCount)
        {
            var portals = new int[poleCount];
            for (int i = 0; i < poleCount; i++) portals[i] = -1;
            return portals;
        }

        private static int GetPortalTarget(int[] portalTargets, int poleId)
        {
            if (portalTargets == null || poleId < 0 || poleId >= portalTargets.Length) return -1;
            return portalTargets[poleId];
        }

        private static void ApplyPortalTeleportForPreview(ref BoardState board, int targetPole, int landingIndex, int[] portalTargets)
        {
            int partner = GetPortalTarget(portalTargets, targetPole);
            if (partner < 0 || partner >= board.PoleCount) return;
            if (landingIndex < 0 || landingIndex >= board.GetRingCount(targetPole)) return;
            if (board.GetRingCount(partner) >= board.MaxCapacity) return;

            var portalRing = board.RemoveRingAtRaw(targetPole, landingIndex);
            if (board.CanAddRing(partner, portalRing.Color, portalRing.Type, board.MaxCapacity, portalRing.AdditionalData))
                board.AddRing(partner, portalRing);
            else
                board.AddRingSimple(targetPole, portalRing, true);
        }

        private static void ClearScene()
        {
            var boardRoot = GameObject.Find(EditorPaths.VisualBoardName);
            if (boardRoot == null) return;

            // Destroy editor-created materials to prevent leaking
            var renderers = boardRoot.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r.sharedMaterial != null && (r.sharedMaterial.hideFlags & HideFlags.DontSave) != 0)
                    Object.DestroyImmediate(r.sharedMaterial);
                r.sharedMaterial = null;
            }

            Undo.DestroyObjectImmediate(boardRoot);
        }
    }
}
