using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace denali_cmd_to_unit_paisan.MiniForm.ForCMD {
    public partial class FormCMD : Form {
        public FormCMD() {
            InitializeComponent();
        }

        private void FormCMD_Load(object sender, EventArgs e) {

        }

        public void Log(string msg) {
            try
            {
                richTextBox.Invoke(new EventHandler(delegate {
                    richTextBox.SelectedText = string.Empty;
                    richTextBox.AppendText(msg + "\r\n");
                    richTextBox.ScrollToCaret();

                    if (richTextBox.TextLength > 50000)
                    {
                        richTextBox.Text = string.Empty;
                    }
                }));
            } catch (Exception) { }
        }
    }
}
