using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;


namespace IIME
{

    internal class Util
    {
        [DllImport("User32.dll")]
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


        [DllImport("User32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("User32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

        [DllImport("User32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("Imm32.dll")]
        private static extern IntPtr ImmGetDefaultIMEWnd(IntPtr unnamedParam1);

        [DllImport("User32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        private static IntPtr GetLeaf(IntPtr hWnd)
        {
            
            IntPtr result;
            do
            {
                result=hWnd;
                hWnd = GetWindow(hWnd, 5);//GW_CHILD

            } while (hWnd!=IntPtr.Zero);
            return result;
        }

        private static uint GetThreadProcessId(IntPtr hWnd)
        {
            return GetWindowThreadProcessId(hWnd, IntPtr.Zero);
        }

        private static bool GetUiInfo(IntPtr hWnd, out GUITHREADINFO Info)
        {
            Info=default(GUITHREADINFO);
            if (hWnd == IntPtr.Zero) return false;

            uint tid = GetThreadProcessId(hWnd);
            bool result = GetGUIThreadInfo(tid, ref Info);
            return result;
        }
        private static IntPtr GetFocus(IntPtr hWnd, bool real)
        {
            if (hWnd != IntPtr.Zero) { hWnd = GetForegroundWindow(); }
            GUITHREADINFO gUITHREADINFO = new GUITHREADINFO();
            gUITHREADINFO.cbSize = Marshal.SizeOf(typeof(GUITHREADINFO));

            bool result = GetUiInfo(hWnd, out gUITHREADINFO);
            if (result)
            {
                if (gUITHREADINFO.hwndFocus != IntPtr.Zero)
                {
                    return gUITHREADINFO.hwndFocus;
                }
                else if (gUITHREADINFO.hwndCaret != IntPtr.Zero && gUITHREADINFO.flags == GuiThreadInfoFlags.GUI_CARETBLINKING)
                {
                    return gUITHREADINFO.hwndCaret;
                }
            }
            if (real) return IntPtr.Zero;
            IntPtr leaf = GetLeaf(hWnd);
            if (leaf != IntPtr.Zero || leaf ==hWnd) return hWnd;

            return IntPtr.Zero;
        }

        private static bool GetOpenStatus(IntPtr hWnd)
        {
            IntPtr result= IntPtr.Zero;
            IntPtr hIme = ImmGetDefaultIMEWnd(hWnd);
            if(hIme != IntPtr.Zero)
            {
                result = SendMessage(hIme, 0x283, 5,0);
            }
            return Convert.ToBoolean(result);
        }
        private static bool GetConversionMode(IntPtr hWnd)
        {
            IntPtr result = IntPtr.Zero;
            IntPtr hIme = ImmGetDefaultIMEWnd(hWnd);
            if (hIme != IntPtr.Zero)
            {
                result = SendMessage(hIme, 0x283, 1, 0);
            }
            return Convert.ToBoolean(result);
        }



        public static void SetOpenStatus(int status)
        {
            IntPtr hWnd = GetForegroundWindow();
            IntPtr handle = ImmGetDefaultIMEWnd(hWnd);
            if (handle.ToInt32() != 0) SendMessage(handle, 0x283, 6, status);
        }

        public static bool CheckImeState()
        {
            bool opened;

            IntPtr hWnd = GetFocus(IntPtr.Zero, false);
            if (hWnd == IntPtr.Zero)return false;
            opened = GetOpenStatus(hWnd);




        }
    }
}
