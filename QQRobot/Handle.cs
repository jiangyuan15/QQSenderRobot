﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace QQRobot
{
    /// <summary>
    /// 处理类，用于集中处理WeiboTaker等抓取器抓取到的结果。主要是3个方向：交给发送器"sende";打印日志"loger";刷新UI"UiShower".
    /// </summary>
    class Handle : WeiboTakeEvent
    {
        public LinkedList<Sender> senders;

        public Loger loger;     // 发送结果日志
        public Loger takeLoger; // 抓取结果日志
        public int Count;       // 最后一次结果抓取计数
        public UiShower shower; // ui刷新器
        public int sendCount;   // 最后一次结果已发送数
        public bool ifLog;      // 是否日志开关

        WebClient wb = new WebClient(); // IE控件，用于下载微博图片

        public void NewWeibos(Weibo[] newWeibos, Weibo[] all, WeiboUser user)
        {
            string userName = "";
            Image userHeader = null;
            if(user != null)
            {
                userName = user.UserName;
                if(user.UserHeader == null)
                {
                    string path = download(user.UserHeaderUri);
                    if (path != null)
                    {
                        user.UserHeader = Image.FromFile(path);
                    }
                }
                userHeader = user.UserHeader;
            }
            if(newWeibos.Length > 3)
            {
                Exception e = new Exception("超过3条新数据，可能存在对比异常");
                string text = format(e);
                shower.showResult(String.Format("第{0}次，{1}条", Count, newWeibos.Length), text);
                takeLoger.log(text);
            }
            else
            {
                foreach (Weibo weibo in newWeibos)
                {
                    Image[] imgs = new Image[weibo.ImgUrls.Length];
                    for (int i = 0; i < weibo.ImgUrls.Length; i++)
                    {
                        string path = download(weibo.ImgUrls[i]);
                        if(path != null)
                        {
                            imgs[i] = Image.FromFile(path);
                        }
                    }
                    Image[] sendImgs ;
                    if (imgs.Length <= 0)
                    {
                        sendImgs = imgs;
                    }
                    else
                    {
                        sendImgs = new Image[1];
                        Image longImage = CLongImgMaker.make(imgs);
                        sendImgs[0] = longImage;
                    }
                    if (senders != null && senders.Count > 0)
                    {
                        sendCount += senders.Count;
                        foreach (Sender sender in senders)
                        {
                            sender.sendWithUser(userName, userHeader, weibo.Text, sendImgs);
                        }
                        shower.showCount("已发送：" + sendCount);
                    }
                    if (ifLog && loger != null)
                    {
                        loger.log(weibo);
                    }
                }
            }

            if(newWeibos != null && newWeibos.Length > 0)
            {
                if ( ifLog && takeLoger != null )
                {
                    takeLoger.log(format(all));
                }
            }else
            {
                if ( ifLog && takeLoger != null && Count != 1 )
                {
                    takeLoger.log(formatNoChange());
                }
            }
        }

        public void TakeWeiboes(Weibo[] takeWeibos, WeiboUser user)
        {
            Count++;
            if (ifLog && Count ==1 && takeLoger != null)
            {
                takeLoger.log(format(takeWeibos));
            }
            if (shower != null)
            {
                shower.showResult(String.Format("第{0}次，{1}条",Count, takeWeibos.Length), format(takeWeibos));
            }
        }

        private string download(string url)
        {
            int index = url.LastIndexOf('/');
            string name = url.Substring(index + 1, url.Length - index -1 ) ;
            if(name.Length > 100)
            {
                name = name.Substring(0, 100);
            }
            if (!Directory.Exists("tmp"))
            {
                Directory.CreateDirectory("tmp");
            }
            string path = "tmp\\" + name;
            if (!File.Exists(path))
            {
                /*
                try
                {
                    File.Delete(path);
                }
                catch (Exception e)
                {
                    int i = 1;
                    while (File.Exists(path))
                    {
                        path += "(" + i + ")";
                        i++;
                    }
                }
                */
                try
                {
                    wb.DownloadFile(url, path);
                }catch(Exception e)
                {
                    return null;
                }
            }
            return path;
        }


        private string format(Exception e)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(String.Format("{0}  [第{1}次]", DateTime.Now.ToString(), Count));
            
            builder.AppendLine("    ===========================================================");
            builder.AppendLine("    " + e.Message);
            builder.AppendLine("    " + e.StackTrace);
            builder.AppendLine("");
            return builder.ToString();
        }

        private string formatNoChange()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(String.Format("{0}  [第{1}次] 无变化", DateTime.Now.ToString(), Count));
            return builder.ToString();
        }

        private string format(Weibo[] all)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(String.Format("{0}  [第{1}次]", DateTime.Now.ToString(), Count));

            foreach (Weibo weibo in all)
            {
                builder.AppendLine("    ===========================================================");
                builder.AppendLine(String.Format("    [Text]  {0}", weibo.Text));
                builder.AppendLine("    [Imgs]");
                if (weibo.ImgUrls != null && weibo.ImgUrls.Length > 0)
                {
                    for (int i = 0; i < weibo.ImgUrls.Length; i++)
                    {
                        builder.AppendLine("           "+ weibo.ImgUrls[i]);
                    }
                }
            }
            builder.AppendLine("");
            return builder.ToString();
        }

        public void OnCountDown(int secounds)
        {
            string show;
            if(secounds == 0)
            {
                show = "抓取中";
            }
            else
            {
                show = "倒计时：" + secounds;
            }
            shower.showCountDown(show);
        }

        public void OnStart()
        {
            shower.showStart();
        }

        public void OnStop()
        {
            shower.showStop();
        }

        public void OnException(Exception e)
        {
            Count++;
            string text = format(e);
            shower.showResult(String.Format("第{0}次，{1}条", Count, 0), text);
            takeLoger.log(text);
        }
    }
}
