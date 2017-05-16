using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;

namespace AutoTextType
{
    public partial class frmAutoTextTyper : Form
    {
        #region Imports
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        //this is a constant indicating the window that we want to send a text message
        const int WM_SETTEXT = 0X000C;
        #endregion

        private bool CancelExecution = false;

        public frmAutoTextTyper()
        {
            InitializeComponent();
        }

        private void btn_Add_Click(object sender, EventArgs e)
        {
            TextBox newTextbox = new TextBox();
            newTextbox.Multiline = true;
            newTextbox.ScrollBars = ScrollBars.Vertical;
            newTextbox.Font = new Font("Consolas", 10);
            newTextbox.Width = flowLP.Width - 30;
            newTextbox.Height = 100;

            flowLP.Controls.Add(newTextbox);
        }

        private void FlowLPSizeChanged(object sender, EventArgs e)
        {
            foreach (TextBox tb in flowLP.Controls)
            {
                tb.Width = flowLP.Width - 30;
            }
        }

        private void btn_SendText_Click(object sender, EventArgs e)
        {
            // Only proceed if we have a selected process.
            if (listBox1.SelectedIndex == -1)
                return;

            // Get the target process and focus the main window
            string[] split = listBox1.SelectedItem.ToString().Split(')');
            int pid = Convert.ToInt32(split[0]);
            Process p;
            try
            {
                p = Process.GetProcessById(pid);
            }
            catch (ArgumentException exception)
            {
                btnRefresh_Click(this, null);
                return;
            }

            // Bring our target window to foreground
            SetForegroundWindow(p.MainWindowHandle);

            // Prepare support variables and components
            Random random = new Random();
            CancelExecution = false;
            btnCancel.Enabled = true;

            foreach (TextBox tb in flowLP.Controls)
            {
                if (tb.Enabled == true)
                {
                    // Process it character by character
                    string text = tb.Text;
                    foreach (char c in text)
                    {
                        // Check if the cancel button was pressed
                        if (CancelExecution)
                            return;

                        // Bring the target window foreground
                        SetForegroundWindow(p.MainWindowHandle);

                        // Replace the special keys and send the content
                        string t = Regex.Replace(c.ToString(), "[+^%~(){}]", "{$0}");
                        SendKeys.SendWait(t.ToString());

                        // Sleep some time between key presses to simulate human behavior
                        int SleepAmount = 100 + random.Next(0, 50);
                        if (Char.IsWhiteSpace(c)) SleepAmount += 100 + random.Next(0, 75); // Sleep a bit more for spaces
                        Thread.Sleep(SleepAmount);
                    }

                    // Disable the text box with the text that was just sent
                    tb.Enabled = false;

                    // Disable Cancel Button
                    btnCancel.Enabled = false;
                    break;
                }
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            // Start empty
            listBox1.Items.Clear();

            // Get all the processes running
            Process[] processes = Process.GetProcesses();

            // Filter processes by name and add them to the list
            foreach (Process p in processes)
            {
                if (p.MainWindowTitle.ToLower().Contains(txtProcessName.Text.ToLower()) && !p.MainWindowTitle.Trim().Equals(""))
                {
                    listBox1.Items.Add(p.Id + ") " + p.MainWindowTitle);
                }
            }

            // If we found at least one process, select the first one
            if (listBox1.Items.Count > 0)
                listBox1.SetSelected(0, true);
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            // Set the flag to cancel the execution and disable the button
            CancelExecution = true;
            btnCancel.Enabled = false;
        }
    }
}
