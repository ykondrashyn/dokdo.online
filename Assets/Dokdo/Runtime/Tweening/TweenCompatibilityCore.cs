using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace DG.Tweening.Core
{
    public enum DOTweenAnimationType
    {
        None,
        Move,
        LocalMove,
        Rotate,
        LocalRotate,
        Scale,
        Color,
        Fade,
        Text,
        PunchPosition,
        PunchRotation,
        PunchScale,
        ShakePosition,
        ShakeRotation,
        ShakeScale,
        CameraAspect,
        CameraBackgroundColor,
        CameraFieldOfView,
        CameraOrthoSize,
        CameraPixelRect,
        CameraRect,
        UIWidthHeight
    }

    public enum OnDisableBehaviour
    {
        None,
        Pause,
        Rewind,
        Kill,
        KillAndComplete,
        DestroyGameObject
    }

    public enum OnEnableBehaviour
    {
        None,
        Play,
        Restart,
        RestartFromSpawnPoint
    }

    public enum TargetType
    {
        Unset,
        Camera,
        CanvasGroup,
        Image,
        Light,
        RectTransform,
        Renderer,
        SpriteRenderer,
        Rigidbody,
        Rigidbody2D,
        Text,
        Transform,
        tk2dBaseSprite,
        tk2dTextMesh,
        TextMeshPro,
        TextMeshProUGUI
    }

    public enum VisualManagerPreset
    {
        Custom,
        PoolingSystem
    }

    public abstract class ABSAnimationComponent : MonoBehaviour
    {
        public UpdateType updateType;
        public bool isSpeedBased;
        public bool hasOnStart;
        public bool hasOnPlay;
        public bool hasOnUpdate;
        public bool hasOnStepComplete;
        public bool hasOnComplete;
        public bool hasOnTweenCreated;
        public UnityEvent onStart = new UnityEvent();
        public UnityEvent onPlay = new UnityEvent();
        public UnityEvent onUpdate = new UnityEvent();
        public UnityEvent onStepComplete = new UnityEvent();
        public UnityEvent onComplete = new UnityEvent();
        public UnityEvent onTweenCreated = new UnityEvent();

        [NonSerialized]
        public Tween tween;

        public abstract void DOPlay();
        public abstract void DOPlayBackwards();
        public abstract void DOPlayForward();
        public abstract void DOPause();
        public abstract void DOTogglePause();
        public abstract void DORewind();
        public abstract void DORestart(bool fromHere = false);
        public abstract void DOComplete();
        public abstract void DOKill();
    }
}
