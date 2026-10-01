public enum ApiErrorKind
{
    None,
    NoInternet,
    ServerOff,
    Timeout,
    Connection,
    Unauthorized,
    BadRequest,
    Unknown,
    ApiNotConfigured,
}

public class ApiResult
{
    public bool Ok;
    public string Raw;              // decrypted payload nếu Ok
    public long HttpStatus;
    public ApiErrorKind ErrorKind;
    public string ErrorMessage;     // friendly message
    public string ErrorRaw;         // raw response (encrypted/unencrypted)

    public static ApiResult Success(string raw, long status = 200) =>
        new ApiResult { Ok = true, Raw = raw, HttpStatus = status, ErrorKind = ApiErrorKind.None };

    public static ApiResult Fail(ApiErrorKind kind, string msg, string raw = null, long status = 0) =>
        new ApiResult { Ok = false, ErrorKind = kind, ErrorMessage = msg, ErrorRaw = raw, HttpStatus = status };
}
