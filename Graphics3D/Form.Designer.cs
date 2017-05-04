namespace Graphics3D
{
   partial class Form
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
         this.PictureBox = new System.Windows.Forms.PictureBox();
         this.timer_draw = new System.Windows.Forms.Timer(this.components);
         ((System.ComponentModel.ISupportInitialize)(this.PictureBox)).BeginInit();
         this.SuspendLayout();
         // 
         // PictureBox
         // 
         this.PictureBox.BackColor = System.Drawing.SystemColors.Control;
         this.PictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
         this.PictureBox.Location = new System.Drawing.Point(0, 0);
         this.PictureBox.Margin = new System.Windows.Forms.Padding(0);
         this.PictureBox.Name = "PictureBox";
         this.PictureBox.Size = new System.Drawing.Size(784, 561);
         this.PictureBox.TabIndex = 0;
         this.PictureBox.TabStop = false;
         this.PictureBox.Paint += new System.Windows.Forms.PaintEventHandler(this.PictureBox_Paint);
         // 
         // timer_draw
         // 
         this.timer_draw.Enabled = true;
         this.timer_draw.Interval = 16;
         this.timer_draw.Tick += new System.EventHandler(this.timer_draw_Tick);
         // 
         // Form
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.BackColor = System.Drawing.SystemColors.ActiveCaption;
         this.ClientSize = new System.Drawing.Size(784, 561);
         this.Controls.Add(this.PictureBox);
         this.DoubleBuffered = true;
         this.Name = "Form";
         this.Text = "Graphics3D";
         this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form_FormClosing);
         this.Load += new System.EventHandler(this.Form_Load);
         this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form_KeyDown);
         this.Resize += new System.EventHandler(this.Form_Resize);
         ((System.ComponentModel.ISupportInitialize)(this.PictureBox)).EndInit();
         this.ResumeLayout(false);

      }

      #endregion

      private System.Windows.Forms.PictureBox PictureBox;
      private System.Windows.Forms.Timer timer_draw;
   }
}

