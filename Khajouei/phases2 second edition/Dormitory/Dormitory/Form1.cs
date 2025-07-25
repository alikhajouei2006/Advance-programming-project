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
using form2;
using form3;
using form4;
using Microsoft.Data.Sqlite;

namespace form1
{
    public partial class frmLogin : Form
    {
        public frmLogin()
        {
            InitializeComponent();

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void lnklblForgetPassword_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.Hide();
            frmResetPassword frmresetpass = new frmResetPassword();
            frmresetpass.FormClosed += (s, args) => frmresetpass.Show();
            frmresetpass.ShowDialog();
        }


        private void btnsubmit_Click(object sender, EventArgs e)
        {

        }

        private void lblsignin_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.Hide();
            frmSignIn frmSignIn = new frmSignIn();
            frmSignIn.FormClosed += (s, args) => this.Show();
            frmSignIn.ShowDialog();
        }

        private void btnlogin_Click(object sender, EventArgs e)
        {
            bool accept = Program.Login(txtusername.Text, txtpassword.Text);
            if (accept)
            {
                MessageBox.Show( $" خوش آمدید {txtusername.Text}.");
            }
            else
            {
                MessageBox.Show(".نام کاربری یا رمز عبور اشتباه است","خطا",MessageBoxButtons.OK,MessageBoxIcon.Information);
                
            }
        }
    }
}
