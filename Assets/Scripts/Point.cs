using UnityEngine;

namespace MazeTemplate
{
    public class Point : MonoBehaviour
    {
        [SerializeField] private int pointValue = 10;

        public int PointValue => pointValue;
    }
}