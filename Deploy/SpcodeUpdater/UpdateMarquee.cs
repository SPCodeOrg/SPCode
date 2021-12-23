using System;
using System.Windows.Forms;

namespace SPCodeUpdater
{
    public partial class UpdateMarquee : Form
    {
        public UpdateMarquee()
        {
            InitializeComponent();
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