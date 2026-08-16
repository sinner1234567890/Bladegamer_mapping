using System;
using System.IO;
using System.IO.Ports;
using System.IO.Compression;
using System.Net;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace BladegamerMapping
{
    public class MainForm : Form
    {
        private PictureBox picController;
        private ComboBox comboPorts;
        private Button btnRefreshPorts;
        private Button btnConnect;
        private Button btnSave;
        private Button btnInitialFlash;
        private TextBox txtLog;
        private Panel pnlMap;
        private ToolTip toolTipHelp;

        private TextBox txtUp, txtDown, txtLeft, txtRight;
        private TextBox txtBtn1, txtBtn2, txtBtn3, txtBtn4, txtBtn5, txtBtn6;
        private ComboBox comboMulti;

        private SerialPort serialPort;

        private const string ARDUINO_CLI_URL = "https://downloads.arduino.cc/arduino-cli/arduino-cli_latest_Windows_64bit.zip";
        private string cliPath;
        private string codePath;

        private const string GITHUB_REPO = "YOUR_USERNAME/YOUR_REPO"; // e.g. "bladegamer123/BladegamerSoftware"
        private const string CURRENT_VERSION = "V14";
        
        public MainForm()
        {
            cliPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "arduino-cli.exe");
            codePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "flash_code.cpp");
            
            InitializeComponent();
            
            // Check for updates in the background
            var updateTask = CheckForUpdatesAsync();
        }

        private TextBox txtTestArea;

        private void InitializeComponent()
        {
            this.Text = "Bladegamer Controller Mapping " + CURRENT_VERSION;
            this.Size = new Size(820, 700);
            this.BackColor = Color.FromArgb(240, 240, 240); // Light Theme
            this.ForeColor = Color.Black;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.FormClosing += new FormClosingEventHandler(MainForm_FormClosing);
            
            // Set the custom Icon
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
            if (File.Exists(iconPath))
            {
                try { this.Icon = new Icon(iconPath); } catch { }
            }

            // Image
            picController = new PictureBox();
            picController.Location = new Point(20, 20);
            picController.Size = new Size(400, 400);
            picController.SizeMode = PictureBoxSizeMode.Zoom;
            
            string imgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "con.png");
            if (File.Exists(imgPath))
            {
                picController.Image = Image.FromFile(imgPath);
            }
            this.Controls.Add(picController);

            // Mappings Panel
            pnlMap = new Panel();
            pnlMap.Location = new Point(440, 20);
            pnlMap.Size = new Size(340, 320);
            this.Controls.Add(pnlMap);

            int y = 0;
            Label lblTitle = new Label();
            lblTitle.Text = "Live Button Mappings";
            lblTitle.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblTitle.Location = new Point(0, y);
            lblTitle.Size = new Size(300, 30);
            pnlMap.Controls.Add(lblTitle);
            y += 35;
            
            Label lblHelp = new Label();
            lblHelp.Text = "Press a physical button to highlight its box!";
            lblHelp.Font = new Font("Segoe UI", 9, FontStyle.Italic);
            lblHelp.Location = new Point(0, y);
            lblHelp.Size = new Size(300, 20);
            lblHelp.ForeColor = Color.DimGray;
            pnlMap.Controls.Add(lblHelp);
            y += 25;

            y = AddField("Joystick Up:", out txtUp, y, "w", "txtUp");
            y = AddField("Joystick Down:", out txtDown, y, "s", "txtDown");
            y = AddField("Joystick Left:", out txtLeft, y, "a", "txtLeft");
            y = AddField("Joystick Right:", out txtRight, y, "d", "txtRight") + 5;

            // Add Button TextBoxes overlaying the image
            txtBtn2 = AddImageTextBox("Btn 2", "t", 180, 32, "txtBtn2");
            txtBtn3 = AddImageTextBox("Btn 3", "f", 295, 32, "txtBtn3");
            txtBtn1 = AddImageTextBox("Btn 1", "r", 130, 70, "txtBtn1");
            txtBtn4 = AddImageTextBox("Btn 4", "c", 340, 197, "txtBtn4");
            txtBtn5 = AddImageTextBox("Btn 5", " ", 340, 262, "txtBtn5");
            txtBtn6 = AddImageTextBox("Btn 6", "g", 335, 310, "txtBtn6");
            
            LoadLayout();
            
            y += 10;

            Label lblMulti = new Label();
            lblMulti.Text = "Counting Button:";
            lblMulti.Location = new Point(0, y + 3);
            lblMulti.Size = new Size(130, 20);
            lblMulti.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            pnlMap.Controls.Add(lblMulti);

            comboMulti = new ComboBox();
            comboMulti.Location = new Point(135, y);
            comboMulti.Size = new Size(90, 25);
            comboMulti.DropDownStyle = ComboBoxStyle.DropDownList;
            comboMulti.Items.AddRange(new string[] { "None", "Button 1", "Button 2", "Button 3", "Button 4", "Button 5", "Button 6" });
            comboMulti.SelectedIndex = 2; // Default to Button 2
            pnlMap.Controls.Add(comboMulti);
            y += 35;

            // Ports & Connection
            y = 350;
            Label lblPort = new Label();
            lblPort.Text = "Port:";
            lblPort.Location = new Point(440, y + 3);
            lblPort.Size = new Size(40, 20);
            lblPort.Font = new Font("Segoe UI", 10);
            this.Controls.Add(lblPort);

            comboPorts = new ComboBox();
            comboPorts.Location = new Point(480, y);
            comboPorts.Size = new Size(80, 25);
            comboPorts.DropDownStyle = ComboBoxStyle.DropDownList;
            this.Controls.Add(comboPorts);

            btnRefreshPorts = new Button();
            btnRefreshPorts.Text = "\u21BB"; 
            btnRefreshPorts.Location = new Point(565, y - 1);
            btnRefreshPorts.Size = new Size(30, 26);
            btnRefreshPorts.FlatStyle = FlatStyle.Flat;
            btnRefreshPorts.Click += new EventHandler(this.BtnRefreshPorts_Click);
            this.Controls.Add(btnRefreshPorts);

            btnConnect = new Button();
            btnConnect.Text = "CONNECT";
            btnConnect.Location = new Point(600, y - 1);
            btnConnect.Size = new Size(160, 26);
            btnConnect.FlatStyle = FlatStyle.Flat;
            btnConnect.BackColor = Color.LightGreen;
            btnConnect.Click += new EventHandler(this.BtnConnect_Click);
            this.Controls.Add(btnConnect);
            y += 35;

            btnSave = new Button();
            btnSave.Text = "SAVE MAPPINGS";
            btnSave.Location = new Point(440, y);
            btnSave.Size = new Size(320, 35);
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.BackColor = Color.FromArgb(0, 122, 204);
            btnSave.ForeColor = Color.White;
            btnSave.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            btnSave.Click += new EventHandler(this.BtnSave_Click);
            this.Controls.Add(btnSave);
            y += 45;

            btnInitialFlash = new Button();
            btnInitialFlash.Text = "Initial Setup / Flash Firmware";
            btnInitialFlash.Location = new Point(440, y);
            btnInitialFlash.Size = new Size(320, 25);
            btnInitialFlash.FlatStyle = FlatStyle.Flat;
            btnInitialFlash.BackColor = Color.White;
            btnInitialFlash.Click += new EventHandler(this.BtnInitialFlash_Click);
            this.Controls.Add(btnInitialFlash);

            // Test Area
            Label lblTest = new Label();
            lblTest.Text = "Test Area (Click here and press buttons):";
            lblTest.Location = new Point(20, 430);
            lblTest.Size = new Size(300, 20);
            lblTest.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            this.Controls.Add(lblTest);

            txtTestArea = new TextBox();
            txtTestArea.Location = new Point(20, 455);
            txtTestArea.Size = new Size(740, 50);
            txtTestArea.Multiline = true;
            txtTestArea.BackColor = Color.White;
            txtTestArea.Font = new Font("Segoe UI", 12);
            this.Controls.Add(txtTestArea);

            // Log
            txtLog = new TextBox();
            txtLog.Location = new Point(20, 520);
            txtLog.Size = new Size(740, 110);
            txtLog.Multiline = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.ReadOnly = true;
            txtLog.BackColor = Color.White;
            txtLog.Font = new Font("Consolas", 9);
            this.Controls.Add(txtLog);

            // ToolTips Setup
            toolTipHelp = new ToolTip();
            toolTipHelp.AutoPopDelay = 10000;
            toolTipHelp.InitialDelay = 500;
            toolTipHelp.ReshowDelay = 500;
            toolTipHelp.ShowAlways = true;

            toolTipHelp.SetToolTip(btnConnect, "Connect to the controller to read and write live mappings.");
            toolTipHelp.SetToolTip(btnSave, "Instantly save the current letters in the text boxes into the controller's memory.");
            toolTipHelp.SetToolTip(btnInitialFlash, "WARNING: Click this ONLY ONCE when setting up a brand new controller to install the core Bladegamer firmware.");
            toolTipHelp.SetToolTip(comboPorts, "Select the USB COM Port your controller is plugged into. The software usually auto-detects this.");
            toolTipHelp.SetToolTip(btnRefreshPorts, "Click to scan for newly plugged in controllers.");
            toolTipHelp.SetToolTip(comboMulti, "Select which button will act as the Counting Button (press to type 1, 2, 3... 10). The selected button will ignore its regular letter mapping.");
            toolTipHelp.SetToolTip(txtTestArea, "Click inside this box and press buttons on your physical controller to see what keyboard keys they are currently outputting.");
            toolTipHelp.SetToolTip(picController, "You can drag the Button text boxes around on this image to point exactly to how your controller is physically wired.");

            RefreshPorts();
        }

        private int AddField(string labelText, out TextBox txt, int currentY, string defaultVal, string name)
        {
            Label lbl = new Label();
            lbl.Text = labelText;
            lbl.Location = new Point(0, currentY + 3);
            lbl.Size = new Size(130, 20);
            lbl.Font = new Font("Segoe UI", 10);

            txt = new TextBox();
            txt.Name = name;
            txt.Location = new Point(135, currentY);
            txt.Size = new Size(40, 25);
            txt.Font = new Font("Segoe UI", 10);
            txt.Text = defaultVal;
            txt.MaxLength = 1;
            txt.BackColor = Color.White;
            txt.ForeColor = Color.Black;
            txt.BorderStyle = BorderStyle.FixedSingle;
            txt.TextAlign = HorizontalAlignment.Center;
            
            if (toolTipHelp != null) {
                toolTipHelp.SetToolTip(txt, "Type the keyboard letter you want this Joystick direction to trigger.");
            }

            pnlMap.Controls.Add(lbl);
            pnlMap.Controls.Add(txt);

            return currentY + 25;
        }

        private TextBox AddImageTextBox(string btnName, string defaultVal, int x, int y, string name)
        {
            TextBox txt = new TextBox();
            txt.Name = name;
            txt.Location = new Point(x, y);
            txt.Size = new Size(30, 20);
            txt.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            txt.Text = defaultVal;
            txt.MaxLength = 1;
            txt.BackColor = Color.White;
            txt.ForeColor = Color.Black;
            txt.BorderStyle = BorderStyle.FixedSingle;
            txt.TextAlign = HorizontalAlignment.Center;
            
            Label lbl = new Label();
            lbl.Text = btnName;
            lbl.Size = new Size(40, 15);
            lbl.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            lbl.ForeColor = Color.Black;
            lbl.BackColor = Color.Transparent;
            lbl.TextAlign = ContentAlignment.BottomCenter;
            
            // Make the label follow the text box whenever it moves
            txt.LocationChanged += (s, e) => {
                lbl.Location = new Point(txt.Left - 5, txt.Top - 15);
            };
            
            if (toolTipHelp != null) {
                string tip = "Type the keyboard letter you want this button to trigger.\nYou can click and drag this box around if your button is wired differently!";
                toolTipHelp.SetToolTip(txt, tip);
                toolTipHelp.SetToolTip(lbl, tip);
            }
            
            // Drag and drop handlers
            txt.MouseDown += Txt_MouseDown;
            txt.MouseMove += Txt_MouseMove;
            txt.MouseUp += Txt_MouseUp;
            
            // Add to main form but bring to front
            this.Controls.Add(lbl);
            lbl.BringToFront();
            
            this.Controls.Add(txt);
            txt.BringToFront();
            
            // Trigger location change to set initial label position
            txt.Location = new Point(x, y);
            
            return txt;
        }

        private bool isDragging = false;
        private Point dragCursorPoint;
        private Point dragFormPoint;

        private void Txt_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Only start drag if holding Ctrl? No, just drag if moving.
                isDragging = true;
                dragCursorPoint = Cursor.Position;
                dragFormPoint = ((Control)sender).Location;
                ((Control)sender).Cursor = Cursors.SizeAll;
            }
        }

        private void Txt_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point dif = Point.Subtract(Cursor.Position, new Size(dragCursorPoint));
                ((Control)sender).Location = Point.Add(dragFormPoint, new Size(dif));
            }
        }

        private void Txt_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = false;
                ((Control)sender).Cursor = Cursors.IBeam;
            }
        }

        private void LoadLayout()
        {
            string file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "layout.txt");
            if (File.Exists(file))
            {
                try
                {
                    string[] lines = File.ReadAllLines(file);
                    foreach (string line in lines)
                    {
                        string[] parts = line.Split('=');
                        if (parts.Length == 2)
                        {
                            string key = parts[0];
                            string val = parts[1];
                            
                            if (key.EndsWith("_Text"))
                            {
                                string name = key.Replace("_Text", "");
                                Control[] found = this.Controls.Find(name, true);
                                if (found.Length > 0) found[0].Text = val;
                            }
                            else if (key == "comboMulti_SelectedIndex")
                            {
                                int idx;
                                if (int.TryParse(val, out idx) && idx >= 0 && idx < comboMulti.Items.Count)
                                    comboMulti.SelectedIndex = idx;
                            }
                            else if (key == "comboPorts_SelectedItem")
                            {
                                if (comboPorts.Items.Contains(val))
                                    comboPorts.SelectedItem = val;
                            }
                            else
                            {
                                string[] coords = val.Split(',');
                                if (coords.Length == 2)
                                {
                                    int x = int.Parse(coords[0]);
                                    int y = int.Parse(coords[1]);
                                    
                                    Control[] found = this.Controls.Find(key, true);
                                    if (found.Length > 0)
                                    {
                                        found[0].Location = new Point(x, y);
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }
            }
        }

        private void SaveLayout()
        {
            string file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "layout.txt");
            try
            {
                using (StreamWriter sw = new StreamWriter(file, false))
                {
                    TextBox[] boxes = new TextBox[] { txtBtn1, txtBtn2, txtBtn3, txtBtn4, txtBtn5, txtBtn6, txtUp, txtDown, txtLeft, txtRight };
                    foreach (TextBox tb in boxes)
                    {
                        if (tb != null && !string.IsNullOrEmpty(tb.Name))
                        {
                            sw.WriteLine(tb.Name + "_Text=" + tb.Text);
                            if (tb.Name.StartsWith("txtBtn"))
                            {
                                sw.WriteLine(tb.Name + "=" + tb.Location.X + "," + tb.Location.Y);
                            }
                        }
                    }
                    sw.WriteLine("comboMulti_SelectedIndex=" + comboMulti.SelectedIndex);
                    if (comboPorts.SelectedItem != null)
                    {
                        sw.WriteLine("comboPorts_SelectedItem=" + comboPorts.SelectedItem.ToString());
                    }
                }
            }
            catch { }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveLayout();
            DisconnectSerial();
        }

        private void BtnRefreshPorts_Click(object sender, EventArgs e)
        {
            RefreshPorts();
        }

        private void RefreshPorts()
        {
            comboPorts.Items.Clear();
            try
            {
                using (var searcher = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption like '%(COM%'"))
                {
                    foreach (var queryObj in searcher.Get())
                    {
                        string caption = queryObj["Caption"] != null ? queryObj["Caption"].ToString() : "";
                        if (!string.IsNullOrEmpty(caption))
                        {
                            var m = Regex.Match(caption, @"\((COM\d+)\)");
                            if (m.Success)
                            {
                                string portName = m.Groups[1].Value;
                                comboPorts.Items.Add(portName);
                                if (caption.Contains("Arduino") || caption.Contains("Leonardo") || caption.Contains("Micro"))
                                {
                                    comboPorts.SelectedItem = portName;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("WMI error: " + ex.Message);
            }

            // Fallback
            if (comboPorts.Items.Count == 0)
            {
                string[] ports = SerialPort.GetPortNames();
                foreach (string p in ports) comboPorts.Items.Add(p);
            }
            
            if (comboPorts.Items.Count > 0 && comboPorts.SelectedIndex == -1) 
                comboPorts.SelectedIndex = 0;
                
            Log("Ports refreshed.");
        }

        private string GetSelectedPortName()
        {
            if (comboPorts.SelectedItem == null) return null;
            string text = comboPorts.SelectedItem.ToString();
            var m = Regex.Match(text, @"\((COM\d+)\)");
            if (m.Success) return m.Groups[1].Value;
            
            var m2 = Regex.Match(text, @"COM\d+");
            if (m2.Success) return m2.Value;
            
            return text;
        }

        private void Log(string msg)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action<string>(Log), new object[] { msg });
                return;
            }
            txtLog.AppendText(string.Format("[{0}] {1}\r\n", DateTime.Now.ToString("HH:mm:ss"), msg));
        }

        private void BtnConnect_Click(object sender, EventArgs e)
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                DisconnectSerial();
            }
            else
            {
                ConnectSerial();
            }
        }

        private void ConnectSerial()
        {
            if (comboPorts.SelectedItem == null)
            {
                MessageBox.Show("Please select a COM port first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string port = GetSelectedPortName();
            try
            {
                serialPort = new SerialPort(port, 115200);
                serialPort.DtrEnable = true;
                serialPort.RtsEnable = true;
                serialPort.DataReceived += new SerialDataReceivedEventHandler(SerialDataReceived);
                serialPort.Open();
                
                // Allow Leonardo to reset/initialize
                Thread.Sleep(500);
                
                btnConnect.Text = "DISCONNECT";
                btnConnect.BackColor = Color.Salmon;
                Log("Connected to " + port);

                // Ask for current mappings
                serialPort.WriteLine("GETMAP");
            }
            catch (Exception ex)
            {
                Log("Connection error: " + ex.Message);
            }
        }

        private void DisconnectSerial()
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                try { serialPort.Close(); } catch { }
            }
            btnConnect.Text = "CONNECT";
            btnConnect.BackColor = Color.LightGreen;
            Log("Disconnected.");
        }

        private void SerialDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string line = serialPort.ReadLine().Trim();
                this.Invoke(new Action<string>(ProcessSerialMessage), new object[] { line });
            }
            catch { }
        }

        private void ProcessSerialMessage(string msg)
        {
            if (msg.StartsWith("MAP:"))
            {
                string[] parts = msg.Substring(4).Split(',');
                if (parts.Length == 11)
                {
                    txtUp.Text = parts[0];
                    txtDown.Text = parts[1];
                    txtLeft.Text = parts[2];
                    txtRight.Text = parts[3];
                    txtBtn1.Text = parts[4];
                    txtBtn3.Text = parts[5];
                    txtBtn4.Text = parts[6];
                    txtBtn5.Text = parts[7];
                    txtBtn6.Text = parts[8];
                    txtBtn2.Text = parts[9];
                    
                    int mIdx;
                    if (int.TryParse(parts[10], out mIdx) && mIdx >= 0 && mIdx <= 6)
                    {
                        comboMulti.SelectedIndex = mIdx;
                    }

                    Log("Loaded mappings from controller.");
                }
            }
            else if (msg.StartsWith("BTN:"))
            {
                string btn = msg.Substring(4);
                HighlightButton(btn);
            }
            else if (msg == "OK")
            {
                Log("Mappings saved successfully to controller!");
            }
        }

        private void HighlightButton(string btn)
        {
            // Reset all
            TextBox[] boxes = new TextBox[] { txtUp, txtDown, txtLeft, txtRight, txtBtn1, txtBtn2, txtBtn3, txtBtn4, txtBtn5, txtBtn6 };
            foreach (TextBox tb in boxes) tb.BackColor = Color.White;

            TextBox toHighlight = null;
            if (btn == "UP") toHighlight = txtUp;
            else if (btn == "DOWN") toHighlight = txtDown;
            else if (btn == "LEFT") toHighlight = txtLeft;
            else if (btn == "RIGHT") toHighlight = txtRight;
            else if (btn == "1") toHighlight = txtBtn1;
            else if (btn == "2") toHighlight = txtBtn2;
            else if (btn == "3") toHighlight = txtBtn3;
            else if (btn == "4") toHighlight = txtBtn4;
            else if (btn == "5") toHighlight = txtBtn5;
            else if (btn == "6") toHighlight = txtBtn6;

            if (toHighlight != null)
            {
                toHighlight.BackColor = Color.LightGreen;
                
                if (txtTestArea.Focused) {
                    // Do not steal focus if they are actively in the Test Area
                } else {
                    toHighlight.Focus();
                    toHighlight.SelectAll();
                }

                Task.Delay(300).ContinueWith(t => 
                {
                    if (this.IsHandleCreated) {
                        this.Invoke(new Action(() => { toHighlight.BackColor = Color.White; }));
                    }
                });
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (serialPort == null || !serialPort.IsOpen)
            {
                MessageBox.Show("Please CONNECT to the controller first to save mappings instantly.", "Not Connected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            string cmd = string.Format("SETMAP:{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}", 
                EnsureChar(txtUp.Text), EnsureChar(txtDown.Text), EnsureChar(txtLeft.Text), EnsureChar(txtRight.Text),
                EnsureChar(txtBtn1.Text), EnsureChar(txtBtn3.Text), EnsureChar(txtBtn4.Text), EnsureChar(txtBtn5.Text), EnsureChar(txtBtn6.Text),
                EnsureChar(txtBtn2.Text), comboMulti.SelectedIndex);
            
            serialPort.WriteLine(cmd);
            Log("Saving mappings to EEPROM...");
        }

        private string EnsureChar(string input)
        {
            if (string.IsNullOrEmpty(input)) return " ";
            return input.Substring(0, 1);
        }

        private async void BtnInitialFlash_Click(object sender, EventArgs e)
        {
            if (comboPorts.SelectedItem == null)
            {
                MessageBox.Show("Please select a COM port first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (serialPort != null && serialPort.IsOpen)
            {
                DisconnectSerial();
            }

            string port = GetSelectedPortName();
            btnInitialFlash.Enabled = false;

            try
            {
                await EnsureArduinoCli();
                await RunCliCommand("core install arduino:avr");
                
                Log("Installing Keyboard library...");
                await RunCliCommand("lib install Keyboard");
                
                Log("Compiling firmware (Original Flash)... This takes a moment.");
                await RunCliCommand("compile --fqbn arduino:avr:leonardo .");

                Log(string.Format("Uploading firmware to {0}...", port));
                await RunCliCommand(string.Format("upload -p {0} --fqbn arduino:avr:leonardo .", port));

                Log("Original Flashing complete! Now click CONNECT to use Live Mapping.");
                MessageBox.Show("Original Firmware installed successfully! You can now CONNECT and use Instant Saving.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log("ERROR: " + ex.Message);
                MessageBox.Show("Flashing failed.\n\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnInitialFlash.Enabled = true;
            }
        }

        private async Task EnsureArduinoCli()
        {
            if (File.Exists(cliPath)) return;

            Log("arduino-cli not found. Downloading...");
            string zipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "arduino-cli.zip");
            
            using (WebClient wc = new WebClient())
            {
                await wc.DownloadFileTaskAsync(new Uri(ARDUINO_CLI_URL), zipPath);
            }

            Log("Extracting arduino-cli...");
            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.FullName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        entry.ExtractToFile(cliPath, true);
                    }
                }
            }
            File.Delete(zipPath);
            Log("arduino-cli ready.");
        }

        private async Task RunCliCommand(string args)
        {
            await Task.Run(new Action(() =>
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = cliPath;
                psi.Arguments = args;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;

                using (Process proc = Process.Start(psi))
                {
                    proc.OutputDataReceived += new DataReceivedEventHandler((s, e) => { if (!string.IsNullOrEmpty(e.Data)) Log("[CLI] " + e.Data); });
                    proc.ErrorDataReceived += new DataReceivedEventHandler((s, e) => { if (!string.IsNullOrEmpty(e.Data)) Log("[CLI ERR] " + e.Data); });
                    
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();
                    
                    proc.WaitForExit();
                    if (proc.ExitCode != 0)
                    {
                        throw new Exception("arduino-cli exited with code " + proc.ExitCode);
                    }
                }
            }));
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
        private async Task CheckForUpdatesAsync()
        {
            if (GITHUB_REPO == "YOUR_USERNAME/YOUR_REPO") return; // Placeholder not updated yet

            try
            {
                using (WebClient wc = new WebClient())
                {
                    wc.Headers.Add("User-Agent", "BladegamerGUI-Updater");
                    string url = "https://api.github.com/repos/" + GITHUB_REPO + "/releases/latest";
                    string json = await wc.DownloadStringTaskAsync(url);

                    var matchTag = Regex.Match(json, @"""tag_name""\s*:\s*""([^""]+)""");
                    if (matchTag.Success)
                    {
                        string latestVersion = matchTag.Groups[1].Value;
                        if (latestVersion != CURRENT_VERSION)
                        {
                            var matchAsset = Regex.Match(json, @"""browser_download_url""\s*:\s*""([^""]+\.exe)""");
                            if (matchAsset.Success)
                            {
                                string downloadUrl = matchAsset.Groups[1].Value;
                                
                                this.Invoke((Action)(() =>
                                {
                                    DialogResult res = MessageBox.Show(
                                        "A new update (" + latestVersion + ") is available on GitHub!\nWould you like to download it now?",
                                        "Update Available", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                                        
                                    if (res == DialogResult.Yes)
                                    {
                                        DownloadUpdate(downloadUrl, latestVersion);
                                    }
                                }));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Update check failed: " + ex.Message);
            }
        }

        private async void DownloadUpdate(string url, string version)
        {
            string newFileName = "BladegamerGUI_" + version + ".exe";
            string destPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, newFileName);
            
            try
            {
                Log("Downloading update " + version + "...");
                using (WebClient wc = new WebClient())
                {
                    await wc.DownloadFileTaskAsync(new Uri(url), destPath);
                }
                
                MessageBox.Show("Update downloaded successfully as: " + newFileName + "\n\nThe application will now close so you can run the new version.", "Update Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Application.Exit();
            }
            catch (Exception ex)
            {
                Log("Download failed: " + ex.Message);
                MessageBox.Show("Download failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
