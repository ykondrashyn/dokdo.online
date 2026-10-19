using System;

namespace Dokdo.Web
{
    public sealed class BrowserLifecycleController
    {
        [Flags]
        private enum Blocker
        {
            None = 0,
            Visibility = 1,
            Orientation = 2
        }

        private Blocker blockers;

        public bool IsHidden
        {
            get { return IsVisibilityBlocked; }
        }

        public bool IsVisibilityBlocked
        {
            get { return (blockers & Blocker.Visibility) != 0; }
        }

        public bool IsOrientationBlocked
        {
            get { return (blockers & Blocker.Orientation) != 0; }
        }

        public bool IsBlocked
        {
            get { return blockers != Blocker.None; }
        }

        public int BlockTransitionCount { get; private set; }

        public int ResumeTransitionCount { get; private set; }

        public void Hide(Action save, Action pause)
        {
            SetBlocker(Blocker.Visibility, true, save, pause, null, null);
        }

        public void Show(Action resume, Action recalculateElapsedTime)
        {
            SetBlocker(Blocker.Visibility, false, null, null, resume,
                recalculateElapsedTime);
        }

        public void SetVisibilityBlocked(bool blocked, Action save,
            Action pause, Action resume, Action recalculateElapsedTime)
        {
            SetBlocker(Blocker.Visibility, blocked, save, pause, resume,
                recalculateElapsedTime);
        }

        public void SetOrientationBlocked(bool blocked, Action save,
            Action pause, Action resume, Action recalculateElapsedTime)
        {
            SetBlocker(Blocker.Orientation, blocked, save, pause, resume,
                recalculateElapsedTime);
        }

        private void SetBlocker(Blocker blocker, bool blocked, Action save,
            Action pause, Action resume, Action recalculateElapsedTime)
        {
            var wasSet = (blockers & blocker) != 0;
            if (wasSet == blocked)
            {
                return;
            }

            var wasBlocked = IsBlocked;
            blockers = blocked ? blockers | blocker : blockers & ~blocker;
            if (!wasBlocked && IsBlocked)
            {
                BlockTransitionCount++;
                save?.Invoke();
                pause?.Invoke();
            }
            else if (wasBlocked && !IsBlocked)
            {
                ResumeTransitionCount++;
                resume?.Invoke();
                recalculateElapsedTime?.Invoke();
            }
        }
    }
}
