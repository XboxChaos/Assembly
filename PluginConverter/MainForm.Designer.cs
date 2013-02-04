namespace PluginConverter
{
    partial class MainForm
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
            this.originalFolder = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.outputFolder = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.browseOriginal = new System.Windows.Forms.Button();
            this.browseOutput = new System.Windows.Forms.Button();
            this.convert = new System.Windows.Forms.Button();
            this.convertProgress = new System.Windows.Forms.ProgressBar();
            this.statusBar = new System.Windows.Forms.StatusStrip();
            this.statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.label3 = new System.Windows.Forms.Label();
            this.targetGame = new System.Windows.Forms.ComboBox();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.alterationFmt = new System.Windows.Forms.RadioButton();
            this.ascensionFmt = new System.Windows.Forms.RadioButton();
            this.assemblyFmt = new System.Windows.Forms.RadioButton();
            this.label4 = new System.Windows.Forms.Label();
            this.statusBar.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // originalFolder
            // 
            this.originalFolder.Location = new System.Drawing.Point(137, 12);
            this.originalFolder.Name = "originalFolder";
            this.originalFolder.Size = new System.Drawing.Size(314, 20);
            this.originalFolder.TabIndex = 0;
            this.originalFolder.TextChanged += new System.EventHandler(this.folder_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(105, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Original plugin folder:";
            // 
            // outputFolder
            // 
            this.outputFolder.Location = new System.Drawing.Point(137, 38);
            this.outputFolder.Name = "outputFolder";
            this.outputFolder.Size = new System.Drawing.Size(314, 20);
            this.outputFolder.TabIndex = 2;
            this.outputFolder.TextChanged += new System.EventHandler(this.folder_TextChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 41);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(119, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Converted plugin folder:";
            // 
            // browseOriginal
            // 
            this.browseOriginal.Location = new System.Drawing.Point(457, 10);
            this.browseOriginal.Name = "browseOriginal";
            this.browseOriginal.Size = new System.Drawing.Size(75, 23);
            this.browseOriginal.TabIndex = 4;
            this.browseOriginal.Text = "Browse...";
            this.browseOriginal.UseVisualStyleBackColor = true;
            this.browseOriginal.Click += new System.EventHandler(this.browseOriginal_Click);
            // 
            // browseOutput
            // 
            this.browseOutput.Location = new System.Drawing.Point(457, 36);
            this.browseOutput.Name = "browseOutput";
            this.browseOutput.Size = new System.Drawing.Size(75, 23);
            this.browseOutput.TabIndex = 5;
            this.browseOutput.Text = "Browse...";
            this.browseOutput.UseVisualStyleBackColor = true;
            this.browseOutput.Click += new System.EventHandler(this.browseOutput_Click);
            // 
            // convert
            // 
            this.convert.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.convert.Enabled = false;
            this.convert.Location = new System.Drawing.Point(15, 128);
            this.convert.Name = "convert";
            this.convert.Size = new System.Drawing.Size(517, 40);
            this.convert.TabIndex = 6;
            this.convert.Text = "Convert!";
            this.convert.UseVisualStyleBackColor = true;
            this.convert.Click += new System.EventHandler(this.convert_Click);
            // 
            // convertProgress
            // 
            this.convertProgress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.convertProgress.Location = new System.Drawing.Point(15, 128);
            this.convertProgress.Name = "convertProgress";
            this.convertProgress.Size = new System.Drawing.Size(517, 40);
            this.convertProgress.TabIndex = 7;
            this.convertProgress.Visible = false;
            // 
            // statusBar
            // 
            this.statusBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusLabel});
            this.statusBar.Location = new System.Drawing.Point(0, 187);
            this.statusBar.Name = "statusBar";
            this.statusBar.Size = new System.Drawing.Size(544, 22);
            this.statusBar.SizingGrip = false;
            this.statusBar.TabIndex = 8;
            // 
            // statusLabel
            // 
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(39, 17);
            this.statusLabel.Text = "Ready";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 67);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(72, 13);
            this.label3.TabIndex = 9;
            this.label3.Text = "Target Game:";
            // 
            // targetGame
            // 
            this.targetGame.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.targetGame.FormattingEnabled = true;
            this.targetGame.Items.AddRange(new object[] {
            "Halo2",
            "Halo3",
            "ODST",
            "ReachBeta",
            "Reach",
            "Halo4"});
            this.targetGame.Location = new System.Drawing.Point(137, 64);
            this.targetGame.Name = "targetGame";
            this.targetGame.Size = new System.Drawing.Size(156, 21);
            this.targetGame.TabIndex = 10;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanel1.Controls.Add(this.alterationFmt);
            this.flowLayoutPanel1.Controls.Add(this.ascensionFmt);
            this.flowLayoutPanel1.Controls.Add(this.assemblyFmt);
            this.flowLayoutPanel1.Location = new System.Drawing.Point(137, 91);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(395, 29);
            this.flowLayoutPanel1.TabIndex = 11;
            // 
            // alterationFmt
            // 
            this.alterationFmt.AutoSize = true;
            this.alterationFmt.Enabled = false;
            this.alterationFmt.Location = new System.Drawing.Point(3, 3);
            this.alterationFmt.Name = "alterationFmt";
            this.alterationFmt.Size = new System.Drawing.Size(69, 17);
            this.alterationFmt.TabIndex = 3;
            this.alterationFmt.Text = "Alteration";
            this.alterationFmt.UseVisualStyleBackColor = true;
            // 
            // ascensionFmt
            // 
            this.ascensionFmt.AutoSize = true;
            this.ascensionFmt.Location = new System.Drawing.Point(78, 3);
            this.ascensionFmt.Name = "ascensionFmt";
            this.ascensionFmt.Size = new System.Drawing.Size(74, 17);
            this.ascensionFmt.TabIndex = 4;
            this.ascensionFmt.Text = "Ascension";
            this.ascensionFmt.UseVisualStyleBackColor = true;
            // 
            // assemblyFmt
            // 
            this.assemblyFmt.AutoSize = true;
            this.assemblyFmt.Checked = true;
            this.assemblyFmt.Location = new System.Drawing.Point(158, 3);
            this.assemblyFmt.Name = "assemblyFmt";
            this.assemblyFmt.Size = new System.Drawing.Size(69, 17);
            this.assemblyFmt.TabIndex = 5;
            this.assemblyFmt.TabStop = true;
            this.assemblyFmt.Text = "Assembly";
            this.assemblyFmt.UseVisualStyleBackColor = true;
            this.assemblyFmt.CheckedChanged += new System.EventHandler(this.assemblyFmt_CheckedChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 96);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(76, 13);
            this.label4.TabIndex = 12;
            this.label4.Text = "Target Format:";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(544, 209);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.targetGame);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.statusBar);
            this.Controls.Add(this.convert);
            this.Controls.Add(this.browseOutput);
            this.Controls.Add(this.browseOriginal);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.outputFolder);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.originalFolder);
            this.Controls.Add(this.convertProgress);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "Plugin Converter";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.statusBar.ResumeLayout(false);
            this.statusBar.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox originalFolder;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox outputFolder;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button browseOriginal;
        private System.Windows.Forms.Button browseOutput;
        private System.Windows.Forms.Button convert;
        private System.Windows.Forms.ProgressBar convertProgress;
        private System.Windows.Forms.StatusStrip statusBar;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox targetGame;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.RadioButton alterationFmt;
        private System.Windows.Forms.RadioButton ascensionFmt;
        private System.Windows.Forms.RadioButton assemblyFmt;
        private System.Windows.Forms.Label label4;
    }
}

