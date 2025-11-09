using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using MaskTransitions;

namespace Game
{
    public class DieCanvas : UISubCanvas
    {
        [Header("UI")]
        [SerializeField] private TMP_Text _scoreTMP;
        [SerializeField] private TMP_Text _maxScoreTMP;
        [SerializeField] private Button _reloadButton;
        [SerializeField] private Button _menuButton;
        [SerializeField] private Button _reviveButton;

        private void OnEnable()
        {
            // Ensure listeners are attached once when shown
            if (_reloadButton != null)
                _reloadButton.onClick.AddListener(OnClickReload);

            if (_menuButton != null)
                _menuButton.onClick.AddListener(OnClickMenu);

            if (_reviveButton != null)
                _reviveButton.onClick.AddListener(OnClickRevive);
        }



        private void OnDisable()
        {
            // Clean up listeners to avoid duplicate subscriptions
            if (_reloadButton != null)
                _reloadButton.onClick.RemoveListener(OnClickReload);

            if (_menuButton != null)
                _menuButton.onClick.RemoveListener(OnClickMenu);

            if (_reviveButton != null)
                _reviveButton.onClick.RemoveListener(OnClickRevive);

        }


        public void Populate()
        {

        }



        public void OnClickLeaderboard()
        {

        }



        public void OnClickReload()
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayButtonSound(SoundManager.ButtonUIType.buy);
            }
            if (_reloadButton != null) _reloadButton.interactable = false;

            LoadGameScene();

        }



        public void OnClickMenu()
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayButtonSound(SoundManager.ButtonUIType.confirm);
            }

            if (_menuButton != null)
            {
                _menuButton.interactable = false;
            }
            LoadMenuScene();
        }



        public void OnClickRevive()
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayButtonSound(SoundManager.ButtonUIType.buy);
            }

            if (_reviveButton != null) _reviveButton.interactable = false;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.Revive();
            }

            if (_reviveButton != null) _reviveButton.interactable = true;
        }



        private void LoadGameScene()
        {
            GameManager.Pause(false);

            // Prefer TransitionManager if present, else fall back to SceneManager
            if (TransitionManager.Instance != null)
                TransitionManager.Instance.LoadLevel("Game", 0f);
            else
                SceneManager.LoadScene("Game");
        }

        private void LoadMenuScene()
        {
            GameManager.Pause(false);

            if (TransitionManager.Instance != null)
                TransitionManager.Instance.LoadLevel("Menu", 0f);
            else
                SceneManager.LoadScene("Menu");
        }
    }

}

