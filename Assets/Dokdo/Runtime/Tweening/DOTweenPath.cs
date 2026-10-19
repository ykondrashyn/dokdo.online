using System;
using System.Collections.Generic;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Core.PathCore;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace DG.Tweening
{
    public enum HandlesDrawMode
    {
        Orthographic,
        Perspective
    }

    public enum HandlesType
    {
        Free,
        Full
    }

    public enum DOTweenInspectorMode
    {
        Default,
        InfoAndWaypointsOnly
    }

    [AddComponentMenu("DOTween/DOKDO Path Compatibility")]
    public sealed class DOTweenPath : ABSAnimationComponent
    {
        public float delay;
        public float duration = 1f;
        public Ease easeType = Ease.OutQuad;
        public AnimationCurve easeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public int loops = 1;
        public string id = string.Empty;
        public LoopType loopType;
        public OrientType orientType;
        public Transform lookAtTransform;
        public Vector3 lookAtPosition;
        public float lookAhead;
        public bool autoPlay = true;
        public bool autoKill = true;
        public bool relative;
        public bool isLocal;
        public bool isClosedPath;
        public int pathResolution = 10;
        public PathMode pathMode;
        public AxisConstraint lockRotation;
        public bool assignForwardAndUp;
        public Vector3 forwardDirection = Vector3.forward;
        public Vector3 upDirection = Vector3.up;
        public List<Vector3> wps = new List<Vector3>();
        public List<Vector3> fullWps = new List<Vector3>();

        [NonSerialized]
        public Path path;

        public DOTweenInspectorMode inspectorMode;
        public PathType pathType = PathType.CatmullRom;
        public HandlesType handlesType;
        public bool livePreview;
        public HandlesDrawMode handlesDrawMode;
        public float perspectiveHandleSize = 0.5f;
        public bool showIndexes = true;
        public bool showWpLength;
        public Color pathColor = new Color(1f, 1f, 1f, 0.5f);
        public Vector3 lastSrcPosition;
        public bool wpsDropdown = true;

        private void Awake()
        {
            CreateTween();
        }

        private void Reset()
        {
            lastSrcPosition = transform.position;
            if (wps.Count == 0)
            {
                wps.Add(isLocal ? Vector3.forward : transform.position + Vector3.forward);
            }
        }

        private void OnDestroy()
        {
            if (tween != null && tween.IsActive())
            {
                tween.Kill();
            }

            tween = null;
            path = null;
        }

        public void CreateTween()
        {
            if (tween != null && tween.IsActive())
            {
                tween.Kill();
            }

            if (wps == null || wps.Count == 0)
            {
                tween = null;
                return;
            }

            var waypoints = wps.ToArray();
            var pathTween = isLocal
                ? transform.DOLocalPath(
                    waypoints, duration, pathType, pathMode, pathResolution)
                : transform.DOPath(
                    waypoints, duration, pathType, pathMode, pathResolution);

            pathTween.SetOptions(
                isClosedPath, AxisConstraint.None, lockRotation);
            ApplyOrientation(pathTween);

            tween = pathTween.SetTarget(gameObject)
                .SetDelay(delay)
                .SetLoops(loops, loopType)
                .SetAutoKill(autoKill)
                .SetRelative(relative)
                .SetUpdate(updateType)
                .OnKill(() => tween = null);

            if (isSpeedBased)
            {
                tween.SetSpeedBased();
            }

            if (easeType == Ease.INTERNAL_Custom)
            {
                tween.SetEase(easeCurve);
            }
            else
            {
                tween.SetEase(easeType);
            }

            if (!string.IsNullOrEmpty(id))
            {
                tween.SetId(id);
            }

            BindCallbacks();
            if (autoPlay)
            {
                tween.Play();
            }
            else
            {
                tween.Pause();
            }

            if (hasOnTweenCreated && onTweenCreated != null)
            {
                onTweenCreated.Invoke();
            }
        }

        public override void DOPlay()
        {
            PlayIfActive(tween => tween.Play());
        }

        public override void DOPlayBackwards()
        {
            PlayIfActive(tween => tween.PlayBackwards());
        }

        public override void DOPlayForward()
        {
            PlayIfActive(tween => tween.PlayForward());
        }

        public override void DOPause()
        {
            PlayIfActive(tween => tween.Pause());
        }

        public override void DOTogglePause()
        {
            PlayIfActive(tween =>
            {
                if (tween.IsPlaying())
                {
                    tween.Pause();
                }
                else
                {
                    tween.Play();
                }
            });
        }

        public override void DORewind()
        {
            PlayIfActive(tween => tween.Rewind());
        }

        public override void DORestart(bool fromHere = false)
        {
            if (fromHere && relative)
            {
                CreateTween();
                return;
            }

            PlayIfActive(tween => tween.Restart());
        }

        public override void DOComplete()
        {
            PlayIfActive(tween => tween.Complete());
        }

        public override void DOKill()
        {
            PlayIfActive(tween => tween.Kill());
            tween = null;
        }

        public Tween GetTween()
        {
            return tween;
        }

        private void ApplyOrientation(
            TweenerCore<Vector3, Path, PathOptions> pathTween)
        {
            Vector3? forward = assignForwardAndUp ? forwardDirection : (Vector3?)null;
            Vector3? up = assignForwardAndUp ? upDirection : (Vector3?)null;
            switch (orientType)
            {
                case OrientType.ToPath:
                    pathTween.SetLookAt(lookAhead, forward, up);
                    break;
                case OrientType.LookAtTransform:
                    if (lookAtTransform != null)
                    {
                        pathTween.SetLookAt(lookAtTransform, forward, up);
                    }
                    break;
                case OrientType.LookAtPosition:
                    pathTween.SetLookAt(lookAtPosition, forward, up);
                    break;
            }
        }

        private void BindCallbacks()
        {
            if (hasOnStart && onStart != null)
            {
                tween.OnStart(onStart.Invoke);
            }
            if (hasOnPlay && onPlay != null)
            {
                tween.OnPlay(onPlay.Invoke);
            }
            if (hasOnUpdate && onUpdate != null)
            {
                tween.OnUpdate(onUpdate.Invoke);
            }
            if (hasOnStepComplete && onStepComplete != null)
            {
                tween.OnStepComplete(onStepComplete.Invoke);
            }
            if (hasOnComplete && onComplete != null)
            {
                tween.OnComplete(onComplete.Invoke);
            }
        }

        private void PlayIfActive(Action<Tween> action)
        {
            if (tween != null && tween.IsActive())
            {
                action(tween);
            }
        }
    }
}
