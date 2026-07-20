using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using RingFlow.Gameplay.Services;
using UnityEngine;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// INexusService implementation of IProceduralAudioService.
    /// Owns all procedural audio clip generation and caching.
    /// Bound in GameplayLifecycle.OnConfigure() via:
    ///   builder.BindService&lt;IProceduralAudioService, ProceduralAudioService&gt;();
    /// </summary>
    public class ProceduralAudioService : IProceduralAudioService, INexusService
    {
        private readonly AudioConfigSO _config;
        private AudioClip _moveClip;
        private AudioClip _winClip;
        private AudioClip _errorClip;
        private AudioClip _explosionClip;
        private AudioClip _poleCompleteClip;
        private readonly System.Collections.Generic.Dictionary<int, AudioClip> _richPoleClips = new();
        private AudioClip _finalPoleCompleteClip;
        private AudioClip _chainClip;
        private AudioClip _magnetClip;
        private AudioClip _paintClip;
        private AudioClip _iceBreakClip;
        private AudioClip _stoneImpactClip;
        private AudioClip _portalClip;
        private readonly System.Collections.Generic.Dictionary<int, AudioClip> _bgmClips = new();

        public ProceduralAudioService(AudioConfigSO config)
        {
            _config = config ?? throw new System.ArgumentNullException(nameof(config),
                "[ProceduralAudioService] AudioConfigSO is required.");
        }

        // INexusService lifecycle
        public ValueTask InitializeAsync(CancellationToken ct) => default;

        public void OnDispose()
        {
            ClearCache();
        }

        private int SampleRate
        {
            get
            {
                if (_config == null)
                    throw new System.InvalidOperationException("[ProceduralAudioService] AudioConfigSO is required.");
                return _config.SampleRate;
            }
        }

        public AudioClip GetOrCreateMoveClip()
        {
            if (_moveClip == null)
            {
                var cfg = _config != null ? _config.Move : default;
                int sr = SampleRate;
                float dur = cfg.Duration > 0 ? cfg.Duration : 0.12f;
                int samplesCount = (int)(sr * dur);
                float[] samples = new float[samplesCount];
                for (int i = 0; i < samplesCount; i++)
                {
                    float t = (float)i / sr;
                    float freq = Mathf.Lerp(cfg.FrequencyStart > 0 ? cfg.FrequencyStart : 600f,
                                             cfg.FrequencyEnd > 0 ? cfg.FrequencyEnd : 400f, t / dur);
                    float vol = Mathf.Cos((t / dur) * Mathf.PI * 0.5f);
                    samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * (cfg.Volume > 0 ? cfg.Volume : 0.25f) * vol;
                }
                _moveClip = AudioClip.Create("MoveSFX", samplesCount, 1, sr, false);
                _moveClip.SetData(samples, 0);
            }
            return _moveClip;
        }

        public AudioClip GetOrCreateWinClip()
        {
            if (_winClip == null)
            {
                var cfg = _config != null ? _config.Win : default;
                int sr = SampleRate;
                float dur = cfg.Duration > 0 ? cfg.Duration : 0.8f;
                int samplesCount = (int)(sr * dur);
                float[] samples = new float[samplesCount];
                for (int i = 0; i < samplesCount; i++)
                {
                    float t = (float)i / sr;
                    float vol = 1f - (t / dur);
                    float freq;
                    if (t < 0.15f) freq = cfg.NoteC5 > 0 ? cfg.NoteC5 : 523.25f;
                    else if (t < 0.30f) freq = cfg.NoteE5 > 0 ? cfg.NoteE5 : 659.25f;
                    else if (t < 0.45f) freq = cfg.NoteG5 > 0 ? cfg.NoteG5 : 783.99f;
                    else freq = cfg.NoteC6 > 0 ? cfg.NoteC6 : 1046.50f;
                    samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * (cfg.Volume > 0 ? cfg.Volume : 0.2f) * vol;
                }
                _winClip = AudioClip.Create("WinSFX", samplesCount, 1, sr, false);
                _winClip.SetData(samples, 0);
            }
            return _winClip;
        }

        public AudioClip GetOrCreateErrorClip()
        {
            if (_errorClip == null)
            {
                var cfg = _config != null ? _config.Error : default;
                int sr = SampleRate;
                float dur = cfg.Duration > 0 ? cfg.Duration : 0.25f;
                int samplesCount = (int)(sr * dur);
                float[] samples = new float[samplesCount];
                float freq = cfg.Frequency > 0 ? cfg.Frequency : 120f;
                float vol = cfg.Volume > 0 ? cfg.Volume : 0.15f;
                for (int i = 0; i < samplesCount; i++)
                {
                    float t = (float)i / sr;
                    float volume = Mathf.Cos((t / dur) * Mathf.PI * 0.5f);
                    float val = Mathf.Sin(2f * Mathf.PI * freq * t) > 0f ? 1f : -1f;
                    samples[i] = val * vol * volume;
                }
                _errorClip = AudioClip.Create("ErrorSFX", samplesCount, 1, sr, false);
                _errorClip.SetData(samples, 0);
            }
            return _errorClip;
        }

        public AudioClip GetOrCreateExplosionClip()
        {
            if (_explosionClip == null)
            {
                var cfg = _config != null ? _config.Explosion : default;
                int sr = SampleRate;
                float dur = cfg.Duration > 0 ? cfg.Duration : 0.7f;
                int samplesCount = (int)(sr * dur);
                float[] samples = new float[samplesCount];
                float vol = cfg.Volume > 0 ? cfg.Volume : 0.25f;

                var seededRng = new System.Random(16180);
                for (int i = 0; i < samplesCount; i++)
                {
                    float t = (float)i / sr;
                    float volume = 1f - (t / dur);
                    float noise = (float)(seededRng.NextDouble() * 2.0 - 1.0);
                    samples[i] = noise * vol * volume;
                }
                _explosionClip = AudioClip.Create("ExplosionSFX", samplesCount, 1, sr, false);
                _explosionClip.SetData(samples, 0);
            }
            return _explosionClip;
        }

        public AudioClip GetOrCreatePoleCompleteClip()
        {
            if (_poleCompleteClip == null)
            {
                var cfg = _config != null ? _config.PoleComplete : default;
                int sr = SampleRate;
                float dur = cfg.Duration > 0 ? cfg.Duration : 0.4f;
                int samplesCount = (int)(sr * dur);
                float[] samples = new float[samplesCount];
                for (int i = 0; i < samplesCount; i++)
                {
                    float t = (float)i / sr;
                    float vol = Mathf.Cos((t / dur) * Mathf.PI * 0.5f);
                    float freq = Mathf.Lerp(cfg.FrequencyStart > 0 ? cfg.FrequencyStart : 523.25f,
                                             cfg.FrequencyEnd > 0 ? cfg.FrequencyEnd : 1046.50f, t / dur);
                    samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * (cfg.Volume > 0 ? cfg.Volume : 0.22f) * vol;
                }
                _poleCompleteClip = AudioClip.Create("PoleCompleteSFX", samplesCount, 1, sr, false);
                _poleCompleteClip.SetData(samples, 0);
            }
            return _poleCompleteClip;
        }

        public AudioClip GetOrCreateRichPoleCompleteClip(int ringCount)
        {
            var cacheKey = ringCount;
            if (!_richPoleClips.TryGetValue(cacheKey, out var clip) || clip == null)
            {
                var cfg = _config != null ? _config.RichPoleComplete : default;
                int sr = SampleRate;
                float dur = cfg.Duration > 0 ? cfg.Duration : 0.6f;
                int samplesCount = (int)(sr * dur);
                float[] samples = new float[samplesCount];

                float ringPitchFactor = cfg.RingPitchFactorBase + (ringCount - 2) * cfg.RingPitchFactorPerRing;

                var seededRng = new System.Random(ringCount * 7919);
                float nextNoise() => (float)(seededRng.NextDouble() * 2.0 - 1.0);

                for (int i = 0; i < samplesCount; i++)
                {
                    float t = (float)i / sr;
                    float progress = t / dur;

                    float thudDurFrac = cfg.ThudDurationFraction > 0 ? cfg.ThudDurationFraction : 0.25f;
                    float thudVolume = progress < thudDurFrac
                        ? Mathf.Cos((progress / thudDurFrac) * Mathf.PI * 0.5f) * (1f - progress / thudDurFrac)
                        : 0f;
                    float thudFreq = Mathf.Lerp(cfg.ThudLowFreq, cfg.ThudHighFreq, progress / thudDurFrac);
                    float thud = Mathf.Sin(2f * Mathf.PI * thudFreq * t) * cfg.ThudVolume * thudVolume;
                    thud += nextNoise() * cfg.ThudNoiseVolume * thudVolume;

                    float sweepStart = cfg.SweepStartFraction;
                    float sweepEnd = cfg.SweepEndFraction;
                    float sweepVolume = progress < sweepStart ? 0f :
                                        progress > sweepEnd ? Mathf.Cos(((progress - sweepEnd) / (1f - sweepEnd)) * Mathf.PI * 0.5f) :
                                        1f;
                    float baseFreq = Mathf.Lerp(cfg.SweepFreqStart, cfg.SweepFreqEnd,
                        (progress - sweepStart) / (sweepEnd - sweepStart)) * ringPitchFactor;
                    float sweep = Mathf.Sin(2f * Mathf.PI * baseFreq * t) * cfg.SweepVolume * sweepVolume;
                    sweep += Mathf.Sin(2f * Mathf.PI * baseFreq * 2f * t) * cfg.SweepHarmony2Volume * sweepVolume;
                    sweep += Mathf.Sin(2f * Mathf.PI * baseFreq * 3f * t) * cfg.SweepHarmony3Volume * sweepVolume;

                    float sparkleStart = cfg.SparkleStartFraction;
                    float sparkleEnd = cfg.SparkleEndFraction;
                    float sparkleVolume = progress < sparkleStart ? 0f :
                                          progress > sparkleEnd ? Mathf.Cos(((progress - sparkleEnd) / (1f - sparkleEnd)) * Mathf.PI * 0.5f) :
                                          Mathf.Sin((progress - sparkleStart) / (sparkleEnd - sparkleStart) * Mathf.PI);
                    float sparkleFreq = Mathf.Lerp(cfg.SparkleFreqStart, cfg.SparkleFreqEnd, progress) * ringPitchFactor;
                    float sparkle = Mathf.Sin(2f * Mathf.PI * sparkleFreq * t) * cfg.SparkleVolume * sparkleVolume;
                    float noise = nextNoise() * cfg.SparkleNoiseVolume * sparkleVolume;

                    float releaseStart = cfg.ReleaseStartFraction;
                    float releaseVolume = progress < releaseStart ? 0f :
                                          Mathf.Exp(-(progress - releaseStart) / cfg.ReleaseDecayFactor);
                    float releaseFreq = cfg.ReleaseFreq * ringPitchFactor;
                    float release = Mathf.Sin(2f * Mathf.PI * releaseFreq * t) * cfg.ReleaseVolume * releaseVolume;
                    release += Mathf.Sin(2f * Mathf.PI * releaseFreq * 1.5f * t) * cfg.ReleaseHarmonyVolume * releaseVolume;

                    float master = cfg.MasterVolume > 0 ? cfg.MasterVolume : 0.65f;
                    samples[i] = (thud + sweep + sparkle + noise + release) * master;
                }

                clip = AudioClip.Create("PoleCompleteRichSFX_" + ringCount, samplesCount, 1, sr, false);
                clip.SetData(samples, 0);
                _richPoleClips[cacheKey] = clip;
            }
            return clip;
        }

        public AudioClip GetOrCreateFinalPoleClip()
        {
            if (_finalPoleCompleteClip == null)
            {
                var cfg = _config != null ? _config.FinalPole : default;
                int sr = SampleRate;
                float dur = cfg.Duration > 0 ? cfg.Duration : 1.0f;
                int samplesCount = (int)(sr * dur);
                float[] samples = new float[samplesCount];

                float[][] chordFreqs = new float[][] {
                    new float[] { 523.25f, 659.25f, 783.99f },
                    new float[] { 659.25f, 783.99f, 987.77f },
                    new float[] { 783.99f, 987.77f, 1174.66f },
                    new float[] { 1046.50f, 1318.51f, 1567.98f }
                };

                for (int i = 0; i < samplesCount; i++)
                {
                    float t = (float)i / sr;
                    float progress = t / dur;

                    int chordIdx = Mathf.Clamp((int)(progress * 4), 0, 3);
                    float chordProgress = (progress * 4f) - chordIdx;

                    float sample = 0f;

                    float bassFreq = Mathf.Lerp(cfg.BassFreqStart, cfg.BassFreqEnd, progress);
                    float bassVolume = Mathf.Cos(progress * Mathf.PI * 0.3f);
                    sample += Mathf.Sin(2f * Mathf.PI * bassFreq * t) * cfg.BassVolume * bassVolume;

                    for (int n = 0; n < chordFreqs[chordIdx].Length; n++)
                    {
                        float noteStart = (float)n / chordFreqs[chordIdx].Length;
                        float noteEnd = noteStart + 0.35f;
                        float noteVolume = chordProgress < noteStart ? 0f :
                                           chordProgress > noteEnd ? Mathf.Exp(-(chordProgress - noteEnd) * 5f) :
                                           Mathf.Sin((chordProgress - noteStart) / (noteEnd - noteStart) * Mathf.PI);
                        float freq = chordFreqs[chordIdx][n];
                        sample += Mathf.Sin(2f * Mathf.PI * freq * t) * 0.1f * noteVolume;
                        sample += Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * 0.05f * noteVolume;
                    }

                    float shimmerFreq = Mathf.Lerp(2000f, 4000f, progress);
                    float shimmerVolume = Mathf.Sin(progress * Mathf.PI) * 0.03f;
                    sample += Mathf.Sin(2f * Mathf.PI * shimmerFreq * t) * shimmerVolume;

                    float sustainStart = 0.7f / dur;
                    if (progress > sustainStart)
                    {
                        float sustainProgress = (progress - sustainStart) / (1f - sustainStart);
                        float sustainVolume = Mathf.Cos(sustainProgress * Mathf.PI * 0.5f) * 0.5f;
                        for (int n = 0; n < chordFreqs[3].Length; n++)
                        {
                            float freq = chordFreqs[3][n];
                            sample += Mathf.Sin(2f * Mathf.PI * freq * t) * 0.08f * sustainVolume;
                        }
                    }

                    samples[i] = sample * cfg.MasterVolume;
                }

                _finalPoleCompleteClip = AudioClip.Create("FinalPoleCompleteSFX", samplesCount, 1, sr, false);
                _finalPoleCompleteClip.SetData(samples, 0);
            }
            return _finalPoleCompleteClip;
        }

        public AudioClip GetOrCreateChainClip()
        {
            if (_chainClip == null)
            {
                int sr = SampleRate;
                float dur = 0.15f;
                int samplesCount = (int)(sr * dur);
                float[] samples = new float[samplesCount];
                for (int i = 0; i < samplesCount; i++)
                {
                    float t = (float)i / sr;
                    float vol = Mathf.Cos((t / dur) * Mathf.PI * 0.5f);
                    // Two metallic tones: high clink + low resonance
                    float freq1 = Mathf.Lerp(1200f, 800f, t / dur);
                    float freq2 = 400f;
                    float val = Mathf.Sin(2f * Mathf.PI * freq1 * t) * 0.30f * vol;
                    val += Mathf.Sin(2f * Mathf.PI * freq2 * t) * 0.15f * vol * Mathf.Sin(t * 30f);
                    samples[i] = val;
                }
                _chainClip = AudioClip.Create("ChainSFX", samplesCount, 1, sr, false);
                _chainClip.SetData(samples, 0);
            }
            return _chainClip;
        }

        public AudioClip GetOrCreateMagnetClip()
        {
            if (_magnetClip == null)
            {
                int sr = SampleRate;
                float dur = 0.25f;
                int samplesCount = (int)(sr * dur);
                float[] samples = new float[samplesCount];
                for (int i = 0; i < samplesCount; i++)
                {
                    float t = (float)i / sr;
                    float progress = t / dur;
                    float vol = Mathf.Sin(progress * Mathf.PI);
                    // Low hum with harmonic sweep
                    float baseFreq = Mathf.Lerp(150f, 80f, progress);
                    float val = Mathf.Sin(2f * Mathf.PI * baseFreq * t) * 0.25f * vol;
                    val += Mathf.Sin(2f * Mathf.PI * baseFreq * 2f * t) * 0.12f * vol;
                    val += Mathf.Sin(2f * Mathf.PI * baseFreq * 3f * t) * 0.08f * vol;
                    samples[i] = val;
                }
                _magnetClip = AudioClip.Create("MagnetSFX", samplesCount, 1, sr, false);
                _magnetClip.SetData(samples, 0);
            }
            return _magnetClip;
        }

        public AudioClip GetOrCreateIceBreakClip()
        {
            if (_iceBreakClip == null)
            {
                int sr = SampleRate;
                float dur = 0.3f;
                int samplesCount = (int)(sr * dur);
                float[] samples = new float[samplesCount];
                var seededRng = new System.Random(8888);
                for (int i = 0; i < samplesCount; i++)
                {
                    float t = (float)i / sr;
                    float progress = t / dur;
                    float vol = Mathf.Sin(progress * Mathf.PI) * (1f - progress * 0.5f);
                    // Ice crackle: high-frequency noise with resonant pitch
                    float noise = (float)(seededRng.NextDouble() * 2.0 - 1.0);
                    float crackleFreq = Mathf.Lerp(3000f, 500f, progress);
                    float val = noise * 0.30f * vol;
                    val += Mathf.Sin(2f * Mathf.PI * crackleFreq * t) * 0.20f * vol;
                    // Add a metallic shimmer
                    val += Mathf.Sin(2f * Mathf.PI * crackleFreq * 1.5f * t) * 0.10f * vol;
                    samples[i] = val;
                }
                _iceBreakClip = AudioClip.Create("IceBreakSFX", samplesCount, 1, sr, false);
                _iceBreakClip.SetData(samples, 0);
            }
            return _iceBreakClip;
        }

        public AudioClip GetOrCreateBombExplosionClip()
        {
            // Reuse explosion clip for bomb explosions
            return GetOrCreateExplosionClip();
        }

        public AudioClip GetOrCreatePortalClip()
        {
            if (_portalClip == null)
            {
                int sr = SampleRate;
                float dur = 0.35f;
                int samplesCount = (int)(sr * dur);
                float[] samples = new float[samplesCount];
                for (int i = 0; i < samplesCount; i++)
                {
                    float t = (float)i / sr;
                    float progress = t / dur;
                    float vol = Mathf.Sin(progress * Mathf.PI);
                    // Ethereal whoosh: rising pitch with harmonics
                    float baseFreq = Mathf.Lerp(200f, 600f, progress);
                    float val = Mathf.Sin(2f * Mathf.PI * baseFreq * t) * 0.25f * vol;
                    val += Mathf.Sin(2f * Mathf.PI * baseFreq * 1.5f * t) * 0.15f * vol;
                    val += Mathf.Sin(2f * Mathf.PI * baseFreq * 2f * t) * 0.10f * vol;
                    samples[i] = val;
                }
                _portalClip = AudioClip.Create("PortalSFX", samplesCount, 1, sr, false);
                _portalClip.SetData(samples, 0);
            }
            return _portalClip;
        }

        public AudioClip GetOrCreateStoneImpactClip()
        {
            if (_stoneImpactClip == null)
            {
                int sr = SampleRate;
                float dur = 0.2f;
                int samplesCount = (int)(sr * dur);
                float[] samples = new float[samplesCount];
                var seededRng = new System.Random(4444);
                for (int i = 0; i < samplesCount; i++)
                {
                    float t = (float)i / sr;
                    float progress = t / dur;
                    float vol = Mathf.Cos((t / dur) * Mathf.PI * 0.5f);
                    // Heavy thud: low-frequency impact with noise
                    float noise = (float)(seededRng.NextDouble() * 2.0 - 1.0) * 0.20f * vol;
                    float thudFreq = Mathf.Lerp(200f, 60f, progress);
                    float val = Mathf.Sin(2f * Mathf.PI * thudFreq * t) * 0.35f * vol;
                    val += Mathf.Sin(2f * Mathf.PI * thudFreq * 0.5f * t) * 0.20f * vol;
                    val += noise;
                    samples[i] = val;
                }
                _stoneImpactClip = AudioClip.Create("StoneImpactSFX", samplesCount, 1, sr, false);
                _stoneImpactClip.SetData(samples, 0);
            }
            return _stoneImpactClip;
        }

        public AudioClip GetOrCreatePaintClip()
        {
            if (_paintClip == null)
            {
                int sr = SampleRate;
                float dur = 0.2f;
                int samplesCount = (int)(sr * dur);
                float[] samples = new float[samplesCount];
                var seededRng = new System.Random(3407);
                for (int i = 0; i < samplesCount; i++)
                {
                    float t = (float)i / sr;
                    float vol = Mathf.Cos((t / dur) * Mathf.PI * 0.5f);
                    // Wet splat: noise burst with low resonance
                    float noise = (float)(seededRng.NextDouble() * 2.0 - 1.0);
                    float freq = 250f;
                    float val = noise * 0.25f * vol;
                    val += Mathf.Sin(2f * Mathf.PI * freq * t) * 0.15f * vol;
                    samples[i] = val;
                }
                _paintClip = AudioClip.Create("PaintSFX", samplesCount, 1, sr, false);
                _paintClip.SetData(samples, 0);
            }
            return _paintClip;
        }

        public AudioClip GetOrCreateBgmClip(int worldIndex)
        {
            if (!_bgmClips.TryGetValue(worldIndex, out var clip) || clip == null)
            {
                var cfg = _config != null ? _config.Bgm : default;
                int sr = SampleRate;
                float dur = cfg.Duration > 0 ? cfg.Duration : 6.0f;
                int samplesCount = (int)(sr * dur);
                float[] samples = new float[samplesCount];

                float baseFreq = (cfg.BaseFrequency > 0 ? cfg.BaseFrequency : 220f)
                    + (worldIndex % 6) * (cfg.FrequencyPerWorldStep > 0 ? cfg.FrequencyPerWorldStep : 32.7f);

                float l1v = cfg.Layer1Volume > 0 ? cfg.Layer1Volume : 1.0f;
                float l2v = cfg.Layer2Volume > 0 ? cfg.Layer2Volume : 0.5f;
                float l3v = cfg.Layer3Volume > 0 ? cfg.Layer3Volume : 0.3f;
                float l4v = cfg.Layer4Volume > 0 ? cfg.Layer4Volume : 0.2f;
                float master = cfg.MasterVolume > 0 ? cfg.MasterVolume : 0.04f;
                float fadeBound = cfg.FadeBound > 0 ? cfg.FadeBound : 0.3f;

                for (int i = 0; i < samplesCount; i++)
                {
                    float t = (float)i / sr;

                    float l1 = Mathf.Sin(2f * Mathf.PI * baseFreq * t);
                    float l2 = Mathf.Sin(2f * Mathf.PI * (baseFreq * 1.25f) * t) * l2v;
                    float l3 = Mathf.Sin(2f * Mathf.PI * (baseFreq * 1.5f) * t) * l3v;
                    float l4 = Mathf.Sin(2f * Mathf.PI * (baseFreq * 2.0f) * t) * l4v;

                    float loopFade = 1.0f;
                    if (t < fadeBound) loopFade = t / fadeBound;
                    else if (t > dur - fadeBound) loopFade = (dur - t) / fadeBound;

                    samples[i] = (l1 + l2 + l3 + l4) * master * loopFade;
                }

                clip = AudioClip.Create("Bgm_World_" + worldIndex, samplesCount, 1, sr, false);
                clip.SetData(samples, 0);
                _bgmClips[worldIndex] = clip;
            }
            return clip;
        }

        public void ClearCache()
        {
            _moveClip = null;
            _winClip = null;
            _errorClip = null;
            _explosionClip = null;
            _poleCompleteClip = null;
            _chainClip = null;
            _magnetClip = null;
            _paintClip = null;
            _iceBreakClip = null;
            _stoneImpactClip = null;
            _portalClip = null;
            _richPoleClips.Clear();
            _finalPoleCompleteClip = null;
            _bgmClips.Clear();
        }
    }
}
