using System;
using System.Collections;
using UnityEngine;

public class FlowRunner
{
    private readonly UnityEngine.MonoBehaviour _host;
    private readonly IFlowUI _ui;
    private readonly Func<ApiFlowId, FlowUiSpec> _specOf;

    public FlowRunner(UnityEngine.MonoBehaviour host, IFlowUI ui, Func<ApiFlowId, FlowUiSpec> specOf)
    {
        _host = host;
        _ui = ui;
        _specOf = specOf;
    }

    public void Run(ApiFlowId flowId, Func<IEnumerator> coroutineFactory)
    {
        _host.StartCoroutine(RunInternal(flowId, coroutineFactory));
    }

    private IEnumerator RunInternal(ApiFlowId flowId, Func<IEnumerator> coroutineFactory)
    {
        var spec = _specOf(flowId);

        if (spec.ShowLoading) _ui.SetLoading(true, spec.LoadingMessage);

        yield return coroutineFactory();

        if (spec.ShowLoading) _ui.SetLoading(false);
    }

    public void HandleResult(ApiFlowId flowId, ApiResult result, Action onSuccess = null, Action onFail = null)
    {
        if(result.ErrorKind == ApiErrorKind.ApiNotConfigured)
        {
            Debug.Log("Server URL not configured, skipping flow handling.");
            return;
        }
        
        var spec = _specOf(flowId);

        if (result.Ok)
        {
            if (spec.ToastOnSuccess && !string.IsNullOrEmpty(spec.SuccessMessage))
                _ui.Toast(spec.SuccessMessage);

            onSuccess?.Invoke();
            return;
        }

        var msg = string.IsNullOrEmpty(result.ErrorMessage) ? spec.FailFallbackMessage : result.ErrorMessage;
        Debug.Log("Error: " + msg);
        
        UiPanelType panel = result.ErrorKind switch
        {
            ApiErrorKind.NoInternet => spec.PanelOnNoInternet,
            ApiErrorKind.ServerOff => spec.PanelOnServerOff,
            ApiErrorKind.Unauthorized => spec.PanelOnAuthExpired,
            _ => spec.PanelOnOtherError
        };

        if (panel != UiPanelType.None)
        {
            _ui.ShowPanel(panel, "Error", msg);
        }
        else if (spec.ToastOnFail)
        {
            _ui.Toast(msg);
        }

        onFail?.Invoke();
    }
}
