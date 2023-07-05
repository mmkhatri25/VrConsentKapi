using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CatPrefab : MonoBehaviour
{
    public int Id;
    public string Name;
    public string ImgUrl;
    public Text CatName;
    public RawImage ThumnailImg;
    public Button CatButton;
    private void OnEnable()
    {
        CatButton = this.GetComponent<Button>();
        CatButton.onClick.AddListener(OnButtoClick);
    }

    void OnButtoClick()
    {
             OVRInput.SetControllerVibration(.5f, .5f, OVRInput.Controller.All);
    
        print("clicked on "+ name);
        VideoManager.CrateVidAction?.Invoke(Id);
    }
}
 