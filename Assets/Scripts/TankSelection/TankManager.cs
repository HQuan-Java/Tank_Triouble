using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

namespace TankSelection
{
    public class TankManager : MonoBehaviour
    {
        [SerializeField] private List<Tank> tanks;
        [SerializeField] private UI3DRotate rotate;
        [SerializeField] private UnityEngine.UI.Button nextButton;
        [SerializeField] private UnityEngine.UI.Button prevButton;
        [SerializeField] private UnityEngine.UI.Button playNowButton;
        [Header("Slide Settings")]
        [SerializeField] private float slideDuration = 0.5f;
        [SerializeField] private Ease slideEase = Ease.OutCubic;
        [Header("UI Text References")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI typeText;
        [SerializeField] private TextMeshProUGUI weightText;
        [SerializeField] private TextMeshProUGUI crewText;
        [SerializeField] private TextMeshProUGUI armorText;
        [SerializeField] private TextMeshProUGUI mainGunText;
        [SerializeField] private TextMeshProUGUI maxSpeedText;
        [SerializeField] private TextMeshProUGUI engineText;
        [SerializeField] private TextMeshProUGUI characteristicsText;
        [SerializeField] private MapSelection mapSelection;
        private int currentTankIndex = 0;
        private Tank CurrentTank => tanks[currentTankIndex];
        
        private void Awake()
        {
            Cursor.visible = true;
            nextButton.onClick.AddListener(Next);
            prevButton.onClick.AddListener(Previous);
            playNowButton.onClick.AddListener(PlayNow);
            rotate.SetTarget(CurrentTank.transform);
            UpdateTankUI();
        }

        private void PlayNow()
        {
            mapSelection.Show();
        }

        private void Next()
        {
            currentTankIndex++;
            if (currentTankIndex >= tanks.Count)
            {
                currentTankIndex = 0;
            }
            SlideToCurrentTank();
        }
        
        private void Previous()
        {
            currentTankIndex--;
            if (currentTankIndex < 0)
            {
                currentTankIndex = tanks.Count - 1;
            }
            SlideToCurrentTank();
        }

        private void SlideToCurrentTank()
        {
            float targetX = -CurrentTank.transform.localPosition.x;
            transform.DOKill();
            transform.DOLocalMoveX(targetX, slideDuration).SetEase(slideEase);
            rotate.SetTarget(CurrentTank.transform);
            UpdateTankUI();
        }

        private void UpdateTankUI()
        {
            if (CurrentTank == null) return;

            float delayStep = 0.05f;
            float currentDelay = 0f;
            
            SetTextAndAnimate(nameText, $"<color=#FFD700><b>{CurrentTank.tankName}</b></color>", currentDelay);
            currentDelay += delayStep;
            
            SetTextAndAnimate(typeText, FormatField("Loại", CurrentTank.tankType), currentDelay);
            currentDelay += delayStep;
            
            SetTextAndAnimate(weightText, FormatField("Khối lượng", CurrentTank.weight), currentDelay);
            currentDelay += delayStep;

            SetTextAndAnimate(crewText, FormatField("Kíp lái", CurrentTank.crew), currentDelay);
            currentDelay += delayStep;

            SetTextAndAnimate(armorText, FormatField("Giáp", CurrentTank.armor), currentDelay);
            currentDelay += delayStep;

            SetTextAndAnimate(mainGunText, FormatField("Pháo chính", CurrentTank.mainGun), currentDelay);
            currentDelay += delayStep;

            SetTextAndAnimate(maxSpeedText, FormatField("Tốc độ tối đa", CurrentTank.maxSpeed), currentDelay);
            currentDelay += delayStep;

            SetTextAndAnimate(engineText, FormatField("Động cơ", CurrentTank.engine), currentDelay);
            currentDelay += delayStep;
            
            if (characteristicsText != null) 
            {
                SetTextAndAnimate(characteristicsText, 
                    $"<color=#FCA311><b>Đặc điểm:</b></color>\n<size=90%><color=#E0E0E0>{CurrentTank.characteristics}</color></size>", 
                    currentDelay);
            }
        }

        private void SetTextAndAnimate(TextMeshProUGUI txt, string content, float delay)
        {
            if (txt == null) return;
            
            // Gán chữ mới
            txt.text = content;
            
            // Dừng mọi hiệu ứng cũ nếu có để không bị lỗi đè hoạt ảnh
            txt.DOKill();
            txt.transform.DOKill();
            
            // Reset Alpha về 0
            Color c = txt.color;
            c.a = 0f;
            txt.color = c;
            
            // Transform Scale nhỏ lại 1 chút
            txt.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            
            // Animation diễn ra
            txt.DOFade(1f, 0.3f).SetDelay(delay);
            txt.transform.DOScale(1f, 0.3f).SetDelay(delay).SetEase(Ease.OutBack);
        }

        private string FormatField(string label, string value)
        {
            return $"<color=#FCA311><b>{label}:</b></color> <color=#FFFFFF>{value}</color>";
        }

        [Button]
        public void ArrangeTanks(float spaceX)
        {
            for (var i = 0; i < tanks.Count; i++)
            {
                var tank = tanks[i];
                var tf = tank.transform;
                tf.localPosition = new Vector3(spaceX * i, 0, 0);
            }
        }
    }
}
