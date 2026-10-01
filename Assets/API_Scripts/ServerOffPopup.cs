using UnityEngine;
using UnityEngine.UI;

public class ServerOffPopup : Popup
{
    
    [SerializeField] private Button logoutButton;

    private void OnEnable()
    {
        logoutButton.onClick.AddListener(OnLogutCliked);
    }
    private void OnDisable()
    {
        logoutButton.onClick.RemoveListener(OnLogutCliked);
    }
    public void ShowServerOff()
    {
        base.Show(null);
    }

    public void OnLogutCliked()
    {
        UserDataManager.Instance.Logout();
        base.Hide();
    }

    public void OnCloseButton()
    {       
        UserDataManager.Instance.Logout();
        base.Hide();
    }
}
