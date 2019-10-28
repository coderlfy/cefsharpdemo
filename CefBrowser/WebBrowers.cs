using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BZXT.ClientFramework.ClientEntity;
using BZXT.ClientFramework.ClientInterface;
using CefSharp.WinForms;
using CefSharp;
using Newtonsoft.Json;
using System.IO;
using System.Threading;
using WDCloud.Service.DAL.Entity;
using WDCloud.Service.DAL;

namespace CefBrowser
{
    public partial class WebBrowers
    {

        private ChromiumWebBrowserEx m_chromeBrowser = null;
        private JavaScriptInteractionObj m_jsInteractionObj = null;
        public WebBrowers()
        {
            InitializeComponent();
        }

        private delegate void MyInvoke(ModuleMsg msg);
        private void RecvieMsg_Handler(ModuleMsg msg)
        {
            switch (msg.MsgType)
            {
                case ModuleMsgType.All:
                    {
                        MyInvoke myinvoke = new MyInvoke(EventHandler_AllMsg);
                        BeginInvoke(myinvoke, new object[] { msg });
                        break;
                    }
                case ModuleMsgType.Single:
                    {
                        MyInvoke myinvoke = new MyInvoke(EventHandler_SingleMsg);
                        BeginInvoke(myinvoke, new object[] { msg });
                        break;
                    }
                default:
                    break;
            }

        }
        private void EventHandler_AllMsg(ModuleMsg msg)
        {
            m_jsInteractionObj.ReciveMsg(msg.TargetKey, msg.Content);
            switch (msg.TargetKey)
            {
                case MsgConstants.fun_ppt_QD:
                    {//PPT发送抢答命令，自动发布抢答任务
                        //step1.将图片生成习题，切换显示习题界面
                        //var script = string.Format("goDgxtView('','pptrw','{0}','');", msg.Content);
                        //m_chromeBrowser.ExecuteScriptAsync(script);
                        //step2.将习题生成任务，切换显示抢答界面，kssk-edit.js/function PPTqdEdit(img) 新增脚本方法
                        var script = string.Format("PPTqdEdit('{0}');", msg.Content.Substring(msg.Content.LastIndexOf("/") + 1));
                        m_chromeBrowser.ExecuteScriptAsync(script);
                        break;
                    }

                default:
                    break;
            }
        }
        private void EventHandler_SingleMsg(ModuleMsg msg)
        {
            if (msg.Content.Equals("ToUrl"))
            {
                var script = "ToUrl();";
                m_jsInteractionObj.ExecJSFromWinForms(script);
            }
        }

        

        public void Init(string CNOrEn = "cn", string webports = "9001,9002,9003,9004,9005")
        {
            if (m_chromeBrowser == null)
            {
                
                string[] webportsarr = webports.Split(',');
                int webport = int.Parse(webportsarr[4]);
                //this.websocket = int.Parse(webportsarr[1]);
                //CefSettings cefSettings = new CefSettings();
                CefSettings cefSettings = new CefSettings();
                cefSettings.CachePath = Directory.GetCurrentDirectory() + @"\Cache";
                cefSettings.Locale = "zh_CN";
                cefSettings.IgnoreCertificateErrors = true;
                cefSettings.LogSeverity = LogSeverity.Disable;
                cefSettings.CefCommandLineArgs.Add("Enable JavaScript source maps", "false");

                //cefSettings.CefCommandLineArgs.Add("--enable-system-flash", null);
                //cefSettings.CefCommandLineArgs.Add("ppapi-flash-version", "19.0.0.226");
                //cefSettings.CefCommandLineArgs.Add("ppapi-flash-path", @"D:\Project\01_代码开发库\trunk\client\src\Service\TeacherClient\bin\x86\Release\PepperFlash\pepflashplayer.dll");
                Cef.Initialize(cefSettings);
                //string EnOrCN = System.Configuration.ConfigurationManager.AppSettings["12studyCnOrEn"];
                // Cef.Initialize();

                //string page = string.Format("http://127.0.0.1:{1}/Default.aspx?cnoren={0}", CNOrEn, webport);
                string page = string.Format("http://127.0.0.1:{0}/WDk12web/app/jsp/tbkt-list.html", webport);
                m_chromeBrowser = new ChromiumWebBrowserEx(page);
                m_chromeBrowser.WebPort = webport;
                m_chromeBrowser.WebapiPort = int.Parse(webportsarr[2]);
                m_chromeBrowser.WebsocketPort = int.Parse(webportsarr[1]);
                BrowserSettings browserSettings = new BrowserSettings();
                
                browserSettings.FileAccessFromFileUrlsAllowed = true;//以file://协议访问文件允许权限
                browserSettings.UniversalAccessFromFileUrlsAllowed = true;
                browserSettings.TextAreaResizeDisabled = true;
                //browserSettings.WebSecurityDisabled = true;
                //browserSettings.JavascriptDisabled = false;
                m_chromeBrowser.BrowserSettings = browserSettings;
                Controls.Add(m_chromeBrowser);
                               

                m_jsInteractionObj = new JavaScriptInteractionObj(sendMsgEvent);
                m_jsInteractionObj.delegateOpenFileDialog += new JavaScriptInteractionObj.delegate_OpenFileDialog(OpenFileDialog);

                m_jsInteractionObj.SetChromeBrowser(m_chromeBrowser);
                m_chromeBrowser.RegisterJsObject("winformObj", m_jsInteractionObj);
                //m_chromeBrowser.Load(page);
                m_chromeBrowser.MenuHandler = new MenuHandler();


            }
            else
            {

            }
            //ChromeDevToolsSystemMenu.CreateSysMenu(this);            
        }
        #region m_jsInteractionObj 委托方法
        private delegate string delegateOpenFileDialog(string gs);
        private string Invoke_OpenFileDialog(string gs)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            //dlg.InitialDirectory = strOpenFileFolder;//初始目录，不赋值也可以
            dlg.Filter = string.Format("{0}", gs);//支持文件类型
            dlg.Multiselect = false;
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                string tmpfilepath = System.Windows.Forms.Application.StartupPath + "\\tmpfile\\";
                if (!Directory.Exists(tmpfilepath))
                {
                    Directory.CreateDirectory(tmpfilepath);
                }
                string newfilename = Guid.NewGuid().ToString().Replace("-", "");
                string curgs = dlg.FileName.Substring(dlg.FileName.LastIndexOf('.') + 1);
                string destFileName = string.Format("{0}\\tmpfile\\{1}.{2}",
                    System.Windows.Forms.Application.StartupPath,
                    newfilename,
                    curgs);
                // string oldName = Path.GetFileName(dlg.FileName);//只有名字
                Bitmap pic = new Bitmap(dlg.FileName);

                int width = pic.Size.Width;   // 图片的宽度
                int height = pic.Size.Height;   // 图片的高度

                int newWidth = 800;
                int newHeight = 600;
                if (width < newWidth || height < newHeight)
                {
                    newWidth = width;
                    newHeight = height;
                }
                string sourcePathName = UploadImage(dlg.FileName, destFileName, newWidth, newHeight);//缩小图片300*168

                //将openfiledlg对话框选择的本机文件临时存储到 \tmpfile。程序关闭时清除
                //File.Copy(dlg.FileName, destFileName, true);
                return string.Format("\\tmpfile\\{0}.{1}", newfilename, curgs);
            }
            return "";
        }
        #region  等比例缩小图片
        /// <summary>
        /// 等比例缩小图片
        /// </summary>
        /// <param name="fileName">图片名称</param>
        /// <param name="sourcePath">原地址</param>
        /// <param name="path">保存地址</param>
        /// <param name="newWidth"></param>
        /// <param name="newHeight"></param>
        private string UploadImage(string sourcePathName, string path, int newWidth, int newHeight)
        {
            //得到小图片Image对象
            
            //if (!Directory.Exists(path)) //判断、创建小图片的存放路径
            //{
            //    Directory.CreateDirectory(path);
            //}
            if (sourcePathName.Split('.').Last().Contains("gif"))
            {
                System.Drawing.Image smallImage = Image.FromFile(sourcePathName);
                smallImage.Save(path,System.Drawing.Imaging.ImageFormat.Gif);
            }
            else
            {
                System.Drawing.Image smallImage = GetNewImage(sourcePathName, newWidth, newHeight);
                smallImage.Save(path);
            }
            
            return path;
        }
        /// <summary>
        /// 对图片进行处理，返回一个Image类别的对象
        /// </summary>
        /// <param name="oldImgPath">原图片路径</param>
        /// <param name="newWidth">新图片宽度</param>
        /// <param name="newHeight">新图片高度</param>
        /// <returns></returns>
        private System.Drawing.Image GetNewImage(string oldImgPath, int newWidth, int newHeight)
        {
            System.Drawing.Image newImage = null;
            System.Drawing.Image oldImage = null;
            oldImage = System.Drawing.Image.FromFile(oldImgPath);  //加载原图片
            newImage = oldImage.GetThumbnailImage(newWidth, newHeight,
                new System.Drawing.Image.GetThumbnailImageAbort(IsTrue), IntPtr.Zero); //对原图片进行缩放
            return newImage;
        }
        /// <summary>
        /// 在Image类别对图片进行缩放的时候，需要一个返回bool类别的委托
        /// </summary>
        /// <returns></returns>
        private bool IsTrue()
        {
            return true;
        }
        #endregion
        private string OpenFileDialog(string gs)
        {
            IAsyncResult asyncRet = null;
            try
            {
                delegateOpenFileDialog tmpOpenFileDialog = new delegateOpenFileDialog(Invoke_OpenFileDialog);
                asyncRet = BeginInvoke(tmpOpenFileDialog, new object[] { gs });
            }
            catch (Exception ex)
            {
                ex.Message.ToString();
            }
            return EndInvoke(asyncRet).ToString();
        }
        #endregion

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            // Test if the About item was selected from the system menu
            //if ((m.Msg == ChromeDevToolsSystemMenu.WM_SYSCOMMAND) && ((int)m.WParam == ChromeDevToolsSystemMenu.SYSMENU_CHROME_DEV_TOOLS))
            //{
            //    m_chromeBrowser.ShowDevTools();
            //}
        }

        public static string GetAppLocation()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        private void MainFrm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Cef.Shutdown();
        }

        private void WebBrowers_Load(object sender, EventArgs e)
        {
            receiveMsgEvent += new ReciveMsgEvent(RecvieMsg_Handler);
        }

        private void WebBrowers_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.F1  )
            {
                m_chromeBrowser.ShowDevTools();
                //button1.PerformClick();// 执行按钮“1”的操作
                //e.Handled = true;
            }
        }
    }

    internal class MenuHandler : IMenuHandler
    {
        public bool OnBeforeContextMenu(IWebBrowser browser, IContextMenuParams parameters)
        {
            return false;
        }
    }
}
