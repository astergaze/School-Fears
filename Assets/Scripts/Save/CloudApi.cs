using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ApiResult
{
    public long status;
    public string text;
    public bool networkError;   // sin conexión / timeout / respuesta ilegible
    public bool Ok => !networkError && status >= 200 && status < 300;
}

/// <summary>Cliente HTTP mínimo. Se usa con StartCoroutine desde SaveManager.</summary>
public class CloudApi
{
    public string BaseUrl;
    public string Token;
    // Render (plan gratis) puede tardar ~1 minuto en despertar: timeout largo.
    public int TimeoutSeconds = 90;

    public IEnumerator Send(string method, string path, string jsonBody, Action<ApiResult> done)
    {
        ApiResult result;
        using (var req = new UnityWebRequest(BaseUrl.TrimEnd('/') + path, method))
        {
            req.timeout = TimeoutSeconds;
            req.downloadHandler = new DownloadHandlerBuffer();
            if (jsonBody != null)
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
                req.SetRequestHeader("Content-Type", "application/json");
            }
            if (!string.IsNullOrEmpty(Token))
                req.SetRequestHeader("Authorization", "Bearer " + Token);

            yield return req.SendWebRequest();

            result = new ApiResult
            {
                status = req.responseCode,
                text = req.downloadHandler != null ? req.downloadHandler.text : "",
                networkError = req.result == UnityWebRequest.Result.ConnectionError
                            || req.result == UnityWebRequest.Result.DataProcessingError
            };
        }
        done(result);
    }
}
