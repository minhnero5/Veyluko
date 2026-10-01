using System;
using UnityEngine;
using UnityEngine.UI;

public class ApiPopupManager : MonoBehaviour
{
    public static ApiPopupManager Instance;

    [SerializeField] private Button openShopButton;
    [SerializeField] private Button closeShopButton;

    [SerializeField] private GameObject userPanel;
    [SerializeField] private TMPro.TMP_Text usernameText;
    [SerializeField] private Button loginButton;
    [SerializeField] private UserStatusPopup userStatusPopup;
    [SerializeField] private AskLogin askLoginPopup;
    [SerializeField] private LoginPopup loginPopup;
    [SerializeField] private SignUpPopup signUpPopup;
    [SerializeField] private LogoutPopup logoutPopup;
    [SerializeField] private ApiLoadingPopup loadingPopup;
    [SerializeField] private ServerOffPopup serverOffPopup;
    [SerializeField] private YesNoPopup yesNoPopup;

    private void Awake()
    {

        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    void Start()
    {
        if (!ServerUrl.IsServerConfigured)
        {
            Debug.LogWarning("Server URL not configured. ApiPopupManager disabled.");
            this.enabled = false;
            gameObject.SetActive(false);
            return;
        }

        Debug.Log("Server URL configured. ApiPopupManager active.");
    }

    void OnEnable()
    {
        if (!ServerUrl.IsServerConfigured) return;

        UserDataManager.OnUserLogoutAction += OnLogoutDone;
        UserDataManager.OnGetUserStatusAction += ShowUserStatusPopup;
        UserDataManager.OnShowLoadingPopupAction += ShowLoadingPopup;
        UserDataManager.OnLoginSuccessAction += OnLoginDone;
        UserDataManager.OnSignUpSuccessAction += OnSignUpDone;
        UserDataManager.OnServerOffAction += ShowServerOffPopup;
        openShopButton.onClick.AddListener(OnShopOpen);
        closeShopButton.onClick.AddListener(OnShopClose);
    }

    void OnDisable()
    {
        if (!ServerUrl.IsServerConfigured) return;

        UserDataManager.OnUserLogoutAction -= OnLogoutDone;
        UserDataManager.OnGetUserStatusAction -= ShowUserStatusPopup;
        UserDataManager.OnShowLoadingPopupAction -= ShowLoadingPopup;
        UserDataManager.OnLoginSuccessAction -= OnLoginDone;
        UserDataManager.OnSignUpSuccessAction -= OnSignUpDone;
        UserDataManager.OnServerOffAction -= ShowServerOffPopup;
        openShopButton.onClick.RemoveListener(OnShopOpen);
        closeShopButton.onClick.RemoveListener(OnShopClose);
    }

    public void InitYesNoPopup(string message, Action yesAction, string yesButtonText = "Yes", string noButtonText = "No", Action noAction = null)
    {
        yesNoPopup.ShowYesNoPopup(message, yesAction, noAction, yesButtonText, noButtonText);
    }

    public void OnShopOpen()
    {
        Debug.Log("OnShopOpen");
        ShowLoginButton(true);
        ShowUserPanel(true);
    }

    public void OnShopClose()
    {
        Debug.Log("OnShopClose");
        ShowLoginButton(false);
        ShowUserPanel(false);
    }

    public void ShowServerOffPopup()
    {
        serverOffPopup.ShowServerOff();
    }

    public void ShowLoginButton(bool isShow)
    {
        if (UserDataManager.Instance.isLoggedIn)
        {
            isShow = false;
        }
        loginButton.gameObject.SetActive(isShow);
    }


    public void SetUserNameAndPassOnLogin(string username, string password)
    {
        loginPopup.SetUserAndPassword(username, password);
    }

    public void ShowUserPanel(bool isShow)
    {
        if (!UserDataManager.Instance.isLoggedIn)
        {
            isShow = false;
        }
        usernameText.text = UserDataManager.Instance.currentUserData.username;
        userPanel.SetActive(isShow);
    }

    public void ShowAskLoginPopup(Action guestAction, Action onLoginAction = null)
    {
        askLoginPopup.ShowAskLogin(guestAction, onLoginAction);
    }

    public void OnLoginDone()
    {
        userPanel.SetActive(true);
        usernameText.text = UserDataManager.Instance.currentUserData.username;
        loginButton.gameObject.SetActive(false);
        loginPopup.Hide();
    }

    public void OnSignUpDone()
    {
        userPanel.SetActive(true);
        //usernameText.text = UserDataManager.Instance.currentUserData.username;
        loginButton.gameObject.SetActive(false);
        signUpPopup.Hide();
    }

    public void ShowLoadingPopup(bool show)
    {
        if (show)
        {
            loadingPopup.Show();
        }
        else
        {
            loadingPopup.Hide();
        }
    }

    public void ShowLogoutPopup()
    {
        logoutPopup.ShowLogoutPopup();
    }

    public void OnLogoutDone()
    {
        HideAllPopups();
        userPanel.SetActive(false);
        loginButton.gameObject.SetActive(true);
    }

    public void ShowLoginPopup()
    {
        loginPopup.ShowLoginPopup();
    }

    public void ShowSignUpPopup()
    {
        signUpPopup.ShowSignUpPopup();
    }

    public void ShowUserStatusPopup(bool isRestricted)
    {
        if (isRestricted)
        {
            userStatusPopup.ShowUserStatus();
        }
    }

    void HideAllPopups()
    {
        userStatusPopup.Hide();
        askLoginPopup.Hide();
        loginPopup.Hide();
        signUpPopup.Hide();
        logoutPopup.Hide();
    }

    public void TestTimeOut()
    {
        //UserDataManager.Instance.TestServerTimeOut();
    }



}
