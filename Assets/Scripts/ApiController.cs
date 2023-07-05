using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;
using UnityEngine.UI;

public class ApiController : MonoBehaviour
{

    public static Action<RawImage, string> GetUrlTextureAction;
    public const string BASE_URL = "https://medixr.link/api/v1/get_categories";
    
    private void OnEnable()
    {
        GetUrlTextureAction += GetTextureFromUrl;
    }
    private void OnDisable()
    {
        GetUrlTextureAction -= GetTextureFromUrl;

    }

    void Start()
    {
        StartCoroutine(GetData());
    }

    IEnumerator GetData()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(BASE_URL))
        {
            yield return request.SendWebRequest();

            if (request.isNetworkError) // Error
            {
                Debug.Log(request.error);

            }
            else // Success
            {
                ApiDataSave.JsonVideoData = string.Empty;
                ApiDataSave.JsonVideoData = request.downloadHandler.text;
                Debug.Log(request.downloadHandler.text);
                VideoManager.CreateCatAction?.Invoke();
            }
        }
    }

    void GetTextureFromUrl(RawImage tex, string textUrl)
    {

        StartCoroutine(GetTexture(tex, textUrl));
    }

    IEnumerator GetTexture(RawImage tex, string textUrl)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(textUrl);
        yield return request.SendWebRequest();

        if (request.isNetworkError)
        {
            Debug.Log(request.error);
        }
        else
        {
            Texture texturl = ((DownloadHandlerTexture)request.downloadHandler).texture;
            tex.texture = texturl;
            Debug.Log(request.downloadHandler.text);
        }
    }
}


