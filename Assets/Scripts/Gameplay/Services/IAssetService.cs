using System;
using System.Threading.Tasks;
using UnityEngine;

namespace RingFlow.Gameplay.Services
{
    /// <summary>
    /// GDD §6 — Asset loading abstraction.
    /// Provides async asset loading that will later be backed by Addressables.
    /// Current implementation uses Resources.Load; swap for AddressablesAssetService
    /// when com.unity.addressables is added to the project.
    /// </summary>
    public interface IAssetService
    {
        Task<T> LoadAsync<T>(string key) where T : UnityEngine.Object;
        Task<T> LoadAssetAsync<T>(string key) where T : UnityEngine.Object;
        void Release(object asset);
    }

    /// <summary>
    /// Resources.Load-based implementation of IAssetService.
    /// This is a placeholder until Addressables is integrated.
    /// Full Addressables migration requires:
    ///   1. Add com.unity.addressables to Packages/manifest.json
    ///   2. Create Addressable groups (UI/, Levels/, Audio/, Localization/, Prefabs/)
    ///   3. Replace this implementation with AddressablesAssetService
    ///   4. Migrate inspector references to AssetReferenceT<T>
    /// </summary>
    public class ResourcesAssetService : IAssetService
    {
        public Task<T> LoadAsync<T>(string key) where T : UnityEngine.Object
        {
            var result = Resources.Load<T>(key);
            return Task.FromResult(result);
        }

        public Task<T> LoadAssetAsync<T>(string key) where T : UnityEngine.Object
        {
            var result = Resources.Load<T>(key);
            return Task.FromResult(result);
        }

        public void Release(object asset)
        {
            // Resources.Load does not track references; no-op.
            // With Addressables this would call Addressables.Release(asset).
        }
    }
}
