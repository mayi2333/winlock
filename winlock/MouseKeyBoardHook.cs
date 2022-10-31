using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace winlock
{
    public class MouseKeyBoardHook
    {


        //使用方式
        //1.在窗口 新建按钮（开始hook） 点击事件中执行Hook_Start
        //2.在窗口 新建按钮（关闭hook） 点击事件中执行Hook_Clear
        //3.在KeyBoardHookProc 中编写hook到键盘要进行的操作
        //4.在MouseHookProc 中编写hook到鼠标要进行的操作

        //一些依赖项
        [DllImport("user32.dll")]
        private static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hInstance, int threadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern bool UnhookWindowsHookEx(int idHook);

        [DllImport("user32.dll")]
        private static extern int CallNextHookEx(int idHook, int nCode, int wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern int GetCurrentThreadId();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string name);

        [DllImport("User32.dll")]
        private static extern void keybd_event(Byte bVk, Byte bScan, Int32 dwFlags, Int32 dwExtraInfo);

        //鼠标事件映射
        private const int WM_MOUSEMOVE = 0x200;
        private const int WM_LBUTTONDOWN = 0x201;
        private const int WM_RBUTTONDOWN = 0x204;
        private const int WM_MBUTTONDOWN = 0x207;
        private const int WM_LBUTTONUP = 0x202;
        private const int WM_RBUTTONUP = 0x205;
        private const int WM_MBUTTONUP = 0x208;
        private const int WM_LBUTTONDBLCLK = 0x203;
        private const int WM_RBUTTONDBLCLK = 0x206;
        private const int WM_MBUTTONDBLCLK = 0x209;

        //鼠标hook
        public const int WH_MOUSE_LL = 14;
        //键盘hook
        public const int WH_KEYBOARD_LL = 13;
        public delegate int HookProc(int nCode, int wParam, IntPtr lParam);

        //回调
        HookProc KeyBoardHookProcedure;
        HookProc _mouseHookProcedure;

        //hook到的消息结构
        [StructLayout(LayoutKind.Sequential)]
        public class KeyBoardHookStruct
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }
        [StructLayout(LayoutKind.Sequential)]
        public class MouseHookStruct
        {
            public Point pt;
            public int hWnd;
            public int wHitTestCode;
            public int dwExtraInfo;
        }

        static MouseKeyBoardHook _instance;
        public static MouseKeyBoardHook Instance { get { return _instance; } private set { _instance = value; } }

        int hHook = 0;
        int _hMouseHook = 0;

        public static event EventHandler<HotKeyEventArgs> HotKeyPressed;

        public bool LockStatus = false;

        //public static void Create(Action<int, int, IntPtr> action)
        public static void Create()
        {
            if (_instance == null)
            {
                _instance = new MouseKeyBoardHook();
            }
        }
        //开启hook
        public void Hook_Start()
        {
            KeyBoardHookProcedure = new HookProc(KeyBoardHookProc);
            hHook = SetWindowsHookEx(WH_KEYBOARD_LL, KeyBoardHookProcedure, GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName), 0);

            _mouseHookProcedure = new HookProc(MouseHookProc);
            _hMouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseHookProcedure, Marshal.GetHINSTANCE(System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0]), 0);
        }

        //关闭hook  
        public void Hook_Clear()
        {
            bool retKeyboard = true;
            bool retMouse = true;
            retKeyboard = UnhookWindowsHookEx(hHook);
            hHook = 0;
            retMouse = UnhookWindowsHookEx(_hMouseHook);
            _hMouseHook = 0;
        }

        //键盘hook到之后的操作
        private int KeyBoardHookProc(int nCode, int wParam, IntPtr lParam)
        {
            Console.WriteLine($"KeyBoardHookProc\r\nnCode:{nCode},wParam:{wParam},lParam:{lParam}");
            //快捷键处理
            if (nCode >= 0)
            {
                KeyBoardHookStruct kbh = (KeyBoardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyBoardHookStruct));
                OnHotKeyPressed(wParam, kbh.vkCode);
                //if (kbh.vkCode == (int)Keys.U || kbh.vkCode == (int)Keys.Alt)  //D
                //{
                //    //如果按下了D 要进行的处理
                //    return CallNextHookEx(hHook, nCode, wParam, lParam);
                //}
            }
            //未锁屏状态下 不拦截键盘输入
            if (!LockStatus)
            {
                return CallNextHookEx(hHook, nCode, wParam, lParam);
            }
            return 1;
        }
        /// <summary>
        /// 处理快捷键
        /// </summary>
        private void OnHotKeyPressed(int wParam, int vkCode)
        {
            if (wParam == 261)
            {
                Console.WriteLine($"OnHotKeyPressed\r\vkCode:{(Keys)vkCode}");
                HotKeyEventArgs hotKeyEventArgs = null;
                if (vkCode == (int)Keys.L && LockStatus == false)
                {
                    hotKeyEventArgs = new HotKeyEventArgs(Keys.L, KeyModifiers.Alt);
                }
                else if (vkCode == (int)Keys.U)
                {
                    hotKeyEventArgs = new HotKeyEventArgs(Keys.U, KeyModifiers.Alt);
                }
                if (HotKeyPressed != null && hotKeyEventArgs != null)
                {
                    HotKeyPressed(null, hotKeyEventArgs);
                }
            }
        }

        //鼠标hook到之后的操作
        private int MouseHookProc(int nCode, Int32 wParam, IntPtr lParam)
        {
            //未锁屏状态下 不拦截鼠标输入
            if (!LockStatus)
            {
                return CallNextHookEx(hHook, nCode, wParam, lParam);
            }
            Console.WriteLine($"MouseHookProc\r\nnCode:{nCode},wParam:{wParam},lParam:{lParam}");
            MouseHookStruct MyMouseHookStruct = (MouseHookStruct)Marshal.PtrToStructure(lParam, typeof(MouseHookStruct));
            if (wParam == WM_LBUTTONDOWN || wParam == WM_RBUTTONDOWN)
            {
                //鼠标左右键按下要进行的处理
                return 1;
            }
            return CallNextHookEx(_hMouseHook, nCode, wParam, lParam);
        }
    }
}
