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
            CboLocalModels = new ComboBox();
            BtnPbiDesktop = new Button();
            BtnValidateSelection = new Button();
            BtnPathSelection = new Button();
            BtnRelationships = new Button();
            BtnGroups = new Button();
            buttonPaths = new Button();
            buttonDump = new Button();
            label1 = new Label();
            BtnLoadFilename = new Button();
            openFile = new OpenFileDialog();
            splitContainer1 = new SplitContainer();
            splitContainer2 = new SplitContainer();
            textResult = new RichTextBox();
            logDisplay = new RichTextBox();
            textRelationships = new RichTextBox();
            panel2 = new Panel();
            btnAddSyntaxExample = new Button();
            label2 = new Label();
            BtnHelp = new Button();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
            splitContainer2.Panel1.SuspendLayout();
            splitContainer2.Panel2.SuspendLayout();
            splitContainer2.SuspendLayout();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(BtnHelp);
            panel1.Controls.Add(CboLocalModels);
            panel1.Controls.Add(BtnPbiDesktop);
            panel1.Controls.Add(BtnValidateSelection);
            panel1.Controls.Add(BtnPathSelection);
            panel1.Controls.Add(BtnRelationships);
            panel1.Controls.Add(BtnGroups);
            panel1.Controls.Add(buttonPaths);
            panel1.Controls.Add(buttonDump);
            panel1.Controls.Add(label1);
            panel1.Controls.Add(BtnLoadFilename);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(1551, 110);
            panel1.TabIndex = 1;
            // 
            // CboLocalModels
            // 
            CboLocalModels.AutoCompleteMode = AutoCompleteMode.Suggest;
            CboLocalModels.AutoCompleteSource = AutoCompleteSource.ListItems;
            CboLocalModels.FormattingEnabled = true;
            CboLocalModels.Location = new Point(85, 11);
            CboLocalModels.Name = "CboLocalModels";
            CboLocalModels.Size = new Size(664, 33);
            CboLocalModels.TabIndex = 11;
            CboLocalModels.SelectedIndexChanged += CboLocalModels_SelectedIndexChanged;
            // 
            // BtnPbiDesktop
            // 
            BtnPbiDesktop.FlatStyle = FlatStyle.System;
            BtnPbiDesktop.Font = new Font("Calibri", 14F, FontStyle.Regular, GraphicsUnit.Point);
            BtnPbiDesktop.Location = new Point(755, 11);
            BtnPbiDesktop.Name = "BtnPbiDesktop";
            BtnPbiDesktop.Size = new Size(51, 34);
            BtnPbiDesktop.TabIndex = 10;
            BtnPbiDesktop.Text = "⟳";
            BtnPbiDesktop.UseVisualStyleBackColor = true;
            BtnPbiDesktop.Click += BtnRefreshLocalInstancesList_Click;
            // 
            // BtnValidateSelection
            // 
            BtnValidateSelection.Location = new Point(445, 60);
            BtnValidateSelection.Name = "BtnValidateSelection";
            BtnValidateSelection.Size = new Size(180, 34);
            BtnValidateSelection.TabIndex = 8;
            BtnValidateSelection.Text = "Validate Selection";
            BtnValidateSelection.UseVisualStyleBackColor = true;
            BtnValidateSelection.Click += BtnValidateSelection_Click;
            // 
            // BtnPathSelection
            // 
            BtnPathSelection.Location = new Point(275, 60);
            BtnPathSelection.Name = "BtnPathSelection";
            BtnPathSelection.Size = new Size(148, 34);
            BtnPathSelection.TabIndex = 7;
            BtnPathSelection.Text = "Path Selection";
            BtnPathSelection.UseVisualStyleBackColor = true;
            BtnPathSelection.Click += BtnPathSelection_Click;
            // 
            // BtnRelationships
            // 
            BtnRelationships.Location = new Point(976, 55);
            BtnRelationships.Name = "BtnRelationships";
            BtnRelationships.Size = new Size(130, 34);
            BtnRelationships.TabIndex = 6;
            BtnRelationships.Text = "Relationships";
            BtnRelationships.UseVisualStyleBackColor = true;
            BtnRelationships.Click += BtnRelationships_Click;
            // 
            // BtnGroups
            // 
            BtnGroups.Location = new Point(641, 60);
            BtnGroups.Name = "BtnGroups";
            BtnGroups.Size = new Size(130, 34);
            BtnGroups.TabIndex = 5;
            BtnGroups.Text = "Ambiguities";
            BtnGroups.UseVisualStyleBackColor = true;
            BtnGroups.Click += BtnAmbiguities_Click;
            // 
            // buttonPaths
            // 
            buttonPaths.Location = new Point(144, 60);
            buttonPaths.Name = "buttonPaths";
            buttonPaths.Size = new Size(112, 34);
            buttonPaths.TabIndex = 4;
            buttonPaths.Text = "Paths";
            buttonPaths.UseVisualStyleBackColor = true;
            buttonPaths.Click += BtnPaths_Click;
            // 
            // buttonDump
            // 
            buttonDump.Location = new Point(12, 60);
            buttonDump.Name = "buttonDump";
            buttonDump.Size = new Size(112, 34);
            buttonDump.TabIndex = 3;
            buttonDump.Text = "Dump";
            buttonDump.UseVisualStyleBackColor = true;
            buttonDump.Click += BtnDump_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 12);
            label1.Name = "label1";
            label1.Size = new Size(67, 25);
            label1.TabIndex = 1;
            label1.Text = "Model:";
            // 
            // BtnLoadFilename
            // 
            BtnLoadFilename.Location = new Point(812, 12);
            BtnLoadFilename.Name = "BtnLoadFilename";
            BtnLoadFilename.Size = new Size(157, 34);
            BtnLoadFilename.TabIndex = 0;
            BtnLoadFilename.Text = "Browse BIM...";
            BtnLoadFilename.UseVisualStyleBackColor = true;
            BtnLoadFilename.Click += BtnBrowse;
            // 
            // openFile
            // 
            openFile.FileName = "openFileDialog1";
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 110);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(splitContainer2);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(textRelationships);
            splitContainer1.Panel2.Controls.Add(panel2);
            splitContainer1.Size = new Size(1551, 739);
            splitContainer1.SplitterDistance = 971;
            splitContainer1.TabIndex = 3;
            // 
            // splitContainer2
            // 
            splitContainer2.Dock = DockStyle.Fill;
            splitContainer2.Location = new Point(0, 0);
            splitContainer2.Name = "splitContainer2";
            splitContainer2.Orientation = Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            splitContainer2.Panel1.Controls.Add(textResult);
            // 
            // splitContainer2.Panel2
            // 
            splitContainer2.Panel2.Controls.Add(logDisplay);
            splitContainer2.Size = new Size(971, 739);
            splitContainer2.SplitterDistance = 355;
            splitContainer2.TabIndex = 4;
            // 
            // textResult
            // 
            textResult.Dock = DockStyle.Fill;
            textResult.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point);
            textResult.Location = new Point(0, 0);
            textResult.Name = "textResult";
            textResult.Size = new Size(971, 355);
            textResult.TabIndex = 0;
            textResult.Text = "";
            // 
            // logDisplay
            // 
            logDisplay.Dock = DockStyle.Fill;
            logDisplay.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point);
            logDisplay.Location = new Point(0, 0);
            logDisplay.Name = "logDisplay";
            logDisplay.Size = new Size(971, 380);
            logDisplay.TabIndex = 4;
            logDisplay.Text = "";
            // 
            // textRelationships
            // 
            textRelationships.Dock = DockStyle.Fill;
            textRelationships.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point);
            textRelationships.ForeColor = Color.DarkBlue;
            textRelationships.Location = new Point(0, 47);
            textRelationships.Name = "textRelationships";
            textRelationships.Size = new Size(576, 692);
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
            // BtnHelp
            // 
            BtnHelp.Location = new Point(987, 13);
            BtnHelp.Name = "BtnHelp";
            BtnHelp.Size = new Size(112, 34);
            BtnHelp.TabIndex = 12;
            BtnHelp.Text = "Help";
            BtnHelp.UseVisualStyleBackColor = true;
            BtnHelp.Click += BtnHelp_Click;
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
            splitContainer2.Panel1.ResumeLayout(false);
            splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
            splitContainer2.ResumeLayout(false);
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private Button BtnLoadFilename;
        private Label label1;
        private OpenFileDialog openFile;
        private Button buttonDump;
        private Button buttonPaths;
        private Button BtnGroups;
        private SplitContainer splitContainer1;
        private Panel panel2;
        private Label label2;
        private RichTextBox textRelationships;
        private Button BtnRelationships;
        private Button btnAddSyntaxExample;
        private SplitContainer splitContainer2;
        private RichTextBox logDisplay;
        private RichTextBox textResult;
        private Button BtnPathSelection;
        private Button BtnValidateSelection;
        private Button BtnPbiDesktop;
        private ComboBox CboLocalModels;
        private Button BtnHelp;
    }
}