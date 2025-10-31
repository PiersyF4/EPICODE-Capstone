using UnityEngine;
using UnityEngine.Audio;

public class SettingsMenu : MonoBehaviour
{

    public AudioMixer audioMixer;

    public void SetVolume(float volume)
    { 
    Debug.Log("Volume settato a: " + volume);
        audioMixer.SetFloat("volume", volume);
    }
}