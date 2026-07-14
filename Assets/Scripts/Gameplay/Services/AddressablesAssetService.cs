using System.Threading.Tasks;
using UnityEngine;

namespace RingFlow.Gameplay.Services
{
    /// <summary>
    /// H6: Addressables-backed implementation of IAssetService.
    ///
    /// MIGRATION GUIDE:
    ///   1. Add com.unity.addressables to Packages/manifest.json.
    ///   2. Mark all assets in Resources/Configs/ as Addressable with matching keys.
    ///   3. In GameplayLifecycle.OnConfigure() replace:
    ///        builder.Bind&lt;IAssetService, ResourcesAssetService&gt;();
    ///      with:
    ///        builder.Bind&lt;IAssetService, AddressablesAssetService&gt;();
    ///   4. Remove Assets/Resources/ folder entries that are now addressed.
    ///   5. Replace remaining Resources.Load calls with IAssetService.LoadAsync.
    ///
    /// Compiles without Addressables installed (stubs to ResourcesAssetService
    /// with a clear log warning). Once com.unity.addressables is added, define
    /// UNITY_ADDRESSABLES in Player Settings > Scripting Define Symbols to
    /// activate the real Addressables implementation.
    /// </summary>
    public class AddressablesAssetService : IAssetService
    {
#if UNITY_ADDRESSABLES
        private readonly System.Collections.Generic.Dictionary<object,
            UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle> _handles = new();

        public async Task<T> LoadAsync<T>(string key) where T : Object
        {
            var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<T>(key);
            var result = await handle.Task;
            if (handle.Status !=
                UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                UnityEngine.Debug.LogError(
                    $"[AddressablesAssetService] Failed to load '{key}'. Status={handle.Status}.");
                return null;
            }
            _handles[result] = handle;
            return result;
        }

        public Task<T> LoadAssetAsync<T>(string key) where T : Object => LoadAsync<T>(key);

        public void Release(object asset)
        {
            if (asset == null) return;
            if (_handles.TryGetValue(asset, out var handle))
            {
                UnityEngine.AddressableAssets.Addressables.Release(handle);
                _handles.Remove(asset);
            }
        }
#else
        // Stub: Addressables package not yet installed.
        // Delegates to ResourcesAssetService so swapping the DI binding in
        // GameplayLifecycle is the ONLY required change after adding the package.
        private readonly ResourcesAssetService _fallback = new();

        public Task<T> LoadAsync<T>(string key) where T : Object
        {
            UnityEngine.Debug.LogWarning(
                $"[AddressablesAssetService] Addressables not installed — " +
                $"falling back to Resources.Load for key '{key}'. " +
                "Add com.unity.addressables to Packages/manifest.json.");
            return _fallback.LoadAsync<T>(key);
        }

        public Task<T> LoadAssetAsync<T>(string key) where T : Object => LoadAsync<T>(key);

        public void Release(object asset) { /* no-op in Resources mode */ }
#endif
    }
}
