using System;
using System.Reflection;
using UnityEngine;

namespace Dokdo.Web
{
    public static class BrowserCameraZoom
    {
        public const float MaximumWheelDeltaPixels = 100f;

        public static float CalculateOrthographicSize(
            float currentSize,
            float wheelDeltaPixels,
            float pinchZoomSpeed,
            float minimumSize,
            float maximumSize)
        {
            if (!IsFinite(currentSize) ||
                !IsFinite(wheelDeltaPixels) ||
                !IsFinite(pinchZoomSpeed) ||
                !IsFinite(minimumSize) ||
                !IsFinite(maximumSize) ||
                pinchZoomSpeed < 0f || maximumSize < minimumSize)
            {
                return currentSize;
            }

            var boundedDelta = Mathf.Clamp(
                wheelDeltaPixels,
                -MaximumWheelDeltaPixels,
                MaximumWheelDeltaPixels);
            return Mathf.Clamp(
                currentSize + boundedDelta * pinchZoomSpeed,
                minimumSize,
                maximumSize);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    public sealed class BrowserCameraZoomRuntime : MonoBehaviour
    {
        private const string CameraZoomTypeName =
            "CameraZoom, Assembly-CSharp";
        private const BindingFlags InstanceFields =
            BindingFlags.Instance | BindingFlags.Public |
            BindingFlags.NonPublic;

        private Component cameraZoom;
        private Camera gameplayCamera;
        private Component shipController;
        private FieldInfo zoomSpeedField;
        private FieldInfo minimumSizeField;
        private FieldInfo maximumSizeField;
        private FieldInfo parkingField;

        public void Refresh()
        {
            ClearBinding();
            var cameraZoomType = Type.GetType(CameraZoomTypeName, false);
            cameraZoom = cameraZoomType == null
                ? null
                : FindSceneComponent(cameraZoomType);
            gameplayCamera = cameraZoom != null
                ? cameraZoom.GetComponent<Camera>()
                : null;
            zoomSpeedField = cameraZoomType?.GetField(
                "orthoZoomSpeed", InstanceFields);
            minimumSizeField = cameraZoomType?.GetField(
                "MinSize", InstanceFields);
            maximumSizeField = cameraZoomType?.GetField(
                "MaxSizePortrait", InstanceFields);

            var shipControllerField = cameraZoomType?.GetField(
                "SC", InstanceFields);
            shipController = cameraZoom != null
                ? shipControllerField?.GetValue(cameraZoom) as Component
                : null;
            parkingField = shipController?.GetType().GetField(
                "isParking", InstanceFields);

            if (gameplayCamera == null || zoomSpeedField == null ||
                minimumSizeField == null || maximumSizeField == null ||
                shipController == null || parkingField == null)
            {
                ClearBinding();
            }
        }

        public bool ApplyWheelDelta(float wheelDeltaPixels)
        {
            if (cameraZoom == null || gameplayCamera == null ||
                !cameraZoom.gameObject.scene.IsValid() ||
                cameraZoom is Behaviour behaviour &&
                    !behaviour.isActiveAndEnabled ||
                !gameplayCamera.orthographic ||
                (bool)parkingField.GetValue(shipController))
            {
                return false;
            }

            var currentSize = gameplayCamera.orthographicSize;
            var updatedSize = BrowserCameraZoom.CalculateOrthographicSize(
                currentSize,
                wheelDeltaPixels,
                (float)zoomSpeedField.GetValue(cameraZoom),
                (float)minimumSizeField.GetValue(cameraZoom),
                (float)maximumSizeField.GetValue(cameraZoom));
            if (Mathf.Approximately(currentSize, updatedSize))
            {
                return false;
            }

            gameplayCamera.orthographicSize = updatedSize;
            return true;
        }

        private void ClearBinding()
        {
            cameraZoom = null;
            gameplayCamera = null;
            shipController = null;
            zoomSpeedField = null;
            minimumSizeField = null;
            maximumSizeField = null;
            parkingField = null;
        }

        private static Component FindSceneComponent(Type type)
        {
            foreach (var candidate in
                Resources.FindObjectsOfTypeAll(type))
            {
                if (candidate is Component component &&
                    component.gameObject.scene.IsValid())
                {
                    return component;
                }
            }
            return null;
        }
    }
}
