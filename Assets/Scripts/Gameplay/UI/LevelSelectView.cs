using System.Collections.Generic;
using Nexus.Core;
using Nexus.Core.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace RingFlow.Gameplay.UI
{
    [Mediator(typeof(LevelSelectMediator))]
    public class LevelSelectView : View, IAuthoredView
    {
        public List<Button> LevelButtons { get; } = new();
        [SerializeField] private Button _backButton;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _worldLabel;
        [SerializeField] private TextMeshProUGUI _progressLabel;
        [SerializeField] private Image _progressBar;
        [SerializeField] private CanvasGroup _cardGroup;
        public Button BackButton => _backButton;
        public TextMeshProUGUI TitleText => _titleText;
        public TextMeshProUGUI WorldLabel => _worldLabel;
        public TextMeshProUGUI ProgressLabel => _progressLabel;
        public Image ProgressBar => _progressBar;
        public CanvasGroup CardGroup => _cardGroup;

        private void Awake()
        {
            // FIX: Always bind references — ensures LevelButtons list is populated
            // even if _backButton is already serialized on the prefab.
            BindReferencesFromChildren();
        }

        /// <summary>
        /// Satisfies IAuthoredView. UI hierarchy is now loaded from prefab;
        /// this method is kept for interface compliance (Editor UI Studio tooling).
        /// </summary>
        public void BuildUI() { }

        public void UpdateLevelButton(int index, bool unlocked, int stars = 0)
        {
            if (index < 0 || index >= LevelButtons.Count) return;
            var btn = LevelButtons[index];
            if (btn == null) return;

            btn.interactable = unlocked;
            var img = btn.GetComponent<Image>();
            var label = btn.GetComponentInChildren<TextMeshProUGUI>();

            if (unlocked)
            {
                img.color = stars > 0 ? GameUIResources.AccentColor : GameUIResources.PrimaryColor;
                if (label != null)
                {
                    label.text = $"{(index + 1)}";
                    label.color = GameUIResources.TextOnPrimary;
                }
            }
            else
            {
                img.color = new Color(0.15f, 0.17f, 0.22f);
                if (label != null)
                {
                    label.text = "\u25A0";
                    label.fontSize = 20;
                    label.color = GameUIResources.MutedTextDark;
                }
            }

            // Show star indicators if stars were earned
            if (stars > 0 && unlocked)
            {
                var starTexts = btn.GetComponentsInChildren<TextMeshProUGUI>();
                foreach (var st in starTexts)
                {
                    if (st != label && st.name.ToUpperInvariant().Contains("STAR"))
                    {
                        st.text = new string('\u2605', stars);
                        st.color = GameUIResources.WarningColor;
                    }
                }
            }
        }

        public void SetProgress(int unlocked, int total)
        {
            if (ProgressLabel != null) ProgressLabel.text = $"{unlocked} / {total}";
            if (ProgressBar != null)
            {
                var max = ProgressBar.GetComponent<RectTransform>().anchorMax;
                max.x = total > 0 ? Mathf.Clamp01((float)unlocked / total) : 0f;
                DOTween.Kill(ProgressBar.transform);
                DOTween.To(() => ProgressBar.GetComponent<RectTransform>().anchorMax.x,
                    v => { var m = ProgressBar.GetComponent<RectTransform>().anchorMax; m.x = v; ProgressBar.GetComponent<RectTransform>().anchorMax = m; },
                    max.x, 0.4f).SetEase(DG.Tweening.Ease.OutCubic).SetAutoKill(true).SetTarget(ProgressBar.transform);
            }
        }

        public void Localize(ILocalizationService loc)
        {
            if (loc == null) return;
            if (TitleText != null) GameUIResources.LocalizeText(TitleText.gameObject, "menu_select_level", loc);
            if (BackButton != null) GameUIResources.LocalizeButtonText(BackButton.gameObject, "menu_back", loc);
        }

        private void BindReferencesFromChildren()
        {
            LevelButtons.Clear();
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                GameUIResources.AddButtonEffects(button);
                if (button.name.ToUpperInvariant().Contains("BACK"))
                {
                    _backButton = button;
                }
                else if (button.name.ToUpperInvariant().Contains("LEVEL"))
                {
                    LevelButtons.Add(button);
                }
            }

            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var txt in texts)
            {
                var upper = txt.name.ToUpperInvariant();
                if (upper.Contains("TITLE")) _titleText = txt;
                else if (upper.Contains("WORLD")) _worldLabel = txt;
                else if (upper.Contains("PROGRESS")) _progressLabel = txt;
            }

            if (_progressBar == null)
            {
                var bars = GetComponentsInChildren<Image>(true);
                foreach (var img in bars)
                    if (img.name.Contains("ProgressBarFill")) _progressBar = img;
            }

            if (_cardGroup == null)
            {
                var group = GetComponent<CanvasGroup>();
                if (group == null) group = GetComponentInChildren<CanvasGroup>(true);
                _cardGroup = group;
            }
        }
    }
}
