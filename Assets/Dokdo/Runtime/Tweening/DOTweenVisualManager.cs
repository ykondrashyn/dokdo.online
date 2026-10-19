using DG.Tweening.Core;
using UnityEngine;

namespace DG.Tweening
{
    [AddComponentMenu("DOTween/DOKDO Visual Manager Compatibility")]
    public sealed class DOTweenVisualManager : MonoBehaviour
    {
        public VisualManagerPreset preset;
        public OnEnableBehaviour onEnableBehaviour;
        public OnDisableBehaviour onDisableBehaviour;

        private void OnEnable()
        {
            switch (onEnableBehaviour)
            {
                case OnEnableBehaviour.Play:
                    DOTween.Play(gameObject);
                    break;
                case OnEnableBehaviour.Restart:
                    DOTween.Restart(gameObject);
                    break;
                case OnEnableBehaviour.RestartFromSpawnPoint:
                    RestartFromSpawnPoint();
                    break;
            }
        }

        private void OnDisable()
        {
            switch (onDisableBehaviour)
            {
                case OnDisableBehaviour.Pause:
                    DOTween.Pause(gameObject);
                    break;
                case OnDisableBehaviour.Rewind:
                    DOTween.Rewind(gameObject);
                    break;
                case OnDisableBehaviour.Kill:
                    DOTween.Kill(gameObject);
                    break;
                case OnDisableBehaviour.KillAndComplete:
                    DOTween.Kill(gameObject, true);
                    break;
                case OnDisableBehaviour.DestroyGameObject:
                    Destroy(gameObject);
                    break;
            }
        }

        private void RestartFromSpawnPoint()
        {
            foreach (var animation in GetComponents<DOTweenAnimation>())
            {
                animation.DORestart(true);
            }

            foreach (var path in GetComponents<DOTweenPath>())
            {
                path.DORestart(true);
            }
        }
    }
}
