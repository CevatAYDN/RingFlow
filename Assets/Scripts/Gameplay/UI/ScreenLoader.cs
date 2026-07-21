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
        private readonly System.Func<Dictionary<ScreenType, GameObject>> _getScreens;
        private readonly Dictionary<ScreenType, GameObject> _localScreens;
        private Dictionary<ScreenType, GameObject> _screens => _getScreens != null ? _getScreens() : _localScreens;

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

        public ScreenLoader(Transform canvasTransform, System.Func<Dictionary<ScreenType, GameObject>> getScreens = null)
        {
            _canvasTransform = canvasTransform ?? throw new ArgumentNullException(nameof(canvasTransform));
            _getScreens = getScreens;
            if (getScreens == null)
            {
                _localScreens = new Dictionary<ScreenType, GameObject>();
            }
        }

        // ── Registry Loading ────────────────────────────────────────────────

        private async Task<ScreenRegistrySO> ResolveScreenRegistryAsync(CancellationToken ct)
        {
            var assets = ResolveAssetService();
            if (assets != null)
            {
                try
                {
                    return await assets.LoadAsync<ScreenRegistrySO>(GameplayAssetKeys.ScreenRegistry).ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    NexusLog.Warn("ScreenLoader", nameof(ResolveScreenRegistryAsync), "",
                        $"AssetService failed: {ex.Message}. Falling back to Resources.Load.");
                }
            }
            ct.ThrowIfCancellationRequested();
            return Resources.Load<ScreenRegistrySO>(GameplayAssetKeys.ScreenRegistry);
        }

        private ScreenRegistrySO _cachedRegistry;

        private static IAssetService ResolveAssetService()
        {
            var ctx = Nexus.Core.NexusRuntime.CurrentContext;
            return ctx?.TryResolve<IAssetService>();
        }

        private async Task<ScreenRegistrySO> GetOrLoadRegistryAsync(CancellationToken ct)
        {
            if (_cachedRegistry != null) return _cachedRegistry;
            _cachedRegistry = await ResolveScreenRegistryAsync(ct).ConfigureAwait(true);
            return _cachedRegistry;
        }

        private static string GetScreenPrefabKey(ScreenType screen, ScreenRegistrySO registry)
        {
            if (registry != null && registry.TryGetMapping(screen, out var mapping))
            {
                if (!string.IsNullOrEmpty(mapping.PrefabPath)) return mapping.PrefabPath;
            }
            return $"{GameplayAssetKeys.UiScreenPrefix}{screen}";
        }

        // ── Prefab Loading ──────────────────────────────────────────────────

        private async Task<GameObject> LoadScreenPrefabAsync(ScreenType screen, ScreenRegistrySO registry, CancellationToken ct)
        {
            string prefabKey = GetScreenPrefabKey(screen, registry);
            var assets = ResolveAssetService();
            if (assets != null)
            {
                try
                {
                    return await assets.LoadAsync<GameObject>(prefabKey).ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    NexusLog.Warn("ScreenLoader", nameof(LoadScreenPrefabAsync), screen.ToString(),
                        $"AssetService failed for '{prefabKey}': {ex.Message}. Falling back to Resources.Load.");
                }
            }
            ct.ThrowIfCancellationRequested();
            return Resources.Load<GameObject>(prefabKey);
        }

        public async Task<List<ScreenType>> GetScreensToLoadAsync(CancellationToken ct)
        {
            var list = new List<ScreenType>();
            var registry = await GetOrLoadRegistryAsync(ct).ConfigureAwait(true);
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

        public async Task LoadAllAsync(CancellationToken ct = default)
        {
            var missingScreens = new List<ScreenType>();
            var registry = await GetOrLoadRegistryAsync(ct).ConfigureAwait(true);
            var screensToLoad = new List<ScreenType>();
            if (registry != null && registry.Mappings.Count > 0)
            {
                for (int i = 0; i < registry.Mappings.Count; i++)
                    screensToLoad.Add(registry.Mappings[i].Screen);
            }
            else
            {
                screensToLoad.AddRange(s_allScreens);
            }

            for (int i = 0; i < screensToLoad.Count; i++)
            {
                if (ct.IsCancellationRequested) return;
                var screen = screensToLoad[i];
                if (_screens.TryGetValue(screen, out var existing) && existing != null)
                    DestroyScreenInstance(existing);

                var loaded = await LoadScreenPrefabAsync(screen, registry, ct).ConfigureAwait(true);
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

            if (missingScreens.Count > 0)
                NexusLog.Warn("ScreenLoader", nameof(LoadAllAsync), "",
                    $"Missing {missingScreens.Count} prefab(s): {string.Join(", ", missingScreens)}");
        }

        public void LoadAllFromResources()
        {
            LoadAllAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        public bool HasScreen(ScreenType screen) => _screens.TryGetValue(screen, out var go) && go != null;

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
        /// Ensures a specific screen prefab is loaded and cached.
        /// </summary>
        public async Task<GameObject> EnsureScreenLoadedAsync(ScreenType screen, CancellationToken ct = default)
        {
            if (_screens.TryGetValue(screen, out var go) && go != null)
                return go;

            var registry = await GetOrLoadRegistryAsync(ct).ConfigureAwait(true);
            var prefab = await LoadScreenPrefabAsync(screen, registry, ct).ConfigureAwait(true);
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
        /// Sets or adds a screen instance in the cache.
        /// Used by editor workflows to restore screens after a clear.
        /// </summary>
        public void SetScreen(ScreenType screen, GameObject instance)
        {
            _screens[screen] = instance;
        }

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
