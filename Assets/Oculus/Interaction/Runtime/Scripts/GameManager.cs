using UnityEngine;

namespace MyProject
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private int boxesGrabbed = 0;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void BoxGrabbed()
        {
            boxesGrabbed++;
            Debug.Log("Boxes grabbed: " + boxesGrabbed);
        }

        public int GetBoxesGrabbed()
        {
            return boxesGrabbed;
        }
    }
}
