using System;

public interface IFlowUI
{
    void SetLoading(bool show, string message = null);
    void Toast(string message);
    void ShowPanel(
        UiPanelType type,
        string title,
        string message,
        Action primary = null,
        Action secondary = null
    );
}
