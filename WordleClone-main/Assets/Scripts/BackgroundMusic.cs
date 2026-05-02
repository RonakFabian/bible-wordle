using UnityEngine;

public class BackgroundMusic : MonoBehaviour
{
  public static BackgroundMusic Instance;

  [Header("Audio Source")]
  [SerializeField] private AudioSource audioSource;

  private void Awake()
  {
    // Singleton setup
    if (Instance == null)
    {
      Instance = this;
      DontDestroyOnLoad(gameObject); // Keeps object between scenes
    }
    else
    {
      Destroy(gameObject);
      return;
    }

    // Auto assign AudioSource if missing
    if (audioSource == null)
      audioSource = GetComponent<AudioSource>();
  }

  public void PlayMusic(AudioClip clip, bool loop = true)
  {
    if (clip == null) return;

    audioSource.clip = clip;
    audioSource.loop = loop;
    audioSource.Play();
  }

  public void StopMusic()
  {
    audioSource.Stop();
  }

  public void SetVolume(float volume)
  {
    audioSource.volume = volume;
  }
}