using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GenerarClasses
{
    /// <summary>
    /// マルチスレッドによる並列処理機能を提供します。
    /// 全てのメソッド呼び出し・プロパティ書き込みは同一のスレッドから行われなければなりません。
    /// </summary>
    class BackgroundMultiWorker : Component
    {
        public class EachDoWorkEventArgs
        {
            public object Argument { get; private set; }
            public object Result { get;  set; }
            public bool Cancel { get; set; }
            public int WorkNumber { get; private set; }
            public int CompletedWorksCount { get; private set; }
            public readonly Guid WorkSetGuid;
            public EachDoWorkEventArgs(DoWorkEventArgs e, int workNumber, int completedWorksCount, Guid workSetGuid)
            {
                Argument = e.Argument;
                Result = e.Result;
                Cancel = e.Cancel;
                WorkNumber = workNumber;
                CompletedWorksCount = completedWorksCount;
                WorkSetGuid = workSetGuid;
            }
            public EachDoWorkEventArgs(object argument, int workNumber, int completedWorksCount, Guid workSetGuid)
            {
                Argument = argument;
                WorkNumber = workNumber;
                CompletedWorksCount = completedWorksCount;
                WorkSetGuid = workSetGuid;
            }
        }
        public class AllRunWorkerCompletedEventArgs
        {
            public IReadOnlyList<object> Results { get; private set; }
            public Exception Error { get; private set; }
            public bool Cancelled { get; private set; }
            public AllRunWorkerCompletedEventArgs(IReadOnlyList<object> results, Exception error, bool cancelled)
            {
                Results = results;
                Error = error;
                Cancelled = cancelled;
            }
        }
        public class EachRunWorkerCompletedEventArgs
        {
            public object Result { get; private set; }
            public Exception Error { get; private set; }
            public bool Cancelled { get; private set; }
            public int WorkNumber { get; private set; }
            public int CompletedWorksCount { get; private set; }
            public readonly Guid WorkSetGuid;
            public EachRunWorkerCompletedEventArgs(object result, Exception error, bool cancelled, int workNumber, int completedWorksCount, Guid workSetGuid)
            {
                Result = result;
                Error = error;
                Cancelled = cancelled;
                WorkNumber = workNumber;
                CompletedWorksCount = completedWorksCount;
                WorkSetGuid = workSetGuid;
            }
            public EachRunWorkerCompletedEventArgs(RunWorkerCompletedEventArgs e, int workNumber, int completedWorksCount, Guid workSetGuid)
            {
                Result = e.Result;
                Error = e.Error;
                Cancelled = e.Cancelled;
                WorkNumber = workNumber;
                CompletedWorksCount = completedWorksCount;
                WorkSetGuid = workSetGuid;
            }
        }
        public class RunWorkerStartingEventArgs
        {
            public RunWorkerStartingEventArgs(IEnumerable arguments)
            {
                Arguments = arguments;
                WorkSetGuid = new Guid();
            }
            public readonly IEnumerable Arguments;
            public readonly Guid WorkSetGuid;
        }
        public delegate void RunWorkerStartingEventHandler(object sender, RunWorkerStartingEventArgs e);
        public delegate void EachDoWorkEventHandler(object sender, EachDoWorkEventArgs e);
        public delegate void AllRunWorkerCompletedEventHandler(object sender, AllRunWorkerCompletedEventArgs e);
        public delegate void EachRunWorkerCompletedEventHandler(object sender, EachRunWorkerCompletedEventArgs e);
        
        private DoWorkEventHandler GetDoWorkEventHandler(EachDoWorkEventHandler DoWork)
        {
            return ((sender, e) => {
                var tpl = (Tuple<object, int, int, Guid>)e.Argument;
                var e2 = new EachDoWorkEventArgs(tpl.Item1, tpl.Item2, tpl.Item3, tpl.Item4);
                try
                {
                    DoWork(sender, e2);
                }
                catch (ObjectDisposedException) { }
                //catch (ObjectDisposedException) { }
                //catch (System.Reflection.TargetInvocationException) { }
                e.Result = e2.Result;
                if(e2.Cancel)
                {
                    e.Cancel = true;
                }
            });
        }

        public int ThreadCount
        {
            get
            {
                return InternalThreadCount;
            }
            set
            {
                if (value >= 0)
                {
                    InternalThreadCount = value;
                    if (!CancellationPending && IsBusy && InternalThreadCount > CurrentThreadCount)
                    {
                        if (WorksOrderChanged) NextWork = 0;
                        WorksOrderChanged = false;
                        while (NextWork < WorksOrder.Length && WorksStarted[WorksOrder[NextWork]]) NextWork++;

                        for (var newThread = CurrentThreadCount; newThread < InternalThreadCount; newThread++)
                        {
                            while (NextWork < WorksOrder.Length && WorksStarted[WorksOrder[NextWork]]) NextWork++;
                            if (NextWork == WorksOrder.Length) break;

                            BackgroundWorker nextWorker;

                            var WorkNumber = WorksOrder[NextWork++];

                            nextWorker = new BackgroundWorker();
                            AllWorkerCount++;
                            nextWorker.WorkerSupportsCancellation = InternalWorkerSupportsCancellation;
                            nextWorker.DoWork += GetDoWorkEventHandlerDoWork;
                            nextWorker.RunWorkerCompleted += privateRunWorkerCompletedEventHandler;
                            CurrentThreadCount++;

                            Workers.Add(nextWorker);

                            WorkersWorks.Add(WorkNumber);
                            WorksStarted[WorkNumber] = true;

                            nextWorker.RunWorkerAsync(Tuple.Create(Works[WorkNumber], WorkNumber, CompletedWorksCount, WorkSetGuid));
                        }
                    }
                }
                else
                {
                    throw new ArgumentOutOfRangeException("ThreadCount", value, null);
                }
            }
        }
        private int InternalThreadCount = 1;

        public bool WorkerSupportsCancellation
        {
            get
            {
                return InternalWorkerSupportsCancellation;
            }
            set
            {
                InternalWorkerSupportsCancellation = value;
                if (Workers != null)
                {
                    foreach (var worker in Workers)
                    {
                        if (worker != null)
                        {
                            worker.WorkerSupportsCancellation = InternalWorkerSupportsCancellation;
                        }
                    }
                }
            }
        }
        private bool InternalWorkerSupportsCancellation = false;

        public enum WorkState { Waiting, InProgress, Finished, NotExists };
        public WorkState GetWorkState(int workNumber)
        {
            if(WorksStarted == null || workNumber < 0 || WorksStarted.Length <= workNumber)
            {
                return WorkState.NotExists;
            }
            else if(!WorksStarted[workNumber])
            {
                return WorkState.Waiting;
            }
            else if(ResultArgs[workNumber] == null)
            {
                return WorkState.InProgress;
            }
            else
            {
                return WorkState.Finished;
            }
        }

        public bool SelfClosing { get; set; } = true;

        public event EachDoWorkEventHandler DoWork;
        private event DoWorkEventHandler GetDoWorkEventHandlerDoWork;

        public event RunWorkerStartingEventHandler RunWorkerStarting;

        /// <summary>
        /// 全ての処理が終了した後に一度だけ発生します。ただしキャンセルの完了を待たない割り込みを行った場合は発生しません。
        /// </summary>
        public event AllRunWorkerCompletedEventHandler AllRunWorkersCompleted;

        /// <summary>
        /// 各処理が完了後に RunWorkerAsync の引数の IEnumerable の順に発生します。
        /// </summary>
        public event EachRunWorkerCompletedEventHandler EachRunWorkerCompletedSequential;

        /// <summary>
        /// 各処理が終了する毎に、終了した順に発生します。開始された処理について一度ずつ発生します。
        /// </summary>
        public event EachRunWorkerCompletedEventHandler EachRunWorkerCompleted;

        /// <summary>
        /// クラスの管理から外れてから終了した Worker に対して発生します。WorkerNumber は -1 になります
        /// </summary>
        public event EachRunWorkerCompletedEventHandler InconclusiveWorkerDisposing;

        public bool CancellationPending { get; private set; }
        
        public BackgroundMultiWorker() { }
        
        /// <summary>
        /// 処理の順番を指定します。処理中でも変更できます。
        /// </summary>
        public void SetWorksOrder(int[] worksOrder)
        {
            WorksOrderChanged = true;
            WorksOrder = worksOrder;
#if DEBUG
            var check = new List<int>(worksOrder);
            check.Sort();
            //if (check.Count > 0 && (check[0] < 0 || Works != null && check[check.Count - 1] >= Works.Length)) throw new Exception($"WorksOrder is not permutation. (0->{check[0]}, {check.Count - 1}->{Works?.Length})");
            if (check.Count > 0 && check[0] < 0 ) throw new Exception($"WorksOrder is not permutation. (0->{check[0]})");
            for (var i = 1; i < check.Count; i++)
            {
                if (check[i] == check[i - 1])
                {
                    throw new Exception($"WorksOrder is not permutation. ({i})");
                }
            }
#endif
        }

        private void privateRunWorkerCompletedEventHandler(object sender, RunWorkerCompletedEventArgs e)
        {
            var localWorker = (BackgroundWorker)sender;
            int WorkerNumber;
            if (Workers == null || (WorkerNumber = Workers.IndexOf(localWorker)) < 0)
            {
                if (InconclusiveWorkerDisposing != null)
                {
                    var ee2 = new EachRunWorkerCompletedEventArgs(e.Result, e.Error, false, -1, CompletedWorksCount, Guid.Empty);
                    InconclusiveWorkerDisposing(sender, ee2);
                }
                localWorker.Dispose();
                AllWorkerCount--;
                ExitQueAccepter();
                return;
            }
            var workSetGuid = WorkSetGuid;

            var WorkNumber = WorkersWorks[WorkerNumber];
            //var e2 = new EachRunWorkerCompletedEventArgs(e, WorkNumber, ++CompletedWorksCount);
            var e2 = new EachRunWorkerCompletedEventArgs(e.Result, e.Error, e.Cancelled || CancellationPending, WorkNumber, ++CompletedWorksCount, workSetGuid); // Cancelled は強制的に設定する
            if (EachRunWorkerCompleted != null) EachRunWorkerCompleted(sender, e2);
            ResultArgs[WorkNumber] = e2;
            while (NextResult < ResultArgs.Length && ResultArgs[NextResult] != null)
            {
                if (EachRunWorkerCompletedSequential != null)
                {
                    EachRunWorkerCompletedSequential(sender, ResultArgs[NextResult]);
                }
                NextResult++;
            }

            if (WorksOrderChanged) NextWork = 0;
            WorksOrderChanged = false;
            while (NextWork < WorksOrder.Length && WorksStarted[WorksOrder[NextWork]]) NextWork++;

            if (e.Error == null && !CancellationPending && NextWork < Works.Length)
            {
                if (InternalThreadCount >= CurrentThreadCount)
                {
                    var CurrentThreadCount0 = CurrentThreadCount;
                    for (var newThread = CurrentThreadCount0 - 1; newThread < InternalThreadCount; newThread++)
                    {
                        while (NextWork < WorksOrder.Length && WorksStarted[WorksOrder[NextWork]]) NextWork++;
                        if (NextWork == WorksOrder.Length) break;


                        BackgroundWorker nextWorker;
                        int nextWorkerNumber;
                        if (newThread < CurrentThreadCount0)
                        {
                            nextWorker = localWorker;
                            nextWorkerNumber = WorkerNumber;
                        }
                        else
                        {
                            nextWorker = new BackgroundWorker();
                            AllWorkerCount++;
                            nextWorker.WorkerSupportsCancellation = InternalWorkerSupportsCancellation;
                            nextWorker.RunWorkerCompleted += privateRunWorkerCompletedEventHandler;
                            nextWorker.DoWork += GetDoWorkEventHandlerDoWork;
                            nextWorkerNumber = newThread;
                            Workers.Add(null);
                            CurrentThreadCount++;
                            WorkersWorks.Add(0);
                        }


                        WorkNumber = WorksOrder[NextWork++];
                        Workers[nextWorkerNumber] = nextWorker;
                        WorkersWorks[nextWorkerNumber] = WorkNumber;
                        WorksStarted[WorkNumber] = true;
                        nextWorker.RunWorkerAsync(Tuple.Create(Works[WorkNumber], WorkNumber, CompletedWorksCount, workSetGuid));
                    }
                   
                }
                else
                {
                    localWorker.Dispose();
                    AllWorkerCount--;
                    ExitQueAccepter();
                    Workers.RemoveAt(WorkerNumber);
                    WorkersWorks.RemoveAt(WorkerNumber);
                    CurrentThreadCount--;
                }
            }
            else
            {
                localWorker.Dispose();
                AllWorkerCount--;
                ExitQueAccepter();
                Workers.RemoveAt(WorkerNumber);
                WorkersWorks.RemoveAt(WorkerNumber);
                CurrentThreadCount--;
                if ((SelfClosing || CancellationPending || e.Error != null) && Workers.Count <= 0)
                {
                    if (AllRunWorkersCompleted != null)
                    {
                        var Results = new object[ResultArgs.Length]; for (var i = 0; i < ResultArgs.Length; i++) Results[i] = ResultArgs[i] != null ? ResultArgs[i].Result : null;
                        var es = new AllRunWorkerCompletedEventArgs(Results, e.Error, CancellationPending);
                        AllRunWorkersCompleted(sender, es);
                    }
                    Workers = null;
                    WorkSetGuid = Guid.Empty;
                    WorkersWorks = null;
                    Works = null;
                    WorksStarted = null;
                    ResultArgs = null;
                    WorksOrder = null;
                    GetDoWorkEventHandlerDoWork = null;
                }
                
                if (WorksQue != null && !IsBusy)
                {
                    var wq = WorksQue;
                    WorksQue = null;
                    RunWorkerAsync(wq);
                }
            }
        }

        public void ReworkOrder(int workNumber)
        {
            if (SelfClosing)
            {
                throw new Exception("ReworkOrder cannot be used when SelfClosing = true.");
            }
            WorksStarted[workNumber] = false;
            WorksOrderChanged = true;
        }

        public int CurrentThreadCount { get; private set; }
        private List<BackgroundWorker> Workers = null;
        private Guid WorkSetGuid = Guid.Empty;
        private List<int> WorkersWorks = null;
        private object[] Works = null;
        private bool[] WorksStarted = null;
        public int[] WorksOrder { get; private set; } = null;
        private bool WorksOrderChanged = false;
        private EachRunWorkerCompletedEventArgs[] ResultArgs = null;
        public int AllWorkerCount { get; private set; } = 0;
        private int NextWork;
        private int NextResult;
        private int CompletedWorksCount;
        public void RunWorkerAsync(IEnumerable<object> arguments)
        {
            if (IsBusy)
            {
                throw new InvalidOperationException();
            }
            if (!SelfClosing && EachRunWorkerCompletedSequential != null)
            {
                throw new Exception("EachRunWorkerCompletedSequential cannot be used when SelfClosing = false.");
            }
            CancellationPending = false;
            if (InternalThreadCount < 1) InternalThreadCount = 1;
            CurrentThreadCount = InternalThreadCount;
            var e = new RunWorkerStartingEventArgs(arguments);
            if (RunWorkerStarting != null) RunWorkerStarting(this, e);
            Workers = new List<BackgroundWorker>();
            var workSetGuid = WorkSetGuid = e.WorkSetGuid;
            WorkersWorks = new List<int>();
            Works = arguments.ToArray();
            WorksStarted = new bool[Works.Length];
            ResultArgs = new EachRunWorkerCompletedEventArgs[Works.Length];
            if (WorksOrder == null || WorksOrder.Length != Works.Length)
            {
                WorksOrder = new int[Works.Length];
                for (var i = 0; i < Works.Length; i++) WorksOrder[i] = i;
            }
            WorksOrderChanged = false;
            NextWork = Math.Min(CurrentThreadCount, Works.Length);
            NextResult = 0;
            CompletedWorksCount = 0;
            GetDoWorkEventHandlerDoWork = GetDoWorkEventHandler(DoWork);
            for (var initialWork = 0; initialWork < CurrentThreadCount && initialWork < Works.Length; initialWork++)
            {
                var wn = WorksOrder[initialWork];

                var worker = new BackgroundWorker();
                AllWorkerCount++;
                worker.WorkerSupportsCancellation = InternalWorkerSupportsCancellation;
                worker.DoWork += GetDoWorkEventHandlerDoWork;
                
                RunWorkerCompletedEventHandler runWorkerCompeted = privateRunWorkerCompletedEventHandler;
                

                worker.RunWorkerCompleted += runWorkerCompeted;
                Workers.Add(worker);
                WorkersWorks.Add(wn);
                WorksStarted[wn] = true;
                worker.RunWorkerAsync(Tuple.Create(Works[wn], wn, 0, workSetGuid));
            }
        }

        private void ExitQueAccepter()
        {
            if (Exit_Que && AllWorkerCount == 0) Environment.Exit(Exit_Code);
            //if (Exit_Que) Environment.Exit(Exit_Code);
        }

        /// <summary>
        /// バックグラウンド処理のキャンセルをリクエストします。割り込み待ちもキャンセルされます。
        /// </summary>
        public void CancelAsync()
        {
            if(!InternalWorkerSupportsCancellation)
            {
                throw new InvalidOperationException();
            }
            WorksQue = null;
            CancellationPending = true;
            var WorkerExists = false;
            if (Workers != null)
            {
                foreach (var worker in Workers)
                {
                    if (worker != null)
                    {
                        WorkerExists = true;
                        worker.CancelAsync();
                    }
                }
            }
            if(!WorkerExists)
            {
                Workers = null;
                WorkSetGuid = Guid.Empty;
                WorkersWorks = null;
                Works = null;
                WorksStarted = null;
                ResultArgs = null;
                WorksOrder = null;
                GetDoWorkEventHandlerDoWork = null;
            }
        }

        /// <summary>
        /// バックグラウンド処理中かどうかを取得します。
        /// </summary>
        public bool IsBusy
        {
            get
            {
                return Workers != null;
            }
        }

        private IEnumerable<object> WorksQue = null;

        public void RunWorkerAsync(int workCount)
        {
            RunWorkerAsync(new object[workCount]);
        }

        public void RunWorkerAsyncWithInterrupt(int workCount, bool waitCancel)
        {
            RunWorkerAsyncWithInterrupt(new object[workCount], waitCancel);
        }
        /// <summary>
        /// 実行中の処理があればそれをキャンセルした上で新しい処理を開始します。
        /// </summary>
        public void RunWorkerAsyncWithInterrupt(IEnumerable<object> arguments, bool waitCancel)
        {
            if (IsBusy)
            {
                if (waitCancel)
                {
                    CancelAsync(); // ここで一度 Que が消される。またワーカーがゼロなら即時にキャンセル
                    if(IsBusy) // ワーカーがゼロでない場合
                    {
                        WorksQue = arguments;
                    }
                    else
                    {
                        RunWorkerAsync(arguments);
                    }
                }
                else
                {
                    var w = Workers;
                    Workers = null;
                    WorkSetGuid = Guid.Empty;
                    for (var i = 0; i < w.Count; i++)
                    {
                        w[i].CancelAsync();
                    }
                    WorksQue = null;
                    RunWorkerAsync(arguments);
                }
            }
            else
            {
                RunWorkerAsync(arguments);
            }
        }

        /// <summary>
        /// プログラムの終了を予約します。FormClosing イベントをハンドルし、そこでは終了をキャンセルして Visible = false とし、この関数を呼び出すとプログラムが安全に終了できます。
        /// </summary>
        private bool Exit_Que = false;
        private int Exit_Code = 0;
        public void ReserveExit(int exitCode, out bool realTime)
        {
            /*
            Exit_Que = true;
            Exit_Code = exitCode;
            CancelAsync();

            realTime = true;
            return;
            
            */
            if (AllWorkerCount == 0)
            {
                realTime = true;
                //Environment.Exit(exitCode);
            }
            else
            {
                Exit_Que = true;
                Exit_Code = exitCode;
                CancelAsync();
                realTime = false;
            }
        }
    }
}
