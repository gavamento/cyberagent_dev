using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 難易度選択のポップアップ。選ぶと画面が割れて難易度ごとのシーンへ進む
public class DifficultyPopup : MonoBehaviour
{
    [SerializeField] private Button easyButton;
    [SerializeField] private Button normalButton;
    [SerializeField] private Button hardButton;
    [SerializeField] private Button endlessButton;
    [SerializeField] private Button backButton;

    [Header("遷移先のシーン名")]
    [SerializeField] private string easyScene = "Game_Easy";
    [SerializeField] private string normalScene = "Game_Normal";
    [SerializeField] private string hardScene = "Game_Hard";
    [SerializeField] private string endlessScene = "Game_Endless";

    private Action onClosed;

    void Awake()
    {
        easyButton.onClick.AddListener(() => Select(easyScene));
        normalButton.onClick.AddListener(() => Select(normalScene));
        hardButton.onClick.AddListener(() => Select(hardScene));
        endlessButton.onClick.AddListener(() => Select(endlessScene));
        backButton.onClick.AddListener(Close);
    }

    public void Open(Action closed)
    {
        onClosed = closed;
        gameObject.SetActive(true);
        SetInteractable(true);
        // キー操作でもすぐ選べるようにする
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(normalButton.gameObject);
    }

    public void Close()
    {
        gameObject.SetActive(false);
        var closed = onClosed;
        onClosed = null;
        closed?.Invoke();
    }

    void Select(string sceneName)
    {
        // 演出中の二重押しを防ぐ。シーンが無くて進めなければ戻す
        SetInteractable(false);
        if (!ScreenShatterTransition.Load(sceneName)) SetInteractable(true);
    }

    void SetInteractable(bool value)
    {
        easyButton.interactable = value;
        normalButton.interactable = value;
        hardButton.interactable = value;
        endlessButton.interactable = value;
        backButton.interactable = value;
    }
}
