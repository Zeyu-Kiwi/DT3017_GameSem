using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// Renders the menu world through its PSX camera, then draws the world-space menu
/// canvas with a separate clean renderer. The canvas never follows the camera.
/// </summary>
[DisallowMultipleComponent, RequireComponent(typeof(Camera))]
public sealed class MenuWorldUiIsolation : MonoBehaviour
{
    [SerializeField] private Canvas menuCanvas;
    [SerializeField, Range(0, 31)] private int menuUiLayer = 5;
    [Tooltip("Index of the clean renderer in the active URP asset's Renderer List.")]
    [SerializeField, Min(0)] private int cleanRendererIndex = 1;

    private Camera worldCamera;
    private Camera uiCamera;
    private Canvas backgroundCanvas;
    private RawImage backgroundImage;
    private RenderTexture worldTexture;
    private RenderTexture originalTarget;
    private Camera originalEventCamera;
    private int originalCullingMask;
    private bool activeSetup;

    private void OnEnable()
    {
        if (!Application.isPlaying) return;

        worldCamera = GetComponent<Camera>();
        if (menuCanvas == null)
        {
            Debug.LogError("Menu World UI Isolation needs the menu's World Space Canvas.", this);
            enabled = false;
            return;
        }

        originalTarget = worldCamera.targetTexture;
        originalCullingMask = worldCamera.cullingMask;
        originalEventCamera = menuCanvas.worldCamera;

        var cameraObject = new GameObject("Menu UI Camera");
        cameraObject.transform.SetParent(transform, false);
        uiCamera = cameraObject.AddComponent<Camera>();
        uiCamera.CopyFrom(worldCamera);
        uiCamera.targetTexture = null;
        uiCamera.depth = worldCamera.depth + 1f;
        uiCamera.cullingMask = 1 << menuUiLayer;
        uiCamera.clearFlags = CameraClearFlags.SolidColor;
        uiCamera.backgroundColor = Color.black;

        var cameraData = cameraObject.GetComponent<UniversalAdditionalCameraData>();
        if (cameraData == null)
            cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderType = CameraRenderType.Base;
        cameraData.SetRenderer(cleanRendererIndex);
        cameraData.renderPostProcessing = false;
        cameraData.renderShadows = false;

        var backgroundObject = new GameObject("Menu World Background", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasRenderer));
        backgroundObject.layer = menuUiLayer;
        backgroundCanvas = backgroundObject.GetComponent<Canvas>();
        backgroundCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        backgroundCanvas.worldCamera = uiCamera;
        backgroundCanvas.planeDistance = Mathf.Min(100f, uiCamera.farClipPlane - 1f);
        backgroundCanvas.sortingOrder = -100;

        var imageObject = new GameObject("PSX Scene Image", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(RawImage));
        imageObject.layer = menuUiLayer;
        imageObject.transform.SetParent(backgroundObject.transform, false);
        var imageRect = (RectTransform)imageObject.transform;
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;
        backgroundImage = imageObject.GetComponent<RawImage>();
        backgroundImage.raycastTarget = false;

        worldCamera.cullingMask = originalCullingMask & ~(1 << menuUiLayer);
        menuCanvas.worldCamera = uiCamera;
        CreateWorldTexture();
        activeSetup = true;
    }

    private void Update()
    {
        if (worldTexture != null &&
            (worldTexture.width != Screen.width || worldTexture.height != Screen.height))
            CreateWorldTexture();
    }

    private void CreateWorldTexture()
    {
        worldCamera.targetTexture = null;
        if (worldTexture != null)
        {
            worldTexture.Release();
            Destroy(worldTexture);
        }

        worldTexture = new RenderTexture(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height),
            24, RenderTextureFormat.DefaultHDR)
        {
            name = "Menu PSX World",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        worldTexture.Create();
        worldCamera.targetTexture = worldTexture;
        backgroundImage.texture = worldTexture;
    }

    private void OnDisable()
    {
        if (!activeSetup) return;
        activeSetup = false;
        if (worldCamera != null)
        {
            worldCamera.targetTexture = originalTarget;
            worldCamera.cullingMask = originalCullingMask;
        }
        if (menuCanvas != null) menuCanvas.worldCamera = originalEventCamera;
        if (uiCamera != null) Destroy(uiCamera.gameObject);
        if (backgroundCanvas != null) Destroy(backgroundCanvas.gameObject);
        if (worldTexture != null)
        {
            worldTexture.Release();
            Destroy(worldTexture);
        }
        uiCamera = null;
        backgroundCanvas = null;
        backgroundImage = null;
        worldTexture = null;
    }
}
