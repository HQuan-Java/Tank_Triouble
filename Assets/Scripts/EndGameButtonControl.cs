using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndGameButtonControl : MonoBehaviour
{
    [SerializeField]
    private Button replayButton;
    [SerializeField]
    private Button backButton;

    private void Start()
    {
        replayButton.onClick.AddListener(Replay);
        backButton.onClick.AddListener(Back);
    }
    private void Replay()
    {
        var pars = SceneParameter.Instance;
        SceneManager.LoadScene(pars.SceneName);
    }
    private void Back()
    {
        SceneManager.LoadScene("MapSelection");
    }
}