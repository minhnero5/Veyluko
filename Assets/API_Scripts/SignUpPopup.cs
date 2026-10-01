using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class SignUpPopup : Popup
{
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_InputField teamInput;
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private Button signUpButton;

    void OnEnable()
    {
        signUpButton.onClick.AddListener(OnSignUpButtonClicked);
    
        UserDataManager.OnSignUpSuccessAction += SetUserAndPasswordInLogin;
    }

    void OnDisable()
    {
        signUpButton.onClick.RemoveListener(OnSignUpButtonClicked);
      
        UserDataManager.OnSignUpSuccessAction -= SetUserAndPasswordInLogin;
    }

    void OnSignUpButtonClicked()
    {
        string username = usernameInput.text;
        string password = passwordInput.text;
        string team = teamInput.text;
        string email = emailInput.text;

        // Validate username - only letters and numbers
        if (string.IsNullOrEmpty(username) || !System.Text.RegularExpressions.Regex.IsMatch(username, "^[a-zA-Z0-9]+$"))
        {
            NoticePopUp.Instance.ShowMessage("Username must contain only letters and numbers");
            return;
        }

        // Validate password - at least 6 characters
        if (string.IsNullOrEmpty(password))
        {
            NoticePopUp.Instance.ShowMessage("Password is required");
            return;
        }
        if (password.Length < 1)
        {
            NoticePopUp.Instance.ShowMessage("Password must be at least 1 character");
            return;
        }

        // Validate team - required
        // if (string.IsNullOrEmpty(team))
        // {
        //     ShowFeedback("Team is required");
        //     return;
        // }

        UserDataManager.Instance.SignUp(username, password, team, email);
    }

    public void ShowSignUpPopup()
    {
        Show();
    }

    public void HideSignUpPopup()
    {
        Hide();
    }

    public void SetUserAndPasswordInLogin()
    {
        GetComponentInParent<ApiPopupManager>().SetUserNameAndPassOnLogin(usernameInput.text, passwordInput.text);
    }

}
