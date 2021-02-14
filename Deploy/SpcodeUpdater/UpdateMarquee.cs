using System;
using System.Windows.Forms;
using SPCodeUpdater.Properties;

namespace SPCodeUpdater
{
    public partial class UpdateMarquee : Form
    {
        public UpdateMarquee()
        {
            InitializeComponent();
            var bmp = Resources.IconPng;
            pictureBox1.Image = bmp;
        }

        public void SetToReadyState()
        {
            label1.Text = "SPCode got updated!";
            progressBar1.Visible = false;
            button1.Visible = true;
            UseWaitCursor = false;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}