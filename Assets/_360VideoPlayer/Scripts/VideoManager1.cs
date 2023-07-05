using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.Video;

public class VideoManager1 : MonoBehaviour
{
    public VideoPlayer myVideoPlayer;
    public string videoUrl;
    public GameObject loadingPanel;

    private void OnEnable()
    {
        videoUrl = ApiDataSave.JsonVideoData;
      // myVideoPlayer.url = videoUrl;// "https://dl.dropboxusercontent.com/s/aur7p6ejecgnzef/delparque-cafe.mp4";//"http://52.220.161.199/videos/test.mp4";//;
        StartCoroutine(DownloadAndPlay(videoUrl));
        
    }
   IEnumerator DownloadAndPlay(string videoUrl)
    {
    
        string _pathToFile = Path.Combine(Application.persistentDataPath, "video.mp4");
        Debug.Log("download file path" + _pathToFile);
    
        UnityWebRequest www = UnityWebRequest.Get(videoUrl);
        yield return www.SendWebRequest();
        Debug.Log("download in download method");
        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log("download "+www.error);
        }
        else
        {
            _pathToFile = Path.Combine(Application.persistentDataPath, "video.mp4");
            Debug.Log("download file path" + _pathToFile);
            File.WriteAllBytes(_pathToFile, www.downloadHandler.data);
            myVideoPlayer.url = Application.persistentDataPath+ "/video.mp4";
            myVideoPlayer.Prepare();
            yield return new WaitUntil(() => myVideoPlayer.isPrepared);
            loadingPanel.SetActive(false);
            myVideoPlayer.Play();
        }
    }

   
}
