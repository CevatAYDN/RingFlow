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

        private GameObject _backBtn, _gridGo;

        private void Awake()
        {
            if (transform.childCount == 0 || NeedsSelfBuild())
                BuildUI();
            else
                BindReferencesFromChildren();
        }

        private bool NeedsSelfBuild()
        {
            BindReferencesFromChildren();
            return LevelButtons.Count == 0 || BackButton == null;
        }

        public void BuildUI()
        {
            // Backdrop
            var bd = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
            bd.transform.SetParent(transform, false);
            bd.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            bd.GetComponent<RectTransform>().anchorMax = Vector2.one;
            bd.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            bd.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            bd.GetComponent<Image>().color = GameUIResources.BgDark;
            bd.GetComponent<Image>().raycastTarget = false;

            var cardGo = GameUIResources.CreatePanel("Card", transform);
            cardGo.GetComponent<Image>().color = GameUIResources.SurfaceDark;
            GameUIResources.SetAnchors(cardGo.GetComponent<RectTransform>(), 0.04f, 0.06f, 0.96f, 0.94f);
            CardGroup = cardGo.GetComponent<CanvasGroup>();

            // Title
            var titleGo = GameUIResources.CreateText("SELECT LEVEL", cardGo.transform, 40, TextAnchor.MiddleCenter, GameUIResources.TextOnDark);
            titleGo.name = "Title";
            TitleText = titleGo.GetComponent<Text>();
            TitleText.fontStyle = FontStyle.Bold;
            GameUIResources.SetAnchors(titleGo.GetComponent<RectTransform>(), 0.10f, 0.88f, 0.90f, 0.96f);

            // World & progress panel
            var infoPanel = GameUIResources.CreatePanel("InfoPanel", cardGo.transform);
            infoPanel.GetComponent<Image>().color = new Color(0.08f, 0.10f, 0.14f);
            GameUIResources.SetAnchors(infoPanel.GetComponent<RectTransform>(), 0.06f, 0.80f, 0.94f, 0.86f);

            var worldGo = GameUIResources.CreateText("WORLD 1", infoPanel.transform, 16, TextAnchor.MiddleLeft, GameUIResources.AccentColor);
            worldGo.name = "WorldLabel";
            WorldLabel = worldGo.GetComponent<Text>();
            GameUIResources.SetAnchors(worldGo.GetComponent<RectTransform>(), 0.04f, 0f, 0.30f, 1f);

            var progGo = GameUIResources.CreateText("0 / 0", infoPanel.transform, 16, TextAnchor.MiddleRight, GameUIResources.MutedTextDark);
            progGo.name = "ProgressLabel";
            ProgressLabel = progGo.GetComponent<Text>();
            GameUIResources.SetAnchors(progGo.GetComponent<RectTransform>(), 0.70f, 0f, 0.96f, 1f);

            var barBg = new GameObject("ProgressBarBg", typeof(RectTransform), typeof(Image));
            barBg.transform.SetParent(infoPanel.transform, false);
            GameUIResources.SetAnchors(barBg.GetComponent<RectTransform>(), 0.32f, 0.30f, 0.68f, 0.70f);
            barBg.GetComponent<Image>().color = new Color(0.20f, 0.22f, 0.28f);
            barBg.GetComponent<Image>().sprite = GameUIResources.GetRoundedSprite();
            barBg.GetComponent<Image>().type = Image.Type.Sliced;

            var barFill = new GameObject("ProgressBarFill", typeof(RectTransform), typeof(Image));
            barFill.transform.SetParent(barBg.transform, false);
            barFill.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            barFill.GetComponent<RectTransform>().anchorMax = new Vector2(0f, 1f);
            barFill.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            barFill.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            barFill.GetComponent<Image>().color = GameUIResources.AccentColor;
            barFill.GetComponent<Image>().sprite = GameUIResources.GetRoundedSprite();
            barFill.GetComponent<Image>().type = Image.Type.Sliced;
            ProgressBar = barFill.GetComponent<Image>();

            // Level grid
            _gridGo = new GameObject("LevelGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            _gridGo.transform.SetParent(cardGo.transform, false);
            var gridRect = _gridGo.GetComponent<RectTransform>();
            GameUIResources.SetAnchors(gridRect, 0.04f, 0.10f, 0.96f, 0.76f);
            var grid = _gridGo.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(130f, 58f);
            grid.spacing = new Vector2(12f, 12f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.padding = new RectOffset(8, 8, 8, 8);

            // Generate level buttons
            for (int i = 1; i <= 12; i++)
            {
                var btnGo = GameUIResources.CreateButton($"LEVEL {i}", _gridGo.transform, 130, 58);
                btnGo.name = $"Btn_Level_{i}";
                btnGo.GetComponent<RectTransform>().sizeDelta = new Vector2(130, 58);
                var btn = btnGo.GetComponent<Button>();
                LevelButtons.Add(btn);
            }

            // Back button
            var backGo = GameUIResources.CreateButton("BACK", cardGo.transform, 140, 44);
            backGo.name = "Btn_BACK";
            GameUIResources.ApplyOutlineStyle(backGo);
            BackButton = backGo.GetComponent<Button>();
            GameUIResources.SetAnchors(backGo.GetComponent<RectTransform>(), 0.04f, 0.02f, 0.20f, 0.08f);
        }

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
                    max.x, 0.4f).SetEase(DG.Tweening.Ease.OutCubic).SetAutoKill(true);
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
