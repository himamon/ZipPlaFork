using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZipPla
{
    public class GraphicalListDragEventArgs : DragEventArgs
    {
        /*
        private DragDropEffects allowedEffect;
        public DragDropEffects AllowedEffect { get { return allowedEffect; } }
        private DragDropEffects effect;
        public DragDropEffects Effect { get { return effect; } set { effect = value; } }
        private int keyState;
        public int KeyState { get { return base.KeyState; } }
        */
        public Keys GetKeyState()
        {
            return (Keys)KeyState;
        }
        //int x, y;

        private int index;
        public int Index { get { return index; } }

        /*
        private Action finallyAction;
        public Action FinallyAction { get { return finallyAction; } }
        */
        public GraphicalListDragEventArgs(DragEventArgs e, int index = -1/*, Action finallyAction = null*/) : base(e.Data, e.KeyState, e.X, e.Y, e.AllowedEffect, e.Effect)
        {
            this.index = index;
            //this.finallyAction = finallyAction;
        }

        /*
        private IDataObject data;
        public IDataObject Data { get { return data; } }

        public DragEventArgs GetDragEventArgs()
        {
            return new DragEventArgs(data, keyState, x, y, allowedEffect, effect);
        }
        */

        public void SetTo(DragEventArgs e)
        {
            e.Effect = Effect;
        }

        /*
        public void Update(DragEventArgs e)
        {
            data = e.Data;
            keyState = e.KeyState;
            x = e.X;
            y = e.Y;
            allowedEffect = e.AllowedEffect;
            effect = e.Effect;
        }
        */
    }
    public delegate void GraphicalListDragEventHandler(IGraphicalList sender, GraphicalListDragEventArgs e);
    public interface IGraphicalList
    {
        event GraphicalListDragEventHandler GraphicalListDragEnter;
        event GraphicalListDragEventHandler GraphicalListDragOver;
        event EventHandler GraphicalListDragLeave;
        event GraphicalListDragEventHandler GraphicalListDragDrop;

        void DrawHighlight(int index);
        void Invalidate(int index);
    }
}
