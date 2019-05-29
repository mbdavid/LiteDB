using LiteDB.Engine;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LiteDB.Studio.Forms
{
    public partial class ShrinkForm : Form
    {
        private readonly string _filename;
        private readonly string _password;
        private readonly Action _disconnect;

        public string NewPassword { get; set; }

        public ShrinkForm(string filename, string password, Action disconnect)
        {
            _filename = filename;
            _password = password;
            _disconnect = disconnect;

            InitializeComponent();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.NewPassword = txtPassword.Text.Trim().Length > 0 ? txtPassword.Text.Trim() : null;

            _disconnect();

            var size = LiteEngine.Shrink(_filename, _password, this.NewPassword);

            MessageBox.Show($"File reduction: {(size / 1024).ToString("n0")} Kb", "Shrink", MessageBoxButtons.OK, MessageBoxIcon.Information);

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
