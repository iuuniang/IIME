using KeePass.Plugins;
using KeePass.Util;
using KeePass.Util.Spr;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;




namespace IIME
{
    public sealed class IIMEExt : Plugin
    {
        private const string UPDATEURL = "https://raw.githubusercontent.com/iuuniang/IIME/main/update.txt";

        private const string m_strPlaceholderTrue = "{IME:ON}";
        private const string m_strPlaceholderFalse = "{IME:OFF}";

        private IPluginHost m_host = null;

        public override bool Initialize(IPluginHost host)
        {
            m_host = host;
            AutoType.FilterCompilePre += this.OnAutoTypeFilterCompilePre;
            SprEngine.FilterPlaceholderHints.Add(m_strPlaceholderTrue);
            SprEngine.FilterPlaceholderHints.Add(m_strPlaceholderFalse);

            return true;
        }

        public override void Terminate()
        {
            SprEngine.FilterPlaceholderHints.Remove(m_strPlaceholderTrue);
            SprEngine.FilterPlaceholderHints.Remove(m_strPlaceholderFalse);
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
            Regex replacerTrue = new Regex(Regex.Escape(m_strPlaceholderTrue), RegexOptions.IgnoreCase);
            Regex replacerFalse = new Regex(Regex.Escape(m_strPlaceholderFalse), RegexOptions.IgnoreCase);

            autoTypeEventArgs.Sequence = replacerTrue.Replace(autoTypeEventArgs.Sequence, match =>
            {

                SetOpenStatus(1);
                return String.Empty;
            });

            autoTypeEventArgs.Sequence = replacerFalse.Replace(autoTypeEventArgs.Sequence, match =>
            {
                SetOpenStatus(0);
                return String.Empty;
            });
        }


        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("Imm32.dll")]
        private static extern IntPtr ImmGetDefaultIMEWnd(IntPtr unnamedParam1);

        [DllImport("user32.dll ")]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);


        private void SetOpenStatus(int status)
        {
            IntPtr hWnd = GetForegroundWindow();
            IntPtr handle = ImmGetDefaultIMEWnd(hWnd);
            if(handle.ToInt32() != 0 ) SendMessage(handle, 0x283, 6, status);
        }
    }
}
