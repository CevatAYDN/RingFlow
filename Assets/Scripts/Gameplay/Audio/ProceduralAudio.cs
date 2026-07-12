using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;

namespace RingFlow.Gameplay
{
    public static class ProceduralAudio
    {
        private static AudioConfigSO _config;
        private static AudioClip _moveClip;
        private static AudioClip _winClip;
        private static AudioClip _errorClip;
        private static AudioClip _explosionClip;
        private static AudioClip _poleCompleteClip;
        private static AudioClip _poleCompleteRichClip;
        private static AudioClip _finalPoleCompleteClip;
        private static readonly System.Collections.Generic.Dictionary<int, AudioClip> _bgmClips = new();

        public static void Initialize(AudioConfigSO config)
        {
            _config = config;
            if (config == null)
                NexusLog.Warn("ProceduralAudio", nameof(Initialize), "", "AudioConfigSO is null; audio will use fallback defaults.");
            else
                NexusLog.Info("ProceduralAudio", nameof(Initialize), "", $"Initialized with AudioConfigSO '{config.name}' (sampleRate={config.SampleRate}).");
        }

        private static int SampleRate => _config != null ? _config.SampleRate : 44100;

        public static AudioClip GetOrCreatePoleCompleteClip()
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

        public static AudioClip GetOrCreateRichPoleCompleteClip(int ringCount)
        {
            if (_poleCompleteRichClip == null)
            {
                var cfg = _config != null ? _config.RichPoleComplete : default;
                int sr = SampleRate;
                float dur = cfg.Duration > 0 ? cfg.Duration : 0.6f;
                int samplesCount = (int)(sr * dur);
                float[] samples = new float[samplesCount];

                float ringPitchFactor = cfg.RingPitchFactorBase + (ringCount - 2) * cfg.RingPitchFactorPerRing;

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
                    thud += (Random.value * 2f - 1f) * cfg.ThudNoiseVolume * thudVolume;

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
                    float noise = (Random.value * 2f - 1f) * cfg.SparkleNoiseVolume * sparkleVolume;

                    float releaseStart = cfg.ReleaseStartFraction;
                    float releaseVolume = progress < releaseStart ? 0f :
                                          Mathf.Exp(-(progress - releaseStart) / cfg.ReleaseDecayFactor);
                    float releaseFreq = cfg.ReleaseFreq * ringPitchFactor;
                    float release = Mathf.Sin(2f * Mathf.PI * releaseFreq * t) * cfg.ReleaseVolume * releaseVolume;
                    release += Mathf.Sin(2f * Mathf.PI * releaseFreq * 1.5f * t) * cfg.ReleaseHarmonyVolume * releaseVolume;

                    float master = cfg.MasterVolume > 0 ? cfg.MasterVolume : 0.65f;
                    samples[i] = (thud + sweep + sparkle + noise + release) * master;
                }

                _poleCompleteRichClip = AudioClip.Create("PoleCompleteRichSFX", samplesCount, 1, sr, false);
                _poleCompleteRichClip.SetData(samples, 0);
            }
            return _poleCompleteRichClip;
        }

        public static AudioClip GetOrCreateFinalPoleClip()
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

        public static AudioClip GetOrCreateMoveClip()
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

        public static AudioClip GetOrCreateWinClip()
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

        public static AudioClip GetOrCreateErrorClip()
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

        public static AudioClip GetOrCreateExplosionClip()
        {
            if (_explosionClip == null)
            {
                var cfg = _config != null ? _config.Explosion : default;
                int sr = SampleRate;
                float dur = cfg.Duration > 0 ? cfg.Duration : 0.7f;
                int samplesCount = (int)(sr * dur);
                float[] samples = new float[samplesCount];
                float vol = cfg.Volume > 0 ? cfg.Volume : 0.25f;
                for (int i = 0; i < samplesCount; i++)
                {
                    float t = (float)i / sr;
                    float volume = 1f - (t / dur);
                    float noise = Random.value * 2f - 1f;
                    samples[i] = noise * vol * volume;
                }
                _explosionClip = AudioClip.Create("ExplosionSFX", samplesCount, 1, sr, false);
                _explosionClip.SetData(samples, 0);
            }
            return _explosionClip;
        }

        public static AudioClip GetOrCreateBgmClip(int worldIndex)
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

        public static void ClearCache()
        {
            _moveClip = null;
            _winClip = null;
            _errorClip = null;
            _explosionClip = null;
            _poleCompleteClip = null;
            _poleCompleteRichClip = null;
            _finalPoleCompleteClip = null;
            _bgmClips.Clear();
        }
    }
}
