#if !AUTOBUILD
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TouchLibrary.Core;

namespace TouchLibrary.TouchWindow
{
    public abstract class WindowTouchListener : TouchListener
    {
        private TouchWindowManager touchWindowManager;

        protected WindowTouchListener(TouchWindowManager touchWindowManager)
        {
            this.touchWindowManager = touchWindowManager;
        }

        private void TouchWindowManager_Touch(TouchWindowManager sender, TouchWindowEventArgs e)
        {
            if (_Touch != null)
            {
                var touchEventArgs = new TouchEventArgs(e.Handled, from input in e.Inputs select GetTouchInput(input));
                _Touch(this, touchEventArgs);
                e.Handled = touchEventArgs.Handled;
            }
        }

        private TouchInput GetTouchInput(TOUCHINPUT input)
        {
            var flags = input.dwFlags;
            var mask = input.dwMask;
            return new TouchInput(
                 location: new PointD(input.x / 100.0, input.y / 100.0),
                 handle: input.hSource,
                 id: input.dwID,
                 move: (flags & TOUCHEVENTF.MOVE) == TOUCHEVENTF.MOVE,
                 down: (flags & TOUCHEVENTF.DOWN) == TOUCHEVENTF.DOWN,
                 up: (flags & TOUCHEVENTF.UP) == TOUCHEVENTF.UP,
                 inRange: (flags & TOUCHEVENTF.INRANGE) == TOUCHEVENTF.INRANGE,
                 primary: (flags & TOUCHEVENTF.PRIMARY) == TOUCHEVENTF.PRIMARY,
                 noCoalesce: (flags & TOUCHEVENTF.NOCOALESCE) == TOUCHEVENTF.NOCOALESCE,
                 palm: (flags & TOUCHEVENTF.PALM) == TOUCHEVENTF.PALM,
                 timeFromSystem: (mask & TOUCHINPUTMASKF.TIMEFROMSYSTEM) == TOUCHINPUTMASKF.TIMEFROMSYSTEM,
                 time: TimeSpan.FromMilliseconds(input.dwTime),
                 extraInfo: (mask & TOUCHINPUTMASKF.EXTRAINFO) == TOUCHINPUTMASKF.EXTRAINFO ? input.dwExtraInfo : UIntPtr.Zero,
                 contact: (mask & TOUCHINPUTMASKF.CONTACTAREA) == TOUCHINPUTMASKF.CONTACTAREA ?
                    new SizeD(input.cxContact / 100.0, input.cyContact / 100.0) : null as SizeD?
                );
        }

        public override void Dispose()
        {
            if (touchWindowManager != null)
            {
                touchWindowManager.Touch -= TouchWindowManager_Touch;
                touchWindowManager.Dispose();
                touchWindowManager = null;
            }
        }

        private event TouchEventHandler _Touch;
        public override event TouchEventHandler Touch
        {
            add
            {
                if (_Touch == null && value != null && touchWindowManager != null) touchWindowManager.Touch += TouchWindowManager_Touch;
                _Touch += value;
            }
            remove
            {
                _Touch -= value;
                if (_Touch == null && touchWindowManager != null) touchWindowManager.Touch -= TouchWindowManager_Touch;
            }
        }
    }
}
#endif
