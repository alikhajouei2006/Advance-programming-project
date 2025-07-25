namespace form1
{
    partial class frmLogin
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lblinfo = new Label();
            txtusername = new TextBox();
            txtpassword = new TextBox();
            lblusername = new Label();
            lblpassword = new Label();
            lnklblForgetPassword = new LinkLabel();
            lblsignin = new LinkLabel();
            btnlogin = new Button();
            SuspendLayout();
            // 
            // lblinfo
            // 
            lblinfo.AutoSize = true;
            lblinfo.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblinfo.Location = new Point(313, 56);
            lblinfo.Name = "lblinfo";
            lblinfo.Size = new Size(160, 23);
            lblinfo.TabIndex = 0;
            lblinfo.Text = "سیسیتم ورود خوابگاه";
            // 
            // txtusername
            // 
            txtusername.Location = new Point(298, 96);
            txtusername.Name = "txtusername";
            txtusername.Size = new Size(187, 27);
            txtusername.TabIndex = 1;
            // 
            // txtpassword
            // 
            txtpassword.Location = new Point(298, 145);
            txtpassword.Name = "txtpassword";
            txtpassword.Size = new Size(187, 27);
            txtpassword.TabIndex = 2;
            txtpassword.UseSystemPasswordChar = true;
            // 
            // lblusername
            // 
            lblusername.AutoSize = true;
            lblusername.Location = new Point(491, 99);
            lblusername.Name = "lblusername";
            lblusername.Size = new Size(74, 20);
            lblusername.TabIndex = 3;
            lblusername.Text = " نام کاربری";
            // 
            // lblpassword
            // 
            lblpassword.AutoSize = true;
            lblpassword.Location = new Point(499, 146);
            lblpassword.Name = "lblpassword";
            lblpassword.Size = new Size(61, 20);
            lblpassword.TabIndex = 4;
            lblpassword.Text = "رمز عبور";
            // 
            // lnklblForgetPassword
            // 
            lnklblForgetPassword.AutoSize = true;
            lnklblForgetPassword.Location = new Point(365, 189);
            lnklblForgetPassword.Name = "lnklblForgetPassword";
            lnklblForgetPassword.Size = new Size(120, 20);
            lnklblForgetPassword.TabIndex = 5;
            lnklblForgetPassword.TabStop = true;
            lnklblForgetPassword.Text = "فراموشی رمز عبور";
            lnklblForgetPassword.LinkClicked += lnklblForgetPassword_LinkClicked;
            // 
            // lblsignin
            // 
            lblsignin.AutoSize = true;
            lblsignin.Location = new Point(304, 189);
            lblsignin.Name = "lblsignin";
            lblsignin.Size = new Size(55, 20);
            lblsignin.TabIndex = 9;
            lblsignin.TabStop = true;
            lblsignin.Text = "ثبت نام";
            lblsignin.LinkClicked += lblsignin_LinkClicked;
            // 
            // btnlogin
            // 
            btnlogin.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnlogin.Location = new Point(400, 225);
            btnlogin.Name = "btnlogin";
            btnlogin.Size = new Size(85, 41);
            btnlogin.TabIndex = 10;
            btnlogin.Text = "ورود";
            btnlogin.UseVisualStyleBackColor = true;
            btnlogin.Click += btnlogin_Click;
            // 
            // frmLogin
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(btnlogin);
            Controls.Add(lblsignin);
            Controls.Add(lnklblForgetPassword);
            Controls.Add(lblpassword);
            Controls.Add(lblusername);
            Controls.Add(txtpassword);
            Controls.Add(txtusername);
            Controls.Add(lblinfo);
            Name = "frmLogin";
            Text = "login";
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblinfo;
        private TextBox txtusername;
        private TextBox txtpassword;
        private Label lblusername;
        private Label lblpassword;
        private LinkLabel lnklblForgetPassword;
        private LinkLabel lblsignin;
        private Button btnlogin;
    }
}
