namespace DisplayServer
{
    partial class frmDisplayServer
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
            this.picCurrState = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblServerIP = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.lblPollingFrequency = new System.Windows.Forms.Label();
            this.lnkOpenHistory = new System.Windows.Forms.LinkLabel();
            this.lblMessageCount = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.lblDebugInfo = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.picCurrState)).BeginInit();
            this.SuspendLayout();
            // 
            // picCurrState
            // 
            this.picCurrState.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picCurrState.Location = new System.Drawing.Point(12, 9);
            this.picCurrState.Name = "picCurrState";
            this.picCurrState.Size = new System.Drawing.Size(700, 669);
            this.picCurrState.TabIndex = 1;
            this.picCurrState.TabStop = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(718, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(43, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Status: ";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(718, 31);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(90, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Sensor Server IP:";
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.ForeColor = System.Drawing.Color.Red;
            this.lblStatus.Location = new System.Drawing.Point(814, 9);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(73, 13);
            this.lblStatus.TabIndex = 4;
            this.lblStatus.Text = "Disconnected";
            // 
            // lblServerIP
            // 
            this.lblServerIP.AutoSize = true;
            this.lblServerIP.Location = new System.Drawing.Point(814, 31);
            this.lblServerIP.Name = "lblServerIP";
            this.lblServerIP.Size = new System.Drawing.Size(10, 13);
            this.lblServerIP.TabIndex = 5;
            this.lblServerIP.Text = "-";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(718, 76);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(97, 13);
            this.label5.TabIndex = 6;
            this.label5.Text = "Polling Frequency: ";
            // 
            // lblPollingFrequency
            // 
            this.lblPollingFrequency.AutoSize = true;
            this.lblPollingFrequency.Location = new System.Drawing.Point(814, 76);
            this.lblPollingFrequency.Name = "lblPollingFrequency";
            this.lblPollingFrequency.Size = new System.Drawing.Size(10, 13);
            this.lblPollingFrequency.TabIndex = 7;
            this.lblPollingFrequency.Text = "-";
            // 
            // lnkOpenHistory
            // 
            this.lnkOpenHistory.AutoSize = true;
            this.lnkOpenHistory.Location = new System.Drawing.Point(724, 155);
            this.lnkOpenHistory.Name = "lnkOpenHistory";
            this.lnkOpenHistory.Size = new System.Drawing.Size(100, 13);
            this.lnkOpenHistory.TabIndex = 8;
            this.lnkOpenHistory.TabStop = true;
            this.lnkOpenHistory.Text = "Open sensor history";
            this.lnkOpenHistory.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkOpenHistory_LinkClicked);
            // 
            // lblMessageCount
            // 
            this.lblMessageCount.AutoSize = true;
            this.lblMessageCount.Location = new System.Drawing.Point(814, 53);
            this.lblMessageCount.Name = "lblMessageCount";
            this.lblMessageCount.Size = new System.Drawing.Size(10, 13);
            this.lblMessageCount.TabIndex = 10;
            this.lblMessageCount.Text = "-";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(718, 53);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(83, 13);
            this.label4.TabIndex = 9;
            this.label4.Text = "Message count:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(718, 496);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(96, 13);
            this.label3.TabIndex = 11;
            this.label3.Text = "Debug information:";
            // 
            // lblDebugInfo
            // 
            this.lblDebugInfo.Location = new System.Drawing.Point(718, 512);
            this.lblDebugInfo.Multiline = true;
            this.lblDebugInfo.Name = "lblDebugInfo";
            this.lblDebugInfo.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.lblDebugInfo.Size = new System.Drawing.Size(258, 159);
            this.lblDebugInfo.TabIndex = 12;
            // 
            // frmDisplayServer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(988, 683);
            this.Controls.Add(this.lblDebugInfo);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.lblMessageCount);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.lnkOpenHistory);
            this.Controls.Add(this.lblPollingFrequency);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.lblServerIP);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.picCurrState);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "frmDisplayServer";
            this.Text = "Display Server";
            this.Load += new System.EventHandler(this.frmDisplayServer_Load);
            ((System.ComponentModel.ISupportInitialize)(this.picCurrState)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox picCurrState;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblServerIP;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label lblPollingFrequency;
        private System.Windows.Forms.LinkLabel lnkOpenHistory;
        private System.Windows.Forms.Label lblMessageCount;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox lblDebugInfo;
    }
}

