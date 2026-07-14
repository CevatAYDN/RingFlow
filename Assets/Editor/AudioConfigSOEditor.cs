using UnityEditor;
using UnityEngine;
using RingFlow.Gameplay;

namespace RingFlow.Editor
{
    /// <summary>
    /// Inspector editor for <see cref="AudioConfigSO"/> providing polished sliders
    /// for volume tuning, frequency ranges, and category layouts.
    /// </summary>
    [CustomEditor(typeof(AudioConfigSO))]
    public sealed class AudioConfigSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var config = (AudioConfigSO)target;

            EditorGUILayout.LabelField("Ses Yapılandırması (Audio Config)", RingFlowEditorUtils.HeaderStyle);
            EditorGUILayout.Space(4f);

            EditorGUI.BeginChangeCheck();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                config.SampleRate = Mathf.Clamp(EditorGUILayout.IntField("Örnekleme Oranı (Sample Rate)", config.SampleRate), 8000, 48000);
            }
            EditorGUILayout.Space(4f);

            config.Move = DrawMove("Hareket Sesi (Move)", config.Move);
            config.Win = DrawWin("Kazanma Sesi (Win)", config.Win);
            config.Error = DrawError("Hata Sesi (Error)", config.Error);
            config.Explosion = DrawExplosion("Patlama Sesi (Explosion)", config.Explosion);
            config.PoleComplete = DrawPoleComplete("Direk Tamamlama (Pole Complete)", config.PoleComplete);
            config.RichPoleComplete = DrawRichPoleComplete("Zengin Direk Tamamlama", config.RichPoleComplete);
            config.FinalPole = DrawFinalPole("Son Direk (Final Pole)", config.FinalPole);
            config.Bgm = DrawBgm("Arka Plan Müziği (BGM)", config.Bgm);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(config);
            }
        }

        private static AudioMoveConfig DrawMove(string title, AudioMoveConfig s)
        {
            RingFlowEditorUtils.SectionTitle(title);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                s.Duration = F("Süre (sn)", s.Duration);
                s.FrequencyStart = F("Başlangıç Frekansı (Hz)", s.FrequencyStart);
                s.FrequencyEnd = F("Bitiş Frekansı (Hz)", s.FrequencyEnd);
                s.Volume = Slider("Ses Seviyesi (Volume)", s.Volume, 0f, 1f);
            }
            EditorGUILayout.Space(6f);
            return s;
        }

        private static AudioWinConfig DrawWin(string title, AudioWinConfig s)
        {
            RingFlowEditorUtils.SectionTitle(title);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                s.Duration = F("Süre (sn)", s.Duration);
                s.Volume = Slider("Ses Seviyesi (Volume)", s.Volume, 0f, 1f);
                s.NoteC5 = F("Nota C5 (Hz)", s.NoteC5);
                s.NoteE5 = F("Nota E5 (Hz)", s.NoteE5);
                s.NoteG5 = F("Nota G5 (Hz)", s.NoteG5);
                s.NoteC6 = F("Nota C6 (Hz)", s.NoteC6);
            }
            EditorGUILayout.Space(6f);
            return s;
        }

        private static AudioErrorConfig DrawError(string title, AudioErrorConfig s)
        {
            RingFlowEditorUtils.SectionTitle(title);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                s.Duration = F("Süre (sn)", s.Duration);
                s.Frequency = F("Frekans (Hz)", s.Frequency);
                s.Volume = Slider("Ses Seviyesi (Volume)", s.Volume, 0f, 1f);
            }
            EditorGUILayout.Space(6f);
            return s;
        }

        private static AudioExplosionConfig DrawExplosion(string title, AudioExplosionConfig s)
        {
            RingFlowEditorUtils.SectionTitle(title);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                s.Duration = F("Süre (sn)", s.Duration);
                s.Volume = Slider("Ses Seviyesi (Volume)", s.Volume, 0f, 1f);
            }
            EditorGUILayout.Space(6f);
            return s;
        }

        private static AudioPoleCompleteConfig DrawPoleComplete(string title, AudioPoleCompleteConfig s)
        {
            RingFlowEditorUtils.SectionTitle(title);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                s.Duration = F("Süre (sn)", s.Duration);
                s.FrequencyStart = F("Başlangıç Frekansı (Hz)", s.FrequencyStart);
                s.FrequencyEnd = F("Bitiş Frekansı (Hz)", s.FrequencyEnd);
                s.Volume = Slider("Ses Seviyesi (Volume)", s.Volume, 0f, 1f);
            }
            EditorGUILayout.Space(6f);
            return s;
        }

        private static AudioRichPoleCompleteConfig DrawRichPoleComplete(string title, AudioRichPoleCompleteConfig s)
        {
            RingFlowEditorUtils.SectionTitle(title);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                s.Duration = F("Süre (sn)", s.Duration);
                s.RingPitchFactorBase = F("Halka Perde Tabanı", s.RingPitchFactorBase);
                s.RingPitchFactorPerRing = F("Halka Başına Perde", s.RingPitchFactorPerRing);
                
                EditorGUILayout.LabelField("Gümbürtü Ayarları (Thud)", EditorStyles.miniBoldLabel);
                s.ThudLowFreq = F("G. Düşük Frekans", s.ThudLowFreq);
                s.ThudHighFreq = F("G. Yüksek Frekans", s.ThudHighFreq);
                s.ThudVolume = Slider("G. Ses Seviyesi", s.ThudVolume, 0f, 1f);
                s.ThudNoiseVolume = Slider("G. Gürültü Seviyesi", s.ThudNoiseVolume, 0f, 1f);
                s.ThudDurationFraction = Slider("G. Süre Oranı", s.ThudDurationFraction, 0f, 1f);

                EditorGUILayout.LabelField("Süpürme Ayarları (Sweep)", EditorStyles.miniBoldLabel);
                s.SweepStartFraction = Slider("S. Başlangıç Oranı", s.SweepStartFraction, 0f, 1f);
                s.SweepEndFraction = Slider("S. Bitiş Oranı", s.SweepEndFraction, 0f, 1f);
                s.SweepFreqStart = F("S. Başlangıç Frekansı", s.SweepFreqStart);
                s.SweepFreqEnd = F("S. Bitiş Frekansı", s.SweepFreqEnd);
                s.SweepVolume = Slider("S. Ses Seviyesi", s.SweepVolume, 0f, 1f);
                s.SweepHarmony2Volume = Slider("S. 2. Harmonik Ses", s.SweepHarmony2Volume, 0f, 1f);
                s.SweepHarmony3Volume = Slider("S. 3. Harmonik Ses", s.SweepHarmony3Volume, 0f, 1f);

                EditorGUILayout.LabelField("Kıvılcım Ayarları (Sparkle)", EditorStyles.miniBoldLabel);
                s.SparkleStartFraction = Slider("K. Başlangıç Oranı", s.SparkleStartFraction, 0f, 1f);
                s.SparkleEndFraction = Slider("K. Bitiş Oranı", s.SparkleEndFraction, 0f, 1f);
                s.SparkleFreqStart = F("K. Başlangıç Frekansı", s.SparkleFreqStart);
                s.SparkleFreqEnd = F("K. Bitiş Frekansı", s.SparkleFreqEnd);
                s.SparkleVolume = Slider("K. Ses Seviyesi", s.SparkleVolume, 0f, 1f);
                s.SparkleNoiseVolume = Slider("K. Gürültü Seviyesi", s.SparkleNoiseVolume, 0f, 1f);

                EditorGUILayout.LabelField("Bırakma Ayarları (Release)", EditorStyles.miniBoldLabel);
                s.ReleaseStartFraction = Slider("B. Başlangıç Oranı", s.ReleaseStartFraction, 0f, 1f);
                s.ReleaseFreq = F("B. Frekansı", s.ReleaseFreq);
                s.ReleaseVolume = Slider("B. Ses Seviyesi", s.ReleaseVolume, 0f, 1f);
                s.ReleaseHarmonyVolume = Slider("B. Harmonik Seviyesi", s.ReleaseHarmonyVolume, 0f, 1f);
                s.ReleaseDecayFactor = F("B. Sönüm Faktörü", s.ReleaseDecayFactor);

                s.MasterVolume = Slider("Ana Ses Seviyesi (Master)", s.MasterVolume, 0f, 1f);
            }
            EditorGUILayout.Space(6f);
            return s;
        }

        private static AudioFinalPoleConfig DrawFinalPole(string title, AudioFinalPoleConfig s)
        {
            RingFlowEditorUtils.SectionTitle(title);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                s.Duration = F("Süre (sn)", s.Duration);
                s.BassFreqStart = F("Bas Başlangıç Frekansı", s.BassFreqStart);
                s.BassFreqEnd = F("Bas Bitiş Frekansı", s.BassFreqEnd);
                s.BassVolume = Slider("Bas Ses Seviyesi", s.BassVolume, 0f, 1f);
                s.MasterVolume = Slider("Ana Ses Seviyesi (Master)", s.MasterVolume, 0f, 1f);
            }
            EditorGUILayout.Space(6f);
            return s;
        }

        private static AudioBgmConfig DrawBgm(string title, AudioBgmConfig s)
        {
            RingFlowEditorUtils.SectionTitle(title);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                s.Duration = F("Süre (sn)", s.Duration);
                s.BaseFrequency = F("Temel Frekans (Hz)", s.BaseFrequency);
                s.FrequencyPerWorldStep = F("Dünya Adımı Başına Frekans", s.FrequencyPerWorldStep);
                s.Layer1Volume = Slider("Katman 1 Ses Seviyesi", s.Layer1Volume, 0f, 1f);
                s.Layer2Volume = Slider("Katman 2 Ses Seviyesi", s.Layer2Volume, 0f, 1f);
                s.Layer3Volume = Slider("Katman 3 Ses Seviyesi", s.Layer3Volume, 0f, 1f);
                s.Layer4Volume = Slider("Katman 4 Ses Seviyesi", s.Layer4Volume, 0f, 1f);
                s.MasterVolume = Slider("Ana Ses Seviyesi (Master)", s.MasterVolume, 0f, 1f);
                s.FadeBound = Slider("Solma Sınırı (Fade Bound)", s.FadeBound, 0f, 1f);
            }
            EditorGUILayout.Space(6f);
            return s;
        }

        private static float F(string label, float value)
        {
            return EditorGUILayout.FloatField(label, value);
        }

        private static float Slider(string label, float value, float min, float max)
        {
            return EditorGUILayout.Slider(label, value, min, max);
        }
    }
}
