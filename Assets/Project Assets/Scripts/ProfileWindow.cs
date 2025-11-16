using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Threading.Tasks;

public class ProfileWindow : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text currentNameText;
    [SerializeField] private TMP_InputField newNameInput;
    [SerializeField] private Button changeNameButton;

    [SerializeField] private TMP_Text currentDescriptionText;
    [SerializeField] private TMP_InputField newDescriptionInput;
    [SerializeField] private Button changeDescriptionButton;

    [SerializeField] private TMP_Text currentPersonalStatusText;
    [SerializeField] private TMP_InputField newPersonalStatusInput;
    [SerializeField] private Button changePersonalStatusButton;

    [SerializeField] private TMP_Text currentBirthDateText;
    [SerializeField] private TMP_InputField dayInput;
    [SerializeField] private TMP_InputField monthInput;
    [SerializeField] private TMP_InputField yearInput;
    [SerializeField] private Button changeBirthDateButton;

    [SerializeField] private Image currentIconImage; // Icono actual (guardado en nube)
    [SerializeField] private Image selectionIconImage; // Icono en selección (temporal)
    [SerializeField] private Button nextIconButton;
    [SerializeField] private Button previousIconButton;
    [SerializeField] private Button changeIconButton;

    [SerializeField] private Button closeButton;

    [Header("Data References")]
    [SerializeField] private PlayerIcons playerIcons;
    [SerializeField] private WindowsController profileWindow;

    private PlayerProfileData currentProfile;
    private int temporaryIconIndex;
    private TaskCompletionSource<bool> uiUpdateCompletionSource;

    private void OnEnable()
    {
        // Configurar listeners de botones
        changeNameButton.onClick.AddListener(ChangeDisplayName);
        changeDescriptionButton.onClick.AddListener(ChangeDescription);
        changePersonalStatusButton.onClick.AddListener(ChangePersonalStatus);
        changeBirthDateButton.onClick.AddListener(ChangeBirthDate);
        nextIconButton.onClick.AddListener(NextIcon);
        previousIconButton.onClick.AddListener(PreviousIcon);
        changeIconButton.onClick.AddListener(ChangeIcon);
        closeButton.onClick.AddListener(CloseWindow);

        // Suscribirse a eventos del CloudSaveManager
        CloudSaveManager.Instance.OnProfileDataLoaded += OnProfileDataLoaded;
        CloudSaveManager.Instance.OnProfileDataSaved += OnProfileDataSaved;

        // Cargar el perfil actual
        LoadCurrentProfile();
    }

    private void OnDisable()
    {
        // Remover listeners de botones
        changeNameButton.onClick.RemoveListener(ChangeDisplayName);
        changeDescriptionButton.onClick.RemoveListener(ChangeDescription);
        changePersonalStatusButton.onClick.RemoveListener(ChangePersonalStatus);
        changeBirthDateButton.onClick.RemoveListener(ChangeBirthDate);
        nextIconButton.onClick.RemoveListener(NextIcon);
        previousIconButton.onClick.RemoveListener(PreviousIcon);
        changeIconButton.onClick.RemoveListener(ChangeIcon);
        closeButton.onClick.RemoveListener(CloseWindow);

        // Desuscribirse de eventos
        if (CloudSaveManager.Instance != null)
        {
            CloudSaveManager.Instance.OnProfileDataLoaded -= OnProfileDataLoaded;
            CloudSaveManager.Instance.OnProfileDataSaved -= OnProfileDataSaved;
        }
    }

    private async void LoadCurrentProfile()
    {
        try
        {
            currentProfile = await CloudSaveManager.Instance.LoadPlayerProfileAsync();
            temporaryIconIndex = currentProfile.iconIndex;
            UpdateUI();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load profile in ProfileWindow: {ex.Message}");
        }
    }

    private async void OnProfileDataLoaded(PlayerProfileData loadedProfile)
    {
        currentProfile = loadedProfile;
        temporaryIconIndex = currentProfile.iconIndex;
        await UpdateUI();
    }

    private async void OnProfileDataSaved(PlayerProfileData savedProfile)
    {
        currentProfile = savedProfile;
        temporaryIconIndex = currentProfile.iconIndex;
        await UpdateUI();
    }

    private async Task UpdateUI()
    {
        if (currentProfile == null) return;

        // Actualizar nombre
        await UpdateDisplayNameText();

        // Actualizar descripción
        currentDescriptionText.text = currentProfile.description;

        // Actualizar estado personal
        currentPersonalStatusText.text = currentProfile.personalStatus;

        // Actualizar fecha de nacimiento
        currentBirthDateText.text = currentProfile.birthDate.ToString();

        // Actualizar iconos
        UpdateIconDisplay();

        // Limpiar campos de entrada
        ClearInputFields();

        // Indicar que la UI se ha actualizado completamente
        uiUpdateCompletionSource?.TrySetResult(true);
    }

    private async Task UpdateDisplayNameText()
    {
        try
        {
            string playerName = await CloudSaveManager.Instance.GetPlayerDisplayNameAsync();
            currentNameText.text = playerName;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to get player name: {ex.Message}");
            currentNameText.text = "Player";
        }
    }

    private void ClearInputFields()
    {
        newNameInput.text = "";
        newDescriptionInput.text = "";
        newPersonalStatusInput.text = "";
        dayInput.text = "";
        monthInput.text = "";
        yearInput.text = "";
    }

    public Task WaitForUIUpdate()
    {
        uiUpdateCompletionSource = new TaskCompletionSource<bool>();
        return uiUpdateCompletionSource.Task;
    }

    private async void ChangeDisplayName()
    {
        string newName = newNameInput.text.Trim();
        if (string.IsNullOrEmpty(newName))
        {
            Debug.LogError("Display name cannot be empty");
            return;
        }

        try
        {
            bool success = await CloudSaveManager.Instance.UpdatePlayerDisplayNameAsync(newName);
            if (success)
            {
                await UpdateDisplayNameText();
                newNameInput.text = "";
                Debug.Log("Display name updated successfully");
            }
            else
            {
                Debug.LogError("Failed to update display name");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to change display name: {ex.Message}");
        }
    }

    private async void ChangeDescription()
    {
        string newDescription = newDescriptionInput.text.Trim();
        if (string.IsNullOrEmpty(newDescription))
        {
            Debug.LogError("Description cannot be empty");
            return;
        }

        try
        {
            currentProfile.description = newDescription;
            bool success = await CloudSaveManager.Instance.SavePlayerProfileAsync(currentProfile);
            if (success)
            {
                currentDescriptionText.text = newDescription;
                newDescriptionInput.text = "";
                Debug.Log("Description updated successfully");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to change description: {ex.Message}");
        }
    }

    private async void ChangePersonalStatus()
    {
        string newStatus = newPersonalStatusInput.text.Trim();
        if (string.IsNullOrEmpty(newStatus))
        {
            Debug.LogError("Personal status cannot be empty");
            return;
        }

        try
        {
            currentProfile.personalStatus = newStatus;
            bool success = await CloudSaveManager.Instance.SavePlayerProfileAsync(currentProfile);
            if (success)
            {
                currentPersonalStatusText.text = newStatus;
                newPersonalStatusInput.text = "";
                Debug.Log("Personal status updated successfully");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to change personal status: {ex.Message}");
        }
    }

    private async void ChangeBirthDate()
    {
        string day = dayInput.text.Trim();
        string month = monthInput.text.Trim();
        string year = yearInput.text.Trim();

        if (string.IsNullOrEmpty(day) || string.IsNullOrEmpty(month) || string.IsNullOrEmpty(year))
        {
            Debug.LogError("Day, month, and year cannot be empty");
            return;
        }

        if (!PlayerBirthDate.TryParseFromStrings(day, month, year, out PlayerBirthDate newBirthDate))
        {
            Debug.LogError("Invalid date. Please check the values.");
            return;
        }

        try
        {
            currentProfile.birthDate = newBirthDate;
            bool success = await CloudSaveManager.Instance.SavePlayerProfileAsync(currentProfile);
            if (success)
            {
                currentBirthDateText.text = currentProfile.birthDate.ToString();
                dayInput.text = "";
                monthInput.text = "";
                yearInput.text = "";
                Debug.Log("Birth date updated successfully");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to change birth date: {ex.Message}");
        }
    }

    private void NextIcon()
    {
        if (playerIcons == null) return;
        temporaryIconIndex = playerIcons.GetNextIconIndex(temporaryIconIndex);
        UpdateSelectionIcon();
    }

    private void PreviousIcon()
    {
        if (playerIcons == null) return;
        temporaryIconIndex = playerIcons.GetPreviousIconIndex(temporaryIconIndex);
        UpdateSelectionIcon();
    }

    private async void ChangeIcon()
    {
        try
        {
            currentProfile.iconIndex = temporaryIconIndex;
            bool success = await CloudSaveManager.Instance.SavePlayerProfileAsync(currentProfile);
            if (success)
            {
                Debug.Log("Icon updated successfully");
                // La UI se actualizará automáticamente a través del evento OnProfileDataSaved
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to change icon: {ex.Message}");
        }
    }

    private void UpdateIconDisplay()
    {
        if (playerIcons != null && currentProfile != null)
        {
            // Icono actual (guardado en nube)
            if (currentIconImage != null)
            {
                currentIconImage.sprite = playerIcons.GetIcon(currentProfile.iconIndex);
            }

            // Icono de selección (temporal)
            UpdateSelectionIcon();
        }
    }

    private void UpdateSelectionIcon()
    {
        if (selectionIconImage != null && playerIcons != null)
        {
            selectionIconImage.sprite = playerIcons.GetIcon(temporaryIconIndex);
        }
    }

    private void CloseWindow()
    {
        if (profileWindow != null)
        {
            profileWindow.HideWindow();
        }
    }
}