using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UserStatusPopup : Popup
{
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button logoutButton;

    public void ShowUserStatus()
    {  
        var userName = UserDataManager.Instance.currentUserData.username;
        statusText.text = "The " + userName + " account is restricted.";
        Show();
        logoutButton.onClick.RemoveAllListeners();
        logoutButton.onClick.AddListener(() =>
        {
            UserDataManager.Instance.Logout();
            Hide();
        });
    }
}
