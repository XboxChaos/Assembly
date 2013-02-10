namespace Assembly
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
            this.txtMapPath = new System.Windows.Forms.TextBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.tvTags = new System.Windows.Forms.TreeView();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.label14 = new System.Windows.Forms.Label();
            this.txtMapMagic = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.txtIndexOffsetMagic = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.txtIndexHeaderAddr = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.txtRawTableSize = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.txtRawTableOffset = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.txtXdk = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.txtVirtSize = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.txtVirtBase = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.txtScenarioName = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.txtInternalName = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtType = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtBuild = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtGame = new System.Windows.Forms.TextBox();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.lbClasses = new System.Windows.Forms.ListBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.lbStrings = new System.Windows.Forms.ListBox();
            this.lbLanguages = new System.Windows.Forms.ListBox();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tabPage4.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(52, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Filename:";
            // 
            // txtMapPath
            // 
            this.txtMapPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtMapPath.Location = new System.Drawing.Point(70, 12);
            this.txtMapPath.Name = "txtMapPath";
            this.txtMapPath.Size = new System.Drawing.Size(741, 20);
            this.txtMapPath.TabIndex = 1;
            // 
            // btnBrowse
            // 
            this.btnBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowse.Location = new System.Drawing.Point(817, 10);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(75, 23);
            this.btnBrowse.TabIndex = 2;
            this.btnBrowse.Text = "Browse...";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // tvTags
            // 
            this.tvTags.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.tvTags.Location = new System.Drawing.Point(6, 6);
            this.tvTags.Name = "tvTags";
            this.tvTags.Size = new System.Drawing.Size(180, 351);
            this.tvTags.TabIndex = 3;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Location = new System.Drawing.Point(192, 6);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(671, 351);
            this.groupBox1.TabIndex = 5;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Tag Information";
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage4);
            this.tabControl1.Location = new System.Drawing.Point(15, 38);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(877, 389);
            this.tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.label14);
            this.tabPage1.Controls.Add(this.txtMapMagic);
            this.tabPage1.Controls.Add(this.label13);
            this.tabPage1.Controls.Add(this.txtIndexOffsetMagic);
            this.tabPage1.Controls.Add(this.label12);
            this.tabPage1.Controls.Add(this.txtIndexHeaderAddr);
            this.tabPage1.Controls.Add(this.label11);
            this.tabPage1.Controls.Add(this.txtRawTableSize);
            this.tabPage1.Controls.Add(this.label10);
            this.tabPage1.Controls.Add(this.txtRawTableOffset);
            this.tabPage1.Controls.Add(this.label9);
            this.tabPage1.Controls.Add(this.txtXdk);
            this.tabPage1.Controls.Add(this.label8);
            this.tabPage1.Controls.Add(this.txtVirtSize);
            this.tabPage1.Controls.Add(this.label7);
            this.tabPage1.Controls.Add(this.txtVirtBase);
            this.tabPage1.Controls.Add(this.label6);
            this.tabPage1.Controls.Add(this.txtScenarioName);
            this.tabPage1.Controls.Add(this.label5);
            this.tabPage1.Controls.Add(this.txtInternalName);
            this.tabPage1.Controls.Add(this.label4);
            this.tabPage1.Controls.Add(this.txtType);
            this.tabPage1.Controls.Add(this.label3);
            this.tabPage1.Controls.Add(this.txtBuild);
            this.tabPage1.Controls.Add(this.label2);
            this.tabPage1.Controls.Add(this.txtGame);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(869, 363);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Map";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(6, 321);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(63, 13);
            this.label14.TabIndex = 25;
            this.label14.Text = "Map Magic:";
            // 
            // txtMapMagic
            // 
            this.txtMapMagic.Location = new System.Drawing.Point(111, 318);
            this.txtMapMagic.Name = "txtMapMagic";
            this.txtMapMagic.Size = new System.Drawing.Size(186, 20);
            this.txtMapMagic.TabIndex = 24;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(6, 295);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(99, 13);
            this.label13.TabIndex = 23;
            this.label13.Text = "Index Offset Magic:";
            // 
            // txtIndexOffsetMagic
            // 
            this.txtIndexOffsetMagic.Location = new System.Drawing.Point(111, 292);
            this.txtIndexOffsetMagic.Name = "txtIndexOffsetMagic";
            this.txtIndexOffsetMagic.Size = new System.Drawing.Size(186, 20);
            this.txtIndexOffsetMagic.TabIndex = 22;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(6, 269);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(99, 13);
            this.label12.TabIndex = 21;
            this.label12.Text = "Index Header Addr:";
            // 
            // txtIndexHeaderAddr
            // 
            this.txtIndexHeaderAddr.Location = new System.Drawing.Point(111, 266);
            this.txtIndexHeaderAddr.Name = "txtIndexHeaderAddr";
            this.txtIndexHeaderAddr.Size = new System.Drawing.Size(186, 20);
            this.txtIndexHeaderAddr.TabIndex = 20;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(6, 243);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(85, 13);
            this.label11.TabIndex = 19;
            this.label11.Text = "Raw Table Size:";
            // 
            // txtRawTableSize
            // 
            this.txtRawTableSize.Location = new System.Drawing.Point(111, 240);
            this.txtRawTableSize.Name = "txtRawTableSize";
            this.txtRawTableSize.Size = new System.Drawing.Size(186, 20);
            this.txtRawTableSize.TabIndex = 18;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(6, 217);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(93, 13);
            this.label10.TabIndex = 17;
            this.label10.Text = "Raw Table Offset:";
            // 
            // txtRawTableOffset
            // 
            this.txtRawTableOffset.Location = new System.Drawing.Point(111, 214);
            this.txtRawTableOffset.Name = "txtRawTableOffset";
            this.txtRawTableOffset.Size = new System.Drawing.Size(186, 20);
            this.txtRawTableOffset.TabIndex = 16;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(6, 191);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(70, 13);
            this.label9.TabIndex = 15;
            this.label9.Text = "XDK Version:";
            // 
            // txtXdk
            // 
            this.txtXdk.Location = new System.Drawing.Point(111, 188);
            this.txtXdk.Name = "txtXdk";
            this.txtXdk.Size = new System.Drawing.Size(186, 20);
            this.txtXdk.TabIndex = 14;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(6, 165);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(51, 13);
            this.label8.TabIndex = 13;
            this.label8.Text = "Virt. Size:";
            // 
            // txtVirtSize
            // 
            this.txtVirtSize.Location = new System.Drawing.Point(111, 162);
            this.txtVirtSize.Name = "txtVirtSize";
            this.txtVirtSize.Size = new System.Drawing.Size(186, 20);
            this.txtVirtSize.TabIndex = 12;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(6, 139);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(80, 13);
            this.label7.TabIndex = 11;
            this.label7.Text = "Virt. Base Addr:";
            // 
            // txtVirtBase
            // 
            this.txtVirtBase.Location = new System.Drawing.Point(111, 136);
            this.txtVirtBase.Name = "txtVirtBase";
            this.txtVirtBase.Size = new System.Drawing.Size(186, 20);
            this.txtVirtBase.TabIndex = 10;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(6, 113);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(83, 13);
            this.label6.TabIndex = 9;
            this.label6.Text = "Scenario Name:";
            // 
            // txtScenarioName
            // 
            this.txtScenarioName.Location = new System.Drawing.Point(111, 110);
            this.txtScenarioName.Name = "txtScenarioName";
            this.txtScenarioName.Size = new System.Drawing.Size(186, 20);
            this.txtScenarioName.TabIndex = 8;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 87);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(76, 13);
            this.label5.TabIndex = 7;
            this.label5.Text = "Internal Name:";
            // 
            // txtInternalName
            // 
            this.txtInternalName.Location = new System.Drawing.Point(111, 84);
            this.txtInternalName.Name = "txtInternalName";
            this.txtInternalName.Size = new System.Drawing.Size(186, 20);
            this.txtInternalName.TabIndex = 6;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 61);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(34, 13);
            this.label4.TabIndex = 5;
            this.label4.Text = "Type:";
            // 
            // txtType
            // 
            this.txtType.Location = new System.Drawing.Point(111, 58);
            this.txtType.Name = "txtType";
            this.txtType.Size = new System.Drawing.Size(186, 20);
            this.txtType.TabIndex = 4;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 35);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(63, 13);
            this.label3.TabIndex = 3;
            this.label3.Text = "Build String:";
            // 
            // txtBuild
            // 
            this.txtBuild.Location = new System.Drawing.Point(111, 32);
            this.txtBuild.Name = "txtBuild";
            this.txtBuild.Size = new System.Drawing.Size(186, 20);
            this.txtBuild.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(38, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Game:";
            // 
            // txtGame
            // 
            this.txtGame.Location = new System.Drawing.Point(111, 6);
            this.txtGame.Name = "txtGame";
            this.txtGame.Size = new System.Drawing.Size(186, 20);
            this.txtGame.TabIndex = 0;
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.lbClasses);
            this.tabPage3.Controls.Add(this.groupBox2);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(869, 363);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Classes";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // lbClasses
            // 
            this.lbClasses.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.lbClasses.FormattingEnabled = true;
            this.lbClasses.IntegralHeight = false;
            this.lbClasses.Location = new System.Drawing.Point(6, 6);
            this.lbClasses.Name = "lbClasses";
            this.lbClasses.Size = new System.Drawing.Size(180, 351);
            this.lbClasses.TabIndex = 8;
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Location = new System.Drawing.Point(192, 6);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(671, 351);
            this.groupBox2.TabIndex = 7;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Class Information";
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.tvTags);
            this.tabPage2.Controls.Add(this.groupBox1);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(869, 363);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Tags";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // tabPage4
            // 
            this.tabPage4.Controls.Add(this.lbStrings);
            this.tabPage4.Controls.Add(this.lbLanguages);
            this.tabPage4.Location = new System.Drawing.Point(4, 22);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage4.Size = new System.Drawing.Size(869, 363);
            this.tabPage4.TabIndex = 3;
            this.tabPage4.Text = "Locales";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // lbStrings
            // 
            this.lbStrings.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbStrings.FormattingEnabled = true;
            this.lbStrings.IntegralHeight = false;
            this.lbStrings.Location = new System.Drawing.Point(192, 6);
            this.lbStrings.Name = "lbStrings";
            this.lbStrings.Size = new System.Drawing.Size(671, 351);
            this.lbStrings.TabIndex = 10;
            // 
            // lbLanguages
            // 
            this.lbLanguages.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.lbLanguages.FormattingEnabled = true;
            this.lbLanguages.IntegralHeight = false;
            this.lbLanguages.Location = new System.Drawing.Point(6, 6);
            this.lbLanguages.Name = "lbLanguages";
            this.lbLanguages.Size = new System.Drawing.Size(180, 351);
            this.lbLanguages.TabIndex = 9;
            this.lbLanguages.SelectedIndexChanged += new System.EventHandler(this.lbLanguages_SelectedIndexChanged);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(904, 439);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.txtMapPath);
            this.Controls.Add(this.label1);
            this.Name = "Form1";
            this.Text = "Extryze - Internal Development Build";
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage3.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.tabPage4.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtMapPath;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.TreeView tvTags;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtBuild;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtGame;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtType;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtScenarioName;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtInternalName;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txtVirtBase;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox txtVirtSize;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox txtXdk;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox txtRawTableSize;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox txtRawTableOffset;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox txtIndexHeaderAddr;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox txtIndexOffsetMagic;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.TextBox txtMapMagic;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.ListBox lbClasses;
        private System.Windows.Forms.TabPage tabPage4;
        private System.Windows.Forms.ListBox lbStrings;
        private System.Windows.Forms.ListBox lbLanguages;
    }
}

