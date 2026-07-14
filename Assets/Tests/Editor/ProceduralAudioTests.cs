using NUnit.Framework;
using RingFlow.Gameplay;
using UnityEngine;

namespace RingFlow.Tests
{
    [TestFixture]
    public class ProceduralAudioTests
    {
        [SetUp]
        public void Setup()
        {
            ProceduralAudio.ClearCache();
        }

        [TearDown]
        public void Teardown()
        {
            ProceduralAudio.ClearCache();
        }

        private static AudioConfigSO CreateDefaultConfig()
        {
            var cfg = ScriptableObject.CreateInstance<AudioConfigSO>();
            cfg.SampleRate = 44100;
            return cfg;
        }

        [Test]
        public void Initialize_WithNullConfig_Throws()
        {
            Assert.Throws<System.InvalidOperationException>(() => ProceduralAudio.Initialize(null));
        }

        [Test]
        public void Initialize_WithValidConfig_DoesNotThrow()
        {
            var cfg = CreateDefaultConfig();
            Assert.DoesNotThrow(() => ProceduralAudio.Initialize(cfg));
        }

        [Test]
        public void GetOrCreateMoveClip_WithValidConfig_ReturnsValidClip()
        {
            ProceduralAudio.Initialize(CreateDefaultConfig());
            var clip = ProceduralAudio.GetOrCreateMoveClip();

            Assert.IsNotNull(clip);
            Assert.AreEqual(1, clip.channels);
            Assert.AreEqual("MoveSFX", clip.name);
            Assert.IsTrue(clip.frequency > 0);
            Assert.IsTrue(clip.samples > 0);
        }

        [Test]
        public void GetOrCreateMoveClip_WithZeroedConfig_ReturnsValidClip()
        {
            ProceduralAudio.Initialize(CreateDefaultConfig());
            var clip = ProceduralAudio.GetOrCreateMoveClip();

            Assert.IsNotNull(clip);
            Assert.AreEqual(1, clip.channels);
            Assert.AreEqual("MoveSFX", clip.name);
        }

        [Test]
        public void GetOrCreateWinClip_WithValidConfig_ReturnsValidClip()
        {
            ProceduralAudio.Initialize(CreateDefaultConfig());
            var clip = ProceduralAudio.GetOrCreateWinClip();

            Assert.IsNotNull(clip);
            Assert.AreEqual(1, clip.channels);
            Assert.AreEqual("WinSFX", clip.name);
        }

        [Test]
        public void GetOrCreateErrorClip_WithValidConfig_ReturnsValidClip()
        {
            ProceduralAudio.Initialize(CreateDefaultConfig());
            var clip = ProceduralAudio.GetOrCreateErrorClip();

            Assert.IsNotNull(clip);
            Assert.AreEqual(1, clip.channels);
            Assert.AreEqual("ErrorSFX", clip.name);
        }

        [Test]
        public void GetOrCreateExplosionClip_WithValidConfig_ReturnsValidClip()
        {
            ProceduralAudio.Initialize(CreateDefaultConfig());
            var clip = ProceduralAudio.GetOrCreateExplosionClip();

            Assert.IsNotNull(clip);
            Assert.AreEqual(1, clip.channels);
            Assert.AreEqual("ExplosionSFX", clip.name);
        }

        [Test]
        public void GetOrCreatePoleCompleteClip_WithValidConfig_ReturnsValidClip()
        {
            ProceduralAudio.Initialize(CreateDefaultConfig());
            var clip = ProceduralAudio.GetOrCreatePoleCompleteClip();

            Assert.IsNotNull(clip);
            Assert.AreEqual(1, clip.channels);
            Assert.AreEqual("PoleCompleteSFX", clip.name);
        }

        [Test]
        public void GetOrCreateFinalPoleClip_WithValidConfig_ReturnsValidClip()
        {
            ProceduralAudio.Initialize(CreateDefaultConfig());
            var clip = ProceduralAudio.GetOrCreateFinalPoleClip();

            Assert.IsNotNull(clip);
            Assert.AreEqual(1, clip.channels);
            Assert.AreEqual("FinalPoleCompleteSFX", clip.name);
        }

        [Test]
        public void GetOrCreateBgmClip_WithValidConfig_ReturnsValidClip()
        {
            ProceduralAudio.Initialize(CreateDefaultConfig());
            var clip = ProceduralAudio.GetOrCreateBgmClip(0);

            Assert.IsNotNull(clip);
            Assert.AreEqual(1, clip.channels);
            Assert.IsTrue(clip.name.StartsWith("Bgm_World_"));
        }

        [Test]
        public void GetOrCreateRichPoleCompleteClip_WithValidConfig_ReturnsValidClip()
        {
            ProceduralAudio.Initialize(CreateDefaultConfig());
            var clip = ProceduralAudio.GetOrCreateRichPoleCompleteClip(3);

            Assert.IsNotNull(clip);
            Assert.AreEqual(1, clip.channels);
            Assert.AreEqual("PoleCompleteRichSFX", clip.name);
        }

        [Test]
        public void Cache_ReturnsSameInstanceOnSecondCall()
        {
            ProceduralAudio.Initialize(CreateDefaultConfig());
            var clip1 = ProceduralAudio.GetOrCreateMoveClip();
            var clip2 = ProceduralAudio.GetOrCreateMoveClip();

            Assert.AreSame(clip1, clip2);
        }

        [Test]
        public void ClearCache_RecreatesClipsOnSubsequentCall()
        {
            ProceduralAudio.Initialize(CreateDefaultConfig());
            var clip1 = ProceduralAudio.GetOrCreateMoveClip();
            ProceduralAudio.ClearCache();
            var clip2 = ProceduralAudio.GetOrCreateMoveClip();

            Assert.IsNotNull(clip2);
            Assert.AreNotSame(clip1, clip2);
        }
    }
}
