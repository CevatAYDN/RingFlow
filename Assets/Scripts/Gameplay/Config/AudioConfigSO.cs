using UnityEngine;

namespace RingFlow.Gameplay
{
    [System.Serializable]
    public struct AudioMoveConfig
    {
        public float Duration;
        public float FrequencyStart;
        public float FrequencyEnd;
        public float Volume;
    }

    [System.Serializable]
    public struct AudioWinConfig
    {
        public float Duration;
        public float Volume;
        public float NoteC5;
        public float NoteE5;
        public float NoteG5;
        public float NoteC6;
    }

    [System.Serializable]
    public struct AudioErrorConfig
    {
        public float Duration;
        public float Frequency;
        public float Volume;
    }

    [System.Serializable]
    public struct AudioExplosionConfig
    {
        public float Duration;
        public float Volume;
    }

    [System.Serializable]
    public struct AudioPoleCompleteConfig
    {
        public float Duration;
        public float FrequencyStart;
        public float FrequencyEnd;
        public float Volume;
    }

    [System.Serializable]
    public struct AudioRichPoleCompleteConfig
    {
        public float Duration;
        public float RingPitchFactorBase;
        public float RingPitchFactorPerRing;
        public float ThudLowFreq;
        public float ThudHighFreq;
        public float ThudVolume;
        public float ThudNoiseVolume;
        public float ThudDurationFraction;
        public float SweepStartFraction;
        public float SweepEndFraction;
        public float SweepFreqStart;
        public float SweepFreqEnd;
        public float SweepVolume;
        public float SweepHarmony2Volume;
        public float SweepHarmony3Volume;
        public float SparkleStartFraction;
        public float SparkleEndFraction;
        public float SparkleFreqStart;
        public float SparkleFreqEnd;
        public float SparkleVolume;
        public float SparkleNoiseVolume;
        public float ReleaseStartFraction;
        public float ReleaseFreq;
        public float ReleaseVolume;
        public float ReleaseHarmonyVolume;
        public float ReleaseDecayFactor;
        public float MasterVolume;
    }

    [System.Serializable]
    public struct AudioFinalPoleConfig
    {
        public float Duration;
        public float MasterVolume;
        public float BassFreqStart;
        public float BassFreqEnd;
        public float BassVolume;
    }

    [System.Serializable]
    public struct AudioBgmConfig
    {
        public float Duration;
        public float BaseFrequency;
        public float FrequencyPerWorldStep;
        public float Layer1Volume;
        public float Layer2Volume;
        public float Layer3Volume;
        public float Layer4Volume;
        public float MasterVolume;
        public float FadeBound;
    }

    [CreateAssetMenu(fileName = "AudioConfig", menuName = "RingFlow/Audio Config", order = 53)]
    public class AudioConfigSO : ScriptableObject
    {
        public int SampleRate = 44100;

        [Header("Move Sound")]
        public AudioMoveConfig Move = new()
        {
            Duration = 0.12f,
            FrequencyStart = 600f,
            FrequencyEnd = 400f,
            Volume = 0.25f
        };

        [Header("Win Sound")]
        public AudioWinConfig Win = new()
        {
            Duration = 0.8f,
            Volume = 0.2f,
            NoteC5 = 523.25f,
            NoteE5 = 659.25f,
            NoteG5 = 783.99f,
            NoteC6 = 1046.50f
        };

        [Header("Error Sound")]
        public AudioErrorConfig Error = new()
        {
            Duration = 0.25f,
            Frequency = 120f,
            Volume = 0.15f
        };

        [Header("Explosion Sound")]
        public AudioExplosionConfig Explosion = new()
        {
            Duration = 0.7f,
            Volume = 0.25f
        };

        [Header("Pole Complete (Legacy)")]
        public AudioPoleCompleteConfig PoleComplete = new()
        {
            Duration = 0.4f,
            FrequencyStart = 523.25f,
            FrequencyEnd = 1046.50f,
            Volume = 0.22f
        };

        [Header("Rich Pole Complete")]
        public AudioRichPoleCompleteConfig RichPoleComplete = new()
        {
            Duration = 0.6f,
            RingPitchFactorBase = 1f,
            RingPitchFactorPerRing = 0.06f,
            ThudLowFreq = 80f,
            ThudHighFreq = 120f,
            ThudVolume = 0.18f,
            ThudNoiseVolume = 0.04f,
            ThudDurationFraction = 0.25f,
            SweepStartFraction = 0.05f / 0.6f,
            SweepEndFraction = 0.45f / 0.6f,
            SweepFreqStart = 523.25f,
            SweepFreqEnd = 1046.50f,
            SweepVolume = 0.12f,
            SweepHarmony2Volume = 0.06f,
            SweepHarmony3Volume = 0.03f,
            SparkleStartFraction = 0.2f / 0.6f,
            SparkleEndFraction = 0.55f / 0.6f,
            SparkleFreqStart = 1800f,
            SparkleFreqEnd = 3600f,
            SparkleVolume = 0.04f,
            SparkleNoiseVolume = 0.02f,
            ReleaseStartFraction = 0.35f / 0.6f,
            ReleaseFreq = 1046.50f,
            ReleaseVolume = 0.06f,
            ReleaseHarmonyVolume = 0.03f,
            ReleaseDecayFactor = 0.15f,
            MasterVolume = 0.65f
        };

        [Header("Final Pole Fanfare")]
        public AudioFinalPoleConfig FinalPole = new()
        {
            Duration = 1.0f,
            MasterVolume = 0.5f,
            BassFreqStart = 130.81f,
            BassFreqEnd = 261.63f,
            BassVolume = 0.15f
        };

        [Header("BGM")]
        public AudioBgmConfig Bgm = new()
        {
            Duration = 6.0f,
            BaseFrequency = 220f,
            FrequencyPerWorldStep = 32.7f,
            Layer1Volume = 1.0f,
            Layer2Volume = 0.5f,
            Layer3Volume = 0.3f,
            Layer4Volume = 0.2f,
            MasterVolume = 0.04f,
            FadeBound = 0.3f
        };
    }
}
