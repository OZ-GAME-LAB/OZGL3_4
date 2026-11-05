using UnityEngine;
using UnityEngine.UI;

public class SettingUI : UIBase
{
    [SerializeField] private UIButton _exitButton;
    [SerializeField] private Slider _masterSlider;
    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private Slider _sfxlider;

    private void OnEnable()
    {
        _exitButton.BindOnClickButtonEvent(ExitSettingUI);
        _masterSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        _bgmSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
        _sfxlider.onValueChanged.AddListener(OnSfxVolumeChanged);
    }

    private void OnDisable()
    {
        _exitButton.UnBindOnClickButtonEvent(ExitSettingUI);
    }

    private void ExitSettingUI()
    {
        UIManager.Instance.CloseUI(UIRootType.PopupUI, UIType.SettingUI);
    }

    private void OnMasterVolumeChanged(float value)
    {
        OnVolumeChanged(VolumeChannel.Master, value);
    }

    private void OnBgmVolumeChanged(float value)
    {
        OnVolumeChanged(VolumeChannel.BGM, value);
    }

    private void OnSfxVolumeChanged(float value)
    {
        OnVolumeChanged(VolumeChannel.SFX, value);
    }

    private void OnVolumeChanged(VolumeChannel channel, float value)
    {
        SoundManager.Instance.SetVolume(channel, value);
    }
}
