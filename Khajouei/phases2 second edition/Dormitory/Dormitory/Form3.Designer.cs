namespace form3
{
    partial class frmSignIn
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lblsignin = new Label();
            txtfname = new TextBox();
            lblname = new Label();
            txtlastname = new TextBox();
            lbllastname = new Label();
            txtsocialnumber = new TextBox();
            lblsocialnumber = new Label();
            txtphonenumber = new TextBox();
            lblhponenumber = new Label();
            lblusername = new Label();
            txtusername = new TextBox();
            txtpassword = new TextBox();
            lblpassword = new Label();
            submitinfo = new Button();
            lblsiginin = new Label();
            pictureBox1 = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // lblsignin
            // 
            lblsignin.AutoSize = true;
            lblsignin.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblsignin.Location = new Point(304, 44);
            lblsignin.Name = "lblsignin";
            lblsignin.Size = new Size(174, 23);
            lblsignin.TabIndex = 0;
            lblsignin.Text = "سیستم ثبت نام خوابگاه";
            // 
            // txtfname
            // 
            txtfname.Location = new Point(298, 86);
            txtfname.Name = "txtfname";
            txtfname.Size = new Size(180, 27);
            txtfname.TabIndex = 1;
            // 
            // lblname
            // 
            lblname.AutoSize = true;
            lblname.Location = new Point(487, 89);
            lblname.Name = "lblname";
            lblname.Size = new Size(27, 20);
            lblname.TabIndex = 2;
            lblname.Text = "نام";
            // 
            // txtlastname
            // 
            txtlastname.Location = new Point(298, 130);
            txtlastname.Name = "txtlastname";
            txtlastname.Size = new Size(180, 27);
            txtlastname.TabIndex = 3;
            // 
            // lbllastname
            // 
            lbllastname.AutoSize = true;
            lbllastname.Location = new Point(484, 133);
            lbllastname.Name = "lbllastname";
            lbllastname.Size = new Size(90, 20);
            lbllastname.TabIndex = 4;
            lbllastname.Text = "نام خانوادگی";
            // 
            // txtsocialnumber
            // 
            txtsocialnumber.Location = new Point(298, 176);
            txtsocialnumber.Name = "txtsocialnumber";
            txtsocialnumber.Size = new Size(180, 27);
            txtsocialnumber.TabIndex = 5;
            // 
            // lblsocialnumber
            // 
            lblsocialnumber.AutoSize = true;
            lblsocialnumber.Location = new Point(484, 180);
            lblsocialnumber.Name = "lblsocialnumber";
            lblsocialnumber.Size = new Size(56, 20);
            lblsocialnumber.TabIndex = 6;
            lblsocialnumber.Text = "کد ملی";
            // 
            // txtphonenumber
            // 
            txtphonenumber.Location = new Point(298, 224);
            txtphonenumber.Name = "txtphonenumber";
            txtphonenumber.Size = new Size(180, 27);
            txtphonenumber.TabIndex = 7;
            // 
            // lblhponenumber
            // 
            lblhponenumber.AutoSize = true;
            lblhponenumber.Location = new Point(484, 227);
            lblhponenumber.Name = "lblhponenumber";
            lblhponenumber.Size = new Size(80, 20);
            lblhponenumber.TabIndex = 8;
            lblhponenumber.Text = "شماره تلفن";
            // 
            // lblusername
            // 
            lblusername.AutoSize = true;
            lblusername.Location = new Point(484, 275);
            lblusername.Name = "lblusername";
            lblusername.Size = new Size(70, 20);
            lblusername.TabIndex = 9;
            lblusername.Text = "نام کاربری";
            // 
            // txtusername
            // 
            txtusername.Location = new Point(298, 272);
            txtusername.Name = "txtusername";
            txtusername.Size = new Size(180, 27);
            txtusername.TabIndex = 10;
            // 
            // txtpassword
            // 
            txtpassword.Location = new Point(298, 321);
            txtpassword.Name = "txtpassword";
            txtpassword.Size = new Size(180, 27);
            txtpassword.TabIndex = 11;
            txtpassword.UseSystemPasswordChar = true;
            // 
            // lblpassword
            // 
            lblpassword.AutoSize = true;
            lblpassword.Location = new Point(484, 325);
            lblpassword.Name = "lblpassword";
            lblpassword.Size = new Size(61, 20);
            lblpassword.TabIndex = 12;
            lblpassword.Text = "رمز عبور";
            // 
            // submitinfo
            // 
            submitinfo.Location = new Point(373, 370);
            submitinfo.Name = "submitinfo";
            submitinfo.Size = new Size(105, 41);
            submitinfo.TabIndex = 13;
            submitinfo.Text = "ثبت اطلاعات";
            submitinfo.UseVisualStyleBackColor = true;
            submitinfo.Click += submitinfo_Click;
            // 
            // lblsiginin
            // 
            lblsiginin.AutoSize = true;
            lblsiginin.Font = new Font("Segoe UI", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblsiginin.Location = new Point(97, 180);
            lblsiginin.Name = "lblsiginin";
            lblsiginin.Size = new Size(0, 41);
            lblsiginin.TabIndex = 14;
            // 
            // pictureBox1
            // 
            pictureBox1.Image = Dormitory.Properties.Resources._3114883;
            pictureBox1.Location = new Point(3, 3);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(72, 37);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 15;
            pictureBox1.TabStop = false;
            pictureBox1.Click += pictureBox1_Click;
            // 
            // frmSignIn
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(pictureBox1);
            Controls.Add(lblsiginin);
            Controls.Add(submitinfo);
            Controls.Add(lblpassword);
            Controls.Add(txtpassword);
            Controls.Add(txtusername);
            Controls.Add(lblusername);
            Controls.Add(lblhponenumber);
            Controls.Add(txtphonenumber);
            Controls.Add(lblsocialnumber);
            Controls.Add(txtsocialnumber);
            Controls.Add(lbllastname);
            Controls.Add(txtlastname);
            Controls.Add(lblname);
            Controls.Add(txtfname);
            Controls.Add(lblsignin);
            Name = "frmSignIn";
            Text = "SingIn";
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblsignin;
        private TextBox txtfname;
        private Label lblname;
        private TextBox txtlastname;
        private Label lbllastname;
        private TextBox txtsocialnumber;
        private Label lblsocialnumber;
        private TextBox txtphonenumber;
        private Label lblhponenumber;
        private Label lblusername;
        private TextBox txtusername;
        private TextBox txtpassword;
        private Label lblpassword;
        private Button submitinfo;
        private Label lblsiginin;
        private PictureBox pictureBox1;
    }
}