using UnityEngine;

namespace RingFlow.Gameplay
{
    public static class ProceduralAudio
    {
        private static AudioClip _moveClip;
        private static AudioClip _winClip;
        private static AudioClip _errorClip;
        private static AudioClip _explosionClip;
        private static readonly System.Collections.Generic.Dictionary<int, AudioClip> _bgmClips = new();

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
                float duration = 6.0f; // Soothing ambient loop
                int samplesCount = (int)(sampleRate * duration);
                float[] samples = new float[samplesCount];

                // Change chord progression or base note by world index
                float baseFreq = 220f + (worldIndex % 6) * 32.7f; // A3 to F4 chromatic-ish steps

                for (int i = 0; i < samplesCount; i++)
                {
                    float t = (float)i / sampleRate;
                    
                    // Construct 4 layered sine waves (Adaptive 4-layer structure)
                    float l1 = Mathf.Sin(2f * Mathf.PI * baseFreq * t);
                    float l2 = Mathf.Sin(2f * Mathf.PI * (baseFreq * 1.25f) * t) * 0.5f; // Major third
                    float l3 = Mathf.Sin(2f * Mathf.PI * (baseFreq * 1.5f) * t) * 0.3f;  // Perfect fifth
                    float l4 = Mathf.Sin(2f * Mathf.PI * (baseFreq * 2.0f) * t) * 0.2f;  // Octave

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
    }
}
