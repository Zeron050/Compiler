namespace Lecser
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            txtInput = new TextBox();
            txtReport = new RichTextBox();
            btnAnalyze = new Button();
            tabControlMain = new TabControl();
            tabPageAnalysis = new TabPage();
            tabPageTables = new TabPage();
            label5 = new Label();
            label4 = new Label();
            label2 = new Label();
            label1 = new Label();
            dgvOperators = new DataGridView();
            dgvNumbers = new DataGridView();
            dgvIdentifiers = new DataGridView();
            dgvKeywords = new DataGridView();
            tabControlMain.SuspendLayout();
            tabPageAnalysis.SuspendLayout();
            tabPageTables.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvOperators).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvNumbers).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvIdentifiers).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvKeywords).BeginInit();
            SuspendLayout();
            // 
            // txtInput
            // 
            txtInput.Location = new Point(6, 3);
            txtInput.Multiline = true;
            txtInput.Name = "txtInput";
            txtInput.ScrollBars = ScrollBars.Vertical;
            txtInput.Size = new Size(833, 450);
            txtInput.TabIndex = 0;
            txtInput.Text = resources.GetString("txtInput.Text");
            // 
            // txtReport
            // 
            txtReport.Font = new Font("Consolas", 10.2F, FontStyle.Regular, GraphicsUnit.Point, 204);
            txtReport.Location = new Point(6, 494);
            txtReport.Name = "txtReport";
            txtReport.ReadOnly = true;
            txtReport.Size = new Size(833, 449);
            txtReport.TabIndex = 1;
            txtReport.Text = "";
            // 
            // btnAnalyze
            // 
            btnAnalyze.Location = new Point(6, 459);
            btnAnalyze.Name = "btnAnalyze";
            btnAnalyze.Size = new Size(198, 29);
            btnAnalyze.TabIndex = 2;
            btnAnalyze.Text = "Анализировать ";
            btnAnalyze.UseVisualStyleBackColor = true;
            btnAnalyze.Click += btnAnalyze_Click;
            // 
            // tabControlMain
            // 
            tabControlMain.Controls.Add(tabPageAnalysis);
            tabControlMain.Controls.Add(tabPageTables);
            tabControlMain.Dock = DockStyle.Fill;
            tabControlMain.Location = new Point(0, 0);
            tabControlMain.Name = "tabControlMain";
            tabControlMain.RightToLeft = RightToLeft.No;
            tabControlMain.SelectedIndex = 0;
            tabControlMain.Size = new Size(850, 982);
            tabControlMain.TabIndex = 3;
            // 
            // tabPageAnalysis
            // 
            tabPageAnalysis.Controls.Add(txtInput);
            tabPageAnalysis.Controls.Add(btnAnalyze);
            tabPageAnalysis.Controls.Add(txtReport);
            tabPageAnalysis.Location = new Point(4, 29);
            tabPageAnalysis.Name = "tabPageAnalysis";
            tabPageAnalysis.Padding = new Padding(3);
            tabPageAnalysis.Size = new Size(842, 949);
            tabPageAnalysis.TabIndex = 0;
            tabPageAnalysis.Text = "Анализ";
            tabPageAnalysis.UseVisualStyleBackColor = true;
            // 
            // tabPageTables
            // 
            tabPageTables.Controls.Add(label5);
            tabPageTables.Controls.Add(label4);
            tabPageTables.Controls.Add(label2);
            tabPageTables.Controls.Add(label1);
            tabPageTables.Controls.Add(dgvOperators);
            tabPageTables.Controls.Add(dgvNumbers);
            tabPageTables.Controls.Add(dgvIdentifiers);
            tabPageTables.Controls.Add(dgvKeywords);
            tabPageTables.Location = new Point(4, 29);
            tabPageTables.Name = "tabPageTables";
            tabPageTables.Padding = new Padding(3);
            tabPageTables.Size = new Size(842, 949);
            tabPageTables.TabIndex = 1;
            tabPageTables.Text = "Таблицы лексем";
            tabPageTables.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(592, 353);
            label5.Name = "label5";
            label5.Size = new Size(51, 20);
            label5.TabIndex = 12;
            label5.Text = "Числа";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(145, 353);
            label4.Name = "label4";
            label4.Size = new Size(89, 20);
            label4.TabIndex = 11;
            label4.Text = "Операторы";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(566, 3);
            label2.Name = "label2";
            label2.Size = new Size(129, 20);
            label2.TabIndex = 9;
            label2.Text = "Идентификаторы";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(122, 3);
            label1.Name = "label1";
            label1.Size = new Size(125, 20);
            label1.TabIndex = 8;
            label1.Text = "Ключевые слова";
            // 
            // dgvOperators
            // 
            dgvOperators.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvOperators.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvOperators.Location = new Point(8, 376);
            dgvOperators.Name = "dgvOperators";
            dgvOperators.ReadOnly = true;
            dgvOperators.RowHeadersWidth = 51;
            dgvOperators.Size = new Size(412, 328);
            dgvOperators.TabIndex = 6;
            // 
            // dgvNumbers
            // 
            dgvNumbers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvNumbers.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvNumbers.Location = new Point(426, 376);
            dgvNumbers.Name = "dgvNumbers";
            dgvNumbers.ReadOnly = true;
            dgvNumbers.RowHeadersWidth = 51;
            dgvNumbers.Size = new Size(410, 328);
            dgvNumbers.TabIndex = 5;
            // 
            // dgvIdentifiers
            // 
            dgvIdentifiers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvIdentifiers.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvIdentifiers.Location = new Point(426, 25);
            dgvIdentifiers.Name = "dgvIdentifiers";
            dgvIdentifiers.ReadOnly = true;
            dgvIdentifiers.RowHeadersWidth = 51;
            dgvIdentifiers.Size = new Size(408, 315);
            dgvIdentifiers.TabIndex = 4;
            // 
            // dgvKeywords
            // 
            dgvKeywords.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvKeywords.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvKeywords.Location = new Point(8, 25);
            dgvKeywords.Name = "dgvKeywords";
            dgvKeywords.ReadOnly = true;
            dgvKeywords.RowHeadersWidth = 51;
            dgvKeywords.Size = new Size(412, 315);
            dgvKeywords.TabIndex = 0;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(850, 982);
            Controls.Add(tabControlMain);
            Name = "MainForm";
            Text = "Analizator";
            tabControlMain.ResumeLayout(false);
            tabPageAnalysis.ResumeLayout(false);
            tabPageAnalysis.PerformLayout();
            tabPageTables.ResumeLayout(false);
            tabPageTables.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvOperators).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvNumbers).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvIdentifiers).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvKeywords).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private TextBox txtInput;
        private RichTextBox txtReport;
        private Button btnAnalyze;
        private TabControl tabControlMain;
        private TabPage tabPageAnalysis;
        private TabPage tabPageTables;
        private DataGridView dgvNumbers;
        private DataGridView dgvIdentifiers;
        private DataGridView dgvKeywords;
        private DataGridView dgvOperators;
        private Label label5;
        private Label label4;
        private Label label2;
        private Label label1;
    }
}
