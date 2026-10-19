using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dokdo.Web
{
    public static class BrowserResponsivePresentation
    {
        public const float ReferenceWidth = 720f;
        public const float ReferenceHeight = 1280f;
        public const float ReferenceAspect =
            ReferenceWidth / ReferenceHeight;
        public const float IntroCoverageMargin = 0.98f;
        public static readonly Color32 MapViewportBackgroundColor =
            new(20, 60, 90, 255);

        private const string IntroSceneName = "Intro";
        private const string IntroWaterName = "water0";
        private const float PolygonEpsilon = 0.0001f;
        private static Camera cachedIntroCamera;
        private static MeshFilter cachedIntroWater;
        private static Mesh cachedIntroMesh;
        private static Matrix4x4 cachedCameraWorldToLocal;
        private static Matrix4x4 cachedWaterLocalToWorld;
        private static List<Vector2> cachedIntroCoverageHull;

        public static bool IsEligible(CanvasScaler scaler)
        {
            if (scaler == null ||
                scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                return false;
            }

            var canvas = scaler.GetComponent<Canvas>();
            var parentCanvas = canvas != null && canvas.transform.parent != null
                ? canvas.transform.parent.GetComponentInParent<Canvas>(true)
                : null;
            return canvas != null && parentCanvas == null &&
                canvas.renderMode != RenderMode.WorldSpace &&
                scaler.gameObject.scene.IsValid();
        }

        public static int ApplyCanvasScalers(
            IEnumerable<CanvasScaler> scalers)
        {
            var changed = 0;
            foreach (var scaler in scalers)
            {
                if (!IsEligible(scaler) || scaler.screenMatchMode ==
                    CanvasScaler.ScreenMatchMode.Expand)
                {
                    continue;
                }

                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                changed++;
            }
            return changed;
        }

        public static int ApplyDiscoveredCanvasScalers()
        {
            return ApplyCanvasScalers(
                Resources.FindObjectsOfTypeAll<CanvasScaler>());
        }

        public static bool IsMapViewportBackground(Image image)
        {
            if (image == null ||
                !image.gameObject.scene.IsValid() ||
                image.gameObject.name != "Viewport")
            {
                return false;
            }

            var scrollView = image.transform.parent;
            var pauseMenu = scrollView != null
                ? scrollView.parent
                : null;
            return scrollView != null &&
                scrollView.name == "Scroll View" &&
                pauseMenu != null &&
                pauseMenu.name == "PauseMenu";
        }

        public static int ApplyMapViewportBackgrounds(
            IEnumerable<Image> images)
        {
            var changed = 0;
            foreach (var image in images)
            {
                if (!IsMapViewportBackground(image) ||
                    ((Color32)image.color).Equals(
                        MapViewportBackgroundColor))
                {
                    continue;
                }

                image.color = MapViewportBackgroundColor;
                changed++;
            }
            return changed;
        }

        public static int ApplyDiscoveredMapViewportBackgrounds()
        {
            return ApplyMapViewportBackgrounds(
                Resources.FindObjectsOfTypeAll<Image>());
        }

        public static int ApplyCanvasPolicyThen(
            IEnumerable<CanvasScaler> scalers, Action reapplySafeArea)
        {
            var changed = ApplyCanvasScalers(scalers);
            reapplySafeArea?.Invoke();
            return changed;
        }

        public static Vector2 CalculateOrthographicHalfExtents(
            float orthographicSize, float viewportAspect)
        {
            if (orthographicSize <= 0f || viewportAspect <= 0f)
            {
                return Vector2.zero;
            }

            var halfHeight = viewportAspect >= ReferenceAspect
                ? orthographicSize
                : orthographicSize * ReferenceAspect / viewportAspect;
            return new Vector2(halfHeight * viewportAspect, halfHeight);
        }

        public static Matrix4x4 CreateOrthographicProjection(
            float orthographicSize, float viewportAspect,
            float nearClipPlane, float farClipPlane)
        {
            var extents = CalculateOrthographicHalfExtents(
                orthographicSize, viewportAspect);
            return Matrix4x4.Ortho(-extents.x, extents.x,
                -extents.y, extents.y, nearClipPlane, farClipPlane);
        }

        public static bool ApplyCameraProjection(Camera camera,
            float viewportAspect)
        {
            if (camera == null || !camera.orthographic ||
                viewportAspect <= 0f)
            {
                return false;
            }

            camera.projectionMatrix = CreateOrthographicProjection(
                camera.orthographicSize, viewportAspect,
                camera.nearClipPlane, camera.farClipPlane);
            return true;
        }

        public static float CalculateCoveringOrthographicSize(
            float requestedSize, float viewportAspect,
            IReadOnlyList<Vector2> coveragePolygon,
            float coverageMargin = IntroCoverageMargin)
        {
            if (requestedSize <= 0f || viewportAspect <= 0f ||
                coveragePolygon == null || coveragePolygon.Count < 3)
            {
                return requestedSize;
            }

            var signedArea = 0f;
            for (var index = 0; index < coveragePolygon.Count; index++)
            {
                signedArea += Cross(coveragePolygon[index],
                    coveragePolygon[(index + 1) % coveragePolygon.Count]);
            }
            if (Mathf.Abs(signedArea) <= PolygonEpsilon)
            {
                return requestedSize;
            }

            var winding = signedArea > 0f ? 1f : -1f;
            var unitExtents = CalculateOrthographicHalfExtents(
                1f, viewportAspect);
            var sizeLimit = float.PositiveInfinity;
            for (var x = -1; x <= 1; x += 2)
            {
                for (var y = -1; y <= 1; y += 2)
                {
                    var direction = new Vector2(
                        unitExtents.x * x, unitExtents.y * y);
                    for (var index = 0;
                        index < coveragePolygon.Count; index++)
                    {
                        var start = coveragePolygon[index];
                        var end = coveragePolygon[
                            (index + 1) % coveragePolygon.Count];
                        var edge = end - start;
                        var distanceAtCenter = winding *
                            Cross(edge, -start);
                        if (distanceAtCenter < -PolygonEpsilon)
                        {
                            return requestedSize;
                        }

                        var distanceRate = winding *
                            Cross(edge, direction);
                        if (distanceRate < -PolygonEpsilon)
                        {
                            sizeLimit = Mathf.Min(sizeLimit,
                                distanceAtCenter / -distanceRate);
                        }
                    }
                }
            }

            if (float.IsInfinity(sizeLimit) || sizeLimit <= 0f)
            {
                return requestedSize;
            }

            var margin = Mathf.Clamp(coverageMargin, 0.01f, 1f);
            return Mathf.Min(requestedSize, sizeLimit * margin);
        }

        public static float CalculateMainCameraOrthographicSize(
            Camera camera, float viewportAspect)
        {
            if (camera == null || camera.gameObject.scene.name !=
                IntroSceneName || !TryGetIntroCoverageHull(
                    camera, out var coverageHull))
            {
                return camera != null ? camera.orthographicSize : 0f;
            }

            return CalculateCoveringOrthographicSize(
                camera.orthographicSize, viewportAspect, coverageHull);
        }

        public static bool IsResponsiveScene(Scene scene)
        {
            return scene.IsValid() &&
                (scene.name == IntroSceneName || scene.name == "Main");
        }

        public static bool ApplyMainCameraProjection(Camera camera)
        {
            if (camera == null || camera != Camera.main ||
                !IsResponsiveScene(camera.gameObject.scene) ||
                camera.pixelHeight <= 0)
            {
                return false;
            }

            var viewportAspect =
                (float)camera.pixelWidth / camera.pixelHeight;
            return ApplyResponsiveCameraProjection(
                camera, viewportAspect);
        }

        public static bool ApplyResponsiveCameraProjection(
            Camera camera, float viewportAspect)
        {
            if (camera == null || !camera.orthographic ||
                !IsResponsiveScene(camera.gameObject.scene) ||
                viewportAspect <= 0f)
            {
                return false;
            }

            var orthographicSize = CalculateMainCameraOrthographicSize(
                camera, viewportAspect);
            camera.projectionMatrix = CreateOrthographicProjection(
                orthographicSize, viewportAspect,
                camera.nearClipPlane, camera.farClipPlane);
            return true;
        }

        private static bool TryGetIntroCoverageHull(Camera camera,
            out IReadOnlyList<Vector2> coverageHull)
        {
            var scene = camera.gameObject.scene;
            var water = cachedIntroWater;
            if (cachedIntroCamera != camera || water == null ||
                water.gameObject.scene != scene)
            {
                water = FindIntroWater(scene);
            }

            var mesh = water != null ? water.sharedMesh : null;
            if (mesh == null || !mesh.isReadable)
            {
                coverageHull = null;
                return false;
            }

            var cameraWorldToLocal = camera.transform.worldToLocalMatrix;
            var waterLocalToWorld = water.transform.localToWorldMatrix;
            if (cachedIntroCamera == camera &&
                cachedIntroWater == water && cachedIntroMesh == mesh &&
                cachedIntroCoverageHull != null &&
                cachedCameraWorldToLocal.Equals(cameraWorldToLocal) &&
                cachedWaterLocalToWorld.Equals(waterLocalToWorld))
            {
                coverageHull = cachedIntroCoverageHull;
                return true;
            }

            var waterToCamera = cameraWorldToLocal * waterLocalToWorld;
            var vertices = mesh.vertices;
            var projected = new List<Vector2>(vertices.Length);
            foreach (var vertex in vertices)
            {
                var cameraPoint = waterToCamera.MultiplyPoint3x4(vertex);
                projected.Add(new Vector2(cameraPoint.x, cameraPoint.y));
            }

            var hull = CreateConvexHull(projected);
            if (hull.Count < 3)
            {
                coverageHull = null;
                return false;
            }

            cachedIntroCamera = camera;
            cachedIntroWater = water;
            cachedIntroMesh = mesh;
            cachedCameraWorldToLocal = cameraWorldToLocal;
            cachedWaterLocalToWorld = waterLocalToWorld;
            cachedIntroCoverageHull = hull;
            coverageHull = hull;
            return true;
        }

        private static MeshFilter FindIntroWater(Scene scene)
        {
            foreach (var candidate in
                Resources.FindObjectsOfTypeAll<MeshFilter>())
            {
                if (candidate != null &&
                    candidate.gameObject.scene == scene &&
                    candidate.gameObject.name == IntroWaterName)
                {
                    return candidate;
                }
            }
            return null;
        }

        private static List<Vector2> CreateConvexHull(
            List<Vector2> points)
        {
            points.Sort((left, right) =>
            {
                var x = left.x.CompareTo(right.x);
                return x != 0 ? x : left.y.CompareTo(right.y);
            });

            var unique = new List<Vector2>(points.Count);
            foreach (var point in points)
            {
                if (unique.Count == 0 ||
                    (point - unique[unique.Count - 1]).sqrMagnitude >
                        PolygonEpsilon * PolygonEpsilon)
                {
                    unique.Add(point);
                }
            }
            if (unique.Count <= 2)
            {
                return unique;
            }

            var hull = new List<Vector2>(unique.Count * 2);
            foreach (var point in unique)
            {
                while (hull.Count >= 2 && Cross(
                    hull[hull.Count - 1] - hull[hull.Count - 2],
                    point - hull[hull.Count - 1]) <= 0f)
                {
                    hull.RemoveAt(hull.Count - 1);
                }
                hull.Add(point);
            }

            var upperStart = hull.Count + 1;
            for (var index = unique.Count - 2; index >= 0; index--)
            {
                var point = unique[index];
                while (hull.Count >= upperStart && Cross(
                    hull[hull.Count - 1] - hull[hull.Count - 2],
                    point - hull[hull.Count - 1]) <= 0f)
                {
                    hull.RemoveAt(hull.Count - 1);
                }
                hull.Add(point);
            }
            hull.RemoveAt(hull.Count - 1);
            return hull;
        }

        private static float Cross(Vector2 left, Vector2 right)
        {
            return left.x * right.y - left.y * right.x;
        }
    }
}
