using UnityEngine;
using TMPro;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

public class InputFieldValidator : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    
    [SerializeField] private ValidationType validationType = ValidationType.Alphanumeric;
    [SerializeField] private bool removeAccents = true;
    
    public enum ValidationType
    {
        Alphanumeric,           // Chỉ chữ và số
        AlphanumericWithUnderscore, // Chữ, số, _
        Email,                  // Email format
        Password,               // Chữ, số, và một số ký tự đặc biệt an toàn
        NoSpecialCharacters     // Không ký tự đặc biệt, cho phép dấu cách
    }

    void OnEnable()
    {
        if (inputField == null)
            inputField = GetComponent<TMP_InputField>();
        
        if (inputField != null)
        {
            inputField.onValueChanged.AddListener(ValidateInput);
        }
    }

    void OnDisable()
    {
        if (inputField != null)
        {
            inputField.onValueChanged.RemoveListener(ValidateInput);
        }
    }

    private void ValidateInput(string input)
    {
        string filteredText = FilterText(input);
        
        if (filteredText != input)
        {
            inputField.text = filteredText;
        }
    }

    private string FilterText(string text)
    {
        if (removeAccents)
            text = RemoveAccents(text);
        
        switch (validationType)
        {
            case ValidationType.Alphanumeric:
                return Regex.Replace(text, @"[^a-zA-Z0-9]", "");
            
            case ValidationType.AlphanumericWithUnderscore:
                return Regex.Replace(text, @"[^a-zA-Z0-9_]", "");
            
            case ValidationType.Email:
                return Regex.Replace(text, @"[^a-zA-Z0-9@._\-]", "");
            
            case ValidationType.Password:
                return Regex.Replace(text, @"[^a-zA-Z0-9!@#$%^&*\-_.]", "");
            
            case ValidationType.NoSpecialCharacters:
                return Regex.Replace(text, @"[^a-zA-Z0-9\s]", "");
            
            default:
                return text;
        }
    }

    private string RemoveAccents(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
            
        string normalized = text.Normalize(System.Text.NormalizationForm.FormD);
        Regex regex = new Regex(@"\p{Mn}+");
        return regex.Replace(normalized, string.Empty).Normalize(System.Text.NormalizationForm.FormC);
    }

    public void SetValidationType(ValidationType type)
    {
        validationType = type;
    }

    public void SetRemoveAccents(bool remove)
    {
        removeAccents = remove;
    }
}