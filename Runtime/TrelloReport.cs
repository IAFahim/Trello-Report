﻿using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Trello_Report.Runtime
{
    public class TrelloReportView : MonoBehaviour
    {
        [Header("UI Elements")] [SerializeField]
        private CanvasGroup canvasGroup;

        [SerializeField] private TMP_InputField titleInput;
        [SerializeField] private TextMeshProUGUI placeholderTitle;
        [SerializeField] private TMP_InputField descriptionInput;
        [SerializeField] private TextMeshProUGUI placeholderDescription;
        [SerializeField] private TMP_Text descriptionCharCount;
        [SerializeField] private TMP_Text errorText;
        [SerializeField] private Button bugButton;
        [SerializeField] private Button feedbackButton;
        [SerializeField] private Button sendButton;
        [SerializeField] private Button cancelButton;


        [Header("Placeholder Texts")] [TextArea(3, 6)] [SerializeField]
        private string bugPlaceholderText;

        [TextArea(3, 6)] [SerializeField] private string feedbackPlaceholderText;

        [Header("Visual Settings")] [SerializeField]
        private Color selectedColor;

        [SerializeField] private Color unselectedColor;

        [Header("Screenshot Settings")] [SerializeField]
        private Toggle screenshotToggle;

        [SerializeField] private string bugsListId;
        [SerializeField] private string feedbackListId;
        [SerializeField] private TrelloCardReporter trelloReporter;

        private string _selectedListId;
        private Action _onCloseCallback;

        public void Setup(CanvasGroup canvasGroupToToggle, string apiKey, string token, Action onCloseCallback)
        {
            trelloReporter.Initialize(apiKey, token);
            canvasGroup = canvasGroupToToggle;
            SetupUI();
            _onCloseCallback = onCloseCallback;
        }

        private void SetupUI()
        {
            sendButton.onClick.AddListener(OnSend);
            cancelButton.onClick.AddListener(OnCancel);
            bugButton.onClick.AddListener(SelectBug);
            feedbackButton.onClick.AddListener(SelectFeedback);
            descriptionInput.onValueChanged.AddListener(UpdateCharCount);
            errorText.text = "";
            descriptionCharCount.text = "0/500";
            SelectBug();
        }
        
        public void SelectBug()
        {
            _selectedListId = bugsListId;
            placeholderTitle.text = "Bug title...";
            placeholderDescription.text = bugPlaceholderText;
            UpdateButtonColors(bugButton, feedbackButton);
        }

        public void SelectFeedback()
        {
            _selectedListId = feedbackListId;
            placeholderTitle.text = "Feedback title...";
            placeholderDescription.text = feedbackPlaceholderText;
            UpdateButtonColors(feedbackButton, bugButton);
        }

        private void UpdateButtonColors(Button selectedButton, Button unselectedButton)
        {
            selectedButton.GetComponent<Image>().color = selectedColor;
            unselectedButton.GetComponent<Image>().color = unselectedColor;
        }

        public void OnSend()
        {
            string cardTitle = titleInput.text;
            string description = descriptionInput.text;

            if (string.IsNullOrEmpty(cardTitle) || string.IsNullOrEmpty(description))
            {
                errorText.text = "Title and Description cannot be empty.";
                return;
            }

            string additionalInfo =
                $"\n\n### Extra Info\nApplication Version: {Application.version}\nDate and Time: {DateTime.Now}";
            description = "### Details\n" + description + additionalInfo;

            StartCoroutine(trelloReporter.CreateTrelloCard(cardTitle, description, _selectedListId, OnCardCreated));
        }

        private void OnCardCreated(bool success, string cardId)
        {
            if (success)
            {
                if (screenshotToggle.isOn)
                {
                    StartCoroutine(CaptureAndUploadScreenshot(cardId));
                }
                else
                {
                    ResetForm("Form sent successfully.");
                }
            }
            else
            {
                errorText.text = "Failed to send the form. Please try again.";
            }
        }

        private System.Collections.IEnumerator CaptureAndUploadScreenshot(string cardId)
        {
            canvasGroup.alpha = 0;
            yield return new WaitForEndOfFrame();

            Texture2D screenImage = ScreenCapture.CaptureScreenshotAsTexture();
            byte[] imageBytes = screenImage.EncodeToPNG();
            Destroy(screenImage);

            canvasGroup.alpha = 1;

            StartCoroutine(trelloReporter.UploadScreenshot(cardId, imageBytes, OnScreenshotUploaded));
        }

        private void OnScreenshotUploaded(bool success)
        {
            if (success)
            {
                ResetForm("Form and screenshot sent successfully.");
            }
            else
            {
                errorText.text = "Form sent, but failed to upload screenshot.";
            }
        }

        public void OnCancel()
        {
            ResetForm("");
            _onCloseCallback?.Invoke();
        }

        private void ResetForm(string message)
        {
            titleInput.text = "";
            descriptionInput.text = "";
            errorText.text = message;
        }

        private void UpdateCharCount(string text)
        {
            if (text.Length > 500)
            {
                descriptionInput.text = text.Substring(0, 500);
                descriptionInput.caretPosition = 500;
            }

            descriptionCharCount.text = $"{descriptionInput.text.Length}/500";
        }
    }
}