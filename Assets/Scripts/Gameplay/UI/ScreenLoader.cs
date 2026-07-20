using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;
using RingFlow.Gameplay;
using RingFlow.Gameplay.Services;
using UnityEngine;

namespace RingFlow.Gameplay.UI
{
    /// <summary>
    /// Handles loading, caching, and lifecycle of screen prefabs (Canvas children).
    /// Extracted from UIRoot to reduce its responsibilities.
    /// Screens are stored in a dictionary keyed by ScreenType and managed as children
    /// of the Canvas transform provided at construction time.
    /// </summary>
    public class ScreenLoader
    {
        private readonly Transform _canvasTransform;
        private readonly Dictionary<ScreenType, GameObject> _screens = new();

        /// <summary>All registered screen types in default load order.</summary>
        private static readonly ScreenType[] s_allScreens =
        {
            ScreenType.Splash,
            ScreenType.MainMenu,
            ScreenType.LevelSelect,
            ScreenType.Gameplay,
            ScreenType.Pause,
            ScreenType.Win,
            ScreenType.Settings,
            ScreenType.DailyReward,
            ScreenType.ChestPopup,
            ScreenType.GameOver,
            ScreenType.ParentalGate,
            ScreenType.WorldMap,
            ScreenType.Onboarding,
        };

        public IReadOnlyDictionary<ScreenType, GameObject> Screens => _screens;

        public ScreenLoader(Transform canvasTransform)
        {
            _canvasTransform = canvasTransform ?? throw new ArgumentNullException(nameof(canvasTransform));
        }

        // ── Registry Loading ────────────────────────────────────────────────

        private ScreenRegistrySO ResolveScreenRegistry()
        {
            var assets = ResolveAssetService();
            if (assets != null)
            {
                try
                {
                    var task = assets.LoadAsync<ScreenRegistrySO>(GameplayAssetKeys.ScreenRegistry);
                    return task.GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    NexusLog.Warn("ScreenLoader", nameof(ResolveScreenRegistry), "",
                        $"AssetService failed: {ex.Message}. Falling back to Resources.Load.");
                }
            }
            return Resources.Load<ScreenRegistrySO>(GameplayAssetKeys.ScreenRegistry);
        }

        private ScreenRegistrySO _cachedRegistry;

        private static IAssetService ResolveAssetService()
        {
            var ctx = Nexus.Core.NexusRuntime.CurrentContext;
            return ctx?.TryResolve<IAssetService>();
        }

        private ScreenRegistrySO GetOrLoadRegistry()
        {
            if (_cachedRegistry != null) return _cachedRegistry;
            _cachedRegistry = ResolveScreenRegistry();
            return _cachedRegistry;
        }

        private string GetScreenPrefabKey(ScreenType screen)
        {
            var registry = GetOrLoadRegistry();
            if (registry != null && registry.TryGetMapping(screen, out var mapping))
            {
                if (!string.IsNullOrEmpty(mapping.PrefabPath)) return mapping.PrefabPath;
            }
            return $"{GameplayAssetKeys.UiScreenPrefix}{screen}";
        }

        // ── Prefab Loading ──────────────────────────────────────────────────

        private GameObject LoadScreenPrefab(ScreenType screen)
        {
            string prefabKey = GetScreenPrefabKey(screen);
            var assets = ResolveAssetService();
            if (assets != null)
            {
                try
                {
                    var task = assets.LoadAsync<GameObject>(prefabKey);
                    return task.GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    NexusLog.Warn("ScreenLoader", nameof(LoadScreenPrefab), screen.ToString(),
                        $"AssetService failed for '{prefabKey}': {ex.Message}. Falling back to Resources.Load.");
                }
            }
                        return Resources.Load<GameObject>(prefabKey);
        }

        public List<ScreenType> GetScreensToLoad()
        {
            var list = new List<ScreenType>();
            var registry = GetOrLoadRegistry();
            if (registry != null && registry.Mappings.Count > 0)
            {
                for (int i = 0; i < registry.Mappings.Count; i++)
                    list.Add(registry.Mappings[i].Screen);
            }
            else
            {
                list.AddRange(s_allScreens);
            }
            return list;
        }

        // ── Screen Instance Management ──────────────────────────────────────

        /// <summary>
        /// Finds existing screens already parented to the canvas (e.g. from editor setup).
        /// </summary>
        public void BindExistingScreens()
        {
            _screens.Clear();
            foreach (Transform child in _canvasTransform)
            {
                if (child == null || !Enum.TryParse<ScreenType>(child.name, out var screen)) continue;
                _screens[screen] = child.gameObject;
            }
        }

        /// <summary>
        /// Loads all screen prefabs from Resources synchronously.
        /// Used as fallback when async loading fails or during editor workflows.
        /// </summary>
        public void LoadAllFromResources()
        {
            var missingScreens = new List<ScreenType>();
            var screensToLoad = GetScreensToLoad();

            foreach (var screen in screensToLoad)
            {
                if (_screens.TryGetValue(screen, out var existing) && existing != null)
                    DestroyScreenInstance(existing);

                var loaded = LoadScreenPrefab(screen);
                if (loaded == null)
                {
                    missingScreens.Add(screen);
                    continue;
                }

                var instance = UnityEngine.Object.Instantiate(loaded, _canvasTransform);
                instance.name = screen.ToString();
                instance.SetActive(false);
                _screens[screen] = instance;
            }

            if (missingScreens.Count > 0)
                NexusLog.Warn("ScreenLoader", nameof(LoadAllFromResources), "",
                    $"Missing {missingScreens.Count} prefab(s): {string.Join(", ", missingScreens)}");
        }

        /// <summary>
        /// Loads all screen prefabs asynchronously via IAssetService.
        /// Falls back to Resources.Load for any asset the service cannot resolve.
        /// </summary>
        public async Task LoadAllAsync(CancellationToken ct = default)
        {
            var assets = ResolveAssetService();
            var missingScreens = new List<ScreenType>();
            var screensToLoad = GetScreensToLoad();

            foreach (var screen in screensToLoad)
            {
                if (ct.IsCancellationRequested) return;
                if (_screens.TryGetValue(screen, out var existing) && existing != null)
                    DestroyScreenInstance(existing);

                GameObject loaded = null;
                string prefabKey = GetScreenPrefabKey(screen);
                if (assets != null)
                {
                    try
                    {
                        loaded = await assets.LoadAssetAsync<GameObject>(prefabKey).ConfigureAwait(true);
                    }
                    catch (Exception ex)
                    {
                        NexusLog.Warn("ScreenLoader", nameof(LoadAllAsync), screen.ToString(),
                            $"AssetService: {ex.Message}");
                    }
                }
                if (loaded == null) loaded = LoadScreenPrefab(screen);
                if (loaded == null)
                {
                    missingScreens.Add(screen);
                    continue;
                }

                var instance = UnityEngine.Object.Instantiate(loaded, _canvasTransform);
                instance.name = screen.ToString();
                instance.SetActive(false);
                _screens[screen] = instance;
                await Task.Yield();
            }
        }

        /// <summary>
        /// Ensures a specific screen prefab is loaded and cached.
        /// </summary>
        public GameObject EnsureScreenLoaded(ScreenType screen)
        {
            if (_screens.TryGetValue(screen, out var go) && go != null)
                return go;

            var prefab = LoadScreenPrefab(screen);
            if (prefab == null) return null;

            go = UnityEngine.Object.Instantiate(prefab, _canvasTransform);
            go.name = screen.ToString();
            go.SetActive(false);
            _screens[screen] = go;
            return go;
        }

        /// <summary>
        /// Returns the cached screen instance, or null if not loaded.
        /// </summary>
        public GameObject GetScreen(ScreenType screen)
        {
            return _screens.TryGetValue(screen, out var go) ? go : null;
        }

        /// <summary>
        /// Returns whether a screen is currently loaded and cached.
        /// </summary>
        public bool HasScreen(ScreenType screen) => _screens.ContainsKey(screen);

        /// <summary>
        /// Sets or adds a screen instance in the cache.
        /// Used by editor workflows to restore screens after a clear.
        /// </summary>
        public void SetScreen(ScreenType screen, GameObject instance)
        {
            _screens[screen] = instance;
        }

        /// <summary>
        /// Destroys all loaded screen instances and clears the cache.
        /// </summary>
        public void Clear()
        {
            foreach (var go in _screens.Values)
                if (go != null) DestroyScreenInstance(go);
            _screens.Clear();
            _cachedRegistry = null;
        }

        private static void DestroyScreenInstance(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(go);
            else UnityEngine.Object.DestroyImmediate(go);
        }

        public static string GetPrefabAssetPath(ScreenType screen)
            => $"Assets/Resources/UI/{screen}.prefab";
    }
}
