using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

// ภาษาในแผง Settings ใช้ร่วมกันได้ทั้งเมนูหลักและเมนู Pause
//
// วิธีใช้:
// 1. ติดสคริปต์นี้ไว้ที่ Settings Panel
// 2. ลาก TMP Dropdown มาใส่ใน languageDropdown
// 3. สร้าง Locale ใน Localization Settings และเพิ่ม Locale เหล่านั้นใน Available Locales
// 4. สร้าง String Table แล้วใช้ LocalizedString / LocalizedStringEvent กับข้อความ UI
public class LanguageSettingsUI : MonoBehaviour
{
    [Header("ตัวเลือกภาษา")]
    [Tooltip("TMP Dropdown ที่จะแสดงภาษาที่มีอยู่ใน Localization Settings")]
    public TMP_Dropdown languageDropdown;

    private readonly List<Locale> availableLocales = new List<Locale>();
    private Coroutine refreshCoroutine;
    private bool isRefreshing;
    private bool isSubscribed;

    private void Awake()
    {
        if (languageDropdown != null)
        {
            languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        }

        LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
        isSubscribed = true;
    }

    private void OnEnable()
    {
        if (refreshCoroutine != null)
        {
            StopCoroutine(refreshCoroutine);
        }

        refreshCoroutine = StartCoroutine(RefreshLanguageDropdown());
    }

    private IEnumerator RefreshLanguageDropdown()
    {
        if (languageDropdown == null)
        {
            Debug.LogError("[LanguageSettingsUI] Language Dropdown is not assigned.", this);
            refreshCoroutine = null;
            yield break;
        }

        yield return LocalizationSettings.InitializationOperation;

        if (!LocalizationSettings.InitializationOperation.IsValid() ||
            LocalizationSettings.InitializationOperation.Status !=
            UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
        {
            Debug.LogError("[LanguageSettingsUI] Unity Localization failed to initialize.", this);
            refreshCoroutine = null;
            yield break;
        }

        availableLocales.Clear();
        availableLocales.AddRange(LocalizationSettings.AvailableLocales.Locales);

        if (availableLocales.Count == 0)
        {
            Debug.LogError(
                "[LanguageSettingsUI] No locales are configured. Add locales to Localization Settings.",
                this);
            languageDropdown.ClearOptions();
            languageDropdown.interactable = false;
            refreshCoroutine = null;
            yield break;
        }

        var options = new List<TMP_Dropdown.OptionData>(availableLocales.Count);
        foreach (Locale locale in availableLocales)
        {
            options.Add(new TMP_Dropdown.OptionData(GetLocaleDisplayName(locale)));
        }

        isRefreshing = true;
        languageDropdown.ClearOptions();
        languageDropdown.AddOptions(options);
        languageDropdown.interactable = true;
        languageDropdown.value = GetSelectedLocaleIndex();
        languageDropdown.RefreshShownValue();
        isRefreshing = false;
        refreshCoroutine = null;
    }

    private void OnLanguageChanged(int index)
    {
        if (isRefreshing || index < 0 || index >= availableLocales.Count)
        {
            return;
        }

        Locale selectedLocale = availableLocales[index];
        if (selectedLocale == null)
        {
            Debug.LogError("[LanguageSettingsUI] The selected locale is missing.", this);
            return;
        }

        LocalizationSettings.SelectedLocale = selectedLocale;
    }

    private void OnSelectedLocaleChanged(Locale locale)
    {
        if (languageDropdown == null || locale == null || availableLocales.Count == 0)
        {
            return;
        }

        int selectedIndex = availableLocales.IndexOf(locale);
        if (selectedIndex < 0)
        {
            return;
        }

        isRefreshing = true;
        languageDropdown.SetValueWithoutNotify(selectedIndex);
        languageDropdown.RefreshShownValue();
        isRefreshing = false;
    }

    private int GetSelectedLocaleIndex()
    {
        Locale selectedLocale = LocalizationSettings.SelectedLocale;
        if (selectedLocale == null)
        {
            return 0;
        }

        int selectedIndex = availableLocales.IndexOf(selectedLocale);
        return selectedIndex >= 0 ? selectedIndex : 0;
    }

    private string GetLocaleDisplayName(Locale locale)
    {
        if (locale == null)
        {
            return "Unknown";
        }

        if (!string.IsNullOrWhiteSpace(locale.LocaleName))
        {
            return locale.LocaleName;
        }

        return locale.Identifier.Code;
    }

    private void OnDestroy()
    {
        if (languageDropdown != null)
        {
            languageDropdown.onValueChanged.RemoveListener(OnLanguageChanged);
        }

        if (isSubscribed)
        {
            LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
            isSubscribed = false;
        }
    }
}
