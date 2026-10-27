using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// タイトル画面のボタン処理 (押すと割れてから次へ進む)
public class TitleMenu : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private UIShatterEffect shatter;
    [SerializeField] private SettingsPopup settingsPopup;
    [SerializeField] private DifficultyPopup difficultyPopup;
    [SerializeField] private CanvasGroup menuGroup;

    private bool busy;

    void Awake()
    {
        startButton.onClick.AddListener(() => Run(startButton, OpenDifficulty));
        settingsButton.onClick.AddListener(() => Run(settingsButton, OpenSettings));
        quitButton.onClick.AddListener(() => Run(quitButton, Quit));
    }

    void Run(Button button, Action next)
    {
        // 演出中の二重押しを防ぐ
        if (busy) return;
        busy = true;
        if (menuGroup != null) menuGroup.blocksRaycasts = false;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        shatter.Shatter((RectTransform)button.transform, next);
    }

    void OpenDifficulty()
    {
        difficultyPopup.Open(() =>
        {
            startButton.gameObject.SetActive(true);
            Unlock(startButton);
        });
    }

    void OpenSettings()
    {
        settingsPopup.Open(() =>
        {
            settingsButton.gameObject.SetActive(true);
            Unlock(settingsButton);
        });
    }

    // 押したボタンを選び直して、キーボードやパッドでもすぐ操作できるようにする
    void Unlock(Button selected)
    {
        busy = false;
        if (menuGroup != null) menuGroup.blocksRaycasts = true;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(selected.gameObject);
    }

    static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
