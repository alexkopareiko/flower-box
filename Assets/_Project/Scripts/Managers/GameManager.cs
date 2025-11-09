using UnityEngine;
using UnityEngine.SceneManagement;
using MaskTransitions;

namespace Game
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance => s_Instance;
        private static GameManager s_Instance;

        private static bool _isPaused = false;
        internal static bool isPaused;

        private void OnEnable()
        {

            SetupInstance();

            LoadSequencer.LastModuleLoaded += Load;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            LoadSequencer.LastModuleLoaded -= Load;
        }

        private void SetupInstance()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            s_Instance = this;
        }

        public void Load()
        {
            CollectReferences();

            // Initialize components
        }

        public void GameOver()
        {
            // Save max score
            if (SaveManager.Instance != null)
            {
                
                SaveManager.Instance.LosesCount = SaveManager.Instance.LosesCount + 1;
            }

            // Play ouch sound on death
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayOuchSound();
            }

            // Pause and show Die canvas
            Pause(true);
            if (UIManager.Instance != null && UIManager.Instance.DieCanvas != null)
            {
                UIManager.Instance.ShowDieCanvas();
            }
        }

        public void Revive()
        {
            // Hide die canvas and resume
            // if (UIManager.Instance != null)
            // {
            //     UIManager.Instance.ShowPlayCanvas();
            //     if (UIManager.Instance.PlayCanvas != null)
            //         // UIManager.Instance.PlayCanvas.SetScore(_score);
            // }
            Pause(false);
        }

        public static void Pause(bool value)
        {
            _isPaused = value;
            Time.timeScale = value ? 0f : 1f;
        }


        void CollectReferences()
        {
            // _comp = FindFirstObjectByType<Comp>();
        }
    }
}
