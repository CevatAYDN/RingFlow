using System;
using UnityEngine;
using UnityEngine.UI;
using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay.UI
{
    /// <summary>
    /// GDD §14 — GDPR / KVKK and COPPA Parental Gate Popup.
    /// Requires verification (e.g., multiplication check) before accepting terms.
    /// </summary>
    [Mediator(typeof(ParentalGatePopupMediator))]
    public class ParentalGatePopupView : View
    {
        public Button AcceptButton;
        public Button TermsButton;
        public Button PrivacyButton;
        
        public InputField AgeInputField;
        public Text QuestionText;
        public Text ErrorText;
        
        private int _num1;
        private int _num2;
        private int _correctAnswer;

        private void Awake()
        {
            if (AcceptButton == null)
            {
                BuildUIDynamically();
            }
        }

        private void BuildUIDynamically()
        {
            // 1. Container Card
            var cardGo = new GameObject("Card", typeof(RectTransform), typeof(Image));
            var cardRect = cardGo.GetComponent<RectTransform>();
            cardRect.SetParent(transform, false);
            cardRect.sizeDelta = new Vector2(340f, 420f);
            cardGo.GetComponent<Image>().color = GameUIResources.SurfaceColor;

            // Title Text
            var titleGo = GameUIResources.CreateText("PARENTAL GATE", cardGo.transform, 22, TextAnchor.MiddleCenter, Color.white);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(300f, 40f);
            titleRect.anchoredPosition = new Vector2(0f, 160f);
            titleGo.GetComponent<Text>().fontStyle = FontStyle.Bold;

            // Info Text
            var infoGo = GameUIResources.CreateText("Ask a parent to solve the math puzzle to continue.", cardGo.transform, 14, TextAnchor.MiddleCenter, GameUIResources.MutedText);
            var infoRect = infoGo.GetComponent<RectTransform>();
            infoRect.sizeDelta = new Vector2(300f, 40f);
            infoRect.anchoredPosition = new Vector2(0f, 120f);

            // Question Text
            var questionGo = GameUIResources.CreateText("Question", cardGo.transform, 18, TextAnchor.MiddleCenter, GameUIResources.AccentColor);
            var questionRect = questionGo.GetComponent<RectTransform>();
            questionRect.sizeDelta = new Vector2(300f, 40f);
            questionRect.anchoredPosition = new Vector2(0f, 60f);
            QuestionText = questionGo.GetComponent<Text>();
            QuestionText.fontStyle = FontStyle.Bold;

            // Input Field Background
            var inputBgGo = new GameObject("InputFieldBg", typeof(RectTransform), typeof(Image));
            var inputBgRect = inputBgGo.GetComponent<RectTransform>();
            inputBgRect.SetParent(cardGo.transform, false);
            inputBgRect.sizeDelta = new Vector2(200f, 44f);
            inputBgRect.anchoredPosition = new Vector2(0f, 0f);
            inputBgGo.GetComponent<Image>().color = GameUIResources.PanelColor;

            // Input Text
            var inputTextGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            inputTextGo.transform.SetParent(inputBgGo.transform, false);
            var inputTextRect = inputTextGo.GetComponent<RectTransform>();
            inputTextRect.anchorMin = new Vector2(0.05f, 0.05f);
            inputTextRect.anchorMax = new Vector2(0.95f, 0.95f);
            inputTextRect.offsetMin = Vector2.zero;
            inputTextRect.offsetMax = Vector2.zero;
            var inputText = inputTextGo.GetComponent<Text>();
            inputText.font = GameUIResources.GetFont();
            inputText.fontSize = 18;
            inputText.color = Color.white;
            inputText.alignment = TextAnchor.MiddleLeft;
            inputText.supportRichText = false;

            // Placeholder Text
            var placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            placeholderGo.transform.SetParent(inputBgGo.transform, false);
            var placeholderRect = placeholderGo.GetComponent<RectTransform>();
            placeholderRect.anchorMin = new Vector2(0.05f, 0.05f);
            placeholderRect.anchorMax = new Vector2(0.95f, 0.95f);
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;
            var placeholderText = placeholderGo.GetComponent<Text>();
            placeholderText.font = GameUIResources.GetFont();
            placeholderText.fontSize = 18;
            placeholderText.color = GameUIResources.MutedText;
            placeholderText.text = "Enter answer...";
            placeholderText.alignment = TextAnchor.MiddleLeft;

            // InputField Component
            var inputField = inputBgGo.AddComponent<InputField>();
            inputField.textComponent = inputText;
            inputField.placeholder = placeholderText;
            inputField.contentType = InputField.ContentType.IntegerNumber;
            AgeInputField = inputField;

            // Error Text
            var errorGo = GameUIResources.CreateText("", cardGo.transform, 14, TextAnchor.MiddleCenter, GameUIResources.DangerColor);
            var errorRect = errorGo.GetComponent<RectTransform>();
            errorRect.sizeDelta = new Vector2(300f, 30f);
            errorRect.anchoredPosition = new Vector2(0f, -40f);
            ErrorText = errorGo.GetComponent<Text>();

            // Accept Button
            var acceptGo = GameUIResources.CreateButton("Accept", cardGo.transform, 240f, 44f);
            var acceptRect = acceptGo.GetComponent<RectTransform>();
            acceptRect.anchoredPosition = new Vector2(0f, -90f);
            AcceptButton = acceptGo.GetComponent<Button>();

            // Terms Button
            var termsGo = GameUIResources.CreateButton("Terms of Service", cardGo.transform, 160f, 30f);
            var termsRect = termsGo.GetComponent<RectTransform>();
            termsRect.anchoredPosition = new Vector2(-85f, -150f);
            GameUIResources.ApplyOutlineStyle(termsGo);
            termsGo.GetComponentInChildren<Text>().fontSize = 12;
            TermsButton = termsGo.GetComponent<Button>();

            // Privacy Button
            var privacyGo = GameUIResources.CreateButton("Privacy Policy", cardGo.transform, 160f, 30f);
            var privacyRect = privacyGo.GetComponent<RectTransform>();
            privacyRect.anchoredPosition = new Vector2(85f, -150f);
            GameUIResources.ApplyOutlineStyle(privacyGo);
            privacyGo.GetComponentInChildren<Text>().fontSize = 12;
            PrivacyButton = privacyGo.GetComponent<Button>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            GenerateQuestion();
            if (ErrorText != null) ErrorText.text = "";
            if (AgeInputField != null) AgeInputField.text = "";
        }

        private void GenerateQuestion()
        {
            var rand = new System.Random();
            _num1 = rand.Next(3, 9);
            _num2 = rand.Next(4, 9);
            _correctAnswer = _num1 * _num2;
            
            if (QuestionText != null)
            {
                QuestionText.text = $"Parental Verification: {_num1} x {_num2} = ?";
            }
        }

        public bool ValidateAnswer()
        {
            if (AgeInputField == null) return true;
            
            if (int.TryParse(AgeInputField.text, out int answer))
            {
                if (answer == _correctAnswer)
                {
                    return true;
                }
            }
            
            if (ErrorText != null)
            {
                ErrorText.text = "Incorrect answer. Verification failed.";
            }
            GenerateQuestion();
            return false;
        }

        public void Localize(ILocalizationService loc)
        {
            // Future localization support
        }
    }
}
