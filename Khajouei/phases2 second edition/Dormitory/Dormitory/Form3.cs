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
using form4;

namespace form3
{
    public partial class frmSignIn : Form
    {
        public frmSignIn()
        {
            InitializeComponent();
        }

        private void submitinfo_Click(object sender, EventArgs e)
        {
            Director director = new Director(txtfname.Text + txtfname.Text, txtsocialnumber.Text, txtphonenumber.Text, txtusername.Text, txtpassword.Text);
            if (Program.signin(director))
            {
                lblsiginin.Text = "اطلاعات با موفقیت ثبت شد.";
                Task.Delay(3000);
                frmLogin frmLogin = new frmLogin();
                frmLogin.ShowDialog();
                this.Close();
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            this.Hide();
            frmLogin frmlogin = new frmLogin();
            frmlogin.ShowDialog();
            
        }
    }
}
