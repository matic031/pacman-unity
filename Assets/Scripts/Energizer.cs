using UnityEngine;


namespace MazeTemplate
{
    public class Energizer : MonoBehaviour
    {
        [SerializeField] private int pointValue = 50; // Točke, ki jih da ta energizer
        [SerializeField] private float frightenedDuration = 10f; // Kako dolgo so duhovi prestrašeni (nastavljeno na 10 sekund)

        public int PointValue => pointValue;
        public float FrightenedDuration => frightenedDuration;

        
    }
}