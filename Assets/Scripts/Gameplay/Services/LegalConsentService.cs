using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay.Services
{
    public interface ILegalConsentService
    {
        bool IsAccepted { get; }
        void Accept();
    }

    /// <summary>
    /// Single Nexus service for GDPR/KVKK/COPPA consent state.
    /// UI mediators must not read/write PlayerPrefs directly.
    /// </summary>
    public sealed class LegalConsentService : ILegalConsentService, INexusService
    {
        [Inject] private IPlayerPrefsService _prefs;

        public bool IsAccepted => _prefs != null &&
            _prefs.GetInt(GameplayAssetKeys.PlayerPrefs.GdprAccepted, 0) == 1;

        public void Accept()
        {
            if (_prefs == null)
            {
                NexusLog.Error(nameof(LegalConsentService), nameof(Accept), string.Empty,
                    "IPlayerPrefsService is not bound; cannot persist legal consent.");
                return;
            }

            _prefs.SetInt(GameplayAssetKeys.PlayerPrefs.GdprAccepted, 1);
            _prefs.Save();
        }

        public ValueTask InitializeAsync(CancellationToken ct) => default;
        public void OnDispose() { }
    }
}
