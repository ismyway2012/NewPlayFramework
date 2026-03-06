using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

namespace NewPlay.ArcadeIdle
{
    [RequireComponent(typeof(Image))]
    public class SettingsWindow : MonoBehaviour
    {
        [SerializeField, Tooltip("Main panel of the settings window.")]
        private RectTransform mainPanel;

        [SerializeField, Tooltip("Audio mixer to control the game's audio groups.")]
        private AudioMixer audioMixer;

        [SerializeField, Tooltip("Toggle to enable or disable sound effects (SFX).")]
        private Toggle sfxToggle;

        [SerializeField, Tooltip("Toggle to enable or disable background music (BGM).")]
        private Toggle bgmToggle;

        private void Start()
        {
            GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.9f);
            mainPanel.anchoredPosition = Vector2.zero;

            // Initialize SFX Settings
            sfxToggle.isOn = PlayerPrefs.GetInt("SFX ON", 1) > 0;
            audioMixer.SetFloat("SFX Volume", sfxToggle.isOn ? 0f : -80f);

            sfxToggle.onValueChanged.AddListener((isOn) =>
            {
                PlayerPrefs.SetInt("SFX ON", isOn ? 1 : 0);
                audioMixer.SetFloat("SFX Volume", isOn ? 0f : -80f);
            });

            // Initialize BGM Settings
            bgmToggle.isOn = PlayerPrefs.GetInt("BGM ON", 1) > 0;
            audioMixer.SetFloat("BGM Volume", bgmToggle.isOn ? 0f : -80f);

            bgmToggle.onValueChanged.AddListener((isOn) =>
            {
                PlayerPrefs.SetInt("BGM ON", isOn ? 1 : 0);
                audioMixer.SetFloat("BGM Volume", isOn ? 0f : -80f);
            });

            gameObject.SetActive(false);
        }
    }
}
