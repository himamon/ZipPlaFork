using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZipPla
{
    public static class LockManager
    {
#if DEBUG
        public static void Test()
        {
            var obj = new object();

            lock(obj)
            {
                lock(obj)
                {
                }
            }
            System.Windows.Forms.MessageBox.Show("test1 ok");

            var token1 = false;
            var token2 = false;
            Monitor.Enter(obj, ref token1);
            Monitor.Enter(obj, ref token2);
            if (token2) Monitor.Exit(obj);
            if (token1) Monitor.Exit(obj);

            System.Windows.Forms.MessageBox.Show($"test2 ok token1 = {token1}, token2 = {token2}");

            lock (obj)
            {
                token2 = false;
                Monitor.Enter(obj, ref token2);
                if (token2) Monitor.Exit(obj);
            }
            System.Windows.Forms.MessageBox.Show($"test3 ok  token2 = {token2}");

            token1 = false;
            Monitor.Enter(obj, ref token1);
            lock(obj)
            {

            }
            if (token1) Monitor.Exit(obj);
            System.Windows.Forms.MessageBox.Show($"test4 ok  token1 = {token1}");

        }
#endif

        public static void ForIReadOnlyList<T>(IReadOnlyList<T> list, int index, Action<T> body)
        {
            var listLockWasTaken = false;
            var itemLockWasTaken = false;
            var tempList = list;
            T item;
            try
            {
                Monitor.Enter(tempList, ref listLockWasTaken);
                {
                    item = list[index];
                    if (item != null)
                    {
                        try
                        {
                            Monitor.Enter(item, ref itemLockWasTaken);
                        }
                        catch
                        {
                            if (itemLockWasTaken)
                            {
                                Monitor.Exit(item);
                            }
                            throw;
                        }
                    }
                }
            }
            finally
            {
                if (listLockWasTaken)
                {
                    Monitor.Exit(tempList);
                }
            }
            if (item != null)
            {
                var tempItem = item;
                try
                {
                    body(item);
                }
                finally
                {
                    if (itemLockWasTaken)
                    {
                        Monitor.Exit(tempItem);
                    }
                }
            }
            else
            {
                body(item);
            }
        }
    }
}
