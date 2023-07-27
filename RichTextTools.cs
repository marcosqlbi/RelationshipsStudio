using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using RelationshipsStudio.Tools;

namespace RelationshipsStudio
{
    public static partial class RichTextTools
    {
        private const int WM_USER = 0x0400;
        private const int EM_SETEVENTMASK = (WM_USER + 69);
        private const int WM_SETREDRAW = 0x0b;

        //[LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
        //private static partial IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        public static void WriteRichText(this RichTextBox richTextbox, string text, bool append=false)
        {
            FontStyle currentStyle = richTextbox.Font.Style;
            BeginUpdate();
            try
            {
                if (!append) richTextbox.Clear();

                int position = 0;
                int textLength = text.Length;
                while (position < textLength)
                {
                    int startIndex = text.IndexOf('{', position);
                    int endIndex = text.IndexOf('}', position);

                    if (endIndex < 0 || startIndex < 0 || startIndex > endIndex)
                    {
                        richTextbox.AppendText(text[position..]);
                        break;
                    }
                    else
                    {
                        richTextbox.AppendText(text[position..startIndex]);
                    }

                    string directive = text.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();
                    // call your function here with the value argument
                    try
                    {
                        bool inversion = (directive.Length > 0 && directive[0] == '!');
                        if (inversion) directive = directive[1..];

                        richTextbox.SelectionStart = richTextbox.Text.Length;
                        richTextbox.SelectionLength = 0;
                        switch (directive.ToLower())
                        {
                            case "reset":
                                ApplyFontStyle(FontStyle.Regular, inversion, reset: true);
                                richTextbox.SelectionBackColor = richTextbox.BackColor;
                                richTextbox.SelectionColor = richTextbox.ForeColor;
                                break;
                            case "bold":
                                ApplyFontStyle(FontStyle.Bold, inversion);
                                break;
                            case "italic":
                                ApplyFontStyle(FontStyle.Italic, inversion);
                                break;
                            case "ul":
                                ApplyFontStyle(FontStyle.Underline, inversion);
                                break;
                            case "strike":
                                ApplyFontStyle(FontStyle.Strikeout, inversion);
                                break;
                            case "fg":
                                richTextbox.SelectionColor = richTextbox.ForeColor;
                                break;
                            case "bg":
                                richTextbox.SelectionBackColor = richTextbox.BackColor;
                                break;
                            default:
                                var color = ColorTranslator.FromHtml(directive);
                                if (inversion) richTextbox.SelectionBackColor = color;
                                else richTextbox.SelectionColor = color;
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Message);
                    }

                    position = endIndex + 1;
                }
            }
            finally
            {
                EndUpdate();
            }
            // Refresh needed to update scrollbars
            richTextbox.Refresh();

            void ApplyFontStyle(FontStyle applyStyle, bool inversion, bool reset = false)
            {
                currentStyle = reset ? richTextbox.Font.Style : (inversion)
                    ? currentStyle & ~applyStyle
                    : currentStyle |= applyStyle;
                var newFont = new Font(richTextbox.Font.FontFamily, richTextbox.Font.Size, currentStyle);
                richTextbox.SelectionFont = newFont;
            }

            IntPtr OldEventMask;

            void BeginUpdate()
            {
                NativeMethods.SendMessage(richTextbox.Handle, WM_SETREDRAW, 0, IntPtr.Zero);
                OldEventMask = (IntPtr)NativeMethods.SendMessage(richTextbox.Handle, EM_SETEVENTMASK, 0, IntPtr.Zero);
            }

            void EndUpdate()
            {
                NativeMethods.SendMessage(richTextbox.Handle, WM_SETREDRAW, 1, IntPtr.Zero);
                NativeMethods.SendMessage(richTextbox.Handle, EM_SETEVENTMASK, 0, OldEventMask);
            }

        }

    }
}
