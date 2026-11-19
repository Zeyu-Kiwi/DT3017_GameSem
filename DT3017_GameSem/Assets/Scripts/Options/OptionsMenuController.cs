using System;
using TMPro;
using UnityEngine;

public sealed class OptionsMenuController : MonoBehaviour
{
    [Serializable] public sealed class BindingRow
    {
        public GameplayAction action;
        public UnityEngine.UI.Button button;
        public TMP_Text keyLabel;
    }
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject[] mainMenuObjects;
    [SerializeField] private UnityEngine.UI.Button openButton, backButton;
    [SerializeField] private UnityEngine.UI.Slider fovSlider, sensitivitySlider;
    [SerializeField] private TMP_Text fovValue, sensitivityValue, bindingMessage;
    [SerializeField] private UnityEngine.UI.Toggle invertMouse;
    [SerializeField] private BindingRow[] bindings;
    private GameplayAction? capturing;
    private int captureFrame;
    private static readonly KeyCode[] keys = (KeyCode[])Enum.GetValues(typeof(KeyCode));
    public bool IsOpen => panel != null && panel.activeSelf;
    private void Awake()
    {
        panel.SetActive(false);
        fovSlider.onValueChanged.AddListener(ChangeFov);
        sensitivitySlider.onValueChanged.AddListener(ChangeSensitivity);
        invertMouse.onValueChanged.AddListener(ChangeInvert);
        foreach(var row in bindings) { var action = row.action; row.button.onClick.AddListener(() => BeginBinding(action)); }
    }
    private void OnEnable() { PlayerOptions.Changed += Refresh; }
    private void OnDisable() { PlayerOptions.Changed -= Refresh; capturing = null; PlayerOptions.Save(); }
    private void Update()
    {
        if (!IsOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape)) { if(capturing.HasValue)CancelBinding(); else Close(); return; }
        if (!capturing.HasValue || Time.frameCount <= captureFrame || !Input.anyKeyDown) return;
        foreach(var key in keys) if (PlayerOptions.IsBindable(key) && Input.GetKeyDown(key)) { TryAssignKey(key); break; }
    }
    public void Open()
    {
        CancelBinding();Refresh();foreach(var item in mainMenuObjects)if(item!=null)item.SetActive(false);
        panel.SetActive(true);backButton.Select();
    }
    public void Close()
    {
        CancelBinding();PlayerOptions.Save();panel.SetActive(false);
        foreach(var item in mainMenuObjects)if(item!=null)item.SetActive(true);openButton.Select();
    }
    private void ChangeFov(float value) { PlayerOptions.SetFieldOfView(value); }
    private void ChangeSensitivity(float value) { PlayerOptions.SetSensitivity(value); }
    private void ChangeInvert(bool value) { PlayerOptions.SetInvertMouse(value); }
    public void BeginBinding(GameplayAction action)
    {
        capturing=action;captureFrame=Time.frameCount;
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        Refresh();
        bindingMessage.text="Press a key for " + PlayerOptions.ActionName(action) + ". Escape cancels.";
    }
    public bool TryAssignKey(KeyCode key)
    {
        if(!capturing.HasValue)return false;
        if(!PlayerOptions.TryBind(capturing.Value,key,out var message)){bindingMessage.text=message;return false;}
        capturing=null;bindingMessage.text="Binding saved.";Refresh();return true;
    }
    public void CancelBinding() { capturing=null;bindingMessage.text="";Refresh(); }
    public void RestoreDefaults() { capturing=null;PlayerOptions.RestoreDefaults();CancelBinding(); }
    public void Refresh()
    {
        fovSlider.SetValueWithoutNotify(PlayerOptions.FieldOfView());fovValue.text=Mathf.RoundToInt(PlayerOptions.FieldOfView()) + "°";
        sensitivitySlider.SetValueWithoutNotify(PlayerOptions.Sensitivity());sensitivityValue.text=PlayerOptions.Sensitivity().ToString("0.0");
        invertMouse.SetIsOnWithoutNotify(PlayerOptions.InvertMouse);
        foreach(var row in bindings)row.keyLabel.text=capturing==row.action?"Press a key…":PlayerOptions.KeyName(PlayerOptions.GetKey(row.action));
    }
}