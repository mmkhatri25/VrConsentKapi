using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VidPrefab : MonoBehaviour
{
    public string VideoUrl;
    public string ThumbUrl;
    public RawImage VidThumbnail;
    public string VideoName;
    public Text videoNameText;
    public int VideoId;
    public Button VideoButton;
    private void OnEnable()
    {
        VideoButton = this.GetComponent<Button>();
        VideoButton.onClick.AddListener(OnButtoClick);
    }

    void OnButtoClick()
    {
             OVRInput.SetControllerVibration(.5f, .5f, OVRInput.Controller.All);
       
        VideoManager.PlayVideoAction?.Invoke(VideoUrl);
    }
}
