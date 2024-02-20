#if !AUTOBUILD
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TouchLibrary.Core;

namespace TouchLibrary
{
    public class TouchAnalyzer : IDisposable
    {
        private TouchListener touchListener;

        public TouchAnalyzer(TouchListener touchListener)
        {
            this.touchListener = touchListener ?? throw new ArgumentNullException(nameof(touchListener));
            touchListener.Touch += TouchListener_Touch;
        }
        
        public event PanEventHandler Pan;

        private PanCondition panCondition = null;
        private void TouchListener_Touch(TouchListener sender, TouchEventArgs e)
        {
            if (e.Handled) return;

            if (Pan != null)
            {
                var inputs = e.Inputs;
                var length = inputs.Length;
                var panEventArgs = null as PanEventArgs;

                if (length == 1)
                {
                    var input = inputs[0];
                    var location = input.Location;
                    if (panCondition == null)
                    {
                        if (input.Down)
                        {
                            panEventArgs = new PanEventArgs(e, TouchGestureCondition.Begin, location, location, location);
                            panCondition = new PanCondition() { ID = input.ID, Start = location, Last = location };
                        }
                    }
                    else if (panCondition.ID == input.ID)
                    {
                        if (input.Move)
                        {
                            panEventArgs = new PanEventArgs(e, TouchGestureCondition.Ongoing, panCondition.Start, panCondition.Last, location);
                            panCondition.Last = location;
                        }
                        else if (input.Up)
                        {
                            panEventArgs = new PanEventArgs(e, TouchGestureCondition.Complete, panCondition.Start, panCondition.Last, location);
                            panCondition = null;
                        }
                    }
                }

                if (panEventArgs == null && panCondition != null)
                {
                    var location = panCondition.Last;
                    panEventArgs = new PanEventArgs(e, TouchGestureCondition.Cancel, panCondition.Start, location, location);
                    panCondition = null;
                }
                
                if (panEventArgs != null)
                {
                    Pan(this, panEventArgs);
                    if (panEventArgs.Handled) { e.Handled = true; return; }
                }
            }
        }
        
        public void Dispose()
        {
            if (touchListener != null)
            {
                touchListener.Touch -= TouchListener_Touch;
                touchListener = null;
            }
        }

        private class PanCondition
        {
            public uint ID;
            public PointD Start;
            public PointD Last;
        }
    }

    public enum TouchGestureCondition { Begin, Ongoing, Cancel, Complete }

    public class TouchGestureEventArgs : TouchEventArgs
    {
        public bool Stop = false;
        public readonly TouchGestureCondition Condition;
        public TouchGestureEventArgs(TouchEventArgs e, TouchGestureCondition condition) : base(e.Handled, e.Inputs)
        {
            Condition = condition;
        }
    }

    public class PanEventArgs : TouchGestureEventArgs
    {
        public readonly PointD StartScreenLocation;
        public readonly PointD PreviousScreenLocation;
        public readonly PointD ScreenLocation;
        public PanEventArgs(TouchEventArgs e, TouchGestureCondition condition, PointD startScreenLocation, PointD prevScreenLocation, PointD screenLocation) : base(e, condition)
        {
            StartScreenLocation = startScreenLocation;
            PreviousScreenLocation = prevScreenLocation;
            ScreenLocation = screenLocation;
        }

        public double TotalVerticalMotion { get { return ScreenLocation.Y - StartScreenLocation.Y; } }
        public double PreviousTotalVerticalMotion { get { return PreviousScreenLocation.Y - StartScreenLocation.Y; } }
    }
    public delegate void PanEventHandler(TouchAnalyzer sender, PanEventArgs e);
}
#endif
