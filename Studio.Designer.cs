namespace RelationshipsStudio
{
    partial class Studio
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
            panel1 = new Panel();
            BtnRelationships = new Button();
            BtnGroups = new Button();
            buttonPaths = new Button();
            buttonDump = new Button();
            textFilename = new TextBox();
            label1 = new Label();
            btnLoadFilename = new Button();
            openFile = new OpenFileDialog();
            splitContainer1 = new SplitContainer();
            logDisplay = new RichTextBox();
            textRelationships = new RichTextBox();
            panel2 = new Panel();
            btnAddSyntaxExample = new Button();
            label2 = new Label();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(BtnRelationships);
            panel1.Controls.Add(BtnGroups);
            panel1.Controls.Add(buttonPaths);
            panel1.Controls.Add(buttonDump);
            panel1.Controls.Add(textFilename);
            panel1.Controls.Add(label1);
            panel1.Controls.Add(btnLoadFilename);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(1551, 96);
            panel1.TabIndex = 1;
            // 
            // BtnRelationships
            // 
            BtnRelationships.Location = new Point(975, 46);
            BtnRelationships.Name = "BtnRelationships";
            BtnRelationships.Size = new Size(130, 34);
            BtnRelationships.TabIndex = 6;
            BtnRelationships.Text = "Relationships";
            BtnRelationships.UseVisualStyleBackColor = true;
            BtnRelationships.Click += BtnRelationships_Click;
            // 
            // BtnGroups
            // 
            BtnGroups.Location = new Point(281, 46);
            BtnGroups.Name = "BtnGroups";
            BtnGroups.Size = new Size(130, 34);
            BtnGroups.TabIndex = 5;
            BtnGroups.Text = "Ambiguities";
            BtnGroups.UseVisualStyleBackColor = true;
            BtnGroups.Click += BtnAmbiguities_Click;
            // 
            // buttonPaths
            // 
            buttonPaths.Location = new Point(144, 46);
            buttonPaths.Name = "buttonPaths";
            buttonPaths.Size = new Size(112, 34);
            buttonPaths.TabIndex = 4;
            buttonPaths.Text = "Paths";
            buttonPaths.UseVisualStyleBackColor = true;
            buttonPaths.Click += BtnPaths_Click;
            // 
            // buttonDump
            // 
            buttonDump.Location = new Point(12, 46);
            buttonDump.Name = "buttonDump";
            buttonDump.Size = new Size(112, 34);
            buttonDump.TabIndex = 3;
            buttonDump.Text = "Dump";
            buttonDump.UseVisualStyleBackColor = true;
            buttonDump.Click += BtnDump_Click;
            // 
            // textFilename
            // 
            textFilename.Location = new Point(93, 9);
            textFilename.Name = "textFilename";
            textFilename.Size = new Size(570, 31);
            textFilename.TabIndex = 2;
            textFilename.Text = "c:\\temp\\relationships-ambiguity-3.bim";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 12);
            label1.Name = "label1";
            label1.Size = new Size(75, 25);
            label1.TabIndex = 1;
            label1.Text = "BIM file:";
            // 
            // btnLoadFilename
            // 
            btnLoadFilename.Location = new Point(669, 7);
            btnLoadFilename.Name = "btnLoadFilename";
            btnLoadFilename.Size = new Size(112, 34);
            btnLoadFilename.TabIndex = 0;
            btnLoadFilename.Text = "Browse...";
            btnLoadFilename.UseVisualStyleBackColor = true;
            btnLoadFilename.Click += BtnBrowse;
            // 
            // openFile
            // 
            openFile.FileName = "openFileDialog1";
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 96);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(logDisplay);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(textRelationships);
            splitContainer1.Panel2.Controls.Add(panel2);
            splitContainer1.Size = new Size(1551, 753);
            splitContainer1.SplitterDistance = 971;
            splitContainer1.TabIndex = 3;
            // 
            // logDisplay
            // 
            logDisplay.Dock = DockStyle.Fill;
            logDisplay.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point);
            logDisplay.Location = new Point(0, 0);
            logDisplay.Name = "logDisplay";
            logDisplay.Size = new Size(971, 753);
            logDisplay.TabIndex = 3;
            logDisplay.Text = "";
            // 
            // textRelationships
            // 
            textRelationships.Dock = DockStyle.Fill;
            textRelationships.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point);
            textRelationships.ForeColor = Color.DarkBlue;
            textRelationships.Location = new Point(0, 47);
            textRelationships.Name = "textRelationships";
            textRelationships.Size = new Size(576, 706);
            textRelationships.TabIndex = 2;
            textRelationships.Text = "1,USERELATIONSHIP,B2[Name],B1[Name]\n";
            // 
            // panel2
            // 
            panel2.Controls.Add(btnAddSyntaxExample);
            panel2.Controls.Add(label2);
            panel2.Dock = DockStyle.Top;
            panel2.Location = new Point(0, 0);
            panel2.Name = "panel2";
            panel2.Size = new Size(576, 47);
            panel2.TabIndex = 1;
            // 
            // btnAddSyntaxExample
            // 
            btnAddSyntaxExample.Location = new Point(227, 6);
            btnAddSyntaxExample.Name = "btnAddSyntaxExample";
            btnAddSyntaxExample.Size = new Size(180, 34);
            btnAddSyntaxExample.TabIndex = 2;
            btnAddSyntaxExample.Text = "Syntax example";
            btnAddSyntaxExample.UseVisualStyleBackColor = true;
            btnAddSyntaxExample.Click += BtnAddSyntaxExample_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 10);
            label2.Name = "label2";
            label2.Size = new Size(185, 25);
            label2.TabIndex = 1;
            label2.Text = "Relationships Settings";
            label2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // Studio
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1551, 849);
            Controls.Add(splitContainer1);
            Controls.Add(panel1);
            Name = "Studio";
            Text = "Relationship Studio";
            FormClosing += Studio_FormClosing;
            Load += Studio_Load;
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private Button btnLoadFilename;
        private TextBox textFilename;
        private Label label1;
        private OpenFileDialog openFile;
        private Button buttonDump;
        private Button buttonPaths;
        private Button BtnGroups;
        private SplitContainer splitContainer1;
        private RichTextBox logDisplay;
        private Panel panel2;
        private Label label2;
        private RichTextBox textRelationships;
        private Button BtnRelationships;
        private Button btnAddSyntaxExample;
    }
}