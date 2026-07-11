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
        private const float PoleSpacing = 2.5f;

        private static Shader s_cachedShader;
        private static Material s_cachedDefaultMaterial;

        private GeneratorSection _generator;

        public override string DisplayName => "Scene Visual Board Builder";
        public override string PrefKey => EditorPrefsKeys.FoldBuilder;

        private List<MoveRecord> _previewMoves = new();
        private int _currentPreviewIndex = -1;
        private BoardState _initialPreviewBoard;
        private bool _solvedSuccessfully;
        private string _solveStatusMsg = "";

        public VisualBuilderSection(GeneratorSection generator) { _generator = generator; }

        public override void OnGUI()
        {
            DrawFoldoutHeader();
            if (!IsFoldedOut) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.HelpBox(
                    "Sahne tahtası kur, temizle veya çözücü adımlarını önizle.",
                    MessageType.Info);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Sahneyi Kur", GUILayout.Height(32)))
                        BuildInScene();

                    if (GUILayout.Button("Sahneyi Temizle", GUILayout.Height(32)))
                    {
                        ClearScene();
                        _previewMoves.Clear();
                        _currentPreviewIndex = -1;
                        _solveStatusMsg = "";
                    }
                }
            }

            EditorGUILayout.Space(6f);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Çözücü Önizleme", EditorStyles.boldLabel);
                EditorGUILayout.Space(2f);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Solve Scene Board", GUILayout.Height(26)))
                    {
                        var board = ReadBoardFromScene();
                        if (board.PoleCount == 0)
                        {
                            _solveStatusMsg = "Sahne tahtası bulunamadı. Önce sahneyi kurun.";
                            _solvedSuccessfully = false;
                        }
                        else
                        {
                            var result = LevelSolver.Solve(board, board.MaxCapacity);
                            _solvedSuccessfully = result.IsSolvable;
                            if (result.IsSolvable && result.Moves != null && result.Moves.Count > 0)
                            {
                                _initialPreviewBoard = board;
                                _previewMoves = result.Moves;
                                _currentPreviewIndex = -1;
                                _solveStatusMsg = $"Solvable! moves: {result.MoveCount}";
                            }
                            else
                            {
                                _previewMoves.Clear();
                                _currentPreviewIndex = -1;
                                _solveStatusMsg = result.IsSolvable ? "Solved state already!" : "Unsolvable board!";
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(_solveStatusMsg))
                {
                    var style = new GUIStyle(EditorStyles.label)
                    {
                        normal = { textColor = _solvedSuccessfully ? new Color(0.2f, 0.7f, 0.2f) : new Color(0.9f, 0.3f, 0.3f) },
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
                            if (GUILayout.Button("<< Prev Move", GUILayout.Height(24)))
                            {
                                _currentPreviewIndex--;
                                RebuildPreviewStep();
                            }
                        }

                        EditorGUILayout.LabelField($"Step: {_currentPreviewIndex + 1} / {_previewMoves.Count}", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(100f));

                        using (new EditorGUI.DisabledScope(_currentPreviewIndex >= _previewMoves.Count - 1))
                        {
                            if (GUILayout.Button("Next Move >>", GUILayout.Height(24)))
                            {
                                _currentPreviewIndex++;
                                RebuildPreviewStep();
                            }
                        }
                    }

                    if (_currentPreviewIndex >= 0 && _currentPreviewIndex < _previewMoves.Count)
                    {
                        var currentMove = _previewMoves[_currentPreviewIndex];
                        EditorGUILayout.HelpBox($"Current Step: Move from Pole {currentMove.FromPoleId} to Pole {currentMove.ToPoleId}", MessageType.Info);
                    }
                    else if (_currentPreviewIndex == -1)
                    {
                        EditorGUILayout.HelpBox("Initial board state. Click 'Next Move >>' to begin.", MessageType.None);
                    }

                    if (GUILayout.Button("Reset Preview", GUILayout.Height(20)))
                    {
                        _currentPreviewIndex = -1;
                        RebuildPreviewStep();
                    }
                }
            }
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
                EditorUtility.DisplayDialog("Error",
                    "Please generate a level first OR enter PlayMode to load from active game!", "OK");
                return;
            }

            int poleCount = polesToBuild != null ? polesToBuild.Count : _generator.GeneratedLevel.Poles.Count;
            NexusLog.Info("RingFlowEditor", nameof(BuildInScene), "", $"Building visual board with {poleCount} poles.");

            var board = new BoardState { PoleCount = poleCount, MaxCapacity = 4 };
            int boardMaxCapacity = 4;
            EditorGUILayout.LabelField($"Kapasite: {boardMaxCapacity}", EditorStyles.miniLabel);
            for (int p = 0; p < poleCount; p++)
            {
                bool isLocked;
                int maxCapacity;
                List<RingData> rings;
                if (polesToBuild != null)
                {
                    isLocked = polesToBuild[p].IsLocked;
                    maxCapacity = polesToBuild[p].MaxCapacity;
                    rings = polesToBuild[p].Rings;
                }
                else
                {
                    isLocked = _generator.GeneratedLevel.Poles[p].IsLocked;
                    maxCapacity = _generator.GeneratedLevel.Poles[p].RingCapacity;
                    rings = _generator.GeneratedLevel.Poles[p].Rings;
                }
                board.SetPoleLocked(p, isLocked);
                board.SetRingCount(p, rings.Count);
                for (int r = 0; r < rings.Count; r++)
                {
                    board.SetRingColor(p, r, rings[r].Color);
                    board.SetRingType(p, r, rings[r].Type);
                    board.SetRingAdditional(p, r, rings[r].AdditionalData);
                }
                boardMaxCapacity = maxCapacity;
            }
            board.MaxCapacity = boardMaxCapacity;

            BuildBoardStateInScene(board);

            var boardRoot = GameObject.Find("RingFlow_VisualBoard");
            if (boardRoot != null)
            {
                EditorApplication.delayCall += () =>
                {
                    Selection.activeGameObject = boardRoot;
                    SceneView.FrameLastActiveSceneView();
                };
            }
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
                    var ring = currentBoard.PopRing(mv.FromPoleId);
                    currentBoard.AddRing(mv.ToPoleId, ring);
                }
            }

            BuildBoardStateInScene(currentBoard);
        }

        private BoardState ReadBoardFromScene()
        {
            var boardRoot = GameObject.Find("RingFlow_VisualBoard");
            if (boardRoot == null) return default;

            var f = GameFeelConfigSO.Instance;
            var polesList = new List<Transform>();
            for (int i = 0; i < 12; i++)
            {
                var pTrans = boardRoot.transform.Find($"Pole_{i}") ?? boardRoot.transform.Find($"Pole_{i} [LOCKED]");
                if (pTrans != null)
                {
                    polesList.Add(pTrans);
                }
            }

            var board = new BoardState { PoleCount = polesList.Count, MaxCapacity = 4 };

            for (int p = 0; p < polesList.Count; p++)
            {
                var pTrans = polesList[p];
                bool isLocked = pTrans.name.Contains("[LOCKED]");
                board.SetPoleLocked(p, isLocked);

                var ringsList = new List<Transform>();
                for (int r = 0; r < 10; r++)
                {
                    foreach (Transform child in pTrans)
                    {
                        if (child.name.StartsWith($"Ring_{r}_"))
                        {
                            ringsList.Add(child);
                            break;
                        }
                    }
                }

                board.SetRingCount(p, ringsList.Count);
                for (int r = 0; r < ringsList.Count; r++)
                {
                    var rTrans = ringsList[r];
                    string[] parts = rTrans.name.Split('_');
                    RingColor color = RingColor.None;
                    RingType type = RingType.Standard;

                    if (parts.Length >= 4)
                    {
                        System.Enum.TryParse(parts[2], out color);
                        System.Enum.TryParse(parts[3], out type);
                    }
                    board.SetRingColor(p, r, color);
                    board.SetRingType(p, r, type);
                }

                if (ringsList.Count > 0)
                {
                    var topRingType = board.GetRingType(p, ringsList.Count - 1);
                    board.SetTopRingFrozen(p, topRingType == RingType.Frozen);
                }
            }

            return board;
        }

        private void BuildBoardStateInScene(BoardState board)
        {
            ClearScene();

            var boardRoot = new GameObject("RingFlow_VisualBoard");
            Undo.RegisterCreatedObjectUndo(boardRoot, "Build Visual Board");

            var torusModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Torus.obj");
            var f = GameFeelConfigSO.Instance;
            float spacing = f != null ? f.PoleSpacing : 2.5f;
            float boardWidth = (board.PoleCount - 1) * spacing;
            float startX = -boardWidth * 0.5f;

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

                CreatePole(boardRoot.transform, p, startX, spacing, isLocked, rings, torusModel, f);
            }
        }

        private static void CreatePole(Transform parent, int index, float startX, float spacing, bool isLocked, List<RingData> rings, GameObject torusModel, GameFeelConfigSO f)
        {
            var poleObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            poleObj.name = $"Pole_{index}" + (isLocked ? " [LOCKED]" : "");
            poleObj.transform.SetParent(parent);
            float poleY = f != null ? f.PoleYPosition : 2.0f;
            poleObj.transform.position = new Vector3(startX + index * spacing, poleY, 0f);
            poleObj.transform.localScale = f != null ? f.PoleScale : new Vector3(0.2f, 2.0f, 0.2f);

            var capacityLabel = new GameObject("CapacityLabel");
            capacityLabel.transform.SetParent(poleObj.transform);
            capacityLabel.transform.localPosition = new Vector3(0f, 2.25f, 0f);
            var text = capacityLabel.AddComponent<TextMesh>();
            int capacity = f != null ? Mathf.Max(1, Mathf.RoundToInt(f.PoleScale.y * 2f)) : 4;
            text.text = $"Cap: {capacity}";
            text.characterSize = 0.08f;
            text.fontSize = 64;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.white;

            var poleView = poleObj.AddComponent<PoleView>();
            poleView.PoleId = index;

            var capsule = poleObj.GetComponent<CapsuleCollider>();
            if (capsule != null)
                Object.DestroyImmediate(capsule);

            var box = poleObj.AddComponent<BoxCollider>();
            float colWidth = f != null ? (spacing * f.PoleColliderWidthFraction) : 2.125f;
            box.size = new Vector3(colWidth, 3.0f, 2.0f);

            var renderer = poleObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                var shader = ResolveShader();
                Material poleMat;
                if (isLocked)
                {
                    var darkColor = new Color(0.12f, 0.12f, 0.14f);
                    poleMat = new Material(shader) { color = darkColor, name = "PoleMat_Locked" };
                    if (poleMat.HasProperty("_BaseColor"))
                        poleMat.SetColor("_BaseColor", darkColor);
                    poleMat.SetFloat("_Metallic", 0.9f);
                    poleMat.SetFloat("_Smoothness", 0.9f);
                }
                else
                {
                    var slateColor = new Color(0.20f, 0.22f, 0.25f);
                    poleMat = new Material(shader) { color = slateColor, name = "PoleMat_Open" };
                    if (poleMat.HasProperty("_BaseColor"))
                        poleMat.SetColor("_BaseColor", slateColor);
                    poleMat.SetFloat("_Metallic", 0.8f);
                    poleMat.SetFloat("_Smoothness", 0.8f);
                }
                renderer.sharedMaterial = poleMat;
            }

            var shaderForRings = ResolveShader();
            for (int r = 0; r < rings.Count; r++)
                CreateRing(poleObj.transform, r, rings[r], torusModel, shaderForRings, f);
        }

        private static void CreateRing(Transform parent, int index, RingData ringData, GameObject torusModel, Shader shader, GameFeelConfigSO f)
        {
            GameObject ringObj;
            float ringBaseY = f != null ? f.RingBaseYOffset : -0.9f;
            float ringSpacing = f != null ? f.RingStackSpacing : 0.4f;

            if (torusModel != null)
            {
                ringObj = Object.Instantiate(torusModel);
                ringObj.transform.SetParent(parent);
                ringObj.transform.localPosition = new Vector3(0f, ringBaseY + (index * ringSpacing), 0f);
                ringObj.transform.localRotation = Quaternion.identity;
                ringObj.transform.localScale = f != null ? f.RingScaleTorus : new Vector3(3.5f, 0.2f, 3.5f);
            }
            else
            {
                ringObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ringObj.transform.SetParent(parent);
                ringObj.transform.localPosition = new Vector3(0f, ringBaseY + (index * ringSpacing), 0f);
                ringObj.transform.localScale = f != null ? f.RingScaleFallback : new Vector3(4.0f, 0.08f, 4.0f);
            }

            ringObj.name = $"Ring_{index}_{ringData.Color}_{ringData.Type}";

            var ringRenderer = ringObj.GetComponentInChildren<Renderer>();
            if (ringRenderer != null)
            {
                var mat = new Material(shader);
                Color baseColor = RingPalette.Get(ringData.Color);
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

        private static void ClearScene()
        {
            var boardRoot = GameObject.Find("RingFlow_VisualBoard");
            if (boardRoot != null) Undo.DestroyObjectImmediate(boardRoot);
        }
    }
}
