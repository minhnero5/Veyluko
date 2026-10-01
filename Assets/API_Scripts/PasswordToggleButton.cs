using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PasswordToggleButton : MonoBehaviour
{
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button toggleButton;
    [SerializeField] private Image buttonIcon;
    [SerializeField] private Sprite showIcon;
    [SerializeField] private Sprite hideIcon;
    
    private bool isPasswordVisible = false;

    void OnEnable()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(TogglePasswordVisibility);
        }
    }

    void OnDisable()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(TogglePasswordVisibility);
        }
    }

    private void TogglePasswordVisibility()
    {
        if (passwordInput == null)
            return;

        isPasswordVisible = !isPasswordVisible;

        // Thay đổi content type để hiển thị/ẩn mật khẩu
        passwordInput.contentType = isPasswordVisible 
            ? TMP_InputField.ContentType.Standard 
            : TMP_InputField.ContentType.Password;

        // Cập nhật icon nếu có
        if (buttonIcon != null)
        {
            buttonIcon.sprite = isPasswordVisible ? hideIcon : showIcon;
        }

        // Refresh display
        passwordInput.ForceLabelUpdate();
    }

    public void SetPasswordInputField(TMP_InputField inputField)
    {
        passwordInput = inputField;
    }
}