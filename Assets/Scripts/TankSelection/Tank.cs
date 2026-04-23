using UnityEngine;

namespace TankSelection
{
    public class Tank : MonoBehaviour
    {
        [Header("Tank Attributes")]
        public string tankName;
        public string tankType;
        public string weight;
        public string crew;
        public string armor;
        public string mainGun;
        public string maxSpeed;
        public string engine;
        
        [TextArea(3, 10)]
        public string characteristics;
    }
}
