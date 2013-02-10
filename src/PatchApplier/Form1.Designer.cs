namespace PatchApplier
{
    partial class Form1
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
            this.label1 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.browseOutput = new System.Windows.Forms.Button();
            this.outPath = new System.Windows.Forms.TextBox();
            this.browsePatch = new System.Windows.Forms.Button();
            this.patchPath = new System.Windows.Forms.TextBox();
            this.browseUnmodded = new System.Windows.Forms.Button();
            this.unmoddedPath = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.patchAuthor = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.patchDescription = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.patchName = new System.Windows.Forms.TextBox();
            this.applyPatch = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 240);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(212, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "hey alex a real ui for this would be nice kthx";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 67);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(65, 13);
            this.label3.TabIndex = 17;
            this.label3.Text = "Output map:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 41);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(78, 13);
            this.label2.TabIndex = 16;
            this.label2.Text = "Patch to apply:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 14);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(85, 13);
            this.label4.TabIndex = 15;
            this.label4.Text = "Unmodded map:";
            // 
            // browseOutput
            // 
            this.browseOutput.Location = new System.Drawing.Point(464, 62);
            this.browseOutput.Name = "browseOutput";
            this.browseOutput.Size = new System.Drawing.Size(75, 23);
            this.browseOutput.TabIndex = 14;
            this.browseOutput.Text = "Browse...";
            this.browseOutput.UseVisualStyleBackColor = true;
            this.browseOutput.Click += new System.EventHandler(this.browseOutput_Click);
            // 
            // outPath
            // 
            this.outPath.Location = new System.Drawing.Point(103, 64);
            this.outPath.Name = "outPath";
            this.outPath.Size = new System.Drawing.Size(355, 20);
            this.outPath.TabIndex = 13;
            // 
            // browsePatch
            // 
            this.browsePatch.Location = new System.Drawing.Point(464, 36);
            this.browsePatch.Name = "browsePatch";
            this.browsePatch.Size = new System.Drawing.Size(75, 23);
            this.browsePatch.TabIndex = 12;
            this.browsePatch.Text = "Browse...";
            this.browsePatch.UseVisualStyleBackColor = true;
            this.browsePatch.Click += new System.EventHandler(this.browsePatch_Click);
            // 
            // patchPath
            // 
            this.patchPath.Location = new System.Drawing.Point(103, 38);
            this.patchPath.Name = "patchPath";
            this.patchPath.Size = new System.Drawing.Size(355, 20);
            this.patchPath.TabIndex = 11;
            // 
            // browseUnmodded
            // 
            this.browseUnmodded.Location = new System.Drawing.Point(464, 10);
            this.browseUnmodded.Name = "browseUnmodded";
            this.browseUnmodded.Size = new System.Drawing.Size(75, 23);
            this.browseUnmodded.TabIndex = 10;
            this.browseUnmodded.Text = "Browse...";
            this.browseUnmodded.UseVisualStyleBackColor = true;
            this.browseUnmodded.Click += new System.EventHandler(this.browseUnmodded_Click);
            // 
            // unmoddedPath
            // 
            this.unmoddedPath.Location = new System.Drawing.Point(103, 12);
            this.unmoddedPath.Name = "unmoddedPath";
            this.unmoddedPath.Size = new System.Drawing.Size(355, 20);
            this.unmoddedPath.TabIndex = 9;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.patchAuthor);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.patchDescription);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.patchName);
            this.groupBox1.Location = new System.Drawing.Point(15, 90);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(524, 77);
            this.groupBox1.TabIndex = 18;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Patch Information";
            // 
            // patchAuthor
            // 
            this.patchAuthor.Location = new System.Drawing.Point(56, 45);
            this.patchAuthor.Name = "patchAuthor";
            this.patchAuthor.ReadOnly = true;
            this.patchAuthor.Size = new System.Drawing.Size(100, 20);
            this.patchAuthor.TabIndex = 5;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(6, 48);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(41, 13);
            this.label6.TabIndex = 4;
            this.label6.Text = "Author:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(191, 23);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(63, 13);
            this.label5.TabIndex = 3;
            this.label5.Text = "Description:";
            // 
            // patchDescription
            // 
            this.patchDescription.Location = new System.Drawing.Point(260, 20);
            this.patchDescription.Name = "patchDescription";
            this.patchDescription.ReadOnly = true;
            this.patchDescription.Size = new System.Drawing.Size(258, 20);
            this.patchDescription.TabIndex = 2;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(6, 23);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(38, 13);
            this.label7.TabIndex = 1;
            this.label7.Text = "Name:";
            // 
            // patchName
            // 
            this.patchName.Location = new System.Drawing.Point(56, 20);
            this.patchName.Name = "patchName";
            this.patchName.ReadOnly = true;
            this.patchName.Size = new System.Drawing.Size(100, 20);
            this.patchName.TabIndex = 0;
            // 
            // applyPatch
            // 
            this.applyPatch.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.applyPatch.Location = new System.Drawing.Point(15, 173);
            this.applyPatch.Name = "applyPatch";
            this.applyPatch.Size = new System.Drawing.Size(524, 64);
            this.applyPatch.TabIndex = 19;
            this.applyPatch.Text = "Apply Patch";
            this.applyPatch.UseVisualStyleBackColor = true;
            this.applyPatch.Click += new System.EventHandler(this.applyPatch_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(551, 261);
            this.Controls.Add(this.applyPatch);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.browseOutput);
            this.Controls.Add(this.outPath);
            this.Controls.Add(this.browsePatch);
            this.Controls.Add(this.patchPath);
            this.Controls.Add(this.browseUnmodded);
            this.Controls.Add(this.unmoddedPath);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "Patch Applier";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button browseOutput;
        private System.Windows.Forms.TextBox outPath;
        private System.Windows.Forms.Button browsePatch;
        private System.Windows.Forms.TextBox patchPath;
        private System.Windows.Forms.Button browseUnmodded;
        private System.Windows.Forms.TextBox unmoddedPath;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox patchAuthor;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox patchDescription;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox patchName;
        private System.Windows.Forms.Button applyPatch;
    }
}

