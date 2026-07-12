using UnityEditor;
using UnityEngine;
using RingFlow.Gameplay;

namespace RingFlow.Editor
{
    /// <summary>
    /// Inspector editor for <see cref="AudioConfigSO"/> so procedural audio tuning
    /// is editable from the dashboard instead of being an orphaned, uneditable asset.
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

            config.SampleRate = Mathf.Max(8000, EditorGUILayout.IntField("Örnekleme Oranı (Sample Rate)", config.SampleRate));

            config.Move = DrawMove("Hareket Sesi (Move)", config.Move);
            config.Win = DrawWin("Kazanma Sesi (Win)", config.Win);
            config.Error = DrawError("Hata Sesi (Error)", config.Error);
            config.Explosion = DrawExplosion("Patlama Sesi (Explosion)", config.Explosion);
            config.PoleComplete = DrawPoleComplete("Direk Tamamlama (Pole Complete)", config.PoleComplete);
            config.RichPoleComplete = DrawRichPoleComplete("Zengin Direk Tamamlama (Rich Pole Complete)", config.RichPoleComplete);
            config.FinalPole = DrawFinalPole("Son Direk (Final Pole)", config.FinalPole);
            config.Bgm = DrawBgm("Arka Plan Müziği (BGM)", config.Bgm);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(config);
            }
        }

        private static AudioMoveConfig DrawMove(string title, AudioMoveConfig s)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            s.Duration = F("Süre (sn)", s.Duration);
            s.FrequencyStart = F("Başlangıç Frekansı (Hz)", s.FrequencyStart);
            s.FrequencyEnd = F("Bitiş Frekansı (Hz)", s.FrequencyEnd);
            s.Volume = F("Ses Seviyesi", s.Volume);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6f);
            return s;
        }

        private static AudioWinConfig DrawWin(string title, AudioWinConfig s)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            s.Duration = F("Süre (sn)", s.Duration);
            s.Volume = F("Ses Seviyesi", s.Volume);
            s.NoteC5 = F("Nota C5 (Hz)", s.NoteC5);
            s.NoteE5 = F("Nota E5 (Hz)", s.NoteE5);
            s.NoteG5 = F("Nota G5 (Hz)", s.NoteG5);
            s.NoteC6 = F("Nota C6 (Hz)", s.NoteC6);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6f);
            return s;
        }

        private static AudioErrorConfig DrawError(string title, AudioErrorConfig s)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            s.Duration = F("Süre (sn)", s.Duration);
            s.Frequency = F("Frekans (Hz)", s.Frequency);
            s.Volume = F("Ses Seviyesi", s.Volume);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6f);
            return s;
        }

        private static AudioExplosionConfig DrawExplosion(string title, AudioExplosionConfig s)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            s.Duration = F("Süre (sn)", s.Duration);
            s.Volume = F("Ses Seviyesi", s.Volume);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6f);
            return s;
        }

        private static AudioPoleCompleteConfig DrawPoleComplete(string title, AudioPoleCompleteConfig s)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            s.Duration = F("Süre (sn)", s.Duration);
            s.FrequencyStart = F("Başlangıç Frekansı (Hz)", s.FrequencyStart);
            s.FrequencyEnd = F("Bitiş Frekansı (Hz)", s.FrequencyEnd);
            s.Volume = F("Ses Seviyesi", s.Volume);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6f);
            return s;
        }

        private static AudioRichPoleCompleteConfig DrawRichPoleComplete(string title, AudioRichPoleCompleteConfig s)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            s.Duration = F("Süre (sn)", s.Duration);
            s.RingPitchFactorBase = F("Halka Perde Tabanı", s.RingPitchFactorBase);
            s.RingPitchFactorPerRing = F("Halka Başına Perde", s.RingPitchFactorPerRing);
            s.ThudLowFreq = F("Gümbürtü Düşük Frekans", s.ThudLowFreq);
            s.ThudHighFreq = F("Gümbürtü Yüksek Frekans", s.ThudHighFreq);
            s.ThudVolume = F("Gümbürtü Ses Seviyesi", s.ThudVolume);
            s.ThudNoiseVolume = F("Gümbürtü Gürültü Seviyesi", s.ThudNoiseVolume);
            s.ThudDurationFraction = F("Gümbürtü Süre Oranı", s.ThudDurationFraction);
            s.SweepStartFraction = F("Süpürme Başlangıç Oranı", s.SweepStartFraction);
            s.SweepEndFraction = F("Süpürme Bitiş Oranı", s.SweepEndFraction);
            s.SweepFreqStart = F("Süpürme Başlangıç Frekansı", s.SweepFreqStart);
            s.SweepFreqEnd = F("Süpürme Bitiş Frekansı", s.SweepFreqEnd);
            s.SweepVolume = F("Süpürme Ses Seviyesi", s.SweepVolume);
            s.SweepHarmony2Volume = F("Süpürme 2. Harmonik", s.SweepHarmony2Volume);
            s.SweepHarmony3Volume = F("Süpürme 3. Harmonik", s.SweepHarmony3Volume);
            s.SparkleStartFraction = F("Kıvılcım Başlangıç Oranı", s.SparkleStartFraction);
            s.SparkleEndFraction = F("Kıvılcım Bitiş Oranı", s.SparkleEndFraction);
            s.SparkleFreqStart = F("Kıvılcım Başlangıç Frekansı", s.SparkleFreqStart);
            s.SparkleFreqEnd = F("Kıvılcım Bitiş Frekansı", s.SparkleFreqEnd);
            s.SparkleVolume = F("Kıvılcım Ses Seviyesi", s.SparkleVolume);
            s.SparkleNoiseVolume = F("Kıvılcım Gürültü Seviyesi", s.SparkleNoiseVolume);
            s.ReleaseStartFraction = F("Bırakma Başlangıç Oranı", s.ReleaseStartFraction);
            s.ReleaseFreq = F("Bırakma Frekansı", s.ReleaseFreq);
            s.ReleaseVolume = F("Bırakma Ses Seviyesi", s.ReleaseVolume);
            s.ReleaseHarmonyVolume = F("Bırakma Harmonik Seviyesi", s.ReleaseHarmonyVolume);
            s.ReleaseDecayFactor = F("Bırakma Sönüm Faktörü", s.ReleaseDecayFactor);
            s.MasterVolume = F("Ana Ses Seviyesi", s.MasterVolume);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6f);
            return s;
        }

        private static AudioFinalPoleConfig DrawFinalPole(string title, AudioFinalPoleConfig s)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            s.Duration = F("Süre (sn)", s.Duration);
            s.MasterVolume = F("Ana Ses Seviyesi", s.MasterVolume);
            s.BassFreqStart = F("Bas Başlangıç Frekansı", s.BassFreqStart);
            s.BassFreqEnd = F("Bas Bitiş Frekansı", s.BassFreqEnd);
            s.BassVolume = F("Bas Ses Seviyesi", s.BassVolume);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6f);
            return s;
        }

        private static AudioBgmConfig DrawBgm(string title, AudioBgmConfig s)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            s.Duration = F("Süre (sn)", s.Duration);
            s.BaseFrequency = F("Temel Frekans (Hz)", s.BaseFrequency);
            s.FrequencyPerWorldStep = F("Dünya Adımı Başına Frekans", s.FrequencyPerWorldStep);
            s.Layer1Volume = F("Katman 1 Ses Seviyesi", s.Layer1Volume);
            s.Layer2Volume = F("Katman 2 Ses Seviyesi", s.Layer2Volume);
            s.Layer3Volume = F("Katman 3 Ses Seviyesi", s.Layer3Volume);
            s.Layer4Volume = F("Katman 4 Ses Seviyesi", s.Layer4Volume);
            s.MasterVolume = F("Ana Ses Seviyesi", s.MasterVolume);
            s.FadeBound = F("Solma Sınırı", s.FadeBound);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6f);
            return s;
        }

        private static float F(string label, float value)
        {
            return EditorGUILayout.FloatField(label, value);
        }
    }
}
