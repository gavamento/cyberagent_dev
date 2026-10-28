using UnityEngine;
using UnityEngine.UI;

public class UIImageScript : MonoBehaviour
{
    private float limitTimer = 0;
    private Text image;

    void Awake()
    {
        image = GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        limitTimer -= Time.deltaTime;
       // this.Image

        if (limitTimer < 0)
        {
            limitTimer = 0;
            Destroy(gameObject);
        }
    }

    public void SetTime(Vector3 position, float time) { transform.position = position;limitTimer = time; }

    public void SetText(string text)
    { image.text = text; }

    public void SetColor(Color color) { image.color = color; }
}
