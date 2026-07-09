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
            BindReferencesFromChildren();
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

        private void BindReferencesFromChildren()
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                if (btn.name.ToUpper().Contains("ACCEPT")) AcceptButton = btn;
                else if (btn.name.ToUpper().Contains("TERMS")) TermsButton = btn;
                else if (btn.name.ToUpper().Contains("PRIVACY")) PrivacyButton = btn;
            }

            AgeInputField = GetComponentInChildren<InputField>(true);

            var texts = GetComponentsInChildren<Text>(true);
            foreach (var txt in texts)
            {
                if (txt.name.ToUpper().Contains("QUESTION")) QuestionText = txt;
                else if (txt.name.ToUpper().Contains("ERROR")) ErrorText = txt;
            }
        }
    }
}
