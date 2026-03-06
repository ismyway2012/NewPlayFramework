using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;

namespace NewPlay.ArcadeIdle
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField, Tooltip("Audio source for playing background music (BGM)")]
        private AudioSource BGMPlayer;

        [SerializeField, Tooltip("Audio source for playing sound effects (SFX)")]
        private AudioSource SFXPlayer;

        [SerializeField, Tooltip("Duration for fading in/out background music")]
        private float fadeDuration = 0.75f;

        [SerializeField, Tooltip("List of sound effects data")]
        private List<AudioData> SFXList;

        // A dictionary that maps AudioID values to their corresponding AudioData,
        // allowing efficient lookup of sound effects by their ID
        private Dictionary<AudioID, AudioData> SFXLookup;

        private AudioClip currentBGM;
        private float originalBGMVolume;

        void Awake()
        {
            // Singleton pattern to ensure only one instance of AudioManager exists across scenes.
            if (Instance == null)
            {
                Instance = this; // If no instance exists, assign this instance as the singleton
                DontDestroyOnLoad(gameObject); // Prevent the instance from being destroyed when loading new scenes
            }
            else
            {
                Destroy(gameObject); // Destroy the duplicate instance
                return; // Exit the method to prevent further execution
            }

            originalBGMVolume = BGMPlayer.volume;
            SFXLookup = SFXList.ToDictionary(x => x.id);
        }

        /// <summary>
        /// Plays the background music (BGM) by fading in the new clip.
        /// If the clip is the same as the current BGM or is null, no action is taken.
        /// </summary>
        /// <param name="clip">The AudioClip to play as the new BGM.</param>
        /// <param name="loop">Determines whether the BGM should loop (default is true).</param>
        /// <param name="fade">Determines whether to fade the BGM in (default is true).</param>
        public void PlayBGM(AudioClip clip, bool loop = true, bool fade = true)
        {
            if (clip == null || clip == currentBGM) return;

            currentBGM = clip;
            StartCoroutine(PlayBGMAsync(clip, loop, fade));
        }

        /// <summary>
        /// Plays a sound effect (SFX) by playing the given AudioClip.
        /// Optionally pauses the BGM while the SFX is playing and restores the BGM after the clip finishes.
        /// </summary>
        /// <param name="clip">The AudioClip representing the sound effect to play.</param>
        /// <param name="pauseBGM">Determines whether to pause the background music while the SFX is playing (default is false).</param>
        public void PlaySFX(AudioClip clip, bool pauseBGM = false)
        {
            if (clip == null) return;

            if (pauseBGM)
            {
                BGMPlayer.Pause();
                StartCoroutine(UnPauseBGM(clip.length));
            }

            SFXPlayer.PlayOneShot(clip);
        }

        /// <summary>
        /// Plays a sound effect (SFX) based on an audio ID.
        /// Optionally pauses the BGM while the SFX is playing and restores the BGM after the clip finishes.
        /// </summary>
        /// <param name="audioID">The AudioID representing the sound effect to play.</param>
        /// <param name="pauseBGM">Determines whether to pause the background music while the SFX is playing (default is false).</param>
        public void PlaySFX(AudioID audioID, bool pauseBGM = false)
        {
            if (!SFXLookup.ContainsKey(audioID)) return;

            var audioData = SFXLookup[audioID];
            PlaySFX(audioData.clip, pauseBGM);
        }

        IEnumerator PlayBGMAsync(AudioClip clip, bool loop, bool fade)
        {
            if (fade) yield return BGMPlayer.DOFade(0, fadeDuration).WaitForCompletion();

            BGMPlayer.clip = clip;
            BGMPlayer.loop = loop;
            BGMPlayer.Play();

            if (fade) yield return BGMPlayer.DOFade(originalBGMVolume, fadeDuration).WaitForCompletion();
        }

        IEnumerator UnPauseBGM(float delay)
        {
            yield return new WaitForSeconds(delay);
            BGMPlayer.volume = 0;
            BGMPlayer.UnPause();
            BGMPlayer.DOFade(originalBGMVolume, fadeDuration);
        }
    }

    public enum AudioID
    {
        Money,
        Pop,
        Trash,
        Bin,
        Magical,
        Kaching
    }

    [System.Serializable]
    public class AudioData
    {
        [Tooltip("Unique ID for each audio clip")]
        public AudioID id;

        [Tooltip("The audio clip associated with the audio ID")]
        public AudioClip clip;
    }
}
