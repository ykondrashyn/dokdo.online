using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Dokdo.Web.Tests
{
    public sealed class BrowserResponsivePresentationTests
    {
        private readonly List<GameObject> objects = new();
        private Scene testScene;

        [SetUp]
        public void SetUp()
        {
            testScene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var candidate in objects)
            {
                if (candidate != null)
                {
                    Object.DestroyImmediate(candidate);
                }
            }
            objects.Clear();
        }

        [Test]
        public void RootCanvasScalerUsesExpandAndKeepsReferenceResolution()
        {
            var scaler = CreateScaler("root");
            scaler.referenceResolution = new Vector2(720f, 1280f);

            Assert.That(scaler.gameObject.scene.IsValid(), Is.True,
                "test Canvas must belong to a valid scene");
            Assert.That(scaler.GetComponent<Canvas>(), Is.Not.Null);
            Assert.That(scaler.GetComponent<Canvas>().renderMode,
                Is.Not.EqualTo(RenderMode.WorldSpace));
            Assert.That(scaler.transform.parent, Is.Null);
            Assert.That(scaler.uiScaleMode, Is.EqualTo(
                CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(BrowserResponsivePresentation.IsEligible(scaler),
                Is.True, "root Canvas should be eligible");

            var changed = BrowserResponsivePresentation.ApplyCanvasScalers(
                new[] { scaler });

            Assert.That(changed, Is.EqualTo(1));
            Assert.That(scaler.screenMatchMode,
                Is.EqualTo(CanvasScaler.ScreenMatchMode.Expand));
            Assert.That(scaler.referenceResolution,
                Is.EqualTo(new Vector2(720f, 1280f)));
        }

        [Test]
        public void NewlyCreatedRootCanvasScalerIsDiscovered()
        {
            var first = CreateScaler("first");
            BrowserResponsivePresentation.ApplyDiscoveredCanvasScalers();

            var popup = CreateScaler("dynamic popup");
            var changed = BrowserResponsivePresentation
                .ApplyDiscoveredCanvasScalers();

            Assert.That(first.screenMatchMode,
                Is.EqualTo(CanvasScaler.ScreenMatchMode.Expand));
            Assert.That(popup.screenMatchMode,
                Is.EqualTo(CanvasScaler.ScreenMatchMode.Expand));
            Assert.That(changed, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void WorldSpaceNestedAndUnscaledCanvasesAreExcluded()
        {
            var root = CreateScaler("root");
            var world = CreateScaler("world");
            world.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var unscaled = CreateScaler("unscaled");
            unscaled.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            var nested = CreateScaler("nested");
            nested.transform.SetParent(root.transform);

            var changed = BrowserResponsivePresentation.ApplyCanvasScalers(
                new[] { world, unscaled, nested });

            Assert.That(changed, Is.Zero);
            Assert.That(world.screenMatchMode,
                Is.Not.EqualTo(CanvasScaler.ScreenMatchMode.Expand));
            Assert.That(nested.screenMatchMode,
                Is.Not.EqualTo(CanvasScaler.ScreenMatchMode.Expand));
            Assert.That(unscaled.uiScaleMode,
                Is.EqualTo(CanvasScaler.ScaleMode.ConstantPixelSize));
        }

        [Test]
        public void ProjectionRetainsVerticalViewForWideViewport()
        {
            var extents = BrowserResponsivePresentation
                .CalculateOrthographicHalfExtents(5f, 16f / 9f);

            Assert.That(extents.y, Is.EqualTo(5f).Within(0.0001f));
            Assert.That(extents.x,
                Is.EqualTo(5f * 16f / 9f).Within(0.0001f));
        }

        [Test]
        public void ProjectionRetainsHorizontalViewForNarrowViewport()
        {
            var extents = BrowserResponsivePresentation
                .CalculateOrthographicHalfExtents(5f, 0.5f);

            Assert.That(extents.x,
                Is.EqualTo(5f * 9f / 16f).Within(0.0001f));
            Assert.That(extents.y,
                Is.EqualTo(5f * 9f / 16f / 0.5f).Within(0.0001f));
        }

        [Test]
        public void ProjectionTracksCameraZoomWithoutCompounding()
        {
            var cameraObject = new GameObject("camera", typeof(Camera));
            objects.Add(cameraObject);
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;

            BrowserResponsivePresentation.ApplyCameraProjection(
                camera, 16f / 9f);
            var first = camera.projectionMatrix;
            camera.orthographicSize = 7f;
            BrowserResponsivePresentation.ApplyCameraProjection(
                camera, 16f / 9f);
            var second = camera.projectionMatrix;
            BrowserResponsivePresentation.ApplyCameraProjection(
                camera, 16f / 9f);
            var repeated = camera.projectionMatrix;

            Assert.That(first.m00,
                Is.EqualTo(1f / (5f * 16f / 9f)).Within(0.0001f));
            Assert.That(second.m00,
                Is.EqualTo(1f / (7f * 16f / 9f)).Within(0.0001f));
            Assert.That(repeated, Is.EqualTo(second));
        }

        [Test]
        public void IntroCoverZoomsToFitWideViewportInsideCoverage()
        {
            var coverage = new[]
            {
                new Vector2(-10f, -10f),
                new Vector2(10f, -10f),
                new Vector2(10f, 10f),
                new Vector2(-10f, 10f)
            };

            var size = BrowserResponsivePresentation
                .CalculateCoveringOrthographicSize(
                    10f, 2f, coverage);

            Assert.That(size, Is.EqualTo(4.9f).Within(0.0001f));
        }

        [Test]
        public void IntroCoverUsesNearestEdgeWhenCameraIsOffCenter()
        {
            var coverage = new[]
            {
                new Vector2(-12f, -10f),
                new Vector2(8f, -10f),
                new Vector2(8f, 10f),
                new Vector2(-12f, 10f)
            };

            var size = BrowserResponsivePresentation
                .CalculateCoveringOrthographicSize(
                    10f, 1f, coverage);

            Assert.That(size, Is.EqualTo(7.84f).Within(0.0001f));
        }

        [Test]
        public void IntroCoverDoesNotZoomWhenCoverageAlreadyHasMargin()
        {
            var coverage = new[]
            {
                new Vector2(-20f, -20f),
                new Vector2(20f, -20f),
                new Vector2(20f, 20f),
                new Vector2(-20f, 20f)
            };

            var size = BrowserResponsivePresentation
                .CalculateCoveringOrthographicSize(
                    5f, 16f / 9f, coverage);

            Assert.That(size, Is.EqualTo(5f));
        }

        [Test]
        public void ActualIntroWaterAdaptsZoomToViewportAspect()
        {
            var intro = OpenRecoveredScene(
                "Assets/0.my/Scenes/Intro.unity");
            var camera = GameObject.Find("Main Camera")
                .GetComponent<Camera>();
            var sourceSize = camera.orthographicSize;

            var portrait = BrowserResponsivePresentation
                .CalculateMainCameraOrthographicSize(
                    camera, 9f / 16f);
            var wide = BrowserResponsivePresentation
                .CalculateMainCameraOrthographicSize(
                    camera, 16f / 9f);
            var ultraWide = BrowserResponsivePresentation
                .CalculateMainCameraOrthographicSize(
                    camera, 3440f / 1440f);

            Assert.That(intro.name, Is.EqualTo("Intro"));
            Assert.That(portrait, Is.EqualTo(camera.orthographicSize));
            Assert.That(wide, Is.InRange(60f, 63f));
            Assert.That(ultraWide, Is.LessThan(wide));
            Assert.That(ultraWide, Is.GreaterThan(0f));

            Assert.That(BrowserResponsivePresentation
                .ApplyResponsiveCameraProjection(camera, 16f / 9f),
                Is.True);
            var firstWideProjection = camera.projectionMatrix;
            Assert.That(camera.orthographicSize, Is.EqualTo(sourceSize));

            BrowserResponsivePresentation
                .ApplyResponsiveCameraProjection(camera, 9f / 16f);
            Assert.That(camera.projectionMatrix, Is.EqualTo(
                BrowserResponsivePresentation.CreateOrthographicProjection(
                    sourceSize, 9f / 16f, camera.nearClipPlane,
                    camera.farClipPlane)));
            Assert.That(camera.orthographicSize, Is.EqualTo(sourceSize));

            BrowserResponsivePresentation
                .ApplyResponsiveCameraProjection(camera, 16f / 9f);
            Assert.That(camera.projectionMatrix,
                Is.EqualTo(firstWideProjection));
            Assert.That(camera.orthographicSize, Is.EqualTo(sourceSize));
        }

        [Test]
        public void MainUsesLiveGameZoomAfterIntroProjection()
        {
            OpenRecoveredScene("Assets/0.my/Scenes/Intro.unity");
            var introCamera = GameObject.Find("Main Camera")
                .GetComponent<Camera>();
            BrowserResponsivePresentation.ApplyResponsiveCameraProjection(
                introCamera, 3440f / 1440f);

            var main = OpenRecoveredScene(
                "Assets/0.my/Scenes/Main.unity");
            var mainCamera = GameObject.Find("Main Camera")
                .GetComponent<Camera>();
            var serializedSize = mainCamera.orthographicSize;

            BrowserResponsivePresentation.ApplyResponsiveCameraProjection(
                mainCamera, 3440f / 1440f);

            Assert.That(main.name, Is.EqualTo("Main"));
            Assert.That(serializedSize, Is.EqualTo(40f));
            Assert.That(mainCamera.orthographicSize,
                Is.EqualTo(serializedSize));
            Assert.That(mainCamera.projectionMatrix, Is.EqualTo(
                BrowserResponsivePresentation.CreateOrthographicProjection(
                    serializedSize, 3440f / 1440f,
                    mainCamera.nearClipPlane, mainCamera.farClipPlane)));

            mainCamera.orthographicSize = 15f;
            BrowserResponsivePresentation.ApplyResponsiveCameraProjection(
                mainCamera, 16f / 9f);

            Assert.That(mainCamera.orthographicSize, Is.EqualTo(15f));
            Assert.That(mainCamera.projectionMatrix, Is.EqualTo(
                BrowserResponsivePresentation.CreateOrthographicProjection(
                    15f, 16f / 9f, mainCamera.nearClipPlane,
                    mainCamera.farClipPlane)));
        }

        [Test]
        public void NonIntroCameraKeepsRequestedSize()
        {
            var candidate = new GameObject("camera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(candidate, testScene);
            objects.Add(candidate);
            var camera = candidate.GetComponent<Camera>();
            camera.orthographicSize = 7f;

            var size = BrowserResponsivePresentation
                .CalculateMainCameraOrthographicSize(
                    camera, 32f / 9f);

            Assert.That(size, Is.EqualTo(7f));
        }

        [Test]
        public void SafeAreaIsReappliedAfterCanvasPolicy()
        {
            var scaler = CreateScaler("resized root");
            var safeAreaCalls = 0;

            BrowserResponsivePresentation.ApplyCanvasPolicyThen(
                new[] { scaler }, () =>
                {
                    Assert.That(scaler.screenMatchMode,
                        Is.EqualTo(CanvasScaler.ScreenMatchMode.Expand));
                    safeAreaCalls++;
                });

            Assert.That(safeAreaCalls, Is.EqualTo(1));
        }

        [Test]
        public void MapViewportUsesExistingOceanBackgroundColor()
        {
            var pauseMenu = CreateUiObject("PauseMenu");
            var scrollView = CreateUiObject("Scroll View",
                pauseMenu.transform);
            var viewport = CreateUiObject("Viewport",
                scrollView.transform);
            var image = viewport.AddComponent<Image>();
            image.color = Color.white;

            var changed = BrowserResponsivePresentation
                .ApplyMapViewportBackgrounds(new[] { image });

            Assert.That(changed, Is.EqualTo(1));
            Assert.That((Color32)image.color, Is.EqualTo(
                BrowserResponsivePresentation.MapViewportBackgroundColor));
        }

        [Test]
        public void UnrelatedViewportBackgroundIsNotChanged()
        {
            var otherMenu = CreateUiObject("OtherMenu");
            var scrollView = CreateUiObject("Scroll View",
                otherMenu.transform);
            var viewport = CreateUiObject("Viewport",
                scrollView.transform);
            var image = viewport.AddComponent<Image>();
            image.color = Color.white;

            var changed = BrowserResponsivePresentation
                .ApplyMapViewportBackgrounds(new[] { image });

            Assert.That(changed, Is.Zero);
            Assert.That(image.color, Is.EqualTo(Color.white));
        }

        [Test]
        public void SteeringOriginIsClampedInsideEveryViewportEdge()
        {
            var viewport = new Rect(0f, 0f, 1366f, 768f);
            var margin = new Vector2(90f, 90f);

            Assert.That(BrowserSteeringBounds.ClampOrigin(
                    new Vector2(-20f, 800f), viewport, margin),
                Is.EqualTo(new Vector2(90f, 678f)));
            Assert.That(BrowserSteeringBounds.ClampOrigin(
                    new Vector2(1400f, -10f), viewport, margin),
                Is.EqualTo(new Vector2(1276f, 90f)));
        }

        [Test]
        public void SteeringOriginIncludesSafeAreaInsets()
        {
            var safeViewport = new Rect(20f, 30f, 350f, 780f);

            var clamped = BrowserSteeringBounds.ClampOrigin(
                new Vector2(10f, 820f), safeViewport,
                new Vector2(40f, 50f));

            Assert.That(clamped, Is.EqualTo(new Vector2(60f, 760f)));
        }

        [Test]
        public void SteeringStickMovesFullyInsideViewport()
        {
            var viewport = new Rect(0f, 0f, 1366f, 768f);

            var offset = BrowserSteeringBounds
                .CalculateContainmentOffset(
                    Rect.MinMaxRect(1300f, 740f, 1400f, 800f),
                    viewport);

            Assert.That(offset, Is.EqualTo(new Vector2(-34f, -32f)));
        }

        [Test]
        public void MouseSteeringCursorMovesBelowTopEdge()
        {
            var viewport = new Rect(0f, 0f, 1366f, 768f);

            var offset = BrowserSteeringBounds
                .CalculateContainmentOffset(
                    Rect.MinMaxRect(1100f, 718f, 1200f, 818f),
                    viewport);

            Assert.That(offset, Is.EqualTo(new Vector2(0f, -50f)));
        }

        [Test]
        public void SteeringMeshRebuildsOriginalRootDome()
        {
            var source = new Mesh
            {
                name = "steering beam",
                vertices = new[]
                {
                    new Vector3(-1f, 0f, 0f),
                    new Vector3(0f, -1f, 0f),
                    new Vector3(0f, 1f, 0f),
                    new Vector3(1f, 0f, 0f)
                },
                uv = new[]
                {
                    Vector2.zero,
                    Vector2.up,
                    Vector2.right,
                    Vector2.one
                },
                boneWeights = new[]
                {
                    EndpointBoneWeight(),
                    RootBoneWeight(),
                    RootBoneWeight(),
                    RootBoneWeight()
                },
                bindposes = new[]
                {
                    Matrix4x4.identity,
                    Matrix4x4.Translate(Vector3.right)
                }
            };
            source.triangles = new[] { 0, 1, 2, 1, 3, 2 };
            source.RecalculateBounds();

            var rebuilt = BrowserSteeringMesh
                .CreateOriginalTeardrop(source);

            var addedVertexCount = BrowserSteeringMesh.DomeArcSegments +
                BrowserSteeringMesh.DomeRadialSegments *
                (BrowserSteeringMesh.DomeArcSegments + 1);
            var rebuiltTriangleCount =
                BrowserSteeringMesh.DomeArcSegments +
                (BrowserSteeringMesh.DomeRadialSegments - 1) *
                BrowserSteeringMesh.DomeArcSegments * 2;
            Assert.That(rebuilt, Is.Not.Null);
            Assert.That(rebuilt.vertexCount, Is.EqualTo(
                source.vertexCount +
                addedVertexCount));
            Assert.That(rebuilt.triangles.Length, Is.EqualTo(
                3 + rebuiltTriangleCount * 3));

            var addedVertices = rebuilt.vertices
                .Skip(source.vertexCount).ToArray();
            var addedTextureCoordinates = rebuilt.uv
                .Skip(source.vertexCount).ToArray();
            var rebuiltWeights = rebuilt.boneWeights;
            Assert.That(addedVertices.Min(vertex => vertex.x),
                Is.EqualTo(0f).Within(0.0001f));
            Assert.That(addedVertices.Max(vertex => vertex.x),
                Is.EqualTo(1f).Within(0.0001f));
            Assert.That(addedVertices.Min(vertex => vertex.y),
                Is.EqualTo(-1f).Within(0.0001f));
            Assert.That(addedVertices.Max(vertex => vertex.y),
                Is.EqualTo(1f).Within(0.0001f));
            Assert.That(addedTextureCoordinates.Min(uv => uv.y),
                Is.EqualTo(0f).Within(0.0001f));
            Assert.That(addedTextureCoordinates.Max(uv => uv.y),
                Is.EqualTo(1f).Within(0.0001f));
            var firstRingBottom = addedVertices[
                BrowserSteeringMesh.DomeArcSegments];
            Assert.That(firstRingBottom.y,
                Is.EqualTo(-Mathf.Sin(Mathf.PI / 16f))
                    .Within(0.0001f));
            Assert.That(addedTextureCoordinates[
                    BrowserSteeringMesh.DomeArcSegments].y,
                Is.EqualTo(0.875f).Within(0.0001f));
            for (var index = source.vertexCount;
                 index < rebuiltWeights.Length;
                 index++)
            {
                Assert.That(rebuiltWeights[index].boneIndex0,
                    Is.EqualTo(0));
                Assert.That(rebuiltWeights[index].weight0,
                    Is.EqualTo(1f));
            }

            Assert.That(rebuilt.vertices[0],
                Is.EqualTo(source.vertices[0]));
            Assert.That(rebuiltWeights[0].boneIndex0, Is.EqualTo(1));
            var triangles = rebuilt.triangles;
            for (var index = 3;
                 index < triangles.Length;
                 index += 3)
            {
                var first = rebuilt.vertices[triangles[index]];
                var second = rebuilt.vertices[triangles[index + 1]];
                var third = rebuilt.vertices[triangles[index + 2]];
                Assert.That(Vector3.Cross(second - first,
                    third - first).z, Is.GreaterThan(0f));
            }

            Object.DestroyImmediate(rebuilt);
            Object.DestroyImmediate(source);
        }

        [Test]
        public void SteeringSkinningUsesBothRecoveredBoneWeights()
        {
            var rendererObject = new GameObject("steering renderer");
            objects.Add(rendererObject);
            var renderer = rendererObject.AddComponent<
                SkinnedMeshRenderer>();
            var previousSkinWeights = QualitySettings.skinWeights;
            try
            {
                QualitySettings.skinWeights = SkinWeights.OneBone;
                renderer.quality = SkinQuality.Bone1;

                BrowserSteeringSkinning.EnsureTwoBoneBlending(renderer);

                Assert.That(QualitySettings.skinWeights,
                    Is.EqualTo(SkinWeights.TwoBones));
                Assert.That(renderer.quality,
                    Is.EqualTo(SkinQuality.Bone2));
            }
            finally
            {
                QualitySettings.skinWeights = previousSkinWeights;
            }
        }

        [Test]
        public void RecoveredSteeringMeshMatchesOriginalDomeTopology()
        {
            OpenRecoveredScene("Assets/0.my/Scenes/Main.unity");
            var source = GameObject.Find("Sphere001")
                .GetComponent<SkinnedMeshRenderer>().sharedMesh;

            var rebuilt = BrowserSteeringMesh
                .CreateOriginalTeardrop(source);

            Assert.That(source.name, Is.EqualTo("Sphere001"));
            Assert.That(source.vertexCount, Is.EqualTo(296));
            Assert.That(rebuilt, Is.Not.Null);
            Assert.That(rebuilt.vertexCount, Is.EqualTo(448));
            Assert.That(rebuilt.triangles.Length,
                Is.EqualTo(source.triangles.Length));
            Assert.That(rebuilt.bounds.min.x,
                Is.EqualTo(source.bounds.min.x).Within(0.001f));
            Assert.That(rebuilt.bounds.max.x,
                Is.EqualTo(source.bounds.max.x).Within(0.001f));
            Assert.That(rebuilt.vertices
                .Skip(source.vertexCount).Min(vertex => vertex.x),
                Is.EqualTo(source.bounds.center.x).Within(0.001f));
            Assert.That(rebuilt.vertices
                .Skip(source.vertexCount).Max(vertex => vertex.x),
                Is.EqualTo(source.bounds.max.x).Within(0.001f));
            Assert.That(rebuilt.boneWeights
                .Skip(source.vertexCount)
                .All(weight => weight.boneIndex0 == 0 &&
                    Mathf.Approximately(weight.weight0, 1f)), Is.True);

            Object.DestroyImmediate(rebuilt);
        }

        private static Scene OpenRecoveredScene(string path)
        {
            var previous = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;
            try
            {
                return EditorSceneManager.OpenScene(
                    path, OpenSceneMode.Single);
            }
            finally
            {
                LogAssert.ignoreFailingMessages = previous;
            }
        }

        private static BoneWeight RootBoneWeight()
        {
            return new BoneWeight
            {
                boneIndex0 = 0,
                weight0 = 1f
            };
        }

        private static BoneWeight EndpointBoneWeight()
        {
            return new BoneWeight
            {
                boneIndex0 = 1,
                weight0 = 1f
            };
        }

        private CanvasScaler CreateScaler(string name)
        {
            var candidate = new GameObject(name, typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler));
            SceneManager.MoveGameObjectToScene(candidate, testScene);
            candidate.GetComponent<Canvas>().renderMode =
                RenderMode.ScreenSpaceOverlay;
            objects.Add(candidate);
            var scaler = candidate.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            return scaler;
        }

        private GameObject CreateUiObject(string name,
            Transform parent = null)
        {
            var candidate = new GameObject(name, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(candidate, testScene);
            if (parent != null)
            {
                candidate.transform.SetParent(parent, false);
            }
            objects.Add(candidate);
            return candidate;
        }
    }
}
