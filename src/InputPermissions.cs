using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MacroExecute
{
    public static class InputPermissions
    {
        [DllImport("kernel32.dll",SetLastError=true)] private static extern IntPtr OpenProcess(uint access,bool inherit,uint pid);
        [DllImport("advapi32.dll",SetLastError=true)] private static extern bool OpenProcessToken(IntPtr process,uint access,out IntPtr token);
        [DllImport("advapi32.dll",SetLastError=true)] private static extern bool GetTokenInformation(IntPtr token,int kind,IntPtr data,int length,out int needed);
        [DllImport("advapi32.dll")] private static extern IntPtr GetSidSubAuthorityCount(IntPtr sid);
        [DllImport("advapi32.dll")] private static extern IntPtr GetSidSubAuthority(IntPtr sid,uint index);
        [DllImport("kernel32.dll")] private static extern bool CloseHandle(IntPtr handle);
        public static string Explain(int? own,int? target,bool uiAccess) { return own.HasValue&&target.HasValue&&target>own&&!uiAccess?"Windows blocks input into this higher-privilege window. Stop playback, then use Settings → Restart as administrator if you trust this workflow. This does not enable input on the UAC secure desktop.":null; }
        internal static int? Integrity(IntPtr process,out bool uiAccess)
        {
            uiAccess=false;IntPtr token;if(!OpenProcessToken(process,8,out token))return null;
            try{int needed;IntPtr flag=Marshal.AllocHGlobal(4);try{if(GetTokenInformation(token,26,flag,4,out needed))uiAccess=Marshal.ReadInt32(flag)!=0;}finally{Marshal.FreeHGlobal(flag);}GetTokenInformation(token,25,IntPtr.Zero,0,out needed);if(needed<=0)return null;IntPtr data=Marshal.AllocHGlobal(needed);try{if(!GetTokenInformation(token,25,data,needed,out needed))return null;IntPtr sid=Marshal.ReadIntPtr(data);byte count=Marshal.ReadByte(GetSidSubAuthorityCount(sid));return count==0?(int?)null:Marshal.ReadInt32(GetSidSubAuthority(sid,(uint)(count-1)));}finally{Marshal.FreeHGlobal(data);}}
            finally{CloseHandle(token);}
        }
        public static string CurrentMode() { bool access;using(var process=Process.GetCurrentProcess()){int? level=Integrity(process.Handle,out access);return access?"UIAccess token":!level.HasValue?"Permissions unavailable":level>=0x3000?"Administrator":"Standard user";} }
        public static void EnsureForeground() { EnsureWindow(Native.GetForegroundWindow()); }
        public static void EnsureWindow(IntPtr window)
        {
            if(window==IntPtr.Zero)return;uint pid;Native.GetWindowThreadProcessId(window,out pid);IntPtr target=OpenProcess(0x1000,false,pid);if(target==IntPtr.Zero)return;
            try{bool access,unused;int? targetLevel=Integrity(target,out unused);using(var own=Process.GetCurrentProcess()){int? ownLevel=Integrity(own.Handle,out access);string error=Explain(ownLevel,targetLevel,access);if(error!=null)throw new InvalidOperationException(error);}}finally{CloseHandle(target);}
        }
    }
}
