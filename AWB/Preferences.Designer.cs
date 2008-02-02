//$Header: /cvsroot/autowikibrowser/src/Project\040select.Designer.cs,v 1.15 2006/06/15 10:14:49 wikibluemoose Exp $

namespace AutoWikiBrowser
{
    partial class MyPreferences
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
                if (f != null) f.Dispose();

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MyPreferences));
            this.cmboLang = new System.Windows.Forms.ComboBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.cmboProject = new System.Windows.Forms.ComboBox();
            this.lblLang = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.btnTextBoxFont = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lblPostfix = new System.Windows.Forms.Label();
            this.cmboCustomProject = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.chkAlwaysConfirmExit = new System.Windows.Forms.CheckBox();
            this.chkSupressAWB = new System.Windows.Forms.CheckBox();
            this.label7 = new System.Windows.Forms.Label();
            this.numTimeOutLimit = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.chkSaveArticleList = new System.Windows.Forms.CheckBox();
            this.chkMinimize = new System.Windows.Forms.CheckBox();
            this.chkBeep = new System.Windows.Forms.CheckBox();
            this.chkFlash = new System.Windows.Forms.CheckBox();
            this.chkLowPriority = new System.Windows.Forms.CheckBox();
            this.chkAutoSaveEdit = new System.Windows.Forms.CheckBox();
            this.fontDialog = new System.Windows.Forms.FontDialog();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.btnSetFile = new System.Windows.Forms.Button();
            this.txtAutosave = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.numEditBoxAutosave = new System.Windows.Forms.NumericUpDown();
            this.label8 = new System.Windows.Forms.Label();
            this.saveFile = new System.Windows.Forms.SaveFileDialog();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.PrivacyCheckBox = new System.Windows.Forms.CheckBox();
            this.PrivacyLabel = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numTimeOutLimit)).BeginInit();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numEditBoxAutosave)).BeginInit();
            this.groupBox4.SuspendLayout();
            this.SuspendLayout();
            // 
            // cmboLang
            // 
            this.cmboLang.DropDownHeight = 212;
            this.cmboLang.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmboLang.FormattingEnabled = true;
            this.cmboLang.IntegralHeight = false;
            this.cmboLang.Location = new System.Drawing.Point(77, 90);
            this.cmboLang.Name = "cmboLang";
            this.cmboLang.Size = new System.Drawing.Size(121, 21);
            this.cmboLang.TabIndex = 0;
            // 
            // btnOK
            // 
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Location = new System.Drawing.Point(475, 301);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 1;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnApply_Click);
            // 
            // cmboProject
            // 
            this.cmboProject.DropDownHeight = 206;
            this.cmboProject.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmboProject.FormattingEnabled = true;
            this.cmboProject.IntegralHeight = false;
            this.cmboProject.Location = new System.Drawing.Point(77, 63);
            this.cmboProject.Name = "cmboProject";
            this.cmboProject.Size = new System.Drawing.Size(121, 21);
            this.cmboProject.TabIndex = 2;
            this.cmboProject.SelectedIndexChanged += new System.EventHandler(this.cmboProject_SelectedIndexChanged);
            // 
            // lblLang
            // 
            this.lblLang.Location = new System.Drawing.Point(13, 93);
            this.lblLang.Name = "lblLang";
            this.lblLang.Size = new System.Drawing.Size(58, 13);
            this.lblLang.TabIndex = 3;
            this.lblLang.Text = "Language:";
            this.lblLang.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(28, 66);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(43, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Project:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 25);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(290, 26);
            this.label3.TabIndex = 5;
            this.label3.Text = "Warning!  For languages and projects other than the English\r\n Wikipedia this soft" +
                "ware may not function properly.";
            this.label3.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // btnTextBoxFont
            // 
            this.btnTextBoxFont.Location = new System.Drawing.Point(6, 19);
            this.btnTextBoxFont.Name = "btnTextBoxFont";
            this.btnTextBoxFont.Size = new System.Drawing.Size(112, 23);
            this.btnTextBoxFont.TabIndex = 11;
            this.btnTextBoxFont.Text = "Set edit box font";
            this.btnTextBoxFont.UseVisualStyleBackColor = true;
            this.btnTextBoxFont.Click += new System.EventHandler(this.btnTextBoxFont_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(556, 301);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 12;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.lblPostfix);
            this.groupBox1.Controls.Add(this.cmboCustomProject);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.cmboLang);
            this.groupBox1.Controls.Add(this.cmboProject);
            this.groupBox1.Controls.Add(this.lblLang);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(307, 141);
            this.groupBox1.TabIndex = 13;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Project";
            // 
            // lblPostfix
            // 
            this.lblPostfix.AutoSize = true;
            this.lblPostfix.Location = new System.Drawing.Point(205, 93);
            this.lblPostfix.Name = "lblPostfix";
            this.lblPostfix.Size = new System.Drawing.Size(48, 13);
            this.lblPostfix.TabIndex = 9;
            this.lblPostfix.Text = "lblPostfix";
            // 
            // cmboCustomProject
            // 
            this.cmboCustomProject.FormattingEnabled = true;
            this.cmboCustomProject.Location = new System.Drawing.Point(77, 90);
            this.cmboCustomProject.Name = "cmboCustomProject";
            this.cmboCustomProject.Size = new System.Drawing.Size(121, 21);
            this.cmboCustomProject.TabIndex = 8;
            this.cmboCustomProject.SelectedIndexChanged += new System.EventHandler(this.cmboCustomProjectChanged);
            this.cmboCustomProject.TextChanged += new System.EventHandler(this.cmboCustomProjectChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(9, 122);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(214, 13);
            this.label6.TabIndex = 6;
            this.label6.Text = "Save settings as default to remember details";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.chkAlwaysConfirmExit);
            this.groupBox2.Controls.Add(this.chkSupressAWB);
            this.groupBox2.Controls.Add(this.label7);
            this.groupBox2.Controls.Add(this.numTimeOutLimit);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.chkSaveArticleList);
            this.groupBox2.Controls.Add(this.chkMinimize);
            this.groupBox2.Controls.Add(this.chkBeep);
            this.groupBox2.Controls.Add(this.chkFlash);
            this.groupBox2.Controls.Add(this.chkLowPriority);
            this.groupBox2.Controls.Add(this.btnTextBoxFont);
            this.groupBox2.Location = new System.Drawing.Point(13, 159);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(306, 181);
            this.groupBox2.TabIndex = 14;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Other";
            // 
            // chkAlwaysConfirmExit
            // 
            this.chkAlwaysConfirmExit.AutoSize = true;
            this.chkAlwaysConfirmExit.Checked = true;
            this.chkAlwaysConfirmExit.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkAlwaysConfirmExit.Location = new System.Drawing.Point(8, 158);
            this.chkAlwaysConfirmExit.Name = "chkAlwaysConfirmExit";
            this.chkAlwaysConfirmExit.Size = new System.Drawing.Size(122, 17);
            this.chkAlwaysConfirmExit.TabIndex = 24;
            this.chkAlwaysConfirmExit.Text = "Always confirm Exit?";
            this.chkAlwaysConfirmExit.UseVisualStyleBackColor = true;
            // 
            // chkSupressAWB
            // 
            this.chkSupressAWB.AutoSize = true;
            this.chkSupressAWB.Enabled = false;
            this.chkSupressAWB.Location = new System.Drawing.Point(124, 23);
            this.chkSupressAWB.Name = "chkSupressAWB";
            this.chkSupressAWB.Size = new System.Drawing.Size(138, 17);
            this.chkSupressAWB.TabIndex = 23;
            this.chkSupressAWB.Text = "Suppress \"Using AWB\"";
            this.chkSupressAWB.UseVisualStyleBackColor = true;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(3, 142);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(117, 13);
            this.label7.TabIndex = 22;
            this.label7.Text = "Timeout limit (seconds):";
            // 
            // numTimeOutLimit
            // 
            this.numTimeOutLimit.Location = new System.Drawing.Point(130, 140);
            this.numTimeOutLimit.Maximum = new decimal(new int[] {
            120,
            0,
            0,
            0});
            this.numTimeOutLimit.Minimum = new decimal(new int[] {
            30,
            0,
            0,
            0});
            this.numTimeOutLimit.Name = "numTimeOutLimit";
            this.numTimeOutLimit.Size = new System.Drawing.Size(58, 20);
            this.numTimeOutLimit.TabIndex = 21;
            this.numTimeOutLimit.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 118);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(106, 13);
            this.label1.TabIndex = 19;
            this.label1.Text = "When ready to save:";
            // 
            // chkSaveArticleList
            // 
            this.chkSaveArticleList.AutoSize = true;
            this.chkSaveArticleList.Checked = true;
            this.chkSaveArticleList.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkSaveArticleList.Location = new System.Drawing.Point(6, 94);
            this.chkSaveArticleList.Name = "chkSaveArticleList";
            this.chkSaveArticleList.Size = new System.Drawing.Size(199, 17);
            this.chkSaveArticleList.TabIndex = 18;
            this.chkSaveArticleList.Text = "Save article list when saving settings";
            this.chkSaveArticleList.UseVisualStyleBackColor = true;
            // 
            // chkMinimize
            // 
            this.chkMinimize.AutoSize = true;
            this.chkMinimize.Location = new System.Drawing.Point(6, 71);
            this.chkMinimize.Name = "chkMinimize";
            this.chkMinimize.Size = new System.Drawing.Size(190, 17);
            this.chkMinimize.TabIndex = 17;
            this.chkMinimize.Text = "Minimize to system tray on minimize";
            this.chkMinimize.UseVisualStyleBackColor = true;
            // 
            // chkBeep
            // 
            this.chkBeep.AutoSize = true;
            this.chkBeep.Checked = true;
            this.chkBeep.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkBeep.Location = new System.Drawing.Point(170, 117);
            this.chkBeep.Name = "chkBeep";
            this.chkBeep.Size = new System.Drawing.Size(51, 17);
            this.chkBeep.TabIndex = 16;
            this.chkBeep.Text = "Beep";
            this.chkBeep.UseVisualStyleBackColor = true;
            // 
            // chkFlash
            // 
            this.chkFlash.AutoSize = true;
            this.chkFlash.Checked = true;
            this.chkFlash.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkFlash.Location = new System.Drawing.Point(118, 117);
            this.chkFlash.Name = "chkFlash";
            this.chkFlash.Size = new System.Drawing.Size(51, 17);
            this.chkFlash.TabIndex = 15;
            this.chkFlash.Text = "Flash";
            this.chkFlash.UseVisualStyleBackColor = true;
            // 
            // chkLowPriority
            // 
            this.chkLowPriority.AutoSize = true;
            this.chkLowPriority.Location = new System.Drawing.Point(6, 48);
            this.chkLowPriority.Name = "chkLowPriority";
            this.chkLowPriority.Size = new System.Drawing.Size(278, 17);
            this.chkLowPriority.TabIndex = 13;
            this.chkLowPriority.Text = "Low thread priority (AWB will work in the background)";
            this.chkLowPriority.UseVisualStyleBackColor = true;
            // 
            // chkAutoSaveEdit
            // 
            this.chkAutoSaveEdit.AutoSize = true;
            this.chkAutoSaveEdit.Location = new System.Drawing.Point(6, 19);
            this.chkAutoSaveEdit.Name = "chkAutoSaveEdit";
            this.chkAutoSaveEdit.Size = new System.Drawing.Size(250, 17);
            this.chkAutoSaveEdit.TabIndex = 23;
            this.chkAutoSaveEdit.Text = "Automatically save edit box to prevent lost work";
            this.chkAutoSaveEdit.UseVisualStyleBackColor = true;
            this.chkAutoSaveEdit.CheckedChanged += new System.EventHandler(this.chkAutoSaveEdit_CheckedChanged);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.btnSetFile);
            this.groupBox3.Controls.Add(this.chkAutoSaveEdit);
            this.groupBox3.Controls.Add(this.txtAutosave);
            this.groupBox3.Controls.Add(this.label10);
            this.groupBox3.Controls.Add(this.label9);
            this.groupBox3.Controls.Add(this.numEditBoxAutosave);
            this.groupBox3.Controls.Add(this.label8);
            this.groupBox3.Location = new System.Drawing.Point(325, 12);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(306, 102);
            this.groupBox3.TabIndex = 15;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Auto save edit box";
            // 
            // btnSetFile
            // 
            this.btnSetFile.Enabled = false;
            this.btnSetFile.Location = new System.Drawing.Point(8, 68);
            this.btnSetFile.Name = "btnSetFile";
            this.btnSetFile.Size = new System.Drawing.Size(88, 23);
            this.btnSetFile.TabIndex = 31;
            this.btnSetFile.Text = "Set File";
            this.btnSetFile.UseVisualStyleBackColor = true;
            this.btnSetFile.Click += new System.EventHandler(this.btnSetFile_Click);
            // 
            // txtAutosave
            // 
            this.txtAutosave.Enabled = false;
            this.txtAutosave.Location = new System.Drawing.Point(134, 70);
            this.txtAutosave.Name = "txtAutosave";
            this.txtAutosave.Size = new System.Drawing.Size(161, 20);
            this.txtAutosave.TabIndex = 30;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(102, 73);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(26, 13);
            this.label10.TabIndex = 29;
            this.label10.Text = "File:";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(137, 44);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(47, 13);
            this.label9.TabIndex = 28;
            this.label9.Text = "seconds";
            // 
            // numEditBoxAutosave
            // 
            this.numEditBoxAutosave.Location = new System.Drawing.Point(73, 42);
            this.numEditBoxAutosave.Maximum = new decimal(new int[] {
            300,
            0,
            0,
            0});
            this.numEditBoxAutosave.Minimum = new decimal(new int[] {
            30,
            0,
            0,
            0});
            this.numEditBoxAutosave.Name = "numEditBoxAutosave";
            this.numEditBoxAutosave.Size = new System.Drawing.Size(58, 20);
            this.numEditBoxAutosave.TabIndex = 27;
            this.numEditBoxAutosave.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(3, 44);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(64, 13);
            this.label8.TabIndex = 26;
            this.label8.Text = "Save every:";
            // 
            // saveFile
            // 
            this.saveFile.Filter = ".txt Files|*.txt";
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.PrivacyCheckBox);
            this.groupBox4.Controls.Add(this.PrivacyLabel);
            this.groupBox4.Location = new System.Drawing.Point(325, 120);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(306, 158);
            this.groupBox4.TabIndex = 16;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Privacy";
            // 
            // PrivacyCheckBox
            // 
            this.PrivacyCheckBox.AutoSize = true;
            this.PrivacyCheckBox.Location = new System.Drawing.Point(8, 119);
            this.PrivacyCheckBox.Name = "PrivacyCheckBox";
            this.PrivacyCheckBox.Size = new System.Drawing.Size(226, 17);
            this.PrivacyCheckBox.TabIndex = 18;
            this.PrivacyCheckBox.Text = "Prevent AWB from recording my username";
            this.PrivacyCheckBox.UseVisualStyleBackColor = true;
            // 
            // PrivacyLabel
            // 
            this.PrivacyLabel.Location = new System.Drawing.Point(5, 16);
            this.PrivacyLabel.Name = "PrivacyLabel";
            this.PrivacyLabel.Size = new System.Drawing.Size(296, 100);
            this.PrivacyLabel.TabIndex = 17;
            this.PrivacyLabel.Text = resources.GetString("PrivacyLabel.Text");
            this.toolTip1.SetToolTip(this.PrivacyLabel, resources.GetString("PrivacyLabel.ToolTip"));
            // 
            // MyPreferences
            // 
            this.AcceptButton = this.btnOK;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(650, 348);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "MyPreferences";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Preferences";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numTimeOutLimit)).EndInit();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numEditBoxAutosave)).EndInit();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox cmboLang;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.ComboBox cmboProject;
        private System.Windows.Forms.Label lblLang;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnTextBoxFont;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.FontDialog fontDialog;
        private System.Windows.Forms.CheckBox chkLowPriority;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.CheckBox chkBeep;
        private System.Windows.Forms.CheckBox chkFlash;
        private System.Windows.Forms.CheckBox chkMinimize;
        private System.Windows.Forms.CheckBox chkSaveArticleList;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown numTimeOutLimit;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.CheckBox chkAutoSaveEdit;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.NumericUpDown numEditBoxAutosave;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox txtAutosave;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.ComboBox cmboCustomProject;
        private System.Windows.Forms.Button btnSetFile;
        private System.Windows.Forms.SaveFileDialog saveFile;
        private System.Windows.Forms.Label lblPostfix;
        private System.Windows.Forms.CheckBox chkSupressAWB;
        private System.Windows.Forms.CheckBox chkAlwaysConfirmExit;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Label PrivacyLabel;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.CheckBox PrivacyCheckBox;
    }
}