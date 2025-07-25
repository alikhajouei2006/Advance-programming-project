using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using form1;
using form3;
using form4;
using Dormitory;


namespace form2
{
    public partial class frmResetPassword : Form
    {
        public frmResetPassword()
        {
            InitializeComponent();
        }

        private void btnsubmitusernamesocialnumber_Click(object sender, EventArgs e)
        {
            bool validData = Program.ResetPassword(txtUserName.Text, txtSocialNumber.Text, txtphonenumber.Text);
            if (validData)
            {
                lblValiData.Text = "تایید شد.";
                Task.Delay(3000);
                this.Hide();
                frmgetnewpassword frmgetnewpassword = new frmgetnewpassword(txtUserName.Text);
                frmgetnewpassword.FormClosed += (s, args) => this.Show();
                frmgetnewpassword.ShowDialog();
            }
            else
            {
                lblValiData.Text = "نام کاربری یا کد ملی یا شماره تلفن اشتباه است.";
                Task.Delay(3000);
                txtUserName.Visible = false; txtSocialNumber.Visible = false; txtphonenumber.Visible = false;
            }
        }

        private void frmResetPassword_Load(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            this.Hide();
            new frmLogin().ShowDialog();
        }
    }
}
