using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using DG.Tweening;

public class LoginPopup : Popup
{
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button signUpButton;
    [SerializeField] private Button showSignInButton;
    [SerializeField] private TMP_Text userNameText;
    [SerializeField] private Toggle rememberMeToggle;

    private const string REMEMBER_ME_KEY = "RememberMe";
    private const string SAVED_USERNAME_KEY = "SavedUsername";
    private const string SAVED_PASSWORD_KEY = "SavedPassword";
    
 


    void OnEnable()
    {
        loginButton.onClick.AddListener(OnLoginButtonClicked);
        signUpButton.onClick.AddListener(OnSignUpButtonClicked);
        rememberMeToggle.onValueChanged.AddListener(OnRememberMeToggled);
        UserDataManager.OnLoginSuccessAction += OnLoginSuccess;
        UserDataManager.OnSignUpSuccessAction += OnSignUpSuccess;
        
        LoadSavedCredentials();
    }

    void OnDisable()
    {
        loginButton.onClick.RemoveListener(OnLoginButtonClicked);
        signUpButton.onClick.RemoveListener(OnSignUpButtonClicked);
        rememberMeToggle.onValueChanged.RemoveListener(OnRememberMeToggled);
        UserDataManager.OnLoginSuccessAction -= OnLoginSuccess;
        UserDataManager.OnSignUpSuccessAction -= OnSignUpSuccess;
    }

    public void ShowLoginPopup(Action purchaseAction = null)
    {
        Show();
    }

    public void HideLoginPopup()
    {
        Hide();
    }

    public void SetUserAndPassword(string username, string password)
    {
        usernameInput.text = username;
        passwordInput.text = password;
    }

    void HideShowLoginBtn()
    {
        showSignInButton.gameObject.SetActive(false);
    }

    private void OnLoginButtonClicked()
    {
        if (string.IsNullOrEmpty(usernameInput.text) || string.IsNullOrEmpty(passwordInput.text))
        {
            NoticePopUp.Instance.ShowMessage("Please enter both username and password.");
            return;
        }
        string username = usernameInput.text;
        string password = passwordInput.text;
        OnRememberMeToggled(rememberMeToggle.isOn);

        UserDataManager.Instance.Login(username, password);
    }

    private void OnSignUpButtonClicked()
    {
        
        GetComponentInParent<ApiPopupManager>().ShowSignUpPopup(); 
    }

    public void SetUserName()
    {
        //userNameText.text = UserDataManager.Instance.currentUserData.userName;
        userNameText.text = usernameInput.text;
    }

    private void OnLoginSuccess()
    {
        SaveLoginCredentials();
        HideLoginPopup();
       
    }

    private void OnSignUpSuccess()
    {
        SaveLoginCredentials();
        HideLoginPopup();
       
    }

    private void SaveLoginCredentials()
    {
        SaveCredentials();
    }

    private void LoadSavedCredentials()
    {
        bool rememberMe = PlayerPrefs.GetInt(REMEMBER_ME_KEY, 0) == 1;
        rememberMeToggle.isOn = rememberMe;
        Debug.Log("Remember Me: " + rememberMe);

        if (rememberMe)
        {
            string savedUsername = PlayerPrefs.GetString(SAVED_USERNAME_KEY, "");
            string savedPassword = PlayerPrefs.GetString(SAVED_PASSWORD_KEY, "");

            if (!string.IsNullOrEmpty(savedUsername))
            {
                usernameInput.text = savedUsername;
                Debug.Log("Loaded username: " + savedUsername);
            }
            else
            {
                Debug.Log("No saved username found");
            }

            if (!string.IsNullOrEmpty(savedPassword))
            {
                passwordInput.text = savedPassword;
                Debug.Log("Loaded password: " + savedPassword);
            }
            else
            {
                Debug.Log("No saved password found");
            }
        }
    }

    private void OnRememberMeToggled(bool isToggled)
    {
        if (isToggled)
        {
            SaveCredentials();
        }
        else
        {
            ClearSavedCredentials();
        }
    }

    private void SaveCredentials()
    {
        string username = usernameInput.text;
        string password = passwordInput.text;
        
        if(rememberMeToggle.isOn == false)
        {
            Debug.Log("Remember Me is not checked, not saving credentials");
            PlayerPrefs.GetInt(REMEMBER_ME_KEY, 0);
            return;
        }

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Debug.LogWarning("Cannot save empty username or password");
            return;
        }
        
        PlayerPrefs.SetInt(REMEMBER_ME_KEY, 1);
        PlayerPrefs.SetString(SAVED_USERNAME_KEY, username);
        PlayerPrefs.SetString(SAVED_PASSWORD_KEY, password);
        PlayerPrefs.Save();
        //Debug.Log("Credentials saved successfully - username: " + username + ", password: " + password);
    }

    private void ClearSavedCredentials()
    {
        PlayerPrefs.SetInt(REMEMBER_ME_KEY, 0);
        PlayerPrefs.DeleteKey(SAVED_USERNAME_KEY);
        PlayerPrefs.DeleteKey(SAVED_PASSWORD_KEY);
        PlayerPrefs.Save();
        Debug.Log("Credentials cleared");
    }

}
