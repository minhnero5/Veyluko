public static class ServerUrl
{
    const string SERVER_URL = "";
    public static bool IsServerConfigured => !string.IsNullOrEmpty(SERVER_URL);
    public const string APP = SERVER_URL + "/apps";
    public const string KEY = SERVER_URL + "/crypto-test/keys";
    public const string ACCESS = SERVER_URL + "/auth/access";
    public const string LOGIN = SERVER_URL + "/auth/login";
    public const string PROFILE = SERVER_URL + "/auth/profile";
    public const string SIGN_UP = SERVER_URL + "/auth/register";
    public const string ADD_COINS = SERVER_URL + "/users/add-coin";
    public const string SPEND_COINS = SERVER_URL + "/users/spend-coin";
    public const string VERIFY_PURCHASE = SERVER_URL + "/inapp/verify-purchase";
}