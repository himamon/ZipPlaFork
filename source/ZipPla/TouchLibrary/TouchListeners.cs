#if !AUTOBUILD
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TouchLibrary.TouchWindow;

namespace TouchLibrary
{
    public class ControlTouchListener : WindowTouchListener
    {
        public ControlTouchListener(Control target, TWF ulFlags = TWF.None) : base(new TouchControlManager(target, ulFlags)) { }
    }
}
#endif
