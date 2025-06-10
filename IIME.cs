using KeePass.Plugins;
using KeePass.Util;
using KeePass.Util.Spr;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;


namespace IIME
{
    public sealed class IIMEExt : Plugin
    {
        private const string UPDATEURL = "https://raw.githubusercontent.com/iuuniang/IIME/main/update.txt";

        private const string m_strPlaceholderEnglish = "IME:EN";
        private const string m_strPlaceholderChinese = "IME:CN";

        private IPluginHost m_host = null;

        private bool _imeStatus = false;

        public override bool Initialize(IPluginHost host)
        {
            m_host = host;
            AutoType.FilterCompilePre += this.OnAutoTypeFilterCompilePre;
            SprEngine.FilterPlaceholderHints.Add(string.Format("{{{0}}}", m_strPlaceholderEnglish));
            SprEngine.FilterPlaceholderHints.Add(string.Format("{{{0}}}", m_strPlaceholderChinese));

            return true;
        }

        public override void Terminate()
        {
            SprEngine.FilterPlaceholderHints.Remove(string.Format("{{{0}}}", m_strPlaceholderEnglish));
            SprEngine.FilterPlaceholderHints.Remove(string.Format("{{{0}}}", m_strPlaceholderChinese));
            AutoType.FilterCompilePre -= this.OnAutoTypeFilterCompilePre;

        }

        public override string UpdateUrl
        {
            get { return UPDATEURL; }
        }

        public override Image SmallIcon
        {
            get { return (Image)KeePass.Program.Resources.GetObject("B16x16_KTouch"); }
        }

        private void OnAutoTypeFilterCompilePre(object sender, AutoTypeEventArgs autoTypeEventArgs)
        {
            Regex replacerEnglish = new Regex(string.Format(@"{{{0}(?:@([\d\s]+))?}}", m_strPlaceholderEnglish), RegexOptions.IgnoreCase);
            Regex replacerChinese = new Regex(string.Format(@"{{{0}(?:@([\d\s]+))?}}", m_strPlaceholderChinese), RegexOptions.IgnoreCase);

            Func<Match, string> keyComboHandler = match =>
            {
                _imeStatus = InputMethodController.GetIMEStatus();

                if (_imeStatus)
                {
                    // 如果有参数（@后面的部分）
                    if (match.Groups[1].Success)
                    {
                        string[] keys = match.Groups[1].Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        if (keys.Length == 1)
                        {
                            // 单个按键：{VKEY X}
                            return string.Format("{{VKEY {0}}}", keys[0]);
                        }
                        else if (keys.Length == 2)
                        {
                            // 双键组合：{VKEY X D}{VKEY Y}{VKEY X U}
                            return string.Format("{{VKEY {0} D}}{{VKEY {1}}}{{VKEY {0} U}}", keys[0], keys[1]);
                        }
                        else if (keys.Length >= 3)
                        {
                            // 多键组合处理（3键或更多）
                            StringBuilder sb = new StringBuilder();

                            // 按下所有修饰键（除了最后一个）
                            for (int i = 0; i < keys.Length - 1; i++)
                            {
                                sb.AppendFormat("{{VKEY {0} D}}", keys[i]);
                            }

                            // 按下并释放主键（最后一个）
                            sb.AppendFormat("{{VKEY {0}}}", keys[keys.Length - 1]);

                            // 释放所有修饰键（反向顺序）
                            for (int i = keys.Length - 2; i >= 0; i--)
                            {
                                sb.AppendFormat("{{VKEY {0} U}}", keys[i]);
                            }

                            return sb.ToString();
                        }
                    }

                    // 默认行为（无参数时使用默认值16）
                    return string.Format("{{VKEY {0}}}", match.Groups[1].Success ? match.Groups[1].Value : "16");
                }
                else
                {
                    _imeStatus = true;
                    return String.Empty;
                }
            };

            autoTypeEventArgs.Sequence = replacerEnglish.Replace(autoTypeEventArgs.Sequence, match => keyComboHandler(match));
            autoTypeEventArgs.Sequence = replacerChinese.Replace(autoTypeEventArgs.Sequence, match => keyComboHandler(match));
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
                return SetOpenStatus(status) == IntPtr.Zero;
            }

            public static bool GetIMEStatus(IntPtr hWnd = default(IntPtr))
            {
                if (hWnd == IntPtr.Zero)
                {
                    hWnd = GetForegroundWindow();
                    hWnd = GetFocus(hWnd,true) ?? hWnd;
                }
                bool opened = GetOpenStatus(hWnd);
                int? convMode = GetConversionMode(hWnd);
                ushort langId = GetCurrentLangIdByHwnd(hWnd);

                if (opened && langId == 0x409) {return false;}
                if (convMode == null) { return false;}
                if ((convMode & IME_CMODE_NOCONVERSION) != 0 ) {return false;}
                return opened && ((convMode & IME_CMODE_LANGUAGE) != 0);
            }
            private static IntPtr? SetOpenStatus(uint status,IntPtr hWnd = default(IntPtr))
            {
                if (hWnd == IntPtr.Zero)
                {
                    hWnd = GetForegroundWindow();
                }
                return ImeControl(hWnd, IMC_SETOPENSTATUS, status);
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

            private static int? GetConversionMode(IntPtr hWnd)
            {
                if (hWnd == IntPtr.Zero)
                {
                    hWnd = GetForegroundWindow();
                }
                IntPtr? result = ImeControl(hWnd, IMC_GETCONVERSIONMODE);
                if (result != null)
                {
                    return result.Value.ToInt32();
                }
                else
                {
                    return null;
                }
            }

            private static bool GetOpenStatus(IntPtr hWnd)
            {
                if (hWnd == IntPtr.Zero)
                {
                    hWnd = GetForegroundWindow();
                }
                return ImeControl(hWnd, IMC_GETOPENSTATUS) != IntPtr.Zero; //返回值 0:英文 1:中文
            }


            private static IntPtr? ImeControl(IntPtr hWnd = default(IntPtr), uint command = 0,uint data = 0)
            {
                if (hWnd == IntPtr.Zero)
                {
                    hWnd = GetForegroundWindow();
                }
                IntPtr hIMEWnd = ImmGetDefaultIMEWnd(hWnd);
                if (hIMEWnd != IntPtr.Zero)
                {
                    return SendMessage(hIMEWnd, WM_IME_CONTROL,(IntPtr)command, (IntPtr)data);
                }
                return null;
            }

            private static IntPtr? GetFocus(IntPtr hWnd,bool real=false)
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
                if (real) { return null;}
                IntPtr leafHwnd = GetLeafWindow(hWnd);
                return (leafHwnd == IntPtr.Zero || leafHwnd == hWnd) ? hWnd : leafHwnd;

            }
            private static IntPtr GetLeafWindow(IntPtr hWnd)
            {
                if (hWnd == IntPtr.Zero) {return IntPtr.Zero;}
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
                    uint threadID = GetWindowThreadProcessId(hWnd,IntPtr.Zero);
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
