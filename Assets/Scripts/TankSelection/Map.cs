using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TankSelection
{
    public class Map : MonoBehaviour
    {
        [SerializeField] private string sceneName;
        [SerializeField] private Button selectButton;
        [SerializeField] private MapSelection mapSelection;
        [SerializeField] private ListMapTypeSelection listMapTypeSelection;
        private void Start()
        {
            selectButton.onClick.AddListener(SelectMap);
        }

        private void SelectMap()
        {
            SceneParameter.Instance.SceneName = sceneName;
            mapSelection.gameObject.SetActive(false);
            listMapTypeSelection.gameObject.SetActive(true);
        }
    }
}
