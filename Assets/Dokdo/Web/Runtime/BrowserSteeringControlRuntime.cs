using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Dokdo.Web
{
    public static class BrowserSteeringBounds
    {
        public static Vector2 ClampOrigin(Vector2 origin,
            Rect viewport, Vector2 margin)
        {
            var minimum = viewport.min + Vector2.Max(
                Vector2.zero, margin);
            var maximum = viewport.max - Vector2.Max(
                Vector2.zero, margin);

            if (minimum.x > maximum.x)
            {
                minimum.x = maximum.x = viewport.center.x;
            }
            if (minimum.y > maximum.y)
            {
                minimum.y = maximum.y = viewport.center.y;
            }

            return new Vector2(
                Mathf.Clamp(origin.x, minimum.x, maximum.x),
                Mathf.Clamp(origin.y, minimum.y, maximum.y));
        }

        public static Vector2 CalculateContainmentOffset(
            Rect content, Rect viewport)
        {
            return new Vector2(
                CalculateAxisOffset(content.xMin, content.xMax,
                    viewport.xMin, viewport.xMax),
                CalculateAxisOffset(content.yMin, content.yMax,
                    viewport.yMin, viewport.yMax));
        }

        private static float CalculateAxisOffset(float contentMin,
            float contentMax, float viewportMin, float viewportMax)
        {
            if (contentMax - contentMin > viewportMax - viewportMin)
            {
                return (viewportMin + viewportMax - contentMin -
                    contentMax) * 0.5f;
            }
            if (contentMin < viewportMin)
            {
                return viewportMin - contentMin;
            }
            return contentMax > viewportMax
                ? viewportMax - contentMax
                : 0f;
        }
    }

    public static class BrowserSteeringSkinning
    {
        public static void EnsureTwoBoneBlending(
            SkinnedMeshRenderer renderer)
        {
            QualitySettings.skinWeights = SkinWeights.TwoBones;
            if (renderer != null)
            {
                renderer.quality = SkinQuality.Bone2;
            }
        }
    }

    public static class BrowserSteeringMesh
    {
        public const int DomeArcSegments = 16;
        public const int DomeRadialSegments = 8;

        private const string OriginalDomeSuffix =
            " Web Original Dome";

        public static Mesh CreateOriginalTeardrop(Mesh source)
        {
            if (source == null ||
                source.name.EndsWith(OriginalDomeSuffix,
                    StringComparison.Ordinal) ||
                source.subMeshCount != 1)
            {
                return null;
            }

            var vertices = source.vertices;
            var textureCoordinates = source.uv;
            var boneWeights = source.boneWeights;
            var bindPoses = source.bindposes;
            if (vertices.Length == 0 ||
                textureCoordinates.Length != vertices.Length ||
                boneWeights.Length != vertices.Length ||
                bindPoses.Length < 2)
            {
                return null;
            }

            var center = source.bounds.center;
            var movingEndpoint = bindPoses[1].inverse
                .MultiplyPoint3x4(Vector3.zero);
            var movingDirection = movingEndpoint - center;
            movingDirection.z = 0f;
            if (movingDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return null;
            }

            var domeDirection = -movingDirection.normalized;
            var perpendicular = new Vector3(-domeDirection.y,
                domeDirection.x, 0f);
            var radius = CalculateRadius(vertices, center,
                perpendicular);
            if (radius <= Mathf.Epsilon)
            {
                return null;
            }

            var centerVertexCount = DomeArcSegments;
            var ringVertexCount = DomeArcSegments + 1;
            var addedVertexCount = centerVertexCount +
                DomeRadialSegments * ringVertexCount;
            var rebuiltVertices = new Vector3[
                vertices.Length + addedVertexCount];
            var rebuiltTextureCoordinates = new Vector2[
                textureCoordinates.Length + addedVertexCount];
            var rebuiltBoneWeights = new BoneWeight[
                boneWeights.Length + addedVertexCount];
            Array.Copy(vertices, rebuiltVertices, vertices.Length);
            Array.Copy(textureCoordinates, rebuiltTextureCoordinates,
                textureCoordinates.Length);
            Array.Copy(boneWeights, rebuiltBoneWeights,
                boneWeights.Length);

            var firstAddedVertex = vertices.Length;
            for (var sector = 0;
                 sector < DomeArcSegments;
                 sector++)
            {
                var vertexIndex = firstAddedVertex + sector;
                rebuiltVertices[vertexIndex] = center;
                rebuiltTextureCoordinates[vertexIndex] = new Vector2(
                    DomeTextureU(sector + 1), 1f);
                rebuiltBoneWeights[vertexIndex] = RootBoneWeight();
            }

            for (var ring = 1;
                 ring <= DomeRadialSegments;
                 ring++)
            {
                var radialFraction = ring /
                    (float)DomeRadialSegments;
                var ringRadius = radius * Mathf.Sin(
                    Mathf.PI * 0.5f * radialFraction);
                var textureV = 1f - radialFraction;
                for (var arc = 0;
                     arc <= DomeArcSegments;
                     arc++)
                {
                    var angle = -Mathf.PI * 0.5f +
                        Mathf.PI * arc / DomeArcSegments;
                    var vertexIndex = RingVertexIndex(
                        firstAddedVertex, ring, arc);
                    rebuiltVertices[vertexIndex] = center +
                        domeDirection * (Mathf.Cos(angle) * ringRadius) +
                        perpendicular * (Mathf.Sin(angle) * ringRadius);
                    rebuiltTextureCoordinates[vertexIndex] =
                        new Vector2(DomeTextureU(arc), textureV);
                    rebuiltBoneWeights[vertexIndex] = RootBoneWeight();
                }
            }

            var rebuiltTriangles = new List<int>();
            var sourceTriangles = source.GetTriangles(0);
            var tolerance = radius * 0.0001f;
            for (var triangle = 0;
                 triangle < sourceTriangles.Length;
                 triangle += 3)
            {
                var first = sourceTriangles[triangle];
                var second = sourceTriangles[triangle + 1];
                var third = sourceTriangles[triangle + 2];
                if (IsDomeVertex(vertices[first], center,
                        domeDirection, tolerance) &&
                    IsDomeVertex(vertices[second], center,
                        domeDirection, tolerance) &&
                    IsDomeVertex(vertices[third], center,
                        domeDirection, tolerance))
                {
                    continue;
                }
                rebuiltTriangles.Add(first);
                rebuiltTriangles.Add(second);
                rebuiltTriangles.Add(third);
            }

            for (var sector = 0;
                 sector < DomeArcSegments;
                 sector++)
            {
                rebuiltTriangles.Add(firstAddedVertex + sector);
                rebuiltTriangles.Add(RingVertexIndex(
                    firstAddedVertex, 1, sector));
                rebuiltTriangles.Add(RingVertexIndex(
                    firstAddedVertex, 1, sector + 1));
            }

            for (var ring = 1;
                 ring < DomeRadialSegments;
                 ring++)
            {
                for (var sector = 0;
                     sector < DomeArcSegments;
                     sector++)
                {
                    var innerCurrent = RingVertexIndex(
                        firstAddedVertex, ring, sector);
                    var innerNext = RingVertexIndex(
                        firstAddedVertex, ring, sector + 1);
                    var outerCurrent = RingVertexIndex(
                        firstAddedVertex, ring + 1, sector);
                    var outerNext = RingVertexIndex(
                        firstAddedVertex, ring + 1, sector + 1);
                    rebuiltTriangles.Add(innerCurrent);
                    rebuiltTriangles.Add(outerCurrent);
                    rebuiltTriangles.Add(outerNext);
                    rebuiltTriangles.Add(innerCurrent);
                    rebuiltTriangles.Add(outerNext);
                    rebuiltTriangles.Add(innerNext);
                }
            }

            var rebuilt = UnityEngine.Object.Instantiate(source);
            rebuilt.name = source.name + OriginalDomeSuffix;
            rebuilt.vertices = rebuiltVertices;
            rebuilt.uv = rebuiltTextureCoordinates;
            rebuilt.boneWeights = rebuiltBoneWeights;
            rebuilt.SetTriangles(rebuiltTriangles, 0, true);
            rebuilt.RecalculateBounds();
            return rebuilt;
        }

        private static float CalculateRadius(Vector3[] vertices,
            Vector3 center, Vector3 perpendicular)
        {
            var radius = 0f;
            foreach (var vertex in vertices)
            {
                radius = Mathf.Max(radius, Mathf.Abs(Vector3.Dot(
                    vertex - center, perpendicular)));
            }
            return radius;
        }

        private static bool IsDomeVertex(Vector3 vertex,
            Vector3 center, Vector3 domeDirection, float tolerance)
        {
            return Vector3.Dot(vertex - center, domeDirection) >=
                -tolerance;
        }

        private static float DomeTextureU(int arc)
        {
            return 0.5f - 0.5f * arc / DomeArcSegments;
        }

        private static int RingVertexIndex(int firstAddedVertex,
            int ring, int arc)
        {
            return firstAddedVertex + DomeArcSegments +
                (ring - 1) * (DomeArcSegments + 1) + arc;
        }

        private static BoneWeight RootBoneWeight()
        {
            return new BoneWeight
            {
                boneIndex0 = 0,
                weight0 = 1f
            };
        }
    }

    [DefaultExecutionOrder(10000)]
    public sealed class BrowserSteeringControlRuntime : MonoBehaviour
    {
        private const string ControllerTypeName =
            "ControllerView, Assembly-CSharp";
        private const string ShipControllerTypeName =
            "ShipController, Assembly-CSharp";
        private const float VisualPaddingPixels = 16f;

        private Component controller;
        private FieldInfo handlePressField;
        private FieldInfo pressField;
        private Camera uiCamera;
        private GameObject handleArea;
        private GameObject handleMove;
        private GameObject handleStick;
        private Renderer[] handleAreaRenderers;
        private Renderer[] handleStickRenderers;
        private SkinnedMeshRenderer originalShapeRenderer;
        private Mesh sourceSteeringMesh;
        private Mesh originalShapeMesh;
        private Bounds sourceRendererBounds;
        private RectTransform mouseCursor;
        private Canvas mouseCursorCanvas;
        private readonly Vector3[] mouseCursorCorners = new Vector3[4];
        private Vector2 originMargin;
        private Rect safeAreaPixels;
        private bool hasSafeArea;
        private bool wasPressed;
        private int marginScreenWidth;
        private int marginScreenHeight;

        public void Refresh()
        {
            ClearBinding();
            BindJoystick();
            BindMouseCursor();
        }

        private void BindJoystick()
        {
            var type = Type.GetType(ControllerTypeName, false);
            controller = type == null
                ? null
                : FindSceneComponent(type);
            if (controller == null)
            {
                return;
            }

            var cameraField = type.GetField("mainUICamera");
            var handleAreaField = type.GetField("handleArea");
            var handleMoveField = type.GetField("handleMove");
            var handleStickField = type.GetField("handleStick");
            handlePressField = type.GetField("handlePress");
            pressField = type.GetField("pressBool");
            uiCamera = cameraField?.GetValue(controller) as Camera;
            handleArea = handleAreaField?.GetValue(controller)
                as GameObject;
            handleMove = handleMoveField?.GetValue(controller)
                as GameObject;
            handleStick = handleStickField?.GetValue(controller)
                as GameObject;

            if (uiCamera == null || handleArea == null ||
                handleMove == null || handleStick == null ||
                handlePressField == null || pressField == null)
            {
                ClearBinding();
                return;
            }
            EnsureOriginalSteeringShape();
            handleAreaRenderers = handleArea
                .GetComponentsInChildren<Renderer>(true);
            handleStickRenderers = handleStick
                .GetComponentsInChildren<Renderer>(true);
        }

        private void EnsureOriginalSteeringShape()
        {
            var renderer = handleArea.GetComponentInChildren<
                SkinnedMeshRenderer>(true);
            BrowserSteeringSkinning.EnsureTwoBoneBlending(renderer);
            var originalShape = BrowserSteeringMesh.CreateOriginalTeardrop(
                renderer != null ? renderer.sharedMesh : null);
            if (renderer == null || originalShape == null)
            {
                return;
            }

            originalShapeRenderer = renderer;
            sourceSteeringMesh = renderer.sharedMesh;
            originalShapeMesh = originalShape;
            sourceRendererBounds = renderer.localBounds;
            renderer.sharedMesh = originalShape;
            renderer.localBounds = sourceRendererBounds;
        }

        private void BindMouseCursor()
        {
            var type = Type.GetType(ShipControllerTypeName, false);
            var shipController = type == null
                ? null
                : FindSceneComponent(type);
            var cursorField = type?.GetField("mousePos");
            mouseCursor = shipController != null
                ? cursorField?.GetValue(shipController) as RectTransform
                : null;
            mouseCursorCanvas = mouseCursor != null
                ? mouseCursor.GetComponentInParent<Canvas>()
                : null;
        }

        public void SetSafeArea(Rect safeArea)
        {
            safeAreaPixels = safeArea;
            hasSafeArea = true;
        }

        private void LateUpdate()
        {
            ClampMouseCursor();
            if (controller == null ||
                !controller.gameObject.scene.IsValid())
            {
                return;
            }
            var isPressed = (bool)pressField.GetValue(controller);
            if (!isPressed)
            {
                wasPressed = false;
                return;
            }

            if (!wasPressed || marginScreenWidth != Screen.width ||
                marginScreenHeight != Screen.height)
            {
                originMargin = CalculateOriginMargin(
                    handleArea.transform.position,
                    handleAreaRenderers);
                marginScreenWidth = Screen.width;
                marginScreenHeight = Screen.height;
            }
            wasPressed = true;
            ClampControlOrigin();
            ClampStickVisual();
        }

        private void ClampMouseCursor()
        {
            if (mouseCursor == null ||
                !mouseCursor.gameObject.activeInHierarchy)
            {
                return;
            }

            var viewport = GetAllowedScreenViewport();
            mouseCursor.GetWorldCorners(mouseCursorCorners);
            var eventCamera = mouseCursorCanvas != null &&
                mouseCursorCanvas.renderMode !=
                    RenderMode.ScreenSpaceOverlay
                ? mouseCursorCanvas.worldCamera
                : null;
            var minimum = new Vector2(
                float.PositiveInfinity, float.PositiveInfinity);
            var maximum = new Vector2(
                float.NegativeInfinity, float.NegativeInfinity);
            foreach (var corner in mouseCursorCorners)
            {
                var screen = RectTransformUtility.WorldToScreenPoint(
                    eventCamera, corner);
                minimum = Vector2.Min(minimum, screen);
                maximum = Vector2.Max(maximum, screen);
            }

            var bounds = Rect.MinMaxRect(
                minimum.x - VisualPaddingPixels,
                minimum.y - VisualPaddingPixels,
                maximum.x + VisualPaddingPixels,
                maximum.y + VisualPaddingPixels);
            var offset = BrowserSteeringBounds
                .CalculateContainmentOffset(bounds, viewport);
            if (offset.sqrMagnitude < 0.01f)
            {
                return;
            }

            var currentScreen = RectTransformUtility.WorldToScreenPoint(
                eventCamera, mouseCursor.position);
            if (mouseCursor.parent is RectTransform parent &&
                RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    parent, currentScreen + offset, eventCamera,
                    out var targetWorld))
            {
                mouseCursor.position = targetWorld;
            }
        }

        private void ClampControlOrigin()
        {
            var viewport = GetAllowedViewport();
            if (viewport.width <= 0f || viewport.height <= 0f)
            {
                return;
            }

            var originWorld = handleArea.transform.position;
            var originScreen = uiCamera.WorldToScreenPoint(originWorld);
            var clamped = BrowserSteeringBounds.ClampOrigin(
                originScreen, viewport, originMargin);
            var offset = clamped - (Vector2)originScreen;
            if (offset.sqrMagnitude < 0.01f)
            {
                return;
            }

            var worldOffset = ScreenOffsetToWorld(offset, originScreen.z);
            handleArea.transform.position += worldOffset;
            handleStick.transform.position += worldOffset;
            var handlePress = (Vector3)handlePressField.GetValue(controller);
            handlePressField.SetValue(controller, handlePress + worldOffset);
        }

        private void ClampStickVisual()
        {
            var viewport = GetAllowedViewport();
            if (!TryCalculateScreenBounds(
                    handleStickRenderers, out var bounds))
            {
                return;
            }

            var offset = BrowserSteeringBounds
                .CalculateContainmentOffset(bounds, viewport);
            if (offset.sqrMagnitude < 0.01f)
            {
                return;
            }

            var depth = uiCamera.WorldToScreenPoint(
                handleStick.transform.position).z;
            var worldOffset = ScreenOffsetToWorld(offset, depth);
            handleStick.transform.position += worldOffset;
            handleMove.transform.position += worldOffset;
        }

        private Vector2 CalculateOriginMargin(Vector3 origin,
            Renderer[] renderers)
        {
            var originScreen = uiCamera.WorldToScreenPoint(origin);
            var radius = 0f;
            foreach (var renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }
                var bounds = renderer.localBounds;
                for (var x = -1; x <= 1; x += 2)
                {
                    for (var y = -1; y <= 1; y += 2)
                    {
                        for (var z = -1; z <= 1; z += 2)
                        {
                            radius = Mathf.Max(radius, Vector2.Distance(
                                originScreen, GetScreenCorner(renderer,
                                    bounds, x, y, z)));
                        }
                    }
                }
            }
            radius += VisualPaddingPixels;
            return new Vector2(radius, radius);
        }

        private bool TryCalculateScreenBounds(Renderer[] renderers,
            out Rect bounds)
        {
            var minimum = new Vector2(
                float.PositiveInfinity, float.PositiveInfinity);
            var maximum = new Vector2(
                float.NegativeInfinity, float.NegativeInfinity);
            var found = false;
            foreach (var renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }
                var localBounds = renderer.localBounds;
                for (var x = -1; x <= 1; x += 2)
                {
                    for (var y = -1; y <= 1; y += 2)
                    {
                        for (var z = -1; z <= 1; z += 2)
                        {
                            var point = GetScreenCorner(renderer,
                                localBounds, x, y, z);
                            minimum = Vector2.Min(minimum, point);
                            maximum = Vector2.Max(maximum, point);
                            found = true;
                        }
                    }
                }
            }
            if (!found)
            {
                bounds = default;
                return false;
            }
            bounds = Rect.MinMaxRect(minimum.x - VisualPaddingPixels,
                minimum.y - VisualPaddingPixels,
                maximum.x + VisualPaddingPixels,
                maximum.y + VisualPaddingPixels);
            return true;
        }

        private Vector2 GetScreenCorner(Renderer renderer,
            Bounds bounds, int x, int y, int z)
        {
            var local = bounds.center + Vector3.Scale(
                bounds.extents, new Vector3(x, y, z));
            var world = renderer.transform.TransformPoint(local);
            return uiCamera.WorldToScreenPoint(world);
        }

        private Rect GetAllowedViewport()
        {
            return IntersectWithSafeArea(uiCamera.pixelRect);
        }

        private Rect GetAllowedScreenViewport()
        {
            return IntersectWithSafeArea(
                new Rect(0f, 0f, Screen.width, Screen.height));
        }

        private Rect IntersectWithSafeArea(Rect viewport)
        {
            if (!hasSafeArea)
            {
                return viewport;
            }

            var minimum = Vector2.Max(viewport.min,
                safeAreaPixels.min);
            var maximum = Vector2.Min(viewport.max,
                safeAreaPixels.max);
            return maximum.x > minimum.x && maximum.y > minimum.y
                ? Rect.MinMaxRect(minimum.x, minimum.y,
                    maximum.x, maximum.y)
                : viewport;
        }

        private Vector3 ScreenOffsetToWorld(Vector2 offset, float depth)
        {
            var start = uiCamera.ScreenToWorldPoint(
                new Vector3(0f, 0f, depth));
            var end = uiCamera.ScreenToWorldPoint(
                new Vector3(offset.x, offset.y, depth));
            return end - start;
        }

        private void ClearBinding()
        {
            if (originalShapeRenderer != null &&
                originalShapeRenderer.sharedMesh == originalShapeMesh)
            {
                originalShapeRenderer.sharedMesh = sourceSteeringMesh;
                originalShapeRenderer.localBounds = sourceRendererBounds;
            }
            if (originalShapeMesh != null)
            {
                Destroy(originalShapeMesh);
            }
            controller = null;
            handlePressField = null;
            pressField = null;
            uiCamera = null;
            handleArea = null;
            handleMove = null;
            handleStick = null;
            handleAreaRenderers = null;
            handleStickRenderers = null;
            originalShapeRenderer = null;
            sourceSteeringMesh = null;
            originalShapeMesh = null;
            sourceRendererBounds = default;
            mouseCursor = null;
            mouseCursorCanvas = null;
            originMargin = Vector2.zero;
            wasPressed = false;
            marginScreenWidth = 0;
            marginScreenHeight = 0;
        }

        private void OnDestroy()
        {
            ClearBinding();
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
