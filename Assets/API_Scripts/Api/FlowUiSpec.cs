public class FlowUiSpec
{
    public string LoadingMessage;
    public bool ShowLoading = true;

    public bool ToastOnSuccess;
    public string SuccessMessage;

    public bool ToastOnFail = true;
    public string FailFallbackMessage = "Something went wrong.";

    public UiPanelType PanelOnNoInternet = UiPanelType.NoInternet;
    public UiPanelType PanelOnServerOff = UiPanelType.ServerOff;
    public UiPanelType PanelOnAuthExpired = UiPanelType.AuthExpired;
    public UiPanelType PanelOnOtherError = UiPanelType.GenericError;
}
