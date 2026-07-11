using UnityEngine;

namespace RingFlow.Gameplay
{
    public static class ProceduralAudio
    {
        private static AudioClip _moveClip;
        private static AudioClip _winClip;
        private static AudioClip _errorClip;
        private static AudioClip _explosionClip;
        private static AudioClip _poleCompleteClip;        // Legacy — kept for backwards compat
        private static AudioClip _poleCompleteRichClip;     // New: 4-layer harmonic
        private static AudioClip _finalPoleCompleteClip;    // New: full fanfare for final pole
        private static readonly System.Collections.Generic.Dictionary<int, AudioClip> _bgmClips = new();

        // ---- Legacy (kept for backward compatibility) ----

        public static AudioClip GetOrCreatePoleCompleteClip()
        {
            if (_poleCompleteClip == null)
            {
                int sampleRate = 44100;
                float duration = 0.4f;
                int samplesCount = (int)(sampleRate * duration);
                float[] samples = new float[samplesCount];
                for (int i = 0; i < samplesCount; i++)
                {
                    float t = (float)i / sampleRate;
                    float volume = Mathf.Cos((t / duration) * Mathf.PI * 0.5f);
                    float freq = Mathf.Lerp(523.25f, 1046.50f, t / duration);
                    samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.22f * volume;
                }
                _poleCompleteClip = AudioClip.Create("PoleCompleteSFX", samplesCount, 1, sampleRate, false);
                _poleCompleteClip.SetData(samples, 0);
            }
            return _poleCompleteClip;
        }

        // ---- New: Adaptive Multi-Layer Pole Complete Audio ----

        /// <summary>
        /// Generates a rich, layered pole completion sound.
        /// Layer 0: Low thud (physical lock feel)
        /// Layer 1: Harmonically rich ascending sweep
        /// Layer 2: High-frequency sparkle/shine
        /// Layer 3: Gentle reverb-like release
        /// </summary>
        public static AudioClip GetOrCreateRichPoleCompleteClip(int ringCount)
        {
            if (_poleCompleteRichClip == null)
            {
                int sampleRate = 44100;
                float duration = 0.6f;
                int samplesCount = (int)(sampleRate * duration);
                float[] samples = new float[samplesCount];

                // Scale pitch by ring count (more rings = higher satisfaction pitch)
                float ringPitchFactor = 1f + (ringCount - 2) * 0.06f; // 2 rings → 1.0, 6 rings → 1.24

                for (int i = 0; i < samplesCount; i++)
                {
                    float t = (float)i / sampleRate;
                    float progress = t / duration;

                    // Layer 0: Low thud (0s - 0.15s)
                    float thudVolume = progress < 0.25f
                        ? Mathf.Cos((progress / 0.25f) * Mathf.PI * 0.5f) * (1f - progress / 0.25f)
                        : 0f;
                    float thudFreq = Mathf.Lerp(80f, 120f, progress / 0.25f);
                    float thud = Mathf.Sin(2f * Mathf.PI * thudFreq * t) * 0.18f * thudVolume;
                    // Add noise component to thud for punch
                    thud += (Random.value * 2f - 1f) * 0.04f * thudVolume;

                    // Layer 1: Harmonically rich ascending sweep (0.05s - 0.45s)
                    float sweepStart = 0.05f / duration;
                    float sweepEnd = 0.45f / duration;
                    float sweepVolume = progress < sweepStart ? 0f :
                                        progress > sweepEnd ? Mathf.Cos(((progress - sweepEnd) / (1f - sweepEnd)) * Mathf.PI * 0.5f) :
                                        1f;
                    float baseFreq = Mathf.Lerp(523.25f, 1046.50f, (progress - sweepStart) / (sweepEnd - sweepStart)) * ringPitchFactor;
                    // Add 2nd and 3rd harmonics for richness
                    float sweep = Mathf.Sin(2f * Mathf.PI * baseFreq * t) * 0.12f * sweepVolume;
                    sweep += Mathf.Sin(2f * Mathf.PI * baseFreq * 2f * t) * 0.06f * sweepVolume;  // 1 octave up
                    sweep += Mathf.Sin(2f * Mathf.PI * baseFreq * 3f * t) * 0.03f * sweepVolume;  // octave + fifth

                    // Layer 2: Sparkle shimmer (0.2s - 0.55s)
                    float sparkleStart = 0.2f / duration;
                    float sparkleEnd = 0.55f / duration;
                    float sparkleVolume = progress < sparkleStart ? 0f :
                                          progress > sparkleEnd ? Mathf.Cos(((progress - sparkleEnd) / (1f - sparkleEnd)) * Mathf.PI * 0.5f) :
                                          Mathf.Sin((progress - sparkleStart) / (sparkleEnd - sparkleStart) * Mathf.PI);
                    float sparkleFreq = Mathf.Lerp(1800f, 3600f, progress) * ringPitchFactor;
                    float sparkle = Mathf.Sin(2f * Mathf.PI * sparkleFreq * t) * 0.04f * sparkleVolume;
                    // Add band-limited noise for shimmer
                    float noise = (Random.value * 2f - 1f) * 0.02f * sparkleVolume;

                    // Layer 3: Release tail with reverb-like decay (0.35s - 0.6s)
                    float releaseStart = 0.35f / duration;
                    float releaseVolume = progress < releaseStart ? 0f :
                                          Mathf.Exp(-(progress - releaseStart) / 0.15f);
                    float releaseFreq = 1046.50f * ringPitchFactor;
                    float release = Mathf.Sin(2f * Mathf.PI * releaseFreq * t) * 0.06f * releaseVolume;
                    release += Mathf.Sin(2f * Mathf.PI * releaseFreq * 1.5f * t) * 0.03f * releaseVolume;

                    float masterVolume = 0.65f; // Prevent clipping from summed layers
                    samples[i] = (thud + sweep + sparkle + noise + release) * masterVolume;
                }

                _poleCompleteRichClip = AudioClip.Create("PoleCompleteRichSFX", samplesCount, 1, sampleRate, false);
                _poleCompleteRichClip.SetData(samples, 0);
            }
            return _poleCompleteRichClip;
        }

        /// <summary>
        /// Generates the final-pole fanfare — a triumphant ascending arpeggio with full harmonic body.
        /// Longer, richer, and more satisfying than regular pole complete.
        /// </summary>
        public static AudioClip GetOrCreateFinalPoleClip()
        {
            if (_finalPoleCompleteClip == null)
            {
                int sampleRate = 44100;
                float duration = 1.0f;
                int samplesCount = (int)(sampleRate * duration);
                float[] samples = new float[samplesCount];

                // Chord progression: C major → E minor → G major → C major (ascending)
                float[][] chordFreqs = new float[][] {
                    new float[] { 523.25f, 659.25f, 783.99f },  // C5, E5, G5
                    new float[] { 659.25f, 783.99f, 987.77f },  // E5, G5, B5
                    new float[] { 783.99f, 987.77f, 1174.66f }, // G5, B5, D6
                    new float[] { 1046.50f, 1318.51f, 1567.98f } // C6, E6, G6
                };

                for (int i = 0; i < samplesCount; i++)
                {
                    float t = (float)i / sampleRate;
                    float progress = t / duration;

                    // Determine current chord segment
                    int chordIdx = Mathf.Clamp((int)(progress * 4), 0, 3);
                    float chordProgress = (progress * 4f) - chordIdx;

                    float sample = 0f;

                    // Layer 0: Solid bass foundation
                    float bassFreq = Mathf.Lerp(130.81f, 261.63f, progress); // C3 → C4
                    float bassVolume = Mathf.Cos(progress * Mathf.PI * 0.3f);
                    sample += Mathf.Sin(2f * Mathf.PI * bassFreq * t) * 0.15f * bassVolume;

                    // Layer 1: Chord arpeggiation (notes ring in sequence within each chord)
                    for (int n = 0; n < chordFreqs[chordIdx].Length; n++)
                    {
                        float noteStart = (float)n / chordFreqs[chordIdx].Length;
                        float noteEnd = noteStart + 0.35f;
                        float noteVolume = chordProgress < noteStart ? 0f :
                                           chordProgress > noteEnd ? Mathf.Exp(-(chordProgress - noteEnd) * 5f) :
                                           Mathf.Sin((chordProgress - noteStart) / (noteEnd - noteStart) * Mathf.PI);
                        // Add harmonics
                        float freq = chordFreqs[chordIdx][n];
                        sample += Mathf.Sin(2f * Mathf.PI * freq * t) * 0.1f * noteVolume;
                        sample += Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * 0.05f * noteVolume;
                    }

                    // Layer 2: Sparkling high-end
                    float shimmerFreq = Mathf.Lerp(2000f, 4000f, progress);
                    float shimmerVolume = Mathf.Sin(progress * Mathf.PI) * 0.03f;
                    sample += Mathf.Sin(2f * Mathf.PI * shimmerFreq * t) * shimmerVolume;

                    // Layer 3: Final chord sustain (0.7s - 1.0s)
                    float sustainStart = 0.7f / duration;
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

                    samples[i] = sample * 0.5f;
                }

                _finalPoleCompleteClip = AudioClip.Create("FinalPoleCompleteSFX", samplesCount, 1, sampleRate, false);
                _finalPoleCompleteClip.SetData(samples, 0);
            }
            return _finalPoleCompleteClip;
        }

        // ---- Existing methods (unchanged) ----

        public static AudioClip GetOrCreateMoveClip()
        {
            if (_moveClip == null)
            {
                int sampleRate = 44100;
                float duration = 0.12f;
                int samplesCount = (int)(sampleRate * duration);
                float[] samples = new float[samplesCount];
                for (int i = 0; i < samplesCount; i++)
                {
                    float t = (float)i / sampleRate;
                    float freq = Mathf.Lerp(600f, 400f, t / duration);
                    float volume = Mathf.Cos((t / duration) * Mathf.PI * 0.5f);
                    samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.25f * volume;
                }
                _moveClip = AudioClip.Create("MoveSFX", samplesCount, 1, sampleRate, false);
                _moveClip.SetData(samples, 0);
            }
            return _moveClip;
        }

        public static AudioClip GetOrCreateWinClip()
        {
            if (_winClip == null)
            {
                int sampleRate = 44100;
                float duration = 0.8f;
                int samplesCount = (int)(sampleRate * duration);
                float[] samples = new float[samplesCount];
                for (int i = 0; i < samplesCount; i++)
                {
                    float t = (float)i / sampleRate;
                    float volume = 1f - (t / duration);

                    float freq;
                    if (t < 0.15f) freq = 523.25f; // C5
                    else if (t < 0.30f) freq = 659.25f; // E5
                    else if (t < 0.45f) freq = 783.99f; // G5
                    else freq = 1046.50f; // C6

                    samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.2f * volume;
                }
                _winClip = AudioClip.Create("WinSFX", samplesCount, 1, sampleRate, false);
                _winClip.SetData(samples, 0);
            }
            return _winClip;
        }

        public static AudioClip GetOrCreateErrorClip()
        {
            if (_errorClip == null)
            {
                int sampleRate = 44100;
                float duration = 0.25f;
                int samplesCount = (int)(sampleRate * duration);
                float[] samples = new float[samplesCount];
                for (int i = 0; i < samplesCount; i++)
                {
                    float t = (float)i / sampleRate;
                    float volume = Mathf.Cos((t / duration) * Mathf.PI * 0.5f);
                    float val = Mathf.Sin(2f * Mathf.PI * 120f * t) > 0f ? 1f : -1f;
                    samples[i] = val * 0.15f * volume;
                }
                _errorClip = AudioClip.Create("ErrorSFX", samplesCount, 1, sampleRate, false);
                _errorClip.SetData(samples, 0);
            }
            return _errorClip;
        }

        public static AudioClip GetOrCreateExplosionClip()
        {
            if (_explosionClip == null)
            {
                int sampleRate = 44100;
                float duration = 0.7f;
                int samplesCount = (int)(sampleRate * duration);
                float[] samples = new float[samplesCount];
                for (int i = 0; i < samplesCount; i++)
                {
                    float t = (float)i / sampleRate;
                    float volume = 1f - (t / duration);
                    float noise = Random.value * 2f - 1f;
                    samples[i] = noise * 0.25f * volume;
                }
                _explosionClip = AudioClip.Create("ExplosionSFX", samplesCount, 1, sampleRate, false);
                _explosionClip.SetData(samples, 0);
            }
            return _explosionClip;
        }

        public static AudioClip GetOrCreateBgmClip(int worldIndex)
        {
            if (!_bgmClips.TryGetValue(worldIndex, out var clip) || clip == null)
            {
                int sampleRate = 44100;
                float duration = 6.0f;
                int samplesCount = (int)(sampleRate * duration);
                float[] samples = new float[samplesCount];

                float baseFreq = 220f + (worldIndex % 6) * 32.7f;

                for (int i = 0; i < samplesCount; i++)
                {
                    float t = (float)i / sampleRate;

                    float l1 = Mathf.Sin(2f * Mathf.PI * baseFreq * t);
                    float l2 = Mathf.Sin(2f * Mathf.PI * (baseFreq * 1.25f) * t) * 0.5f;
                    float l3 = Mathf.Sin(2f * Mathf.PI * (baseFreq * 1.5f) * t) * 0.3f;
                    float l4 = Mathf.Sin(2f * Mathf.PI * (baseFreq * 2.0f) * t) * 0.2f;

                    float loopFade = 1.0f;
                    float fadeBound = 0.3f;
                    if (t < fadeBound) loopFade = t / fadeBound;
                    else if (t > duration - fadeBound) loopFade = (duration - t) / fadeBound;

                    samples[i] = (l1 + l2 + l3 + l4) * 0.04f * loopFade;
                }

                clip = AudioClip.Create("Bgm_World_" + worldIndex, samplesCount, 1, sampleRate, false);
                clip.SetData(samples, 0);
                _bgmClips[worldIndex] = clip;
            }
            return clip;
        }

        /// <summary>
        /// Clear static cached clips (for hot-reload / domain reload scenarios).
        /// </summary>
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
