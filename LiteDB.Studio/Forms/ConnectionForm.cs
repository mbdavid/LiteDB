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
    public partial class ConnectionForm : Form
    {
        private const long MB = 1024 * 1024;

        public ConnectionString ConnectionString = new ConnectionString();

        public ConnectionForm(ConnectionString cs)
        {
            InitializeComponent();

            radTypeExclusive.Checked = cs.Mode == ConnectionMode.Exclusive;
            radTypeShared.Checked = cs.Mode == ConnectionMode.Shared;
            txtFilename.Text = cs.Filename;
            chkUTC.Checked = cs.UtcDate;
            chkReadonly.Checked = cs.UtcDate;
            txtTimeout.Text = cs.Timeout.TotalSeconds.ToString();
            txtInitialSize.Text = (cs.InitialSize / MB).ToString();
            txtLimitSize.Text = cs.LimitSize == long.MaxValue ? "" : (cs.LimitSize / MB).ToString();
        }

        private void BtnConnect_Click(object sender, EventArgs e)
        {
            this.ConnectionString.Mode = radTypeExclusive.Checked ? ConnectionMode.Exclusive : ConnectionMode.Shared;
            this.ConnectionString.Filename = txtFilename.Text;
            this.ConnectionString.UtcDate = chkUTC.Checked;
            this.ConnectionString.ReadOnly = chkReadonly.Checked;

            if (int.TryParse(txtTimeout.Text, out var timeout))
            {
                this.ConnectionString.Timeout = TimeSpan.FromSeconds(timeout);
            }
            if (int.TryParse(txtInitialSize.Text, out var initialSize))
            {
                this.ConnectionString.InitialSize = initialSize * MB;
            }
            if (int.TryParse(txtLimitSize.Text, out var limitSize))
            {
                this.ConnectionString.LimitSize = limitSize * MB;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnOpen_Click(object sender, EventArgs e)
        {
            openFileDialog.FileName = txtFilename.Text;

            openFileDialog.CheckFileExists = false;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                txtFilename.Text = openFileDialog.FileName;
            }
        }
    }
}
