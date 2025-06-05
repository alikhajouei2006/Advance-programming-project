namespace form4
{
    partial class frmgetnewpassword
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
            txtnewpass = new TextBox();
            lblgetnewpass = new Label();
            btnsubmit = new Button();
            lblvalidnewpass = new Label();
            pictureBox1 = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // txtnewpass
            // 
            txtnewpass.Location = new Point(302, 99);
            txtnewpass.Name = "txtnewpass";
            txtnewpass.Size = new Size(175, 27);
            txtnewpass.TabIndex = 0;
            // 
            // lblgetnewpass
            // 
            lblgetnewpass.AutoSize = true;
            lblgetnewpass.Location = new Point(494, 102);
            lblgetnewpass.Name = "lblgetnewpass";
            lblgetnewpass.Size = new Size(95, 20);
            lblgetnewpass.TabIndex = 1;
            lblgetnewpass.Text = "رمز عبور جدید";
            // 
            // btnsubmit
            // 
            btnsubmit.Font = new Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnsubmit.Location = new Point(379, 141);
            btnsubmit.Name = "btnsubmit";
            btnsubmit.Size = new Size(98, 42);
            btnsubmit.TabIndex = 2;
            btnsubmit.Text = "ثبت";
            btnsubmit.UseVisualStyleBackColor = true;
            btnsubmit.Click += btnsubmit_Click;
            // 
            // lblvalidnewpass
            // 
            lblvalidnewpass.AutoSize = true;
            lblvalidnewpass.Font = new Font("Segoe UI", 16.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblvalidnewpass.Location = new Point(99, 183);
            lblvalidnewpass.Name = "lblvalidnewpass";
            lblvalidnewpass.Size = new Size(0, 38);
            lblvalidnewpass.TabIndex = 3;
            // 
            // pictureBox1
            // 
            pictureBox1.Image = Dormitory.Properties.Resources._3114883;
            pictureBox1.Location = new Point(0, 2);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(72, 37);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 17;
            pictureBox1.TabStop = false;
            pictureBox1.Click += pictureBox1_Click_1;
            // 
            // frmgetnewpassword
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(pictureBox1);
            Controls.Add(lblvalidnewpass);
            Controls.Add(btnsubmit);
            Controls.Add(lblgetnewpass);
            Controls.Add(txtnewpass);
            Name = "frmgetnewpassword";
            Text = "Form4";
            Load += frmgetnewpassword_Load;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtnewpass;
        private Label lblgetnewpass;
        private Button btnsubmit;
        private Label lblvalidnewpass;
        private PictureBox pictureBox1;
    }
}