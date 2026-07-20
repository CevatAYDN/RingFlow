using NUnit.Framework;
using RingFlow.Gameplay;
using UnityEngine;

namespace RingFlow.Tests
{
    [TestFixture]
    public class ProceduralAudioTests
    {
        private ProceduralAudioService _service;

        [SetUp]
        public void Setup()
        {
            _service = new ProceduralAudioService(CreateDefaultConfig());
        }

        [TearDown]
        public void Teardown()
        {
            _service?.ClearCache();
            _service = null;
        }

        private static AudioConfigSO CreateDefaultConfig()
        {
            var cfg = ScriptableObject.CreateInstance<AudioConfigSO>();
            cfg.SampleRate = 44100;
            return cfg;
        }

        [Test]
        public void Constructor_WithNullConfig_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() => new ProceduralAudioService(null));
        }

        [Test]
        public void Constructor_WithValidConfig_DoesNotThrow()
        {
            var cfg = CreateDefaultConfig();
            Assert.DoesNotThrow(() => new ProceduralAudioService(cfg));
        }

        [Test]
        public void GetOrCreateMoveClip_WithValidConfig_ReturnsValidClip()
        {
            var clip = _service.GetOrCreateMoveClip();

            Assert.IsNotNull(clip);
            Assert.AreEqual(1, clip.channels);
            Assert.AreEqual("MoveSFX", clip.name);
            Assert.IsTrue(clip.frequency > 0);
            Assert.IsTrue(clip.samples > 0);
        }

        [Test]
        public void GetOrCreateMoveClip_WithZeroedConfig_ReturnsValidClip()
        {
            var clip = _service.GetOrCreateMoveClip();
            Assert.IsNotNull(clip);
            Assert.AreEqual(1, clip.channels);
            Assert.AreEqual("MoveSFX", clip.name);
        }

        [Test]
        public void GetOrCreateWinClip_WithValidConfig_ReturnsValidClip()
        {
            var clip = _service.GetOrCreateWinClip();
            Assert.IsNotNull(clip);
            Assert.AreEqual(1, clip.channels);
            Assert.AreEqual("WinSFX", clip.name);
        }

        [Test]
        public void GetOrCreateErrorClip_WithValidConfig_ReturnsValidClip()
        {
            var clip = _service.GetOrCreateErrorClip();
            Assert.IsNotNull(clip);
            Assert.AreEqual(1, clip.channels);
            Assert.AreEqual("ErrorSFX", clip.name);
        }

        [Test]
        public void GetOrCreateExplosionClip_WithValidConfig_ReturnsValidClip()
        {
            var clip = _service.GetOrCreateExplosionClip();
            Assert.IsNotNull(clip);
            Assert.AreEqual(1, clip.channels);
            Assert.AreEqual("ExplosionSFX", clip.name);
        }

        [Test]
        public void GetOrCreatePoleCompleteClip_WithValidConfig_ReturnsValidClip()
        {
            var clip = _service.GetOrCreatePoleCompleteClip();
            Assert.IsNotNull(clip);
            Assert.AreEqual(1, clip.channels);
            Assert.AreEqual("PoleCompleteSFX", clip.name);
        }

        [Test]
        public void GetOrCreateFinalPoleClip_WithValidConfig_ReturnsValidClip()
        {
            var clip = _service.GetOrCreateFinalPoleClip();
            Assert.IsNotNull(clip);
            Assert.AreEqual(1, clip.channels);
            Assert.AreEqual("FinalPoleCompleteSFX", clip.name);
        }

        [Test]
        public void GetOrCreateBgmClip_WithValidConfig_ReturnsValidClip()
        {
            var clip = _service.GetOrCreateBgmClip(0);
            Assert.IsNotNull(clip);
            Assert.AreEqual(1, clip.channels);
            Assert.IsTrue(clip.name.StartsWith("Bgm_World_"));
        }

        [Test]
        public void GetOrCreateRichPoleCompleteClip_WithValidConfig_ReturnsValidClip()
        {
            var clip = _service.GetOrCreateRichPoleCompleteClip(3);
            Assert.IsNotNull(clip);
            Assert.AreEqual(1, clip.channels);
            Assert.AreEqual("PoleCompleteRichSFX_3", clip.name);
        }

        [Test]
        public void Cache_ReturnsSameInstanceOnSecondCall()
        {
            var clip1 = _service.GetOrCreateMoveClip();
            var clip2 = _service.GetOrCreateMoveClip();
            Assert.AreSame(clip1, clip2);
        }

        [Test]
        public void ClearCache_RecreatesClipsOnSubsequentCall()
        {
            var clip1 = _service.GetOrCreateMoveClip();
            _service.ClearCache();
            var clip2 = _service.GetOrCreateMoveClip();
            Assert.IsNotNull(clip2);
            Assert.AreNotSame(clip1, clip2);
        }
    }
}
