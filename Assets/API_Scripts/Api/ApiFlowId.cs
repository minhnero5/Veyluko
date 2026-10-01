public enum ApiFlowId
{
    BootFetchSkus,
    Login,
    SignUp,
    GetProfileStatus,
    GetProfileCoin,
    VerifyPurchase,
    AddCoins,
    SpendCoins,
    Logout
}

public enum UiPanelType
{
    None,
    ServerOff,
    NoInternet,
    AuthExpired,
    GenericError,
    PurchaseError
}
