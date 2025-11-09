using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class UIManager : MonoBehaviour, IGameModule
    {
        public static UIManager Instance => s_Instance;
        private static UIManager s_Instance;

        [Header("Canvases")]
        [SerializeField] private UISubCanvas _playCanvas;
        [SerializeField] private UISubCanvas _dieCanvas;
        [SerializeField] private UISubCanvas _winCanvas;
        [SerializeField] private UISubCanvas _settingsCanvas;

        [Header("Other")]
        [SerializeField] private AudioClip _startClip;

        private List<UISubCanvas> _canvases = new List<UISubCanvas>();

        public PlayCanvas PlayCanvas => _playCanvas as PlayCanvas;
        public DieCanvas DieCanvas => _dieCanvas as DieCanvas;
        public UISubCanvas WinCanvas => _winCanvas;
        public UISubCanvas SettingsCanvas => _settingsCanvas;

        private void OnEnable()
        {
            SetupInstance();
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

        private void Start()
        {
            //SoundManager.Instance.PlaySoundEffect(_startClip);
        }

        private void ShowCanvas(UISubCanvas canvas)
        {
            foreach (var item in _canvases)
                item.gameObject.SetActive(item == canvas);
        }

        public void ShowPlayCanvas()
        {
            ShowCanvas(_playCanvas);
        }

        public void ShowDieCanvas()
        {
            ShowCanvas(_dieCanvas);
        }
        public void ShowWinCanvas()
        {
            ShowCanvas(_winCanvas);
        }

        public void ShowSettingsCanvas()
        {
            ShowCanvas(_settingsCanvas);
        }

        public void Load()
        {
            _canvases.Add(_playCanvas);
            _canvases.Add(_dieCanvas);
            _canvases.Add(_winCanvas);
            _canvases.Add(_settingsCanvas);

            GameManager.Pause(false);

            ShowPlayCanvas();
        }
    }

}
