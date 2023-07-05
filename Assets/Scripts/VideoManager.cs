using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class VideoManager : MonoBehaviour
{
    public Root DataRoot;
    public static Action CreateCatAction;
    public static Action<int>  CrateVidAction;
    public static Action<string> PlayVideoAction;
    public static Action OnReturnDashAction;
    public Transform CatParent;
    public GameObject CatPrefab;
    public Transform VidParent;
    public GameObject VidPrefab;
    public GameObject CategoryPanel, VideoPanel , LoadingPanel;
    public Button BackButtonVideo;
    public Text CategoryNameVideoPanel;
    private void OnEnable()
    {    
        CreateCatAction += CreateCat;
        CrateVidAction += CreatVideo;
        PlayVideoAction += PlayVideo;
        OnReturnDashAction += OnReturnToDashboard;
        BackButtonVideo.onClick.AddListener(OnBackButton);
    }


  

    private void OnDisable()
    {
        CreateCatAction -= CreateCat;
        CrateVidAction -= CreatVideo;
        PlayVideoAction -= PlayVideo;
        OnReturnDashAction -= OnReturnToDashboard;
        

    }
    private void PlayVideo(string vidUrl)
    {
        print("rquiested vide "+ vidUrl);
        ApiDataSave.JsonVideoData = vidUrl;
        SceneManager.LoadScene(1);
    }


    void CreateCat()
    {
        foreach (Transform item in CatParent)
        {
            Destroy(item.gameObject);
        }
       
        string data = ApiDataSave.JsonVideoData;
        DataRoot = JsonUtility.FromJson<Root>(data);
        
        for (int i = 0; i < DataRoot.data.Count; i++)
        {
            var item_go = Instantiate(CatPrefab);
            item_go.transform.SetParent(CatParent);
            item_go.GetComponent<RectTransform>().localPosition = new Vector3(item_go.transform.position.x, item_go.transform.position.y, 0);
            item_go.GetComponent<RectTransform>().localScale = Vector3.one;
            var obj = item_go.GetComponent<CatPrefab>();
            obj.Id = i;//int.Parse(DataRoot.data[i].id);
            obj.name = DataRoot.data[i].name;
            obj.CatName.text = DataRoot.data[i].name;
            ApiController.GetUrlTextureAction?.Invoke(obj.ThumnailImg, DataRoot.data[i].image);
        }
        CategoryPanel.SetActive(true);
        LoadingPanel.SetActive(false);

    }
    void CreatVideo(int catIndex)
    {
        LoadingPanel.SetActive(true);
        print("catIndex -- " + catIndex);
        foreach (Transform item in VidParent)
        {
            Destroy(item.gameObject);
        }
        string data = ApiDataSave.JsonVideoData;
        DataRoot = JsonUtility.FromJson<Root>(data);
        CategoryNameVideoPanel.text = DataRoot.data[catIndex].name;
        for (int i = 0; i < DataRoot.data[catIndex].videos.Count; i++)
        {
            var item_go = Instantiate(VidPrefab);
            item_go.transform.SetParent(VidParent);
            item_go.GetComponent<RectTransform>().localPosition = new Vector3(item_go.transform.position.x, item_go.transform.position.y, 0);
            item_go.GetComponent<RectTransform>().localScale = Vector3.one;
            var obj =  item_go.GetComponent<VidPrefab>();
            obj.VideoUrl = DataRoot.data[catIndex].videos[i].video;
            obj.ThumbUrl = DataRoot.data[catIndex].videos[i].thumbnail;
            obj.name = DataRoot.data[catIndex].videos[i].name;
            obj.videoNameText.text = obj.name;
            obj.VideoId = int.Parse( DataRoot.data[catIndex].videos[i].id);
            ApiController.GetUrlTextureAction?.Invoke(obj.VidThumbnail, DataRoot.data[catIndex].videos[i].thumbnail);
        }

        CategoryPanel.SetActive(false);
        VideoPanel.SetActive(true);
        LoadingPanel.SetActive(false);
    }
    
    void OnBackButton()
    {
             OVRInput.SetControllerVibration(.5f, .5f, OVRInput.Controller.All);
    
        VideoPanel.SetActive(false);
        CategoryPanel.SetActive(true);
        CategoryNameVideoPanel.text = string.Empty;
    }
    
    void OnReturnToDashboard()
    {
             OVRInput.SetControllerVibration(.5f, .5f, OVRInput.Controller.All);
    
        SceneManager.LoadScene(0);
        
        print("scene change rquest ");
    }

}
// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
[Serializable]
public class Datum
{
    public string id;
    public string name;
    public string image;
    public List<Video> videos;
}
[Serializable]
public class Root
{
    public int status;
    public string message;
    public List<Datum> data;
}
[Serializable]
public class Video
{
    public string id;
    public string name;
    public string video;
    public string thumbnail;
}







