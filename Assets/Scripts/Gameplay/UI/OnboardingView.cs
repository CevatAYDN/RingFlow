using System;
using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace RingFlow.Gameplay.UI
{
    /// <summary>
    /// First-launch tutorial overlay (GDD §14). Walks new players through 4 short
    /// steps: welcome, sort-rings demo, special rings, ready. Skippable. Marks
    /// completion via the mediator so it only shows once per install.
    /// </summary>
    [Mediator(typeof(OnboardingMediator))]
    public class OnboardingView : View, IAuthoredView
    {
        // Step content. Title/body keys flow through ILocalizationService; the
        // fallback strings here are English-only and only render if loc is null.
        private struct OnboardingStep
        {
            public readonly string TitleKey;
            public readonly string BodyKey;
            public readonly string TitleFallback;
            public readonly string BodyFallback;

            public OnboardingStep(string titleKey, string bodyKey, string titleFallback, string bodyFallback)
            {
                TitleKey = titleKey;
                BodyKey = bodyKey;
                TitleFallback = titleFallback;
                BodyFallback = bodyFallback;
            }
        }

        private static readonly OnboardingStep[] Steps =
        {
            new OnboardingStep("onboarding_step1_title", "onboarding_step1_body", "", ""),
            new OnboardingStep("onboarding_step2_title", "onboarding_step2_body", "", ""),
            new OnboardingStep("onboarding_step3_title", "onboarding_step3_body", "", ""),
            new OnboardingStep("onboarding_step4_title", "onboarding_step4_body", "", "")
        };

        public event Action NextClicked;
        public event Action SkipClicked;

        private Text _titleText;
        private Text _bodyText;
        private GameObject[] _dots;
        private Button _nextButton;
        private Button _skipButton;
        private Text _nextLabel;
        private CanvasGroup _cardFader;
        private GameObject _root;

        private int _currentIndex;
        private bool _reduceMotion;

        private void Awake()
        {
            BindReferencesFromChildren();
        }

        /// <summary>
        /// Satisfies IAuthoredView. UI hierarchy is now loaded from prefab;
        /// this method is kept for interface compliance (Editor UI Studio tooling).
        /// </summary>
        /// <summary>
        /// Satisfies IAuthoredView. UI hierarchy is now loaded from prefab;
        /// this method is kept for interface compliance (Editor UI Studio tooling).
        /// </summary>
        public void BuildUI() { }

        public void Configure(bool reduceMotion)
        {
            _reduceMotion = reduceMotion;
        }

        public void Localize(ILocalizationService loc)
        {
            if (loc == null) return;
            RenderStep(_currentIndex, loc);
            if (_nextLabel != null)
            {
                string key = _currentIndex >= Steps.Length - 1 ? "onboarding_start" : "onboarding_next";
                _nextLabel.text = loc.GetString(key, _nextLabel.text);
            }
            if (_skipButton != null)
            {
                GameUIResources.LocalizeButtonText(_skipButton.gameObject, "onboarding_skip", loc);
            }
        }

        public void ShowStep(int index, ILocalizationService loc)
        {
            _currentIndex = UnityEngine.Mathf.Clamp(index, 0, Steps.Length - 1);
            UpdateDots();
            if (_nextLabel != null && loc != null)
            {
                string key = _currentIndex >= Steps.Length - 1 ? "onboarding_start" : "onboarding_next";
                _nextLabel.text = loc.GetString(key, _currentIndex >= Steps.Length - 1 ? "START" : "NEXT");
            }
            AnimateStepTransition(loc);
        }

        private void AnimateStepTransition(ILocalizationService loc)
        {
            if (_root == null) return;
            DOTween.Kill(_cardFader);
            DOTween.Kill(_root.transform);

            if (_reduceMotion)
            {
                RenderStep(_currentIndex, loc);
                return;
            }

            _cardFader.alpha = 0.15f;
            _root.transform.localScale = Vector3.one * 0.96f;
            RenderStep(_currentIndex, loc);
            DOTween.To(() => _cardFader.alpha, v => _cardFader.alpha = v, 1f, 0.28f)
                .SetEase(DG.Tweening.Ease.OutCubic).SetTarget(_cardFader);
            _root.transform.DOScale(Vector3.one, 0.32f)
                .SetEase(DG.Tweening.Ease.OutBack).SetAutoKill(true);
        }

        private void RenderStep(int index, ILocalizationService loc)
        {
            var step = Steps[UnityEngine.Mathf.Clamp(index, 0, Steps.Length - 1)];
            if (_titleText != null)
            {
                _titleText.text = loc != null ? loc.GetString(step.TitleKey, step.TitleFallback) : step.TitleFallback;
            }
            if (_bodyText != null)
            {
                _bodyText.text = loc != null ? loc.GetString(step.BodyKey, step.BodyFallback) : step.BodyFallback;
            }
        }

        private void UpdateDots()
        {
            if (_dots == null) return;
            for (int i = 0; i < _dots.Length; i++)
            {
                if (_dots[i] == null) continue;
                var img = _dots[i].GetComponent<Image>();
                if (img != null)
                    img.color = i == _currentIndex ? GameUIResources.AccentColor : GameUIResources.DisabledText;
            }
        }



        private void OnNextPressed()
        {
            if (_currentIndex < Steps.Length - 1)
            {
                NextClicked?.Invoke();
            }
            else
            {
                _skipButton.interactable = false;
                _nextButton.interactable = false;
                SkipClicked?.Invoke();
            }
        }

        private void OnSkipPressed()
        {
            _skipButton.interactable = false;
            _nextButton.interactable = false;
            SkipClicked?.Invoke();
        }

        private void BindReferencesFromChildren()
        {
            var texts = GetComponentsInChildren<Text>(true);
            foreach (var txt in texts)
            {
                var upper = txt.name.ToUpperInvariant();
                if (upper.Contains("TITLE")) _titleText = txt;
                else if (upper.Contains("BODY")) _bodyText = txt;
                else if (upper.Contains("NEXT") && txt.transform.parent.name.ToUpperInvariant().Contains("NEXTBUTTON"))
                    _nextLabel = txt;
            }
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                GameUIResources.AddButtonEffects(btn);
                var upper = btn.name.ToUpperInvariant();
                if (upper.Contains("NEXT")) _nextButton = btn;
                else if (upper.Contains("SKIP")) _skipButton = btn;
            }
            var rootCandidate = transform.Find("OnboardingRoot");
            if (rootCandidate != null)
            {
                _root = rootCandidate.gameObject;
                _cardFader = _root.GetComponent<CanvasGroup>();
                if (_cardFader == null)
                    _cardFader = _root.AddComponent<CanvasGroup>();
            }
            _dots = new GameObject[Steps.Length];
            for (int i = 0; i < Steps.Length; i++)
            {
                var candidate = transform.Find($"OnboardingRoot/ProgressBar/Dot{i}");
                if (candidate != null) _dots[i] = candidate.gameObject;
            }

            if (_nextButton != null)
            {
                _nextButton.onClick.RemoveListener(OnNextPressed);
                _nextButton.onClick.AddListener(OnNextPressed);
            }
            if (_skipButton != null)
            {
                _skipButton.onClick.RemoveListener(OnSkipPressed);
                _skipButton.onClick.AddListener(OnSkipPressed);
            }
        }
    }
}
