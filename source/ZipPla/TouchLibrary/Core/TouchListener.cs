#if !AUTOBUILD
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TouchLibrary.Core
{
    public abstract class TouchListener : IDisposable
    {
        public abstract void Dispose();
        public abstract event TouchEventHandler Touch;
    }

    public class TouchInput
    {
        public readonly PointD Location;
        public readonly IntPtr Handle;
        public readonly uint ID;
        public readonly bool Move;
        public readonly bool Down;
        public readonly bool Up;
        public readonly bool InRange;
        public readonly bool Primary;
        public readonly bool NoCoalesce;
        public readonly bool Palm;
        public readonly bool TimeFromSystem;
        public readonly TimeSpan Time;
        public readonly UIntPtr ExtraInfo;
        public readonly SizeD? Contact;

        public TouchInput(
            PointD location,
            IntPtr handle,
            uint id,
            bool move,
            bool down,
            bool up,
            bool inRange,
            bool primary,
            bool noCoalesce,
            bool palm,
            bool timeFromSystem,
            TimeSpan time,
            UIntPtr extraInfo,
            SizeD? contact)
        {
            Location = location;
            Handle = handle;
            ID = id;
            Move = move;
            Down = down;
            Up = up;
            InRange = inRange;
            Primary = primary;
            NoCoalesce = noCoalesce;
            Palm = palm;
            TimeFromSystem = timeFromSystem;
            Time = time;
            ExtraInfo = extraInfo;
            Contact = contact;
        }
    }

    public class TouchEventArgs : EventArgs
    {
        public bool Handled;
        public readonly TouchInput[] Inputs;
        public TouchEventArgs(bool handled, IEnumerable<TouchInput> inputs)
        {
            Inputs = inputs as TouchInput[] ?? inputs?.ToArray() ?? throw new ArgumentNullException(nameof(inputs));
        }
    }
    public delegate void TouchEventHandler(TouchListener sender, TouchEventArgs e);
}
#endif
