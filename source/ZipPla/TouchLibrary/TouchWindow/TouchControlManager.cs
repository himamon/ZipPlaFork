#if !AUTOBUILD
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TouchLibrary.TouchWindow
{
    public class TouchControlManager : TouchWindowManager
    {
        private Control target;

        public TouchControlManager(Control target, TWF ulFlags) : base(ulFlags)
        {
            this.target = target;
        }

        protected override void AddHandleCreated(EventHandler handleCreated)
        {
            target.HandleCreated += handleCreated;
        }

        protected override void AddHandleDestroyed(EventHandler handleDestroyed)
        {
            target.HandleDestroyed += handleDestroyed;
        }

        protected override IntPtr GetHandle()
        {
            return target.Handle;
        }

        protected override bool IsHandleCreated()
        {
            return target.IsHandleCreated;
        }

        protected override void RemoveHandleCreated(EventHandler handleCreated)
        {
            target.HandleCreated -= handleCreated;
        }

        protected override void RemoveHandleDestroyed(EventHandler handleDestroyed)
        {
            target.HandleDestroyed -= handleDestroyed;
        }
    }

}
#endif
