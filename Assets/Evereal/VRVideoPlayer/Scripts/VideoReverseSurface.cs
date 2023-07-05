using UnityEngine;
using System.Collections;

namespace Evereal.VRVideoPlayer
{
    [RequireComponent(typeof(MeshFilter))]
    public class VideoReverseSurface : MonoBehaviour
    {
        private void Start()
        {
            Vector2[] vec2UVs = GetComponent<MeshFilter>().mesh.uv;

            for (int i = 0; i < vec2UVs.Length; i++)
            {
                vec2UVs[i] = new Vector2(1.0f - vec2UVs[i].x, vec2UVs[i].y);
            }
            GetComponent<MeshFilter>().mesh.uv = vec2UVs;
        }
    }
}