using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace TankSelection
{
    public class MapSelection : MonoBehaviour
    {
        [SerializeField] private Button backButton;
        [SerializeField] private CanvasGroup canvas;
        private Tween tween;
        
        private void Start()
        {
            backButton.onClick.AddListener(Back);
        }

        public void Show()
        {
            tween?.Kill();
            gameObject.SetActive(true);
            canvas.alpha = 0;
            tween = canvas.DOFade(1, 0.75f);
        }
        private void Back()
        {
            tween?.Kill();
            tween = canvas.DOFade(0,0.75f).OnComplete(() => gameObject.SetActive(false));
        }
    }
}
