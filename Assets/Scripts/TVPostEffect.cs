using UnityEngine;

/// <summary>カメラ単位のテレビ風ポストエフェクト設定。</summary>
[DisallowMultipleComponent, RequireComponent(typeof(Camera))]
[AddComponentMenu("Rendering/TV Post Effect")]
public sealed class TVPostEffect : MonoBehaviour
{
    [Range(0f, 1f)] public float scanlineIntensity = 0.2f;
    [Range(2f, 32f)] public float scanlineSpacing = 4f;
    [Range(0f, 10f)] public float chromaticAberration = 1.5f;
}
