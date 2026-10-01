using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DG.Tweening;

public class NoticePopUp : MonoBehaviour
{
    public static NoticePopUp Instance;
    [SerializeField] GameObject popUp;
    [SerializeField] TextMeshProUGUI messageTxt;
    [SerializeField] Ease ease;
    [SerializeField] LoopType loopType;
    Sequence popUpSequence;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {

        popUpSequence = DOTween.Sequence();
        popUpSequence.Append(popUp.transform.DOScale(Vector3.one * 1.1f, 0.1f).SetLoops(2, loopType).SetEase(ease))
                     .AppendInterval(2f)
                     .Append(popUp.transform.DOScale(Vector3.one * 0.5f, 0.1f))
                     .OnComplete(() => popUp.SetActive(false))
                     .Pause()
                     .SetAutoKill(false);
    }

    public void ShowMessage(string message)
    {
        messageTxt.text = message;
        popUp.SetActive(true);
        popUp.transform.localScale = Vector3.one;
        popUpSequence.Restart();
    }


    private void Update()
    {
      
    }
}
