using UnityEngine;

public class CrickChangeScene : MonoBehaviour
{
    [Header("遷移先のシーン名")]
    [SerializeField] private string targetSceneName;

    void Update()
    {
        // 左クリックすると指定されたシーンへ遷移
        if (Input.GetMouseButtonUp(0))
        {
            if (!string.IsNullOrEmpty(targetSceneName))
            {
                SceneController.ChangeScene(targetSceneName);
            }
        }
    }
}