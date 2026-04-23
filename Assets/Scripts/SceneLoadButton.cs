using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoadButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private string sceneName;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (button != null)
            button.onClick.AddListener(LoadSceneByName);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(LoadSceneByName);
    }

    public void LoadSceneByName()
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[SceneLoadButton] Scene name is empty.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
