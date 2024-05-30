using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Timers;
using System.Windows.Forms;

namespace denali_cmd_to_unit_paisan {
    public partial class Form1 : Form {
        public Form1() {
            InitializeComponent();
        }

        private MiniForm.ForCMD.FormCMD formCMD = new MiniForm.ForCMD.FormCMD();
        private SerialPort mySerialPort = new SerialPort();
        private string port_name = "COM5";
        private bool flag_open_form = false;
        private string AT_MODE3 = "AT+MODE=3";
        private string AT_MODE = "AT+MODE?";
        private string AT_ICCID = "AT+ICCID";
        private string AT_VERSION = "AT+VERSION=HARDWARE";
        private string AT_IMEI = "AT+IMEI";
        private string AT_TEMP = "AT+UTEST=TEMP";
        private void Form1_Load(object sender, EventArgs e) {
            comboBox2.SelectedIndex = 0;
            lb_status.Text = "";
            string comport_old = "";
            try { comport_old = File.ReadAllText("../../config/denali_cmd_to_unit_paisan_uart.txt"); } catch { }
            try { checkBox1.Checked = Convert.ToBoolean(File.ReadAllText("../../config/denali_cmd_to_unit_paisan_auto.txt")); } catch { }
            ManagementObjectSearcher objOSDetails2 = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption like '%(COM%'");
            ManagementObjectCollection osDetailsCollection2 = objOSDetails2.Get();
            foreach (ManagementObject usblist in osDetailsCollection2) {
                string[] arrport = usblist.GetPropertyValue("NAME").ToString().Split('(', ')');
                comboBox1.Items.Add(arrport[1]);
            }
            if (comboBox1.Items.Contains(comport_old)) {
                comboBox1.SelectedIndex = comboBox1.Items.IndexOf(comport_old);
            } else comboBox1.SelectedIndex = 0;
            port_name = comboBox1.Text;

            mySerialPort.PortName = port_name;
            mySerialPort.BaudRate = 9600;
            mySerialPort.DataBits = 8;
            mySerialPort.StopBits = StopBits.One;
            mySerialPort.Parity = Parity.None;
            mySerialPort.Handshake = Handshake.None;
            mySerialPort.RtsEnable = true;
            mySerialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            Stopwatch t = new Stopwatch();
            t.Restart();
            while (t.ElapsedMilliseconds < 2000) {
                try {
                    mySerialPort.Open();
                    t.Stop();
                    break;
                } catch {
                    DelaymS(250);
                }
                try { mySerialPort.Close(); } catch { }
            }
            if (t.IsRunning) {
                try { mySerialPort.Close(); } catch { }
                MessageBox.Show("_ไม่สามารถเปิด port " + port_name + "ได้");
                return;
            }
            flag_open_form = true;
            Application.Idle += Application_Idle;
        }

        private static List<string> rx_ = new List<string>();
        private static List<int> rx_hex = new List<int>();
        private static bool flag_data = false;
        private static bool flag_32768k = false;
        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e) {
            DelaymS(50);
            if (flag_32768k) {
                int length = 0;
                mySerialPort = (SerialPort)sender;
                try { length = mySerialPort.BytesToRead; } catch { return; }
                int buf = 0;
                for (int i = 0; i < length; i++) {
                    buf = mySerialPort.ReadByte();
                    rx_hex.Add(buf);
                }
                mySerialPort.DiscardInBuffer();
                mySerialPort.DiscardOutBuffer();
                flag_data = true;
            } else {
                string s = mySerialPort.ReadExisting();
                string[] ss = s.Replace("\r", "").Split('\n');
                for (int i = 0; i < ss.Length; i++) {
                    if (ss[i] == "") continue;
                    rx_.Add(ss[i]);
                }
                mySerialPort.DiscardInBuffer();
                mySerialPort.DiscardOutBuffer();
                flag_data = true;
            }
        }
        public static void DelaymS(int mS) {
            Stopwatch stopwatchDelaymS = new Stopwatch();
            stopwatchDelaymS.Restart();
            while (mS > stopwatchDelaymS.ElapsedMilliseconds) {
                if (!stopwatchDelaymS.IsRunning) stopwatchDelaymS.Start();
                Application.DoEvents();
            }
            stopwatchDelaymS.Stop();
        }

        private void button1_Click(object sender, EventArgs e) {
            flag_run = false;
            DelaymS(250);
            progressBar1.Value = 0;
            TextBox nn = new TextBox();
            send_cmd("AT+MODE=2", nn);
            DelaymS(3000);
            flag_32768k = true;
            send_cmd_hex("0x21", "0x00");//0A 0D 46 54 4D DA 0D
            DelaymS(1000);
            send_cmd_hex("0x58", "0x01");
            if (progressBar1.Value == 2) button1.BackColor = Color.Blue;
            else button1.BackColor = Color.Red;
            flag_32768k = false;
            flag_run = true;
        }
        private void button2_Click(object sender, EventArgs e) {
            send_cmd(AT_ICCID, textBox1);
        }
        private void button3_Click(object sender, EventArgs e) {
            send_cmd(AT_VERSION, textBox2);
        }
        private void button4_Click(object sender, EventArgs e) {
            send_cmd(AT_IMEI, textBox3);
        }
        private void bt_temp_Click(object sender, EventArgs e) {
            send_cmd(AT_TEMP, tb_temp);
        }
        private void send_cmd(string cmd, TextBox tb) {
            flag_run = false;
            DelaymS(250);
            mySerialPort.DiscardInBuffer();
            mySerialPort.DiscardOutBuffer();
            rx_.Clear();
            flag_data = false;
            formCMD.Log($"Write : {cmd}");
            mySerialPort.Write(cmd + "\r\n");
            Stopwatch t = new Stopwatch();
            t.Restart();
            while (t.ElapsedMilliseconds < 1500) {
                if (flag_data != true) { DelaymS(100); continue; }
                flag_data = false;
                t.Stop();
                break;
            }
            if (t.IsRunning) return;
            t.Restart();
            while (t.ElapsedMilliseconds < 10000) {
                if (rx_[rx_.Count - 1] != "OK" && rx_[rx_.Count - 1] != "ERROR") { DelaymS(100); continue; }
                break;
            }
            t.Stop();
            foreach (string readCmd in rx_)
            {
                formCMD.Log($"Read : {readCmd}");
            }
            if (rx_.Count > 1) {
                tb.Text = rx_[rx_.Count - 2];
                if(cmd == AT_ICCID) button2.BackColor = Color.Blue;
                else if(cmd == AT_VERSION) button3.BackColor = Color.Blue;
                else if (cmd == AT_IMEI) button4.BackColor = Color.Blue;
                else if (cmd == AT_TEMP)
                {
                    CheckTemp(tb);
                    bt_temp.BackColor = Color.Blue;
                }
            } else {
                tb.Text = rx_[0];
                if (cmd == AT_ICCID) button2.BackColor = Color.Blue;
                else if (cmd == AT_VERSION) button3.BackColor = Color.Blue;
                else if (cmd == AT_IMEI) button4.BackColor = Color.Blue;
                else if (cmd == AT_TEMP)
                {
                    CheckTemp(tb);
                    bt_temp.BackColor = Color.Blue;
                }
            }
            flag_run = true;
        }
        private void send_cmd_auto(string cmd, TextBox tb) {
            if (!flag_run) return;
            mySerialPort.DiscardInBuffer();
            mySerialPort.DiscardOutBuffer();
            rx_.Clear();
            flag_data = false;
            formCMD.Log($"Write : {cmd}");
            mySerialPort.Write(cmd + "\r\n");
            Stopwatch t = new Stopwatch();
            t.Restart();
            while (t.ElapsedMilliseconds < 3000) {
                if (flag_data != true) { DelaymS(100); continue; }
                flag_data = false;
                t.Stop();
                break;
            }
            if (t.IsRunning) return;
            t.Restart();
            while (t.ElapsedMilliseconds < 10000) {
                if (!flag_run) return;
                if (rx_[rx_.Count - 1] != "OK" && rx_[rx_.Count - 1] != "ERROR") { DelaymS(100); continue; }
                break;
            }
            t.Stop();
            foreach (string readCmd in rx_)
            {
                formCMD.Log($"Read : {readCmd}");
            }
            if (rx_.Count > 1) {
                tb.Text = rx_[rx_.Count - 2];
                if (cmd == AT_ICCID) button2.BackColor = Color.Blue;
                else if (cmd == AT_VERSION) button3.BackColor = Color.Blue;
                else if (cmd == AT_IMEI) button4.BackColor = Color.Blue;
                else if (cmd == AT_TEMP)
                {
                    CheckTemp(tb);
                    bt_temp.BackColor = Color.Blue;
                }
            } else {
                tb.Text = rx_[0];
                if (cmd == AT_ICCID) button2.BackColor = Color.Blue;
                else if (cmd == AT_VERSION) button3.BackColor = Color.Blue;
                else if (cmd == AT_IMEI) button4.BackColor = Color.Blue;
                else if (cmd == AT_TEMP)
                {
                    CheckTemp(tb);
                    bt_temp.BackColor = Color.Blue;
                }
            }
        }
        private void send_cmd_hex(string tx, string rx) {
            byte dataa = Convert.ToByte(tx.Substring(2, 2), 16);
            byte[] data = { 0xAA };
            data[0] = dataa;
            mySerialPort.DiscardInBuffer();
            mySerialPort.DiscardOutBuffer();
            rx_hex.Clear();
            flag_data = false;
            try { mySerialPort.Write(data, 0, 1); } catch { return; }
            Stopwatch t = new Stopwatch();
            t.Restart();
            while (t.ElapsedMilliseconds < 3000) {
                if (flag_data != true) { DelaymS(100); continue; }
                flag_data = false;
                t.Stop();
                break;
            }
            if (t.IsRunning) return;
            if(rx_hex.Count == 1) {
                byte rx_byte;
                rx_byte = Convert.ToByte(rx.Substring(2, 2), 16);
                if (rx_byte == rx_hex[0]) progressBar1.Value++;
            }else {
                string str_result = "";
                foreach (int i in rx_hex) {
                    if (i == 10 || i == 13) continue;
                    str_result += Convert.ToChar(i).ToString();
                }
                if(str_result.Contains("FTM")) progressBar1.Value++;
            }
        }
        private void send_cmd_rx_tx(string cmd, TextBox tb) {
            mySerialPort.DiscardInBuffer();
            mySerialPort.DiscardOutBuffer();
            rx_.Clear();
            flag_data = false;
            mySerialPort.Write(cmd + "\r\n");
            Stopwatch t = new Stopwatch();
            t.Restart();
            while (t.ElapsedMilliseconds < 1500) {
                if (flag_data != true) { DelaymS(100); continue; }
                flag_data = false;
                t.Stop();
                break;
            }
            if (t.IsRunning) return;
            if (rx_.Count > 1) tb.Text = rx_[rx_.Count - 2];
            else tb.Text = rx_[0];
        }

        private bool flag_run = true;
        private void Application_Idle(object sender, EventArgs e) {
            if (!flag_run) return;
            mySerialPort.DiscardInBuffer();
            mySerialPort.DiscardOutBuffer();
            rx_.Clear();
            flag_data = false;
            mySerialPort.Write("AT\r\n");
            Stopwatch t = new Stopwatch();
            t.Restart();
            while (t.ElapsedMilliseconds < 2000) {
                if (flag_data != true) { DelaymS(100); continue; }
                flag_data = false;
                t.Stop();
                break;
            }
            if (t.IsRunning) { disconnect(); return; }
            t.Restart();
            while (t.ElapsedMilliseconds < 2000) {
                if (rx_[rx_.Count - 1] != "OK" && rx_[rx_.Count - 1] != "ERROR") { DelaymS(100); continue; }
                break;
            }
            t.Stop();
            if (rx_.Count != 2) { disconnect(); return; }
            if (rx_[1] != "OK") { disconnect(); return; }
            lb_connect.ForeColor = Color.Lime;
            button1.Enabled = true;
            button2.Enabled = true;
            button3.Enabled = true;
            button4.Enabled = true;
            bt_temp.Enabled = true;
            if (checkBox1.Checked) {
                send_cmd_auto(AT_ICCID, textBox1);
                send_cmd_auto(AT_VERSION, textBox2);
                send_cmd_auto(AT_IMEI, textBox3);
                send_cmd_auto(AT_MODE3, tb_sup);
                send_cmd_auto(AT_MODE, tb_sup);
                send_cmd_auto(AT_TEMP, tb_temp);
            }
            DelaymS(2000);
        }
        private void disconnect() {
            lb_connect.ForeColor = Color.Red;
            button1.Enabled = false;
            button2.Enabled = false;
            button2.BackColor = Color.Red;
            button3.Enabled = false;
            button3.BackColor = Color.Red;
            button4.Enabled = false;
            button4.BackColor = Color.Red;
            bt_temp.Enabled= false;
            bt_temp.BackColor = Color.Red;
            textBox1.Text = "";
            textBox2.Text = "";
            textBox3.Text = "";
            tb_temp.Text = "";
            progressBar1.Value = 0;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e) {
            if (!flag_open_form) return;
            port_name = comboBox1.Text;
            disconnect();
            Stopwatch t = new Stopwatch();
            t.Restart();
            while (t.ElapsedMilliseconds < 2000) {
                try {
                    mySerialPort.Open();
                    t.Stop();
                    break;
                } catch {
                    DelaymS(250);
                }
                try { mySerialPort.Close(); } catch { }
            }
            if (t.IsRunning) {
                try { mySerialPort.Close(); } catch { }
                MessageBox.Show("_ไม่สามารถเปิด port " + port_name + "ได้");
                return;
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e) {
            File.WriteAllText("../../config/denali_cmd_to_unit_paisan_uart.txt", port_name);
        }

        private void textBox1_DoubleClick(object sender, EventArgs e) {
            textBox1.Clear();
        }
        private void textBox2_DoubleClick(object sender, EventArgs e) {
            textBox2.Clear();
        }
        private void textBox3_DoubleClick(object sender, EventArgs e) {
            textBox3.Clear();
        }

        private void checkBox1_Click(object sender, EventArgs e) {
            File.WriteAllText("../../config/denali_cmd_to_unit_paisan_auto.txt", checkBox1.Checked.ToString());
        }

        private void lb_connect_DoubleClick(object sender, EventArgs e) {
            send_cmd_rx_tx("TEST", textBox1);
        }

        private string HARDWARE = "1";
        private string AT_SAVE_HARDWARE = "AT+VERSION=HARDWARE,";
        private string AT_UN_HARDWARE = "AT+VERSION=HARDWARE,UNSAFE,";
        private string AT_GET_HARDWARE = "AT+VERSION=HARDWARE";
        private void ComboBoxSelect_Denali() {
            switch (comboBox2.Text) {
                case "FG-62T245-LF":
                    label2.Text = "TEMPTALE GEO / VIZCOMM VIEW-LTE (Original)";
                    HARDWARE = "1";
                    label3.Text = "HARDWARE = " + HARDWARE;
                    break;
                case "FG-63T305-LF":
                    label2.Text = "TEMPTALE GEO / VIZCOMM VIEW-LTE";
                    HARDWARE = "3";
                    label3.Text = "HARDWARE = " + HARDWARE;
                    break;
                case "FG-63T306-LF":
                    label2.Text = "TEMPTALE GEO / VIZCOMM VIEW-ULTRA";
                    HARDWARE = "5";
                    label3.Text = "HARDWARE = " + HARDWARE;
                    break;
                case "FG-63T307-LF":
                    label2.Text = "TEMPTALE GEO / ULTRA DRY ICE";
                    HARDWARE = "7";
                    label3.Text = "HARDWARE = " + HARDWARE;
                    break;
                case "FG-63T334-LF":
                    label2.Text = "TEMPTALE GEO / VIZCOMM VIEW-LTE EXTENDED";
                    HARDWARE = "2";
                    label3.Text = "HARDWARE = " + HARDWARE;
                    break;
                case "FG-63T335-LF":
                    label2.Text = "TEMPTALE GEO / VIZCOMM VIEW-ULTRA EXTENDED";
                    HARDWARE = "4";
                    label3.Text = "HARDWARE = " + HARDWARE;
                    break;
                case "FG-63T336-LF":
                    label2.Text = "TEMPTALE GEO / ULTRA DRY ICE EXTENDED";
                    HARDWARE = "6";
                    label3.Text = "HARDWARE = " + HARDWARE;
                    break;
                case "FG-64T151-LF":
                    label2.Text = "TEMPTALE GEO / VIZCOMM VIEW-LTE_BG95-M3";
                    HARDWARE = "3";
                    label3.Text = "HARDWARE = " + HARDWARE;
                    break;
                case "FG-64T152-LF":
                    label2.Text = "TEMPTALE GEO / VIZCOMM VIEW-LTE EXTENDED_BG95-M3";
                    HARDWARE = "2";
                    label3.Text = "HARDWARE = " + HARDWARE;
                    break;
            }
        }
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e) {
            ComboBoxSelect_Denali();
        }
        private void send_cmd_hardware(string cmd, TextBox tb) {
            flag_run = false;
            DelaymS(250);
            mySerialPort.DiscardInBuffer();
            mySerialPort.DiscardOutBuffer();
            rx_.Clear();
            flag_data = false;
            mySerialPort.Write(cmd + "\r\n");
            Stopwatch t = new Stopwatch();
            t.Restart();
            while (t.ElapsedMilliseconds < 1500) {
                if (flag_data != true) { DelaymS(100); continue; }
                flag_data = false;
                t.Stop();
                break;
            }
            if (t.IsRunning) return;
            t.Restart();
            while (t.ElapsedMilliseconds < 10000) {
                if (rx_[rx_.Count - 1] != "OK" && rx_[rx_.Count - 1] != "ERROR") { DelaymS(100); continue; }
                break;
            }
            t.Stop();
            if (rx_.Count > 1) {
                tb.Text = rx_[rx_.Count - 2];
            } else {
                tb.Text = rx_[0];
            }
            lb_status.Text = "OK";
            flag_run = true;
        }

        private void button5_Click(object sender, EventArgs e) {
            lb_status.Text = "";
            DelaymS(1000);
            send_cmd_hardware(AT_SAVE_HARDWARE + HARDWARE, tb_sup);
        }
        private void button7_Click(object sender, EventArgs e) {
            lb_status.Text = "";
            DelaymS(1000);
            send_cmd_hardware(AT_GET_HARDWARE, tb_sup);
        }
        private void button6_Click(object sender, EventArgs e) {
            lb_status.Text = "";
            DelaymS(1000);
            send_cmd_hardware(AT_UN_HARDWARE + HARDWARE, tb_sup);
        }

        private void CheckTemp(TextBox textBox) {
            double mid = 41;
            double min = mid - 0.9;
            double max = mid + 0.9;

            double valueF = Cal.TempF(textBox.Text);
            double valueC = Cal.TempC(textBox.Text);
            formCMD.Log($"Celsius : {valueC}");
            formCMD.Log($"Fahrenheit : {valueF}");

            if (valueF >= min & valueF <= max)
            {
                textBox.Text = $"PASS : {valueF}";
                //textBox.ForeColor = Color.Green;
            }
            else
            {
                textBox.Text = $"FAIL : {valueF}";
                //textBox.ForeColor = Color.Red;
            }
        }

        private void bt_showCMD_Click(object sender, EventArgs e) {
            formCMD.Show();
        }
    }
}
