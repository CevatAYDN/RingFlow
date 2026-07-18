using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace RingFlow.Gameplay.UI
{
    [Mediator(typeof(HUDMediator))]
    public class HUDView : View, IAuthoredView
    {
        public Text MovesText { get; private set; }
        public Text LevelText { get; private set; }
        public Text CoinsText { get; private set; }
        public Text DiamondsText { get; private set; }
        public Text TimerText { get; private set; }
        public Button UndoButton { get; private set; }
        public Button RestartButton { get; private set; }
        public Button PauseButton { get; private set; }
        public Button HintButton { get; private set; }
        public Image MovesIcon { get; private set; }
        public CanvasGroup HudGroup { get; private set; }

        private GameObject _undoBtn, _restartBtn, _hintBtn, _pauseBtn, _topBar;

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
            return MovesText == null;
        }

        public void BuildUI()
        {
            // Transparent full-screen backdrop (allows touch passthrough)
            var bd = GetComponent<Image>();
            if (bd != null)
            {
                bd.color = Color.clear;
                bd.raycastTarget = false;
            }

            HudGroup = gameObject.AddComponent<CanvasGroup>();

            // Top bar
            _topBar = GameUIResources.CreatePanel("TopBar", transform);
            _topBar.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.12f, 0.88f);
            GameUIResources.SetAnchors(_topBar.GetComponent<RectTransform>(), 0f, 0.88f, 1f, 0.98f);

            // Level
            var lvlGo = GameUIResources.CreateText("Level 1", _topBar.transform, 18, TextAnchor.MiddleLeft, GameUIResources.TextOnDark);
            lvlGo.name = "LevelText";
            LevelText = lvlGo.GetComponent<Text>();
            LevelText.fontStyle = FontStyle.Bold;
            GameUIResources.SetAnchors(lvlGo.GetComponent<RectTransform>(), 0.04f, 0f, 0.22f, 1f);

            // Moves
            var movesGo = GameUIResources.CreateText("Moves: 0", _topBar.transform, 16, TextAnchor.MiddleLeft, GameUIResources.MutedTextDark);
            movesGo.name = "MovesText";
            MovesText = movesGo.GetComponent<Text>();
            GameUIResources.SetAnchors(movesGo.GetComponent<RectTransform>(), 0.24f, 0f, 0.44f, 1f);

            // Timer (challenge mode)
            var timerGo = GameUIResources.CreateText("", _topBar.transform, 16, TextAnchor.MiddleCenter, GameUIResources.WarningColor);
            timerGo.name = "TimerText";
            TimerText = timerGo.GetComponent<Text>();
            GameUIResources.SetAnchors(timerGo.GetComponent<RectTransform>(), 0.44f, 0f, 0.60f, 1f);

            // Coins
            var coinsGo = GameUIResources.CreateText("🪙 0", _topBar.transform, 16, TextAnchor.MiddleRight, GameUIResources.CoinColor);
            coinsGo.name = "CoinsText";
            CoinsText = coinsGo.GetComponent<Text>();
            GameUIResources.SetAnchors(coinsGo.GetComponent<RectTransform>(), 0.62f, 0f, 0.78f, 1f);

            // Diamonds
            var gemsGo = GameUIResources.CreateText("💎 0", _topBar.transform, 16, TextAnchor.MiddleRight, GameUIResources.DiamondColor);
            gemsGo.name = "DiamondsText";
            DiamondsText = gemsGo.GetComponent<Text>();
            GameUIResources.SetAnchors(gemsGo.GetComponent<RectTransform>(), 0.80f, 0f, 0.96f, 1f);

            // Bottom action bar
            var botBar = GameUIResources.CreatePanel("ActionBar", transform);
            botBar.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.12f, 0.88f);
            GameUIResources.SetAnchors(botBar.GetComponent<RectTransform>(), 0f, 0.02f, 1f, 0.12f);

            float btnW = 120f;
            float btnH = 44f;

            _undoBtn = CreateActionButton(botBar.transform, "Btn_UNDO", "↩", btnW, btnH, new Vector2(0.05f, 0.5f));
            UndoButton = _undoBtn.GetComponent<Button>();

            _restartBtn = CreateActionButton(botBar.transform, "Btn_RESTART", "↻", btnW, btnH, new Vector2(0.28f, 0.5f));
            RestartButton = _restartBtn.GetComponent<Button>();

            _hintBtn = CreateActionButton(botBar.transform, "Btn_HINT", "💡", btnW, btnH, new Vector2(0.50f, 0.5f));
            HintButton = _hintBtn.GetComponent<Button>();

            _pauseBtn = CreateActionButton(botBar.transform, "Btn_PAUSE", "⏸", btnW, btnH, new Vector2(0.72f, 0.5f));
            PauseButton = _pauseBtn.GetComponent<Button>();
        }

        private GameObject CreateActionButton(Transform parent, string name, string icon, float w, float h, Vector2 anchor)
        {
            var go = GameUIResources.CreateIconButton(icon, parent, 48f);
            go.name = name;
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor - new Vector2(w / 2160f, h / 3840f);
            rect.anchorMax = anchor + new Vector2(w / 2160f, h / 3840f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            GameUIResources.ApplyDarkStyle(go);
            return go;
        }

        public void UpdateMoves(int moves, ILocalizationService loc = null)
        {
            if (MovesText == null) return;
            string format = loc != null ? loc.GetString("format_moves", "Moves: {0}") : "Moves: {0}";
            MovesText.text = string.Format(format, moves);
            DOTween.Kill(MovesText.transform);
            MovesText.transform.DOPunchScale(Vector3.one * 0.15f, 0.2f, 3, 0.5f).SetAutoKill(true);
        }

        public void UpdateLevel(int level, ILocalizationService loc = null)
        {
            if (LevelText == null) return;
            string format = loc != null ? loc.GetString("format_level", "Level {0}") : "Level {0}";
            LevelText.text = string.Format(format, level);
        }

        public void UpdateCoins(int coins)
        {
            if (CoinsText != null) CoinsText.text = $"🪙 {coins:N0}";
        }

        public void UpdateDiamonds(int diamonds)
        {
            if (DiamondsText != null) DiamondsText.text = $"💎 {diamonds:N0}";
        }

        public void UpdateTimer(float remainingSecs)
        {
            if (TimerText == null) return;
            if (remainingSecs > 0)
            {
                int mins = Mathf.FloorToInt(remainingSecs / 60);
                int secs = Mathf.FloorToInt(remainingSecs % 60);
                TimerText.text = $"{mins}:{secs:D2}";
                TimerText.color = remainingSecs < 30f ? GameUIResources.DangerColor : GameUIResources.WarningColor;
            }
            else
            {
                TimerText.text = "";
            }
        }

        public void Localize(ILocalizationService loc)
        {
            if (loc == null) return;
            GameUIResources.LocalizeButtonText(_undoBtn, "game_undo", loc);
            GameUIResources.LocalizeButtonText(_restartBtn, "game_restart", loc);
            GameUIResources.LocalizeButtonText(_hintBtn, "game_hint", loc);
        }

        private void BindReferencesFromChildren()
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                GameUIResources.AddButtonEffects(btn);
                if (btn.name.ToUpper().Contains("UNDO")) { _undoBtn = btn.gameObject; UndoButton = btn; }
                else if (btn.name.ToUpper().Contains("RESTART")) { _restartBtn = btn.gameObject; RestartButton = btn; }
                else if (btn.name.ToUpper().Contains("HINT")) { _hintBtn = btn.gameObject; HintButton = btn; }
                else if (btn.name.ToUpper().Contains("PAUSE")) { _pauseBtn = btn.gameObject; PauseButton = btn; }
            }

            var texts = GetComponentsInChildren<Text>(true);
            foreach (var txt in texts)
            {
                var upper = txt.name.ToUpperInvariant();
                if (upper.Contains("MOVE")) MovesText = txt;
                else if (upper.Contains("LEVEL")) LevelText = txt;
                else if (upper.Contains("COIN")) CoinsText = txt;
                else if (upper.Contains("DIAMOND") || upper.Contains("DIA") || upper.Contains("GEM")) DiamondsText = txt;
                else if (upper.Contains("TIMER")) TimerText = txt;
            }
        }
    }
}
