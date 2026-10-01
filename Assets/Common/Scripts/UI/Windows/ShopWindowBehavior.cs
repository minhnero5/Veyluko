using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ShopWindowBehavior : MonoBehaviour
{
    private Canvas canvas;
    public static Action OnShopWindowOpened;

    [Space]
    [SerializeField] Button backButton;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
    }

    public void Init(UnityAction onBackButtonClicked)
    {
        backButton.onClick.AddListener(onBackButtonClicked);
    }

    public void Open()
    {
        OnShopWindowOpened?.Invoke();
        gameObject.SetActive(true);
        canvas.enabled = true;
    }

    public void Close()
    {
        canvas.enabled = false;
    }
}
