using System.Collections.Generic;

public static class FlowUiRegistry
{
    public static readonly Dictionary<ApiFlowId, FlowUiSpec> Specs = new()
    {
        [ApiFlowId.BootFetchSkus] = new FlowUiSpec
        {
            LoadingMessage = "Connecting...",
            ToastOnFail = false,
            PanelOnServerOff = UiPanelType.ServerOff
        },

        [ApiFlowId.Login] = new FlowUiSpec
        {
            LoadingMessage = "Logging in...",
            ToastOnSuccess = true,
            SuccessMessage = "Login Successful!",
            ToastOnFail = true,
            FailFallbackMessage = "Login failed."
        },

        [ApiFlowId.SignUp] = new FlowUiSpec
        {
            LoadingMessage = "Creating account...",
            ToastOnSuccess = true,
            SuccessMessage = "Sign Up Successful!",
            FailFallbackMessage = "Sign up failed."
        },

        [ApiFlowId.VerifyPurchase] = new FlowUiSpec
        {
            LoadingMessage = "Verifying purchase...",
            ToastOnSuccess = true,
            SuccessMessage = "Purchase verified successfully!",
            PanelOnOtherError = UiPanelType.PurchaseError,
            FailFallbackMessage = "Failed to verify purchase."
        },

        [ApiFlowId.GetProfileCoin] = new FlowUiSpec { LoadingMessage = "Syncing..." },
        [ApiFlowId.GetProfileStatus] = new FlowUiSpec { LoadingMessage = "Checking status..." },
        [ApiFlowId.AddCoins] = new FlowUiSpec { LoadingMessage = "Updating..." },
        [ApiFlowId.SpendCoins] = new FlowUiSpec { LoadingMessage = "Processing..." },

        [ApiFlowId.Logout] = new FlowUiSpec
        {
            ShowLoading = false,
            ToastOnFail = false
        }
    };
}
