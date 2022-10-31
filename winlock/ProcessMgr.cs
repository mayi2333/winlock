using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace winlock
{
    public class ProcessMgr
    {
        /// <summary>
        /// The process-specific access rights.
        /// </summary>
        [Flags]
        public enum ProcessAccess : uint
        {
            /// <summary>
            /// Required to terminate a process using TerminateProcess.
            /// </summary>
            Terminate = 0x1,

            /// <summary>
            /// Required to create a thread.
            /// </summary>
            CreateThread = 0x2,

            /// <summary>
            /// Undocumented.
            /// </summary>
            SetSessionId = 0x4,

            /// <summary>
            /// Required to perform an operation on the address space of a process (see VirtualProtectEx and WriteProcessMemory).
            /// </summary>
            VmOperation = 0x8,

            /// <summary>
            /// Required to read memory in a process using ReadProcessMemory.
            /// </summary>
            VmRead = 0x10,

            /// <summary>
            /// Required to write to memory in a process using WriteProcessMemory.
            /// </summary>
            VmWrite = 0x20,

            /// <summary>
            /// Required to duplicate a handle using DuplicateHandle.
            /// </summary>
            DupHandle = 0x40,

            /// <summary>
            /// Required to create a process.
            /// </summary>
            CreateProcess = 0x80,

            /// <summary>
            /// Required to set memory limits using SetProcessWorkingSetSize.
            /// </summary>
            SetQuota = 0x100,

            /// <summary>
            /// Required to set certain information about a process, such as its priority class (see SetPriorityClass).
            /// </summary>
            SetInformation = 0x200,

            /// <summary>
            /// Required to retrieve certain information about a process, such as its token, exit code, and priority class (see OpenProcessToken, GetExitCodeProcess, GetPriorityClass, and IsProcessInJob).
            /// </summary>
            QueryInformation = 0x400,

            /// <summary>
            /// Undocumented.
            /// </summary>
            SetPort = 0x800,

            /// <summary>
            /// Required to suspend or resume a process.
            /// </summary>
            SuspendResume = 0x800,

            /// <summary>
            /// Required to retrieve certain information about a process (see QueryFullProcessImageName). A handle that has the PROCESS_QUERY_INFORMATION access right is automatically granted PROCESS_QUERY_LIMITED_INFORMATION.
            /// </summary>
            QueryLimitedInformation = 0x1000,

            /// <summary>
            /// Required to wait for the process to terminate using the wait functions.
            /// </summary>
            Synchronize = 0x100000
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID
        {
            public int LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TOKEN_PRIVILEGES
        {
            public int PrivilegeCount;
            public LUID Luid;
            public int Attributes;
        }

        [DllImport("advapi32", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, TokenAccessLevels DesiredAccess, out IntPtr TokenHandle);

        //[DllImport("advapi32.dll", SetLastError = true)]
        //private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, uint BufferLength, out TOKEN_PRIVILEGES PreviousState, out uint ReturnLength);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int AdjustTokenPrivileges(IntPtr tokenhandle, bool disableprivs, [MarshalAs(UnmanagedType.Struct)] ref TOKEN_PRIVILEGES Newstate, int bufferlength, IntPtr PreivousState, int Returnlength);


        [DllImport("advapi32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern int LookupPrivilegeValueA(string lpsystemname, string lpname, [MarshalAs(UnmanagedType.Struct)] ref LUID lpLuid);

        [DllImport("ntdll.dll")]
        private static extern uint NtResumeProcess([In] IntPtr processHandle);

        [DllImport("ntdll.dll")]
        private static extern uint NtSuspendProcess([In] IntPtr processHandle);

        //[DllImport("ntdll.dll")]
        //public static extern int ZwSuspendProcess(IntPtr ProcessId);
        //[DllImport("ntdll.dll")]
        //public static extern int ZwResumeProcess(IntPtr ProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(
            ProcessAccess desiredAccess,
            bool inheritHandle,
            int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle([In] IntPtr handle);
        //[DllImport("user32.dll")]
        //private static extern void BlockInput(bool Block);

        /// <summary>
        /// 给当前进程获取权限
        /// 参考Url: http://t.zoukankan.com/jszyx-p-12521539.html
        /// </summary>
        /// <param name="PrivilegeName">权限名称,默认debug权限</param>
        public static bool SetPrivilege(string PrivilegeName = "SeDebugPrivilege")
        {
            var htok = IntPtr.Zero;
            if (!OpenProcessToken(Process.GetCurrentProcess().Handle, TokenAccessLevels.AdjustPrivileges | TokenAccessLevels.Query, out htok))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                return false;
            }
            if (!AdjustTokenPrivilege(htok, PrivilegeName))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                return false;
            }
            return true;
        }
        private static bool AdjustTokenPrivilege(IntPtr tokenHandle, string lpname)
        {
            var serLuid = new LUID();
            if (LookupPrivilegeValueA(null, lpname, ref serLuid) == 0)
            { 
                return false;
            }

            var serTokenp = new TOKEN_PRIVILEGES
            {
                PrivilegeCount = 1,
                Luid = serLuid,
                Attributes = 0x00000002
            };
            if (AdjustTokenPrivileges(tokenHandle, false, ref serTokenp, 0, IntPtr.Zero, 0) == 0)
            { 
                return false;
            }

            return true;
        }

        /// <summary>
        /// 挂起进程
        /// </summary>
        /// <param name="processId"></param>
        public static void SuspendProcess(int processId)
        {
            IntPtr hProc = IntPtr.Zero;
            try
            {
                // Gets the handle to the Process
                hProc = OpenProcess(ProcessAccess.SuspendResume, false, processId);
                if (hProc != IntPtr.Zero)
                    NtSuspendProcess(hProc);
            }
            finally
            {
                // Don't forget to close handle you created.
                if (hProc != IntPtr.Zero)
                    CloseHandle(hProc);
            }
        }
        ///// <summary>
        ///// 挂起进程
        ///// </summary>
        ///// <param name="processId"></param>
        //public static void SuspendProcess(IntPtr hProc)
        //{
        //    NtSuspendProcess(hProc);
        //}

        /// <summary>
        /// 恢复进程
        /// </summary>
        /// <param name="processId"></param>
        public static void ResumeProcess(int processId)
        {
            IntPtr hProc = IntPtr.Zero;
            try
            {
                // Gets the handle to the Process
                hProc = OpenProcess(ProcessAccess.SuspendResume, false, processId);
                if (hProc != IntPtr.Zero)
                    NtResumeProcess(hProc);
            }
            finally
            {
                // Don't forget to close handle you created.
                if (hProc != IntPtr.Zero)
                    CloseHandle(hProc);
            }
        }
        //public static void BlockInput()
        //{
        //    BlockInput(true);
        //}
        //public static void UnBlockInput()
        //{
        //    BlockInput(false);
        //}
        ///// <summary>
        ///// 恢复进程
        ///// </summary>
        ///// <param name="processId"></param>
        //public static void ResumeProcess(IntPtr hProc)
        //{
        //    NtResumeProcess(hProc);
        //}
    }
}
