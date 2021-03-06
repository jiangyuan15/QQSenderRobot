﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QQRobot
{
    /// <summary>
    /// 线程服务类，提供一个轮询抓取线程，目前采用单例。
    /// </summary>
    class Server : Object
    {
        private Server() { }
        private static Server singleton;
        private static Object lockObj = new Object();

        public BaseTakeEvent Callback; // 抓取结果回调
        private int flag = 0;       // 线程状态标识，其他线程通过改变标识操作线程执行流程
        private Thread thread;      // 线程核心
        private BaseTaker taker;    // 本线程的抓取器
        private BackgroundWorker onceWork;

        public static Server getInstance()
        {
            if (singleton == null)
            {
                lock (lockObj)
                {
                    if(singleton == null)
                    {
                        singleton = new Server();
                    }
                }
            }
            return singleton;
        }

        /// <summary>
        /// 开始轮询抓取工作
        /// </summary>
        /// <param name="taker"></param>
        public void Start(BaseTaker taker)
        {
            flag = 1;
            this.taker = taker;
            countdown = taker.Interval;
            if (thread != null)
            {
                try
                {
                    thread.Abort();
                }catch(Exception )
                {
                    
                }
                thread = null;
            }

            thread = new Thread(new ThreadStart(run));
            thread.Start();
        }
        /// <summary>
        /// 异步执行一次抓取任务
        /// </summary>
        /// <param name="taker"></param>
        public void StartOnce(BaseTaker taker)
        {
            if(this.taker == null)
            {
                this.taker = taker;
            }
            if(onceWork == null)
            {
                onceWork = new BackgroundWorker();
                onceWork.WorkerReportsProgress = false; // 设置可以通告进度
                onceWork.WorkerSupportsCancellation = true; // 设置可以取消
                onceWork.DoWork += new DoWorkEventHandler(new DoWorkEventHandler(doWorkFunc));
            }
            //new Thread(new ThreadStart(runOnce)).Start() ;
            //onceWork.CancelAsync();
            if (!onceWork.IsBusy)
            {
                onceWork.RunWorkerAsync();
            }
        }

        public void Stop()
        {
            flag = 0;
        }

        public void AbortStop()
        {
            flag = 0;
            if (thread != null)
            {
                try
                {
                    thread.Abort();
                }
                catch (Exception)
                {

                }
                thread = null;
            }
        }

        private int countdown;
        private IntPtr mWnd;
        /// <summary>
        /// 线程工作主方法
        /// </summary>
        private void run()
        {
            if(flag == 1)
            {
                if (Callback != null)
                {
                    Callback.OnStart();
                }
                try
                {
                    runOnce();
                }catch(Exception e)
                {
                    if (Callback != null)
                    {
                        Callback.OnException(e);
                    }
                }
            }
            while (flag == 1)
            {
                if (Callback != null)
                {
                    Callback.OnCountDown(countdown);
                }
                if (countdown == 0)
                {
                    try
                    {
                        runOnce();
                    }
                    catch(Exception e)
                    {
                        if (Callback != null)
                        {
                            Callback.OnException(e);
                        }
                    }
                    countdown = taker.Interval;
                }
                Thread.Sleep(1000);
                countdown--;
            }
            if (Callback != null)
            {
                Callback.OnStop();
            }
        }

        private void doWorkFunc(object sender, DoWorkEventArgs e)
        {
            try
            {
                runOnce();
            }
            catch(Exception ex)
            {
                if(Callback != null)
                {
                    Callback.OnException(ex);
                }
            }
        }

        /// <summary>
        /// 同步执行一次抓取
        /// </summary>
        private void runOnce()
        {
            string html = taker.takePage();
            if (taker.User == null)
            {
                taker.paserUser(html);
                taker.downloadUserHeader(taker.User);
            }
            BaseData[] newObjs = taker.paser(html);
            if (newObjs == null) newObjs = taker.createData(0);
            foreach (BaseData item in newObjs)
            {
                if (newObjs != null)
                {
                    item.Taker = taker;
                }
            }
            if (Callback != null)
            {
                Callback.TakeData(taker.getShowData(newObjs), taker.User);
            }

            BaseData[] weibos = taker.checkNew(newObjs);
            if (weibos != null)
            {
                if (Callback != null)
                {
                    Callback.NewData(weibos, newObjs, taker.User);
                }
            }
        }
        /// <summary>
        /// 提取最后若干条结果
        /// </summary>
        /// <param name="count"></param>
        public void handLastTime(int count)
        {
            if (taker == null) return;
            BaseData[] last = taker.getLastTake();
            BaseData[] result = new BaseData[count];
            int j = 0;
            for(int i = count -1; i  >= 0; i --)
            {
                result[i] = last[j++];
            }
            if(Callback != null)
            {
                Callback.NewData(result, last, taker.User);
            }
        }
    }
}
