// GUID-preserving scene adapter implemented against the free DOTween core.
using System;
using System.Collections.Generic;
using DG.Tweening.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DG.Tweening
{
    public class DOTweenAnimation : ABSAnimationComponent
    {
        public float delay, duration = 1f;
        public Ease easeType = Ease.OutQuad;
        public AnimationCurve easeCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public LoopType loopType;
        public int loops = 1;
        public string id = "";
        public bool isRelative, isFrom, isIndependentUpdate, autoKill = true;
        public bool isActive = true, isValid, autoPlay = true, useTargetAsV3;
        public Component target;
        public DOTweenAnimationType animationType;
        public TargetType targetType, forcedTargetType;
        public float endValueFloat;
        public Vector3 endValueV3;
        public Vector2 endValueV2;
        public Color endValueColor = Color.white;
        public string endValueString = "";
        public Rect endValueRect;
        public Transform endValueTransform;
        public bool optionalBool0;
        public float optionalFloat0;
        public int optionalInt0;
        public RotateMode optionalRotationMode;
        public ScrambleMode optionalScrambleMode;
        public string optionalString;
        private int nextIndex;

        private void Awake() { if (isActive && isValid) CreateTween(); }
        private void OnDestroy() { tween?.Kill(); tween = null; }

        public void CreateTween()
        {
            if (!isActive || !isValid || target == null) return;
            tween?.Kill();
            var t = target.transform;
            var v = useTargetAsV3 && endValueTransform ? endValueTransform.position : endValueV3;
            switch (animationType)
            {
                case DOTweenAnimationType.Move:
                    tween = target is RectTransform rect ? rect.DOAnchorPos3D(v, duration, optionalBool0) : t.DOMove(v, duration, optionalBool0);
                    break;
                case DOTweenAnimationType.LocalMove: tween = t.DOLocalMove(v, duration, optionalBool0); break;
                case DOTweenAnimationType.Rotate: tween = t.DORotate(v, duration, optionalRotationMode); break;
                case DOTweenAnimationType.LocalRotate: tween = t.DOLocalRotate(v, duration, optionalRotationMode); break;
                case DOTweenAnimationType.Scale: tween = t.DOScale(optionalBool0 ? Vector3.one * endValueFloat : v, duration); break;
                case DOTweenAnimationType.UIWidthHeight: tween = ((RectTransform)target).DOSizeDelta(optionalBool0 ? Vector2.one * endValueFloat : endValueV2, duration); break;
                case DOTweenAnimationType.Color:
                    if (target is Graphic graphic) tween = graphic.DOColor(endValueColor, duration);
                    else if (target is SpriteRenderer sprite) tween = sprite.DOColor(endValueColor, duration);
                    else if (target is Light light) tween = light.DOColor(endValueColor, duration);
                    else if (target is Renderer renderer) tween = renderer.material.DOColor(endValueColor, duration);
                    break;
                case DOTweenAnimationType.Fade:
                    if (target is CanvasGroup group) tween = group.DOFade(endValueFloat, duration);
                    else if (target is Graphic fadeGraphic) tween = fadeGraphic.DOFade(endValueFloat, duration);
                    else if (target is SpriteRenderer fadeSprite) tween = fadeSprite.DOFade(endValueFloat, duration);
                    else if (target is Renderer fadeRenderer) tween = fadeRenderer.material.DOFade(endValueFloat, duration);
                    break;
                case DOTweenAnimationType.Text: tween = ((Text)target).DOText(endValueString, duration, optionalBool0, optionalScrambleMode, optionalString); break;
                case DOTweenAnimationType.PunchPosition: tween = t.DOPunchPosition(v, duration, optionalInt0, optionalFloat0, optionalBool0); break;
                case DOTweenAnimationType.PunchRotation: tween = t.DOPunchRotation(v, duration, optionalInt0, optionalFloat0); break;
                case DOTweenAnimationType.PunchScale: tween = t.DOPunchScale(v, duration, optionalInt0, optionalFloat0); break;
                case DOTweenAnimationType.ShakePosition: tween = t.DOShakePosition(duration, v, optionalInt0, optionalFloat0, optionalBool0); break;
                case DOTweenAnimationType.ShakeRotation: tween = t.DOShakeRotation(duration, v, optionalInt0, optionalFloat0); break;
                case DOTweenAnimationType.ShakeScale: tween = t.DOShakeScale(duration, v, optionalInt0, optionalFloat0); break;
                case DOTweenAnimationType.CameraAspect: tween = ((Camera)target).DOAspect(endValueFloat, duration); break;
                case DOTweenAnimationType.CameraBackgroundColor: tween = ((Camera)target).DOColor(endValueColor, duration); break;
                case DOTweenAnimationType.CameraFieldOfView: tween = ((Camera)target).DOFieldOfView(endValueFloat, duration); break;
                case DOTweenAnimationType.CameraOrthoSize: tween = ((Camera)target).DOOrthoSize(endValueFloat, duration); break;
                case DOTweenAnimationType.CameraPixelRect: tween = ((Camera)target).DOPixelRect(endValueRect, duration); break;
                case DOTweenAnimationType.CameraRect: tween = ((Camera)target).DORect(endValueRect, duration); break;
            }
            if (tween == null) return;
            if (isFrom) ((Tweener)tween).From(isRelative);
            else tween.SetRelative(isRelative);
            tween.SetTarget(gameObject).SetId(id).SetDelay(delay).SetLoops(loops, loopType).SetAutoKill(autoKill).SetUpdate(updateType, isIndependentUpdate).SetSpeedBased(isSpeedBased);
            if (easeType == Ease.INTERNAL_Custom) tween.SetEase(easeCurve);
            else tween.SetEase(easeType);
            if (hasOnStart) tween.OnStart(() => onStart.Invoke());
            if (hasOnPlay) tween.OnPlay(() => onPlay.Invoke());
            if (hasOnUpdate) tween.OnUpdate(() => onUpdate.Invoke());
            if (hasOnStepComplete) tween.OnStepComplete(() => onStepComplete.Invoke());
            if (hasOnComplete) tween.OnComplete(() => onComplete.Invoke());
            if (autoPlay) tween.Play(); else tween.Pause();
            if (hasOnTweenCreated) onTweenCreated.Invoke();
        }

        public override void DOPlay() => DOTween.Play(gameObject);
        public override void DOPlayBackwards() => DOTween.PlayBackwards(gameObject);
        public override void DOPlayForward() => DOTween.PlayForward(gameObject);
        public override void DOPause() => DOTween.Pause(gameObject);
        public override void DOTogglePause() => DOTween.TogglePause(gameObject);
        public override void DORewind() { nextIndex = 0; DOTween.Rewind(gameObject); }
        public override void DORestart(bool fromHere = false) => DOTween.Restart(gameObject);
        public override void DOComplete() => DOTween.Complete(gameObject);
        public override void DOKill() { DOTween.Kill(gameObject); tween = null; }
        private void ForId(string value, Action<Tween> action)
        {
            foreach (var animation in GetComponents<DOTweenAnimation>())
                if (animation.id == value && animation.tween != null) action(animation.tween);
        }
        public void DOPlayById(string value) => ForId(value, t => t.Play());
        public void DOPlayAllById(string value) => DOTween.Play(value);
        public void DOPauseAllById(string value) => DOTween.Pause(value);
        public void DOPlayBackwardsById(string value) => ForId(value, t => t.PlayBackwards());
        public void DOPlayBackwardsAllById(string value) => DOTween.PlayBackwards(value);
        public void DOPlayForwardById(string value) => ForId(value, t => t.PlayForward());
        public void DOPlayForwardAllById(string value) => DOTween.PlayForward(value);
        public void DOPlayNext()
        {
            var animations = GetComponents<DOTweenAnimation>();
            while (nextIndex < animations.Length)
            {
                var animation = animations[nextIndex++];
                if (animation.tween != null && !animation.tween.IsComplete()) { animation.tween.Play(); break; }
            }
        }
        public void DORewindAndPlayNext() { DORewind(); DOPlayNext(); }
        public void DORestartById(string value) => ForId(value, t => t.Restart());
        public void DORestartAllById(string value) => DOTween.Restart(value);
        public List<Tween> GetTweens()
        {
            var result = new List<Tween>();
            foreach (var animation in GetComponents<DOTweenAnimation>()) if (animation.tween != null) result.Add(animation.tween);
            return result;
        }
        public static TargetType TypeToDOTargetType(Type type)
        {
            if (typeof(RectTransform).IsAssignableFrom(type)) return TargetType.RectTransform;
            if (typeof(CanvasGroup).IsAssignableFrom(type)) return TargetType.CanvasGroup;
            if (typeof(Image).IsAssignableFrom(type)) return TargetType.Image;
            if (typeof(Text).IsAssignableFrom(type)) return TargetType.Text;
            if (typeof(Camera).IsAssignableFrom(type)) return TargetType.Camera;
            if (typeof(SpriteRenderer).IsAssignableFrom(type)) return TargetType.SpriteRenderer;
            if (typeof(Renderer).IsAssignableFrom(type)) return TargetType.Renderer;
            if (typeof(Light).IsAssignableFrom(type)) return TargetType.Light;
            return TargetType.Transform;
        }
    }
}
