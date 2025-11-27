using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Velopack;

// dotnet tool install -g vpk
// dotnet build -c Release
// vpk pack -u "ClipboardListener" -v 1.0.1 -p .\bin\Release\net48 -o .\Releases --icon .\corrected_app_icon.ico

namespace ClipboardListener
{
    public partial class Form1 : Form
    {
        // Import Windows API functions to monitor clipboard updates
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        private const int WM_CLIPBOARDUPDATE = 0x031D;

        public Form1()
        {
            InitializeComponent();
            AddClipboardFormatListener(this.Handle);// Add this form as a clipboard format listener

            // Point to a local directory or UNC network share
            var mgr = new UpdateManager(@"c:\Users\per\source\repos\ClipboardListener\ClipboardListener\Releases\");

            if (mgr.CurrentVersion == null)
                return;

            var v = mgr.CurrentVersion.ToString();

            // add version to form text
            this.Text += $" - {v}";

        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            saveButton.Focus(); // set focus when form finishes loading
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            RemoveClipboardFormatListener(this.Handle);
            base.OnFormClosing(e);
        }

        // Handle clipboard update messages
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_CLIPBOARDUPDATE)
            {
                if (Clipboard.ContainsText())
                {
                    string newText = Clipboard.GetText();
                    textBox1.AppendText(newText + Environment.NewLine + Environment.NewLine);
                }
            }
            base.WndProc(ref m);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Let user pick where to save the file
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Markdown Files (*.md)|*.md|Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
                sfd.Title = "Save Clipboard Text";
                sfd.FileName = "clipboard_log";

                // Restore the last used directory if it exists
                if (!string.IsNullOrEmpty(Properties.Settings.Default.LastSaveDirectory))
                {
                    sfd.InitialDirectory = Properties.Settings.Default.LastSaveDirectory;
                }

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(sfd.FileName, textBox1.Text);
                    MessageBox.Show("File saved successfully!", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    textBox1.Clear();

                    // Save the directory for next time
                    Properties.Settings.Default.LastSaveDirectory = Path.GetDirectoryName(sfd.FileName);
                    Properties.Settings.Default.Save();
                }
            }
        }

        // Change the event handler to async and update its signature
        private async void button1_Click_1(object sender, EventArgs e)
        {
            try
            {
                // Point to a local directory or UNC network share
                var mgr = new UpdateManager(@"c:\Users\per\source\repos\ClipboardListener\ClipboardListener\Releases\");
                // Or for a network share:
                // var mgr = new UpdateManager(@"\\server\share\updates");

                // Check for new version
                var newVersion = await mgr.CheckForUpdatesAsync();

                if (newVersion == null)
                {
                    MessageBox.Show("No updates available.", "Update Check", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return; // no update available
                }

                // Let user choose with a messagebox if he wants to update
                var availableVersion = newVersion.TargetFullRelease?.Version.ToString() ?? "unknown";
                var confirm = MessageBox.Show(
                    $"A new version ({availableVersion}) is available. Do you want to download and install it now?",
                    "Update Available",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1);

                if (confirm != DialogResult.Yes)
                {
                    return;
                }

                // Download new version
                await mgr.DownloadUpdatesAsync(newVersion);

                // Install new version and restart app
                mgr.ApplyUpdatesAndRestart(newVersion);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Update failed: {ex.Message}", "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
