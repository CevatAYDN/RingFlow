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

        private static AudioConfigSO CreateZeroedConfig()
        {
            // All fields remain 0 (default struct values)
            return ScriptableObject.CreateInstance<AudioConfigSO>();
        }

        [Test]
        public void Initialize_WithNullConfig_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => ProceduralAudio.Initialize(null));
        }

        [Test]
        public void Initialize_WithValidConfig_DoesNotThrow()
        {
            var cfg = CreateDefaultConfig();
            Assert.DoesNotThrow(() => ProceduralAudio.Initialize(cfg));
        }

        [Test]
        public void GetOrCreateMoveClip_WithNullConfig_ReturnsValidClip()
        {
            ProceduralAudio.Initialize(null);
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
            ProceduralAudio.Initialize(CreateZeroedConfig());
            var clip = ProceduralAudio.GetOrCreateMoveClip();

            Assert.IsNotNull(clip);
            Assert.AreEqual(1, clip.channels);
            Assert.AreEqual("MoveSFX", clip.name);
        }

        [Test]
        public void GetOrCreateWinClip_WithNullConfig_ReturnsValidClip()
        {
            ProceduralAudio.Initialize(null);
            var clip = ProceduralAudio.GetOrCreateWinClip();

            Assert.IsNotNull(clip);
            Assert.AreEqual(1, clip.channels);
            Assert.AreEqual("WinSFX", clip.name);
        }

        [Test]
        public void GetOrCreateErrorClip_WithNullConfig_ReturnsValidClip()
        {
            ProceduralAudio.Initialize(null);
            var clip = ProceduralAudio.GetOrCreateErrorClip();

            Assert.IsNotNull(clip);
            Assert.AreEqual(1, clip.channels);
            Assert.AreEqual("ErrorSFX", clip.name);
        }

        [Test]
        public void GetOrCreateExplosionClip_WithNullConfig_ReturnsValidClip()
        {
            ProceduralAudio.Initialize(null);
            var clip = ProceduralAudio.GetOrCreateExplosionClip();

            Assert.IsNotNull(clip);
            Assert.AreEqual(1, clip.channels);
            Assert.AreEqual("ExplosionSFX", clip.name);
        }

        [Test]
        public void GetOrCreatePoleCompleteClip_WithNullConfig_ReturnsValidClip()
        {
            ProceduralAudio.Initialize(null);
            var clip = ProceduralAudio.GetOrCreatePoleCompleteClip();

            Assert.IsNotNull(clip);
            Assert.AreEqual(1, clip.channels);
            Assert.AreEqual("PoleCompleteSFX", clip.name);
        }

        [Test]
        public void GetOrCreateFinalPoleClip_WithNullConfig_ReturnsValidClip()
        {
            ProceduralAudio.Initialize(null);
            var clip = ProceduralAudio.GetOrCreateFinalPoleClip();

            Assert.IsNotNull(clip);
            Assert.AreEqual(1, clip.channels);
            Assert.AreEqual("FinalPoleCompleteSFX", clip.name);
        }

        [Test]
        public void GetOrCreateBgmClip_WithNullConfig_ReturnsValidClip()
        {
            ProceduralAudio.Initialize(null);
            var clip = ProceduralAudio.GetOrCreateBgmClip(0);

            Assert.IsNotNull(clip);
            Assert.AreEqual(1, clip.channels);
            Assert.IsTrue(clip.name.StartsWith("Bgm_World_"));
        }

        [Test]
        public void GetOrCreateRichPoleCompleteClip_WithNullConfig_ReturnsValidClip()
        {
            ProceduralAudio.Initialize(null);
            var clip = ProceduralAudio.GetOrCreateRichPoleCompleteClip(3);

            Assert.IsNotNull(clip);
            Assert.AreEqual(1, clip.channels);
            Assert.AreEqual("PoleCompleteRichSFX", clip.name);
        }

        [Test]
        public void GetOrCreateMoveClip_UsesConfigValuesWhenAvailable()
        {
            var cfg = CreateDefaultConfig();
            cfg.SampleRate = 22050;
            cfg.Move.Duration = 0.5f;
            cfg.Move.Volume = 0.5f;

            ProceduralAudio.Initialize(cfg);
            var clip = ProceduralAudio.GetOrCreateMoveClip();

            Assert.IsNotNull(clip);
            Assert.AreEqual(22050, clip.frequency);
            int expectedSamples = (int)(22050 * 0.5f);
            Assert.AreEqual(expectedSamples, clip.samples);
        }

        [Test]
        public void Cache_ReturnsSameInstanceOnSecondCall()
        {
            ProceduralAudio.Initialize(null);
            var clip1 = ProceduralAudio.GetOrCreateMoveClip();
            var clip2 = ProceduralAudio.GetOrCreateMoveClip();

            Assert.AreSame(clip1, clip2);
        }

        [Test]
        public void ClearCache_RecreatesClipsOnSubsequentCall()
        {
            ProceduralAudio.Initialize(null);
            var clip1 = ProceduralAudio.GetOrCreateMoveClip();
            ProceduralAudio.ClearCache();
            var clip2 = ProceduralAudio.GetOrCreateMoveClip();

            Assert.IsNotNull(clip2);
            // After clear, a new instance is created
            Assert.AreNotSame(clip1, clip2);
        }
    }
}
