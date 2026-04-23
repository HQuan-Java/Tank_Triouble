using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TankSelection
{
    public class SelectionMapType : MonoBehaviour
    {
        [SerializeField] private MapType mapType;
        [SerializeField] private Button button;
        [SerializeField] private int totalEnemy = 1;
        private void Start()
        {
            button.onClick.AddListener(StartGame);
        }

        private void StartGame()
        {
            var pars = SceneParameter.Instance;
            pars.MapType = mapType;
            pars.StartEnemy = 1;
            SceneManager.LoadScene(pars.SceneName);
        }
    }
}