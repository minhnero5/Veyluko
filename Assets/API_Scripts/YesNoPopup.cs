using UnityEngine;
using UnityEngine.UI;
using System;

public class YesNoPopup : Popup
{
    [SerializeField] private TMPro.TMP_Text messageText;
    [SerializeField] private TMPro.TMP_Text yesText;
    [SerializeField] private TMPro.TMP_Text noText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    private Action yesAction;
    private Action noAction;

    void OnEnable()
    {
        yesButton.onClick.AddListener(OnYesButtonClicked);
        noButton.onClick.AddListener(OnNoButtonClicked);
    }

    void OnDisable()
    {
        yesButton.onClick.RemoveListener(OnYesButtonClicked);
        noButton.onClick.RemoveListener(OnNoButtonClicked);
    }

    public void ShowYesNoPopup(string message, Action onYes , Action onNo , string yesButtonText , string noButtonText )
    {
        messageText.text = message;
        yesText.text = yesButtonText;
        noText.text = noButtonText;
        yesAction = onYes;
        noAction = onNo;
        base.Show();
    }

    private void OnYesButtonClicked()
    {
        base.Hide();
        yesAction?.Invoke();
    }

    private void OnNoButtonClicked()
    {
        base.Hide();
        noAction?.Invoke();
    }
    
}
