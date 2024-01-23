namespace CellEvolutionGraphics
{
    partial class ErrorStat
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
            cartesianChart1 = new LiveCharts.WinForms.CartesianChart();
            SuspendLayout();
            // 
            // cartesianChart1
            // 
            cartesianChart1.BackColor = SystemColors.ControlLightLight;
            cartesianChart1.Dock = DockStyle.Fill;
            cartesianChart1.ForeColor = SystemColors.ControlText;
            cartesianChart1.Location = new Point(0, 0);
            cartesianChart1.Name = "cartesianChart1";
            cartesianChart1.Size = new Size(1294, 664);
            cartesianChart1.TabIndex = 0;
            cartesianChart1.Text = "cartesianChart1";
            // 
            // ErrorStat
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1294, 664);
            Controls.Add(cartesianChart1);
            Name = "ErrorStat";
            Text = "Form1";
            WindowState = FormWindowState.Maximized;
            Load += Form1_Load;
            ResumeLayout(false);
        }

        #endregion

        private LiveCharts.WinForms.CartesianChart cartesianChart1;
    }
}