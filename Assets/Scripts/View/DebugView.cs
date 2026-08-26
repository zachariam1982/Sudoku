using UnityEngine;
using UnityEngine.UI;

public class DebugView : MonoBehaviour
{
    [SerializeField] private GameObject scrollview;
    private bool isEnabled = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        scrollview.SetActive(isEnabled);
        GetComponent<Button>().onClick.AddListener(() => 
        { 
            isEnabled = !isEnabled; 
            scrollview.SetActive(isEnabled);
        });
    }
}
