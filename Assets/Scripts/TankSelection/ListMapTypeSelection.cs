using UnityEngine;
using UnityEngine.UI;

namespace TankSelection
{
    public class ListMapTypeSelection : MonoBehaviour
    {
        [SerializeField] private Button backButton;
        [SerializeField] private GameObject mapSelection;
        private void Start()
        {
            backButton.onClick.AddListener(Back);
        }
        private void Back()
        {
            gameObject.SetActive(false);
            mapSelection.SetActive(true);
        }
    }
}
