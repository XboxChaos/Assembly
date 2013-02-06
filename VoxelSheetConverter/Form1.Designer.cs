namespace VoxelSheetConverter
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
			this.btnOpenVoxel = new System.Windows.Forms.Button();
			this.txtVoxelXmlDefinition = new System.Windows.Forms.TextBox();
			this.progressBar1 = new System.Windows.Forms.ProgressBar();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.txtOutputFloats = new System.Windows.Forms.TextBox();
			this.btnConvert = new System.Windows.Forms.Button();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnOpenVoxel
			// 
			this.btnOpenVoxel.Location = new System.Drawing.Point(6, 19);
			this.btnOpenVoxel.Name = "btnOpenVoxel";
			this.btnOpenVoxel.Size = new System.Drawing.Size(75, 23);
			this.btnOpenVoxel.TabIndex = 0;
			this.btnOpenVoxel.Text = "Open";
			this.btnOpenVoxel.UseVisualStyleBackColor = true;
			this.btnOpenVoxel.Click += new System.EventHandler(this.btnOpenVoxel_Click);
			// 
			// txtVoxelXmlDefinition
			// 
			this.txtVoxelXmlDefinition.BackColor = System.Drawing.Color.White;
			this.txtVoxelXmlDefinition.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.txtVoxelXmlDefinition.Location = new System.Drawing.Point(87, 21);
			this.txtVoxelXmlDefinition.Name = "txtVoxelXmlDefinition";
			this.txtVoxelXmlDefinition.ReadOnly = true;
			this.txtVoxelXmlDefinition.Size = new System.Drawing.Size(350, 20);
			this.txtVoxelXmlDefinition.TabIndex = 2;
			// 
			// progressBar1
			// 
			this.progressBar1.Location = new System.Drawing.Point(12, 93);
			this.progressBar1.Name = "progressBar1";
			this.progressBar1.Size = new System.Drawing.Size(443, 21);
			this.progressBar1.TabIndex = 3;
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.btnConvert);
			this.groupBox1.Controls.Add(this.btnOpenVoxel);
			this.groupBox1.Controls.Add(this.txtVoxelXmlDefinition);
			this.groupBox1.Location = new System.Drawing.Point(12, 12);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(443, 75);
			this.groupBox1.TabIndex = 4;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Voxel File";
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.txtOutputFloats);
			this.groupBox2.Location = new System.Drawing.Point(12, 120);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(443, 208);
			this.groupBox2.TabIndex = 7;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Output:";
			// 
			// txtOutputFloats
			// 
			this.txtOutputFloats.BackColor = System.Drawing.Color.White;
			this.txtOutputFloats.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.txtOutputFloats.Dock = System.Windows.Forms.DockStyle.Fill;
			this.txtOutputFloats.Location = new System.Drawing.Point(3, 16);
			this.txtOutputFloats.Multiline = true;
			this.txtOutputFloats.Name = "txtOutputFloats";
			this.txtOutputFloats.ReadOnly = true;
			this.txtOutputFloats.Size = new System.Drawing.Size(437, 189);
			this.txtOutputFloats.TabIndex = 2;
			// 
			// btnConvert
			// 
			this.btnConvert.Location = new System.Drawing.Point(87, 47);
			this.btnConvert.Name = "btnConvert";
			this.btnConvert.Size = new System.Drawing.Size(350, 23);
			this.btnConvert.TabIndex = 3;
			this.btnConvert.Text = "Convert";
			this.btnConvert.UseVisualStyleBackColor = true;
			this.btnConvert.Click += new System.EventHandler(this.btnConvert_Click);
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(465, 340);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.progressBar1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.Name = "Form1";
			this.Text = "Voxel Definition -> Float";
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button btnOpenVoxel;
		private System.Windows.Forms.TextBox txtVoxelXmlDefinition;
		private System.Windows.Forms.ProgressBar progressBar1;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.TextBox txtOutputFloats;
		private System.Windows.Forms.Button btnConvert;
	}
}

