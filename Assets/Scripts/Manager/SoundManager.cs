using System.Threading;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;

public enum VolumeChannel : byte
{ 
    Master,
    BGM,
    SFX,
}

public enum SoundId 
{
    None,
    // BGM
    BGM_Title,
    BGM_Title2,

    // SFX
}

public class SoundManager : SingletonBase<SoundManager>
{
    [SerializeField] private SoundDatabase _database;
    [SerializeField] private AudioMixer _mixer;
    [SerializeField] private AudioMixerGroup _bgmGroup;
    [SerializeField] private AudioMixerGroup _sfxGroup;

    private AudioSource[] _bgmSources;
    private int _activeBgmIndex;
    private SoundId _currentBgm = SoundId.None;
    private CancellationTokenSource _bgmFadeCts;
    private float _bgmFadeTime = 1f;

    private AudioSource[] _sfxSources;
    private int _sfxMaxSize = 16;
    private int _nextSfxIndex;

    private void Start()
    {
        _bgmSources = new AudioSource[2];
        for(int i = 0; i < _bgmSources.Length; i++)
        {
            _bgmSources[i] = CreateSource("BGM_" + (i + 1), _bgmGroup, loop: true);
        }

        _sfxSources = new AudioSource[_sfxMaxSize];
        for(int i = 0; i < _sfxMaxSize; i++)
        {
            _sfxSources[i] = CreateSource("SFX_" + (i + 1), _sfxGroup, loop: false);
        }
    }

    private AudioSource CreateSource(string name, AudioMixerGroup group, bool loop)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(transform);
        AudioSource source = child.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = group;
        source.loop = loop;
        source.playOnAwake = false;
        return source;
    }

    // BGM
    public void PlayBGM(SoundId id)
    {
        if (id == _currentBgm)
            return;
        if(_database.TryGet(id, out SoundDatabase.Entry entry) == false || entry.Clip == null || entry.Clip.length == 0)
        {
            Debug.LogWarning("[SoundManager] 등록되짖 않은 BGM: " + id);
        }

        _currentBgm = id;
        AudioSource from = _bgmSources[_activeBgmIndex];
        _activeBgmIndex = 1 - _activeBgmIndex;
        AudioSource to =_bgmSources[_activeBgmIndex];

        to.clip = entry.Clip;
        to.volume = 0;
        to.Play();

        _ = CrossFade(from, to, 1f);
    }

    public void StopBGM()
    {
        _currentBgm = SoundId.None;
        _ = CrossFade(_bgmSources[_activeBgmIndex], null, 0f);
    }

    private async Awaitable CrossFade(AudioSource from, AudioSource to, float toVolume)
    {
        _bgmFadeCts?.Cancel();
        _bgmFadeCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        CancellationToken token = _bgmFadeCts.Token;

        float fromStart = from != null ? from.volume : 0f;
        float time = 0f;

        try
        {
            while (time < _bgmFadeTime)
            {
                time += Time.unscaledDeltaTime;
                float t = time / _bgmFadeTime;
                if (from != null) from.volume = Mathf.Lerp(fromStart, 0f, t);
                if (to != null) to.volume = Mathf.Lerp(0f, 1f, t);
                await Awaitable.NextFrameAsync(token);
            }
        }
        catch(System.OperationCanceledException)
        {
            return;
        }
    }
    // SFX
    public void PlaySFX(SoundId id)
    {
        if (!_database.TryGet(id, out SoundDatabase.Entry entry) || entry.Clip == null)
        {
            Debug.LogWarning("[SoundManager] 등록되지 않은 사운드: " + id);
            return;
        }

        AudioSource source = GetFreeSfxSource();
        source.clip = entry.Clip;
        source.volume = 1f;
        source.Play();
    }

    private AudioSource GetFreeSfxSource()
    {
        // 재생 중이 아닌 소스를 찾고, 전부 사용 중이면 가장 오래된 것을 덮어씀
        for (int i = 0; i < _sfxSources.Length; i++)
        {
            int index = (_nextSfxIndex + i) % _sfxSources.Length;
            if (!_sfxSources[index].isPlaying)
            {
                _nextSfxIndex = (index + 1) % _sfxSources.Length;
                return _sfxSources[index];
            }
        }
        AudioSource oldest = _sfxSources[_nextSfxIndex];
        _nextSfxIndex = (_nextSfxIndex + 1) % _sfxSources.Length;
        return oldest;
    }

    // Set Volume
    public void SetVolume(VolumeChannel channel, float value)
    {
        value = Mathf.Clamp01(value);
        float db = value > 0.0001f ? Mathf.Log10(value) * 20f : -80f;
        _mixer.SetFloat(channel + "Volume", db);
        PlayerPrefs.SetFloat("Volume_" + channel, value);
    }
}
