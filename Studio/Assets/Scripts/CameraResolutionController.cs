using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class CameraResolutionController : MonoBehaviour
{
    [SerializeField] private int targetWidth = 960;
    [SerializeField] private int targetHeight = 540;
    
    private Camera _camera;
    private RenderTexture _renderTexture;
    private bool _initialized;

    private void OnEnable()
    {
        _camera = GetComponent<Camera>();
        UpdateRenderTexture();
    }

    private void OnDisable()
    {
        if (_camera != null)
        {
            _camera.targetTexture = null;
        }
        CleanupRenderTexture();
    }

    private void UpdateRenderTexture()
    {
        CleanupRenderTexture();

        if (_camera != null)
        {
            _renderTexture = new RenderTexture(targetWidth, targetHeight, 24);
            _renderTexture.filterMode = FilterMode.Bilinear;
            _camera.targetTexture = _renderTexture;
            _initialized = true;
        }
    }

    private void CleanupRenderTexture()
    {
        if (_camera != null)
        {
            _camera.targetTexture = null;
        }

        if (_renderTexture != null)
        {
            _renderTexture.Release();
            DestroyImmediate(_renderTexture);
            _renderTexture = null;
        }
        _initialized = false;
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (!_initialized || src == null || dest == null) 
        {
            return;
        }
        Graphics.Blit(src, dest);
    }

    public void SetResolution(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            Debug.LogError($"Invalid resolution: {width}x{height}", this);
            return;
        }

        targetWidth = width;
        targetHeight = height;
        UpdateRenderTexture();
    }

    #if UNITY_EDITOR
    private void OnValidate()
    {
        if (enabled && gameObject.activeInHierarchy)
        {
            UpdateRenderTexture();
        }
    }
    #endif
} 