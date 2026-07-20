using System.Collections.Generic;
using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace RingFlow.Gameplay.UI
{
    [Mediator(typeof(LevelSelectMediator))]
    public class LevelSelectView : View, IAuthoredView
    {
        public List<Button> LevelButtons { get; } = new();
        public Button BackButton { get; private set; }
        public Text TitleText { get; private set; }
        public Text WorldLabel { get; private set; }
        public Text ProgressLabel { get; private set; }
        public Image ProgressBar { get; private set; }
        public CanvasGroup CardGroup { get; private set; }

        private void Awake()
        {
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
            var label = btn.GetComponentInChildren<Text>();

            if (unlocked)
            {
                img.color = GameUIResources.PrimaryColor;
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
                    label.text = "🔒";
                    label.fontSize = 20;
                    label.color = GameUIResources.MutedTextDark;
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
                    BackButton = button;
                }
                else if (button.name.ToUpperInvariant().Contains("LEVEL"))
                {
                    LevelButtons.Add(button);
                }
            }

            var texts = GetComponentsInChildren<Text>(true);
            foreach (var txt in texts)
            {
                var upper = txt.name.ToUpperInvariant();
                if (upper.Contains("TITLE")) TitleText = txt;
                else if (upper.Contains("WORLD")) WorldLabel = txt;
                else if (upper.Contains("PROGRESS")) ProgressLabel = txt;
            }
        }
    }
}
