using UnityEngine;

public class HideCursor : MonoBehaviour
{
    void  Start()
    {
        bool shouldShow = UISettingPanel.IsPopupOpen;
        Cursor.visible = shouldShow;
        Cursor.lockState = shouldShow ? CursorLockMode.None : CursorLockMode.Confined;
    }
}