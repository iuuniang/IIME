using KeePass.Plugins;
using KeePass.Util;
using KeePass.Util.Spr;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

//.\KeePass.exe --plgx-create D:\IIME --plugx-prereq-os:Windows
namespace IIME
{
    public sealed class IIMEExt : Plugin
    {

        private const string UPDATEURL = "https://raw.githubusercontent.com/iuuniang/IIME/main/update.txt";

        private IPluginHost m_host = null;
        public override bool Initialize(IPluginHost host)
        {
            if (host == null) return false;
            m_host = host;

            AutoType.FilterSendPre += this.OnAutoTypeFilterSendPre;
            AutoType.SendPost += this.OnAutoTypeSendPost;

            return true;
        }

        public override void Terminate()
        {
            if (m_host != null)
            {
                AutoType.FilterSendPre -= this.OnAutoTypeFilterSendPre;
                AutoType.SendPost -= this.OnAutoTypeSendPost;
                m_host = null;
            }
        }

        public override string UpdateUrl
        {
            get { return UPDATEURL; }
        }

        public override Image SmallIcon
        {
            get { return (Image)KeePass.Program.Resources.GetObject("B16x16_KTouch"); }
        }

        private void OnAutoTypeFilterSendPre(object sender, AutoTypeEventArgs autoTypeEventArgs)
        {
            // 关闭IME（切换到英文）
            InputMethodController.SetIMEStatus(0u);
        }

        private void OnAutoTypeSendPost(object sender, AutoTypeEventArgs autoTypeEventArgs)
        {
            // 添加短暂延迟，确保目标应用程序已完成输入处理
            Thread.Sleep(100);
            InputMethodController.SetIMEStatus(1u);
        }



        public static class InputMethodController
        {
            private const uint GW_CHILD = 0x5;
            private const uint WM_IME_CONTROL = 0x283;

            private const uint IMC_GETCONVERSIONMODE = 0x0001;
            private const uint IMC_SETCONVERSIONMODE = 0x0002;
            private const uint IMC_GETOPENSTATUS = 0x0005;
            private const uint IMC_SETOPENSTATUS = 0x0006;
            private const uint IME_CMODE_NOCONVERSION = 0x100;
            private const uint IME_CMODE_LANGUAGE = 0x3;
            private const uint IME_CMODE_NATIVE = 0x1;
            private const uint IME_CMODE_ALPHANUMERIC = 0x0;

            [DllImport("user32.dll", SetLastError = true)]
            private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

            [DllImport("Imm32.dll")]
            private static extern bool ImmSetConversionStatus(IntPtr hIMC, int fdwConversion, int fdwSentence);
            [DllImport("Imm32.dll")]
            private static extern IntPtr ImmGetContext(IntPtr hWnd);
            [DllImport("imm32.dll")]
            private static extern IntPtr ImmGetDefaultIMEWnd(IntPtr hWnd);
            [DllImport("Imm32.dll")]
            private static extern bool ImmReleaseContext(IntPtr hWnd, IntPtr hIMC);
            [DllImport("user32.dll ", SetLastError = true)]
            private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr GetKeyboardLayout(uint idThread);
            [DllImport("imm32.dll")]
            private static extern bool ImmGetConversionStatus(IntPtr himc, ref int fdwConversion, ref int fdwSentence);
            [DllImport("user32.dll", SetLastError = true)]
            private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr processId);
            [DllImport("user32.dll")]
            private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
            [DllImport("kernel32.dll")]
            private static extern uint GetCurrentThreadId();
            [DllImport("user32.dll")]
            private static extern IntPtr GetForegroundWindow();
            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

            [Flags]
            private enum GuiThreadInfoFlags
            {
                GUI_CARETBLINKING = 0x00000001,
                GUI_INMENUMODE = 0x00000004,
                GUI_INMOVESIZE = 0x00000002,
                GUI_POPUPMENUMODE = 0x00000010,
                GUI_SYSTEMMENUMODE = 0x00000008
            }
            [StructLayout(LayoutKind.Sequential)]
            private struct GUITHREADINFO
            {
                public int cbSize;
                public GuiThreadInfoFlags flags;
                public IntPtr hwndActive;
                public IntPtr hwndFocus;
                public IntPtr hwndCapture;
                public IntPtr hwndMenuOwner;
                public IntPtr hwndMoveSize;
                public IntPtr hwndCaret;
                public System.Drawing.Rectangle rcCaret;
            }
            public static bool SetIMEStatus(uint status)
            {
                IntPtr? result1 = SetOpenStatus(status);
                IntPtr? result2 = SetConversionMode(status);

                return (result1 != null && result1.Value != IntPtr.Zero) || (result2 != null && result2.Value != IntPtr.Zero);
            }
            public static bool GetIMEStatus(IntPtr hWnd = default(IntPtr))
            {
                if (hWnd == IntPtr.Zero)
                {
                    hWnd = GetForegroundWindow();
                    hWnd = GetFocus(hWnd, true) ?? hWnd;
                }
                bool opened = GetOpenStatus(hWnd);
                int? convMode = GetConversionMode(hWnd);
                ushort langId = GetCurrentLangIdByHwnd(hWnd);

                if (opened && langId == 0x409) { return false; }
                if (convMode == null) { return false; }
                if ((convMode & IME_CMODE_NOCONVERSION) != 0) { return false; }
                return opened && ((convMode & IME_CMODE_LANGUAGE) != 0);
            }
            private static IntPtr? SetOpenStatus(uint status, IntPtr hWnd = default(IntPtr))
            {
                if (hWnd == IntPtr.Zero)
                {
                    hWnd = GetForegroundWindow();
                    // 获取实际拥有焦点的窗口
                    IntPtr? focusWindow = GetFocus(hWnd, true);
                    if (focusWindow != null && focusWindow.Value != IntPtr.Zero)
                    {
                        hWnd = focusWindow.Value;
                    }
                }
                return Control(hWnd, IMC_SETOPENSTATUS, status);
            }
            private static bool GetOpenStatus(IntPtr hWnd)
            {
                if (hWnd == IntPtr.Zero)
                {
                    hWnd = GetForegroundWindow();
                }
                return Control(hWnd, IMC_GETOPENSTATUS) != IntPtr.Zero; //返回值 0:英文 1:中文
            }
            public static IntPtr? SetConversionMode(uint cMode, IntPtr hWnd = default(IntPtr))
            {
                if (hWnd == IntPtr.Zero)
                {
                    hWnd = GetForegroundWindow();
                    // 获取实际拥有焦点的窗口
                    IntPtr? focusWindow = GetFocus(hWnd, true);
                    if (focusWindow != null && focusWindow.Value != IntPtr.Zero)
                    {
                        hWnd = focusWindow.Value;
                    }
                }
                return Control(hWnd, IMC_SETCONVERSIONMODE, cMode);
            }
            private static int? GetConversionMode(IntPtr hWnd)
            {
                if (hWnd == IntPtr.Zero)
                {
                    hWnd = GetForegroundWindow();
                }
                IntPtr? result = Control(hWnd, IMC_GETCONVERSIONMODE);
                if (result != null)
                {
                    return result.Value.ToInt32();
                }
                else
                {
                    return null;
                }
            }
            private static ushort GetCurrentLangIdByHwnd(IntPtr hWnd)
            {
                if (hWnd == IntPtr.Zero)
                {
                    hWnd = GetForegroundWindow();
                }
                uint threadId = GetWindowThreadProcessId(hWnd, IntPtr.Zero);

                return (ushort)((uint)GetKeyboardLayout(threadId).ToInt32() & 0xFFFF);
            }
            private static IntPtr? Control(IntPtr hWnd = default(IntPtr), uint command = 0, uint data = 0)
            {
                if (hWnd == IntPtr.Zero)
                {
                    hWnd = GetForegroundWindow();
                }
                IntPtr hIMEWnd = ImmGetDefaultIMEWnd(hWnd);
                if (hIMEWnd != IntPtr.Zero)
                {
                    return SendMessage(hIMEWnd, WM_IME_CONTROL, (IntPtr)command, (IntPtr)data);
                }
                return null;
            }
            private static IntPtr? GetFocus(IntPtr hWnd, bool real = false)
            {
                if (hWnd == IntPtr.Zero)
                {
                    hWnd = GetForegroundWindow();
                }
                GUITHREADINFO? guiThreadInfo = GetGuiThreadInfo(hWnd);
                if (guiThreadInfo != null)
                {
                    if (guiThreadInfo.Value.hwndFocus != IntPtr.Zero)
                        return guiThreadInfo.Value.hwndFocus;
                    if ((guiThreadInfo.Value.hwndCaret != IntPtr.Zero) && (guiThreadInfo.Value.flags.HasFlag(GuiThreadInfoFlags.GUI_CARETBLINKING)))
                    {
                        return guiThreadInfo.Value.hwndCaret;
                    }
                }
                if (real) { return null; }
                IntPtr leafHwnd = GetLeafWindow(hWnd);
                return (leafHwnd == IntPtr.Zero || leafHwnd == hWnd) ? hWnd : leafHwnd;

            }
            private static IntPtr GetLeafWindow(IntPtr hWnd)
            {
                if (hWnd == IntPtr.Zero) { return IntPtr.Zero; }
                IntPtr currentHwnd = hWnd;
                IntPtr childHwnd;

                while ((childHwnd = GetWindow(currentHwnd, GW_CHILD)) != IntPtr.Zero)
                {
                    currentHwnd = childHwnd;
                }
                return currentHwnd;
            }
            private static GUITHREADINFO? GetGuiThreadInfo(IntPtr hWnd)
            {
                if (hWnd != IntPtr.Zero)
                {
                    uint threadID = GetWindowThreadProcessId(hWnd, IntPtr.Zero);
                    GUITHREADINFO guiThreadInfo = new GUITHREADINFO();
                    guiThreadInfo.cbSize = Marshal.SizeOf(guiThreadInfo);
                    if (GetGUIThreadInfo(threadID, ref guiThreadInfo) == false)
                    {
                        return null;
                    }
                    return guiThreadInfo;
                }
                return null;
            }
        }
    }
}
