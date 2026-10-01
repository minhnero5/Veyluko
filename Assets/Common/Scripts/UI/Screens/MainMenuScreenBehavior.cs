using OctoberStudio.Audio;
using OctoberStudio.Upgrades.UI;
using UnityEngine;
using UnityEngine.Events;

namespace OctoberStudio.UI
{
    public class MainMenuScreenBehavior : MonoBehaviour
    {
        private Canvas canvas;

        [SerializeField] LobbyWindowBehavior lobbyWindow;
        [SerializeField] UpgradesWindowBehavior upgradesWindow;
        [SerializeField] SettingsWindowBehavior settingsWindow;
        [SerializeField] CharactersWindowBehavior charactersWindow;
        [SerializeField] ShopWindowBehavior shopWindow;

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
        }

        private void Start()
        {
            lobbyWindow.Init(ShowUpgrades, ShowSettings, ShowCharacters, ShowShop);
            upgradesWindow.Init(HideUpgrades);
            settingsWindow.Init(HideSettings);
            charactersWindow.Init(HideCharacters);
            shopWindow.Init(HideShop);
        }

        private void ShowUpgrades()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);

            lobbyWindow.Close();
            upgradesWindow.Open();
        }

        private void HideUpgrades()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);

            upgradesWindow.Close();
            lobbyWindow.Open();
        }

        private void ShowCharacters()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);

            lobbyWindow.Close();
            charactersWindow.Open();
        }

        private void ShowShop()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);

            lobbyWindow.Close();
            shopWindow.Open();
        }

        private void HideShop()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);

            shopWindow.Close();
            lobbyWindow.Open();
        }

        private void HideCharacters()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);

            charactersWindow.Close();
            lobbyWindow.Open();
        }

        private void ShowSettings()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);

            lobbyWindow.Close();
            settingsWindow.Open();
        }

        private void HideSettings()
        {
            GameController.AudioManager.PlaySound(AudioManager.BUTTON_CLICK_HASH);

            settingsWindow.Close();
            lobbyWindow.Open();
        }

        public void Show(UnityAction onFinish = null)
        {
            canvas.enabled = true;

            onFinish?.Invoke();
        }

        public void Hide(UnityAction onFinish = null)
        {
            canvas.enabled = false;

            onFinish?.Invoke();
        }
    }
}