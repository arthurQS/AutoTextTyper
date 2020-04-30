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
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern int ShowWindow(IntPtr hWnd, uint Msg);
        #endregion

        private int sleepAmount;
        private int value;
        private int speed;

        //this is a constant indicating the window that we want to send a text message
        const int WM_SETTEXT = 0X000C;
        //this is a constant indicating the window that we want to restore from a minimized state
        private const uint SW_RESTORE = 0x09;

        private bool CancelExecution = false;
        private int sleep;

        public frmAutoTextTyper()
        {
            InitializeComponent();
            
            // When window state changed, trigger state update.
            this.Resize += SetMinimizeState;

            // When tray icon clicked, trigger window state change.       
            notifyIcon1.Click += ToggleMinimizeState;
        }
        // Toggle state between Normal and Minimized.
        private void ToggleMinimizeState(object sender, EventArgs e)
        {
            bool isMinimized = this.WindowState == FormWindowState.Minimized;
            this.WindowState = (isMinimized) ? FormWindowState.Normal : FormWindowState.Minimized;
        }

        // Show/Hide window and tray icon to match window state.
        private void SetMinimizeState(object sender, EventArgs e)
        {
            bool isMinimized = this.WindowState == FormWindowState.Minimized;

            this.ShowInTaskbar = !isMinimized;
            notifyIcon1.Visible = isMinimized;
            if (isMinimized) notifyIcon1.ShowBalloonTip(500, "Auto Text", "Application minimized to tray.", ToolTipIcon.Info);
        }

        private void Btn_Add_Click(object sender, EventArgs e)
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

        private void Btn_SendText_Click(object sender, EventArgs e)
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
                BtnRefresh_Click(this, null);
                return;
            }
            
            //Bring our target if is minimized
            ShowWindow(p.MainWindowHandle, SW_RESTORE);
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

                        // Replace the special keys and send the content
                        string t = Regex.Replace(c.ToString(), "[+^%~(){}]", "{$0}");
                        SendKeys.SendWait(t.ToString());

                        // Sleep some time between key presses to simulate human behavior
                        sleep = 100 + random.Next(0, 50);
                        if (Char.IsWhiteSpace(c)) sleep += 100 + random.Next(0, 75); // Sleep a bit more for spaces
                        sleepAmount = sleep - speed;
                        Thread.Sleep(sleepAmount);

                        //check if the target Pid is in foreground or minimized
                        var activatedHandle = GetForegroundWindow();
                        if (activatedHandle != p.MainWindowHandle)
                        {
                            DialogResult result1 = MessageBox.Show("Your main window is not selected, do you want to continue typing?",
                                "Interruption", MessageBoxButtons.YesNo);
                            if (result1 == DialogResult.Yes)
                            {
                                //Bring our target if is minimized
                                ShowWindow(p.MainWindowHandle, SW_RESTORE);
                                // Bring back the target window foreground
                                SetForegroundWindow(p.MainWindowHandle);
                                if (p.HasExited)
                                {
                                    MessageBox.Show("The targe application was closed", "Ops...", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    CancelExecution = true;
                                    btnCancel.Enabled = false;
                                }
                                continue;
                            }
                            else
                            {
                                CancelExecution = true;
                                btnCancel.Enabled = false;
                                break;
                            } 
                        }
                    }

                    // Disable the text box with the text that was just sent
                    tb.Enabled = false;

                    // Disable Cancel Button
                    btnCancel.Enabled = false;
                    break;
                }
            }
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
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

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            // Set the flag to cancel the execution and disable the button
            CancelExecution = true;
            btnCancel.Enabled = false;
        }

        private void TxtProcessName_TextChanged(object sender, EventArgs e)
        {

        }

        private void TextBox1_TextChanged(object sender, EventArgs e)
        {
            bool success = Int32.TryParse(textBox1.Text, out value);
            if (success == true)
            {
                speed = value;
            }
            else
            {
                MessageBox.Show("Only numbers 1 to 1000", "Ops...", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void setSpeed_Click(object sender, EventArgs e)
        {
            sleepAmount = speed;
        }

        private void flowLP_Paint(object sender, PaintEventArgs e)
        {

        }

        private void frmAutoTextTyper_Load(object sender, EventArgs e)
        {

        }
    }
}
