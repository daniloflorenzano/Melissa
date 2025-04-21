namespace Melissa.DesktopClient;

partial class Form1
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
        MicBtn = new Button();
        SuspendLayout();
        // 
        // MicBtn
        // 
        MicBtn.Image = Properties.Resources.mic;
        MicBtn.Location = new Point(40, 99);
        MicBtn.Name = "MicBtn";
        MicBtn.Size = new Size(253, 252);
        MicBtn.TabIndex = 0;
        MicBtn.Text = "button1";
        MicBtn.UseVisualStyleBackColor = true;
        MicBtn.MouseDown += MicBtn_MouseDown;
        MicBtn.MouseUp += MicBtn_MouseUp;
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(332, 450);
        Controls.Add(MicBtn);
        Name = "Form1";
        Text = "Form1";
        ResumeLayout(false);
    }

    #endregion

    private Button MicBtn;
}