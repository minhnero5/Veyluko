using UnityEngine;
using UnityEngine.UI;

public class LogoutPopup : Popup
{
    [SerializeField] private Button logoutButton;

    public void ShowLogoutPopup()
    {
        Show();
        logoutButton.onClick.RemoveAllListeners();
        logoutButton.onClick.AddListener(() =>
        {
            UserDataManager.Instance.Logout();
            Hide();
        });
    }
   
}
