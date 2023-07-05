using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;

public class testdownlaodvideo : MonoBehaviour
{
    // Start is called before the first frame update
    public VideoPlayer videoPlayer;
    void Start()
    {
        StartCoroutine(download());
    }

    IEnumerator download()
    {
        UnityWebRequest www = UnityWebRequest.Get("https://medixr-bucket.s3.ap-southeast-1.amazonaws.com/categories/7/PvKk3cHgks659mw8tduOgXaEuMLHwQGg9daz5n4Y.mp4");
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
        }
        else
        {
            string _pathToFile = Path.Combine(Application.persistentDataPath, "video.mp4");
            Debug.Log("m" + _pathToFile);
            File.WriteAllBytes(_pathToFile, www.downloadHandler.data);
        }
    }
}
