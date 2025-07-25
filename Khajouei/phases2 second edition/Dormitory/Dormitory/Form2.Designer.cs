namespace form2
{
    partial class frmResetPassword
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
            txtUserName = new TextBox();
            txtSocialNumber = new TextBox();
            lblUsername = new Label();
            lblSocialNumber = new Label();
            btnsubmitusernamesocialnumber = new Button();
            txtphonenumber = new TextBox();
            lblphonenumber = new Label();
            lblValiData = new Label();
            lblresetdata = new Label();
            pictureBox1 = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // txtUserName
            // 
            txtUserName.Location = new Point(301, 89);
            txtUserName.Name = "txtUserName";
            txtUserName.Size = new Size(170, 27);
            txtUserName.TabIndex = 0;
            // 
            // txtSocialNumber
            // 
            txtSocialNumber.Location = new Point(301, 136);
            txtSocialNumber.Name = "txtSocialNumber";
            txtSocialNumber.Size = new Size(170, 27);
            txtSocialNumber.TabIndex = 1;
            // 
            // lblUsername
            // 
            lblUsername.AutoSize = true;
            lblUsername.Font = new Font("Microsoft Sans Serif", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblUsername.Location = new Point(477, 92);
            lblUsername.Name = "lblUsername";
            lblUsername.Size = new Size(80, 20);
            lblUsername.TabIndex = 2;
            lblUsername.Text = "نام کاربری";
            // 
            // lblSocialNumber
            // 
            lblSocialNumber.AutoSize = true;
            lblSocialNumber.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblSocialNumber.Location = new Point(477, 140);
            lblSocialNumber.Name = "lblSocialNumber";
            lblSocialNumber.Size = new Size(63, 23);
            lblSocialNumber.TabIndex = 3;
            lblSocialNumber.Text = "کد ملی";
            // 
            // btnsubmitusernamesocialnumber
            // 
            btnsubmitusernamesocialnumber.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnsubmitusernamesocialnumber.Location = new Point(365, 232);
            btnsubmitusernamesocialnumber.Name = "btnsubmitusernamesocialnumber";
            btnsubmitusernamesocialnumber.Size = new Size(106, 36);
            btnsubmitusernamesocialnumber.TabIndex = 4;
            btnsubmitusernamesocialnumber.Text = "برسی";
            btnsubmitusernamesocialnumber.UseVisualStyleBackColor = true;
            btnsubmitusernamesocialnumber.Click += btnsubmitusernamesocialnumber_Click;
            // 
            // txtphonenumber
            // 
            txtphonenumber.Location = new Point(301, 187);
            txtphonenumber.Name = "txtphonenumber";
            txtphonenumber.Size = new Size(170, 27);
            txtphonenumber.TabIndex = 5;
            // 
            // lblphonenumber
            // 
            lblphonenumber.AutoSize = true;
            lblphonenumber.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblphonenumber.Location = new Point(477, 191);
            lblphonenumber.Name = "lblphonenumber";
            lblphonenumber.Size = new Size(90, 23);
            lblphonenumber.TabIndex = 6;
            lblphonenumber.Text = "شماره تلفن";
            // 
            // lblValiData
            // 
            lblValiData.AutoSize = true;
            lblValiData.BackColor = SystemColors.Desktop;
            lblValiData.Font = new Font("Segoe UI Historic", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblValiData.Location = new Point(101, 187);
            lblValiData.Name = "lblValiData";
            lblValiData.Size = new Size(0, 41);
            lblValiData.TabIndex = 7;
            // 
            // lblresetdata
            // 
            lblresetdata.AutoSize = true;
            lblresetdata.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblresetdata.Location = new Point(327, 53);
            lblresetdata.Name = "lblresetdata";
            lblresetdata.Size = new Size(123, 23);
            lblresetdata.TabIndex = 8;
            lblresetdata.Text = "بازیابی رمز عبور";
            // 
            // pictureBox1
            // 
            pictureBox1.Image = Dormitory.Properties.Resources._3114883;
            pictureBox1.Location = new Point(2, 1);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(72, 37);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 16;
            pictureBox1.TabStop = false;
            pictureBox1.Click += pictureBox1_Click;
            // 
            // frmResetPassword
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(pictureBox1);
            Controls.Add(lblresetdata);
            Controls.Add(lblValiData);
            Controls.Add(lblphonenumber);
            Controls.Add(txtphonenumber);
            Controls.Add(btnsubmitusernamesocialnumber);
            Controls.Add(lblSocialNumber);
            Controls.Add(lblUsername);
            Controls.Add(txtSocialNumber);
            Controls.Add(txtUserName);
            Name = "frmResetPassword";
            Text = "ResetPassword";
            Load += frmResetPassword_Load;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtUserName;
        private TextBox txtSocialNumber;
        private Label lblUsername;
        private Label lblSocialNumber;
        private Button btnsubmitusernamesocialnumber;
        private TextBox txtphonenumber;
        private Label lblphonenumber;
        private Label lblValiData;
        private Label lblresetdata;
        private PictureBox pictureBox1;
    }
}