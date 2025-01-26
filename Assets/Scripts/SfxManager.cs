using UnityEngine;
using UnityEngine.Serialization;

public class SfxManager : MonoBehaviour
{
    public static SfxManager Instance { get; private set; }

    [Header("Audio Files")]
    public AudioSource jumpSound;
    public AudioSource dashSound;
    public AudioSource dashStopSound;
    public AudioSource walkingSound;
    
    public AudioSource refillSound;
    public AudioSource retrySound; 

    public AudioSource defaultSource;

    private bool _walkingPlaying;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("More than one audio manager!");
            return;
        }
        
        Instance = this;

        defaultSource = GetComponent<AudioSource>();
        if (defaultSource == null)
        {
            Debug.LogError("No default audiosource set for SfxManager");
        }

        _walkingPlaying = false;
    }

    public void PlayAudio(AudioClip clip, AudioSource source = null)
    {
        (source ?? defaultSource).PlayOneShot(clip);
    }

    public void PlayJumpSound()
    {
        jumpSound.Play();
    }

    public void PlayDashSound()
    {
        dashSound.Play();
    }

    public void PlayDashStopSound()
    {
        dashSound.Stop();
        dashStopSound.Play();
    }

    public void StartWalkSound()
    {
        if(_walkingPlaying) return;
        
        walkingSound.loop = true;
        walkingSound.Play();
        _walkingPlaying = true;
    }

    public void StopWalkSound()
    {
        if(!_walkingPlaying) return;
        
        walkingSound.loop = false;
        walkingSound.Stop();
        _walkingPlaying = false;
    }

    public void RefillSound()
    {
        refillSound.Play();
    }

    public void StopRefillSound()
    {
        refillSound.Stop();
    }

    public void RetrySound()
    {
        retrySound.Play();
    }
}
