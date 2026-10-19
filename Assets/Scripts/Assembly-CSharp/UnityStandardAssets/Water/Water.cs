using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityStandardAssets.Water
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Renderer))]
    public class Water : MonoBehaviour
    {
        public enum WaterMode
        {
            Simple = 0,
            Reflective = 1,
            Refractive = 2
        }

        public WaterMode waterMode = WaterMode.Refractive;
        public bool disablePixelLights = true;
        public int textureSize = 256;
        public float clipPlaneOffset = 0.07f;
        public LayerMask reflectLayers = -1;
        public LayerMask refractLayers = -1;

        [Tooltip("Hard cap for the recovered serialized reflection size.")]
        [Range(64, 512)]
        public int maximumTextureSize = 256;

        [Tooltip("Disable to use the normal/gradient-only fallback.")]
        public bool allowPlanarReflection = true;

        [Tooltip("Disable to force the fallback on Android and iOS.")]
        public bool allowReflectionOnMobile = true;

        private readonly Dictionary<Camera, Camera> reflectionCameras =
            new Dictionary<Camera, Camera>();
        private RenderTexture reflectionTexture;
        private int oldReflectionTextureSize;
        private static bool insideWaterRender;

        public int EffectiveTextureSize
        {
            get
            {
                return Mathf.Clamp(textureSize, 64,
                    Mathf.Clamp(maximumTextureSize, 64, 512));
            }
        }

        public bool IsUsingSimpleFallback
        {
            get { return GetWaterMode() == WaterMode.Simple; }
        }

        public void SetPlanarReflectionEnabled(bool enabled)
        {
            allowPlanarReflection = enabled;
            ApplyShaderMode(enabled && CanRenderReflection()
                ? WaterMode.Reflective
                : WaterMode.Simple);
            if (!enabled)
            {
                ReleaseReflectionResources();
            }
        }

        public void OnWillRenderObject()
        {
            var waterRenderer = GetComponent<Renderer>();
            if (!enabled || waterRenderer == null || !waterRenderer.enabled ||
                waterRenderer.sharedMaterial == null || insideWaterRender)
            {
                return;
            }

            var currentCamera = Camera.current;
            if (currentCamera == null)
            {
                return;
            }

            insideWaterRender = true;
            var previousPixelLightCount = QualitySettings.pixelLightCount;
            try
            {
                if (disablePixelLights)
                {
                    QualitySettings.pixelLightCount = 0;
                }

                var mode = GetWaterMode();
                if (mode == WaterMode.Reflective)
                {
                    Camera reflectionCamera;
                    CreateReflectionObjects(currentCamera, out reflectionCamera);
                    RenderReflection(currentCamera, reflectionCamera);
                    waterRenderer.sharedMaterial.SetTexture(
                        "_ReflectionTex", reflectionTexture);
                }
                else
                {
                    ReleaseReflectionResources();
                }
                ApplyShaderMode(mode);
            }
            finally
            {
                if (disablePixelLights)
                {
                    QualitySettings.pixelLightCount = previousPixelLightCount;
                }
                insideWaterRender = false;
            }
        }

        private void OnDisable()
        {
            ReleaseReflectionResources();
            ApplyShaderMode(WaterMode.Simple);
        }

        private void Update()
        {
            var waterRenderer = GetComponent<Renderer>();
            if (waterRenderer == null || waterRenderer.sharedMaterial == null)
            {
                return;
            }

            var material = waterRenderer.sharedMaterial;
            var waveSpeed = material.GetVector("WaveSpeed");
            var waveScale = material.GetFloat("_WaveScale");
            var scale = new Vector4(
                waveScale, waveScale, waveScale * 0.4f, waveScale * 0.45f);
            var time = Time.timeSinceLevelLoad / 20.0;
            var offset = new Vector4(
                (float)Math.IEEERemainder(waveSpeed.x * scale.x * time, 1.0),
                (float)Math.IEEERemainder(waveSpeed.y * scale.y * time, 1.0),
                (float)Math.IEEERemainder(waveSpeed.z * scale.z * time, 1.0),
                (float)Math.IEEERemainder(waveSpeed.w * scale.w * time, 1.0));
            material.SetVector("_WaveOffset", offset);
            material.SetVector("_WaveScale4", scale);
        }

        private WaterMode GetWaterMode()
        {
            if (waterMode == WaterMode.Simple || !CanRenderReflection())
            {
                return WaterMode.Simple;
            }
            return WaterMode.Reflective;
        }

        private bool CanRenderReflection()
        {
            if (!allowPlanarReflection || SystemInfo.graphicsShaderLevel < 30 ||
                SystemInfo.graphicsDeviceType ==
                UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                return false;
            }
            if (Application.isMobilePlatform && !allowReflectionOnMobile)
            {
                return false;
            }

            var waterRenderer = GetComponent<Renderer>();
            var material = waterRenderer == null
                ? null
                : waterRenderer.sharedMaterial;
            return material != null && string.Equals(
                material.GetTag("WATERMODE", false), "Reflective",
                StringComparison.Ordinal);
        }

        private static void ApplyShaderMode(WaterMode mode)
        {
            var reflective = mode == WaterMode.Reflective;
            if (reflective)
            {
                Shader.EnableKeyword("WATER_REFLECTIVE");
                Shader.DisableKeyword("WATER_SIMPLE");
            }
            else
            {
                Shader.EnableKeyword("WATER_SIMPLE");
                Shader.DisableKeyword("WATER_REFLECTIVE");
            }
            Shader.DisableKeyword("WATER_REFRACTIVE");
        }

        private void CreateReflectionObjects(Camera source,
            out Camera reflectionCamera)
        {
            var size = EffectiveTextureSize;
            if (reflectionTexture == null || oldReflectionTextureSize != size)
            {
                DestroyResource(reflectionTexture);
                reflectionTexture = new RenderTexture(
                    size, size, 16, RenderTextureFormat.ARGB32)
                {
                    name = "__DOKDOWaterReflection" + GetInstanceID(),
                    hideFlags = HideFlags.DontSave,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    useMipMap = false,
                    autoGenerateMips = false
                };
                oldReflectionTextureSize = size;
            }

            reflectionCameras.TryGetValue(source, out reflectionCamera);
            if (reflectionCamera != null)
            {
                return;
            }

            var cameraObject = new GameObject(
                "DOKDO Water Reflection " + GetInstanceID() + " for " +
                source.GetInstanceID(), typeof(Camera), typeof(Skybox));
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            reflectionCamera = cameraObject.GetComponent<Camera>();
            reflectionCamera.enabled = false;
            reflectionCameras[source] = reflectionCamera;
        }

        private void RenderReflection(Camera source, Camera reflectionCamera)
        {
            CopyCameraSettings(source, reflectionCamera);

            var position = transform.position;
            var normal = transform.up;
            var plane = new Vector4(
                normal.x, normal.y, normal.z,
                -Vector3.Dot(normal, position) - clipPlaneOffset);
            var reflectionMatrix = Matrix4x4.zero;
            CalculateReflectionMatrix(ref reflectionMatrix, plane);

            var sourcePosition = source.transform.position;
            var reflectedPosition = reflectionMatrix.MultiplyPoint(sourcePosition);
            reflectionCamera.worldToCameraMatrix =
                source.worldToCameraMatrix * reflectionMatrix;
            var clipPlane = CameraSpacePlane(
                reflectionCamera, position, normal, 1f);
            reflectionCamera.projectionMatrix =
                source.CalculateObliqueMatrix(clipPlane);
            reflectionCamera.cullingMask = reflectLayers.value & ~(1 << 4);
            reflectionCamera.targetTexture = reflectionTexture;
            reflectionCamera.transform.position = reflectedPosition;
            var sourceAngles = source.transform.eulerAngles;
            reflectionCamera.transform.eulerAngles = new Vector3(
                -sourceAngles.x, sourceAngles.y, sourceAngles.z);

            var previousInvertCulling = GL.invertCulling;
            try
            {
                GL.invertCulling = !previousInvertCulling;
                reflectionCamera.Render();
            }
            finally
            {
                GL.invertCulling = previousInvertCulling;
                reflectionCamera.transform.position = sourcePosition;
            }
        }

        private static void CopyCameraSettings(Camera source, Camera destination)
        {
            destination.clearFlags = source.clearFlags;
            destination.backgroundColor = source.backgroundColor;
            destination.farClipPlane = source.farClipPlane;
            destination.nearClipPlane = source.nearClipPlane;
            destination.orthographic = source.orthographic;
            destination.fieldOfView = source.fieldOfView;
            destination.aspect = source.aspect;
            destination.orthographicSize = source.orthographicSize;
            destination.renderingPath = RenderingPath.Forward;
            destination.depthTextureMode = DepthTextureMode.None;

            if (source.clearFlags != CameraClearFlags.Skybox)
            {
                return;
            }
            var sourceSkybox = source.GetComponent<Skybox>();
            var destinationSkybox = destination.GetComponent<Skybox>();
            destinationSkybox.enabled = sourceSkybox != null &&
                sourceSkybox.material != null;
            if (destinationSkybox.enabled)
            {
                destinationSkybox.material = sourceSkybox.material;
            }
        }

        private Vector4 CameraSpacePlane(Camera camera, Vector3 position,
            Vector3 normal, float sideSign)
        {
            var offsetPosition = position + normal * clipPlaneOffset;
            var matrix = camera.worldToCameraMatrix;
            var cameraPosition = matrix.MultiplyPoint(offsetPosition);
            var cameraNormal = matrix.MultiplyVector(normal).normalized * sideSign;
            return new Vector4(cameraNormal.x, cameraNormal.y, cameraNormal.z,
                -Vector3.Dot(cameraPosition, cameraNormal));
        }

        private void ReleaseReflectionResources()
        {
            DestroyResource(reflectionTexture);
            reflectionTexture = null;
            oldReflectionTextureSize = 0;

            foreach (var camera in reflectionCameras.Values)
            {
                if (camera != null)
                {
                    DestroyResource(camera.gameObject);
                }
            }
            reflectionCameras.Clear();
        }

        private static void DestroyResource(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }
            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private static void CalculateReflectionMatrix(
            ref Matrix4x4 reflectionMatrix, Vector4 plane)
        {
            reflectionMatrix.m00 = 1f - 2f * plane[0] * plane[0];
            reflectionMatrix.m01 = -2f * plane[0] * plane[1];
            reflectionMatrix.m02 = -2f * plane[0] * plane[2];
            reflectionMatrix.m03 = -2f * plane[3] * plane[0];
            reflectionMatrix.m10 = -2f * plane[1] * plane[0];
            reflectionMatrix.m11 = 1f - 2f * plane[1] * plane[1];
            reflectionMatrix.m12 = -2f * plane[1] * plane[2];
            reflectionMatrix.m13 = -2f * plane[3] * plane[1];
            reflectionMatrix.m20 = -2f * plane[2] * plane[0];
            reflectionMatrix.m21 = -2f * plane[2] * plane[1];
            reflectionMatrix.m22 = 1f - 2f * plane[2] * plane[2];
            reflectionMatrix.m23 = -2f * plane[3] * plane[2];
            reflectionMatrix.m30 = 0f;
            reflectionMatrix.m31 = 0f;
            reflectionMatrix.m32 = 0f;
            reflectionMatrix.m33 = 1f;
        }
    }
}
