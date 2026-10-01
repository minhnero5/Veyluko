using System;

public class DefaultFlowUI : IFlowUI
{
    public void SetLoading(bool show, string message = null)
    {
        UserDataManager.OnShowLoadingPopupAction?.Invoke(show);
        // Nếu bạn có Loading popup có text, set message ở đây.
    }

    public void Toast(string message)
    {
        if (!string.IsNullOrEmpty(message))
            NoticePopUp.Instance.ShowMessage(message);
    }

    public void ShowPanel(UiPanelType type, string title, string message, Action primary = null, Action secondary = null)
    { 
        if( type == UiPanelType.ServerOff)
        {
            ApiPopupManager.Instance.ShowServerOffPopup();
            return;
        } 
        // Bạn có thể map type -> panel UI riêng.
        // Tạm thời fallback: NoticePopUp
        Toast($"{message}");
        primary?.Invoke();
    }
}
