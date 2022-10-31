using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace winlock
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Process[] pc =  Process.GetProcessesByName("winlogon");
            if (pc.Length > 0 && ProcessMgr.SetPrivilege())
            {
                //ProcessMgr.SuspendProcess(pc[0].Id);
                HotKeyMgr.RegisterHotKey(Keys.L, KeyModifiers.Alt);
                HotKeyMgr.RegisterHotKey(Keys.U, KeyModifiers.Alt);
                HotKeyMgr.HotKeyPressed += new EventHandler<HotKeyEventArgs>(HotKeyMgr_HotKeyPressed);
                //Console.ReadLine();
                Application.Run();
            }
        }
        static void HotKeyMgr_HotKeyPressed(object sender, HotKeyEventArgs e)
        {
            if (e.Key == Keys.L && e.Modifiers == KeyModifiers.Alt)
            {
                //var pc = Process.GetProcessesByName("winlogon");
                //if (pc.Length > 0)
                //{
                //    MessageBox.Show("Alt + L");
                //    ProcessMgr.SuspendProcess(pc[0].Id);
                //}
                //Console.WriteLine("Alt + U");
            }
            else if (e.Key == Keys.U && e.Modifiers == KeyModifiers.Alt)
            {

                //var pc = Process.GetProcessesByName("winlogon");
                //if (pc.Length > 0)
                //{
                //    ProcessMgr.ResumeProcess(pc[0].Id);
                //    MessageBox.Show("Alt + U");
                ProcessMgr.UnBlockInput();
                //}
                //Console.WriteLine("Alt + L");
            }
            else
            {
                Console.WriteLine("HotKeyMgr_HotKeyPressed");
            }
        }
    }
}
