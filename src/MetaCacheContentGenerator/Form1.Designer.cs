namespace MetaCacheContentGenerator
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
			this.components = new System.ComponentModel.Container();
			this.btnAddEntry = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.lvList = new System.Windows.Forms.ListView();
			this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.txtTargets = new System.Windows.Forms.TextBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.label3 = new System.Windows.Forms.Label();
			this.txtBlfPath = new System.Windows.Forms.TextBox();
			this.txtMapInfoPath = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.btnGenerate = new System.Windows.Forms.Button();
			this.txtOutput = new System.Windows.Forms.TextBox();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.contextMenuStrip1.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnAddEntry
			// 
			this.btnAddEntry.Location = new System.Drawing.Point(113, 100);
			this.btnAddEntry.Name = "btnAddEntry";
			this.btnAddEntry.Size = new System.Drawing.Size(472, 23);
			this.btnAddEntry.TabIndex = 0;
			this.btnAddEntry.Text = "Add";
			this.btnAddEntry.UseVisualStyleBackColor = true;
			this.btnAddEntry.Click += new System.EventHandler(this.btnAddEntry_Click);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(6, 25);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(91, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Game Target List:";
			// 
			// lvList
			// 
			this.lvList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
			this.lvList.ContextMenuStrip = this.contextMenuStrip1;
			this.lvList.GridLines = true;
			this.lvList.Location = new System.Drawing.Point(12, 151);
			this.lvList.MultiSelect = false;
			this.lvList.Name = "lvList";
			this.lvList.Size = new System.Drawing.Size(591, 156);
			this.lvList.TabIndex = 2;
			this.lvList.UseCompatibleStateImageBehavior = false;
			this.lvList.View = System.Windows.Forms.View.Details;
			// 
			// columnHeader1
			// 
			this.columnHeader1.Text = "Targets";
			this.columnHeader1.Width = 100;
			// 
			// columnHeader2
			// 
			this.columnHeader2.Text = "MapInfo Path";
			this.columnHeader2.Width = 400;
			// 
			// contextMenuStrip1
			// 
			this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.deleteToolStripMenuItem});
			this.contextMenuStrip1.Name = "contextMenuStrip1";
			this.contextMenuStrip1.Size = new System.Drawing.Size(108, 26);
			// 
			// txtTargets
			// 
			this.txtTargets.Location = new System.Drawing.Point(113, 22);
			this.txtTargets.Name = "txtTargets";
			this.txtTargets.Size = new System.Drawing.Size(472, 20);
			this.txtTargets.TabIndex = 3;
			this.txtTargets.Text = "Halo3|Halo3Beta";
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.label3);
			this.groupBox1.Controls.Add(this.txtBlfPath);
			this.groupBox1.Controls.Add(this.txtMapInfoPath);
			this.groupBox1.Controls.Add(this.btnAddEntry);
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Controls.Add(this.txtTargets);
			this.groupBox1.Location = new System.Drawing.Point(12, 12);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(591, 133);
			this.groupBox1.TabIndex = 5;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Entry Addition";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(6, 81);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(47, 13);
			this.label3.TabIndex = 7;
			this.label3.Text = "Blf Path:";
			// 
			// txtBlfPath
			// 
			this.txtBlfPath.Location = new System.Drawing.Point(113, 74);
			this.txtBlfPath.Name = "txtBlfPath";
			this.txtBlfPath.Size = new System.Drawing.Size(472, 20);
			this.txtBlfPath.TabIndex = 6;
			this.txtBlfPath.Text = "A:\\Xbox\\Games\\Halo 3\\Map Info\'s. & BLF\'s\\images";
			// 
			// txtMapInfoPath
			// 
			this.txtMapInfoPath.Location = new System.Drawing.Point(113, 48);
			this.txtMapInfoPath.Name = "txtMapInfoPath";
			this.txtMapInfoPath.Size = new System.Drawing.Size(472, 20);
			this.txtMapInfoPath.TabIndex = 5;
			this.txtMapInfoPath.Text = "A:\\Xbox\\Games\\Halo 3\\Map Info\'s. & BLF\'s\\info";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(6, 51);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(74, 13);
			this.label2.TabIndex = 4;
			this.label2.Text = "MapInfo Path:";
			// 
			// btnGenerate
			// 
			this.btnGenerate.Location = new System.Drawing.Point(12, 313);
			this.btnGenerate.Name = "btnGenerate";
			this.btnGenerate.Size = new System.Drawing.Size(591, 23);
			this.btnGenerate.TabIndex = 6;
			this.btnGenerate.Text = "Generate";
			this.btnGenerate.UseVisualStyleBackColor = true;
			this.btnGenerate.Click += new System.EventHandler(this.btnGenerate_Click);
			// 
			// txtOutput
			// 
			this.txtOutput.BackColor = System.Drawing.Color.White;
			this.txtOutput.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.txtOutput.Dock = System.Windows.Forms.DockStyle.Fill;
			this.txtOutput.Location = new System.Drawing.Point(3, 16);
			this.txtOutput.Multiline = true;
			this.txtOutput.Name = "txtOutput";
			this.txtOutput.Size = new System.Drawing.Size(551, 305);
			this.txtOutput.TabIndex = 7;
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.txtOutput);
			this.groupBox2.Location = new System.Drawing.Point(609, 12);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(557, 324);
			this.groupBox2.TabIndex = 8;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Entry Addition";
			// 
			// deleteToolStripMenuItem
			// 
			this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
			this.deleteToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
			this.deleteToolStripMenuItem.Text = "Delete";
			this.deleteToolStripMenuItem.Click += new System.EventHandler(this.deleteToolStripMenuItem_Click);
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1175, 348);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.btnGenerate);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.lvList);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.Name = "Form1";
			this.Text = "Cache Meta Content - Generator";
			this.contextMenuStrip1.ResumeLayout(false);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button btnAddEntry;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ListView lvList;
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
		private System.Windows.Forms.TextBox txtTargets;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TextBox txtBlfPath;
		private System.Windows.Forms.TextBox txtMapInfoPath;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.Button btnGenerate;
		private System.Windows.Forms.TextBox txtOutput;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
	}
}

