using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ApiClient
{
    private readonly int _timeout;
    private readonly string _rsaPublicKeyPem;
    private readonly Func<string> _getToken;

    public ApiClient(int timeout, string rsaPublicKeyPem, Func<string> getToken)
    {
        _timeout = timeout;
        _rsaPublicKeyPem = rsaPublicKeyPem;
        _getToken = getToken;
    }

    public static bool HasInternet() =>
        Application.internetReachability != NetworkReachability.NotReachable;


    public IEnumerator CheckInternet(Action<bool> isHasInternet)
    {
        Debug.Log("Checking internet connectivity...");
        const string url = "https://clients3.google.com/generate_204";
        const int timeoutSec = 5;

        using var req = UnityWebRequest.Get(url);
        req.timeout = timeoutSec;
        yield return req.SendWebRequest();

        bool ok = req.result == UnityWebRequest.Result.Success &&
                  (req.responseCode == 204 || req.responseCode == 200);
        if (ok)
        {
            isHasInternet(true);
        }
        else
        {
            isHasInternet(false);
        }
    }

    public IEnumerator PostEncrypted(string url, string jsonBody, Action<ApiResult> done)
    {
        if (string.IsNullOrEmpty(url) || url.StartsWith("/"))
        {
            Debug.Log("Server URL not configured");
            done(ApiResult.Fail(ApiErrorKind.ApiNotConfigured, "Server URL not configured"));
            yield break;
        }

        if (!HasInternet())
        {
            done(ApiResult.Fail(ApiErrorKind.NoInternet, "Internet is not available!"));
            yield break;
        }

        var rsa = ApiEncoding.ReadPublicKey(_rsaPublicKeyPem);
        var aesKey = ApiEncoding.CreateAesKey();
        var iv = ApiEncoding.CreateIv();
        string encryptedData = ApiEncoding.BuildSecureRequest(rsa, jsonBody, aesKey, iv);

        var encryptedDataJson = JObject.Parse(encryptedData);

        var key = new
        {
            key = encryptedDataJson["key"],
            iv = encryptedDataJson["iv"],
        };
        var jsonKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(key)));

        var body = new
        {
            data = encryptedDataJson["data"],
            tag = encryptedDataJson["tag"]
        };
        var encryptedDataToSend = JsonConvert.SerializeObject(body);

        var device = new
        {
            deviceId = SystemInfo.deviceUniqueIdentifier,
            deviceModel = SystemInfo.deviceModel,
            deviceType = SystemInfo.deviceType.ToString(),
            operatingSystem = SystemInfo.operatingSystem
        };
        string deviceJson = JsonConvert.SerializeObject(device);

        using (var www = UnityWebRequest.PostWwwForm(url, ""))
        {
            www.timeout = _timeout;
            www.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(encryptedDataToSend));
            www.downloadHandler = new DownloadHandlerBuffer();

            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("X-Device", deviceJson);
            www.SetRequestHeader("X-Game", Application.identifier);
            www.SetRequestHeader("X-Key", jsonKey);

            var token = _getToken?.Invoke();
            if (!string.IsNullOrEmpty(token))
                www.SetRequestHeader("Authorization", "Bearer " + token);

            yield return www.SendWebRequest();
            done(ParseEncryptedResponse(www, aesKey));
        }
    }

    public IEnumerator GetEncrypted(string url, Action<ApiResult> done)
    {
        if (string.IsNullOrEmpty(url) || url.StartsWith("/"))
        {
            Debug.Log("Server URL not configured");
            done(ApiResult.Fail(ApiErrorKind.ApiNotConfigured, "Server URL not configured"));
            yield break;
        }

        if (!HasInternet())
        {
            done(ApiResult.Fail(ApiErrorKind.NoInternet, "Internet is not available!"));
            yield break;
        }

        using (var www = UnityWebRequest.Get(url))
        {
            www.timeout = _timeout;

            var rsa = ApiEncoding.ReadPublicKey(_rsaPublicKeyPem);
            var aesKey = ApiEncoding.CreateAesKey();
            var iv = ApiEncoding.CreateIv();

            var encryptedKey = ApiEncoding.RsaEncryptAesKey(rsa, aesKey, out string alg);
            var key = new
            {
                key = Convert.ToBase64String(encryptedKey),
                iv = Convert.ToBase64String(iv),
            };
            var jsonKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(key)));

            www.SetRequestHeader("X-Game", Application.identifier);
            www.SetRequestHeader("X-Key", jsonKey);

            var token = _getToken?.Invoke();
            if (!string.IsNullOrEmpty(token))
                www.SetRequestHeader("Authorization", "Bearer " + token);

            yield return www.SendWebRequest();
            done(ParseEncryptedResponse(www, aesKey));
        }
    }

    private ApiResult ParseEncryptedResponse(UnityWebRequest www, byte[] aesKey)
    {
        if (www.result == UnityWebRequest.Result.ConnectionError)
        {
            var msg = string.IsNullOrEmpty(www.error) ? "Connection error" : www.error;
            var kind = msg != null && msg.ToLower().Contains("timeout") ? ApiErrorKind.Timeout : ApiErrorKind.Connection;
            return ApiResult.Fail(kind, msg, www.downloadHandler?.text, www.responseCode);
        }

        if (www.result != UnityWebRequest.Result.Success)
        {
            var status = www.responseCode;
            var raw = www.downloadHandler?.text;

            if (status == 401 || status == 403)
                return ApiResult.Fail(ApiErrorKind.Unauthorized, TryParseMessage(raw) ?? "Unauthorized", raw, status);

            if (status >= 400 && status < 500)
                return ApiResult.Fail(ApiErrorKind.BadRequest, TryParseMessage(raw) ?? "Bad request", raw, status);

            return ApiResult.Fail(ApiErrorKind.Unknown, TryParseMessage(raw) ?? "Request failed", raw, status);
        }

        try
        {
            var jsonResponse = JObject.Parse(www.downloadHandler.text);
            var datar = jsonResponse["data"];
            string decrypted = ApiEncoding.DecryptResponseAesGcm(datar.ToString(), aesKey);
#if UNITY_EDITOR
            Debug.Log("Decrypted response: " + decrypted);
#endif
            return ApiResult.Success(decrypted, www.responseCode);
        }
        catch (Exception e)
        {
            return ApiResult.Fail(ApiErrorKind.Unknown, "Failed to decrypt/parse response: " + e.Message, www.downloadHandler?.text, www.responseCode);
        }
    }

    private string TryParseMessage(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;
        try
        {
            var j = JObject.Parse(raw);
            return j["message"]?.ToString();
        }
        catch { return null; }
    }
}
