using UnityEngine;
using UnityEngine.Audio;

public enum VolumeChannel : byte
{ 
    Master,
    BGM,
    SFX,
}

public class SoundManager : SingletonBase<SoundManager>
{
    [SerializeField] private AudioMixer _mixer;
    [SerializeField] private AudioMixerGroup _bgmGroup;
    [SerializeField] private AudioMixerGroup _sfxGroup;

    private void Awake()
    {
        
    }
}
