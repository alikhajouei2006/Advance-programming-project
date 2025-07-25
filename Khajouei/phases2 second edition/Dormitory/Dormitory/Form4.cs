using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dormitory;
using form1;
using form2;
using form3;

namespace form4
{
    public partial class frmgetnewpassword : Form
    {
        private string _username;
        public frmgetnewpassword(string username)
        {
            _username = username;
            InitializeComponent();
        }

        private void btnsubmit_Click(object sender, EventArgs e)
        {
            string res = Program.GetNewPassword(_username, txtnewpass.Text);
            if (res == "success")
            {
                lblvalidnewpass.Text = "رمز عبور جدید با موفقیت ثبت شد.";
                lblvalidnewpass.ForeColor = Color.Green;
                frmLogin frmLogin = new frmLogin();
                frmLogin.ShowDialog();
                this.Close();

            }
            else if (res == "lenerror")
            {
                lblvalidnewpass.Text = "طول رمز عبور باید حداقل 8 کارکتر باشد.";
                Task.Delay(3);
                txtnewpass.Visible = false;
            }

        }

        private void frmgetnewpassword_Load(object sender, EventArgs e)
        {

        }


        private void pictureBox1_Click_1(object sender, EventArgs e)
        {
            this.Hide();
            new frmResetPassword().ShowDialog();
        }
    }
}
