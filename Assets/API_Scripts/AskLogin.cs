using System;
using UnityEngine;
using UnityEngine.UI;

public class AskLogin : Popup
{
    [SerializeField] private LoginPopup loginPopup;
    [SerializeField] private Button btnLogin;
    [SerializeField] private Button btnPurchaseWithoutLogin;
    
    private Action purchaseAction;

    public void ShowAskLogin(Action guestsAction, Action onLoginAction = null)
    {
        // if (!UserDataManager.Instance.isTest)
        // {
        //     if (!UserDataManager.Instance.hasAccess)
        //     {
        //         guestsAction?.Invoke();
        //         return;
        //     }
        // }

        purchaseAction = onLoginAction;
        Show();
        btnPurchaseWithoutLogin.onClick.RemoveAllListeners();
        btnPurchaseWithoutLogin.onClick.AddListener(() =>
        {
            Hide(() =>
            {
                guestsAction?.Invoke();
            });
        });
    }

    void OnEnable()
    {
        btnLogin.onClick.AddListener(() =>
        {
            Hide(() =>
            {
                loginPopup.ShowLoginPopup(purchaseAction);
            });
        });
    }

    void OnDisable()
    {
        btnLogin.onClick.RemoveAllListeners();
    }
}
