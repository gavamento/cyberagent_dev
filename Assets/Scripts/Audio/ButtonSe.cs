using UnityEngine;
using UnityEngine.UI;

// Button を押したときに SE を鳴らす
[RequireComponent(typeof(Button))]
public class ButtonSe : MonoBehaviour
{
    [SerializeField] private AudioClip clickSe;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() => AudioManager.PlaySE(clickSe, volume));
    }
}
