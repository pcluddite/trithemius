﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Diagnostics;

namespace Trithemius
{
    public partial class TrithemiusForm : Form
    {
        public string message = null;
        private int maxSize = 0;
        private Finished finishedDialog;

        public TrithemiusForm()
        {
            InitializeComponent();
        }

        private void TrithemiusForm_Load(object sender, EventArgs e)
        {
            pixelValueComboBox.SelectedIndex = 0;
        }

        private void LockWindow()
        {
            Enabled = false;
            Text = encodeRadio.Checked ? "Encoding..." : "Decoding...";
        }

        private void UnlockWindow()
        {
            Enabled = true;
            Text = "Trithemius";
        }
        
        /// <summary>
        /// Loads a bitmap into memory so the original file can be saved over
        /// </summary>
        /// <param name="path">the bitmap path</param>
        /// <returns></returns>
        public Bitmap OpenBitmap(string path)
        {
            MemoryStream stream = new MemoryStream();
            using (Image image = Image.FromFile(path)) {
                image.Save(stream, ImageFormat.Png);
            }
            return (Bitmap)Image.FromStream(stream);
        }

        private void encodeButton_Click(object sender, EventArgs e)
        {
            if (imageSaveDialog.ShowDialog() != DialogResult.OK) {
                return;
            }

            LockWindow();

            try {
                Trithemius t = MakeTrithemius();

                byte[] msg;
                if (textRadioButton.Checked) {
                    msg = Encoding.ASCII.GetBytes(message);
                }
                else {
                    msg = File.ReadAllBytes(msgOpenDialog.FileName);
                }
                
                
                encodeWorker.RunWorkerAsync(new object[] { t, msg });
            }
            catch(FileNotFoundException ex) {
                ShowError(ex);
                UnlockWindow();
            }
        }

        private void decodeButton_Click(object sender, EventArgs e)
        {
            LockWindow();
            if (fileRadioButton.Checked) {
                if (msgSaveDialog.ShowDialog() != DialogResult.OK) {
                    UnlockWindow();
                    return;
                }
            }
            try {
                decodeWorker.RunWorkerAsync(new object[] { false, MakeTrithemius(), passwordBox.Text });
            }
            catch (FileNotFoundException ex) {
                UnlockWindow();
                ShowError(ex);
            }
        }

        private void encodeWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try {
                object[] args = (object[])e.Argument;
                Trithemius t = (Trithemius)args[0];
                byte[] msg = (byte[])args[1];

                t.Encode(msg, imageSaveDialog.FileName);
                
                t.Dispose();
                e.Result = true;
            }
            catch (Exception ex) {
                ShowError(ex);
                e.Result = false;
            }
        }

        private void decodeWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try {
                object[] args = (object[])e.Argument;

                bool checkSize = (bool)args[0];
                Trithemius t = (Trithemius)args[1];
                string password = (string)args[2];

                if (checkSize) {
                    int size = t.CheckSize();
                    if (size < 0) {
                        ErrorNoData();
                        e.Result = false;
                        return;
                    }
                    Invoke(new MethodInvoker(delegate
                    {
                        label6.Text = "Detected Message Size: " +
                            Program.SizeToString(size);
                    }));
                    e.Result = true;
                    return;
                }

                byte[] data = t.Decode();

                if (data == null) {
                    ErrorNoData();
                    e.Result = false;
                    return;
                }

                if (!password.Equals("")) {
                    data = Crypto.DecryptStringAES(data, password);
                }

                if (textRadioButton.Checked) {
                    finishedDialog = new Finished(Encoding.ASCII.GetString(data));
                    finishedDialog.ShowDialog();
                }
                else {
                    File.WriteAllBytes(msgSaveDialog.FileName, data);
                }

                t.Dispose();
                e.Result = true;
            }
            catch (Exception ex) {
                if (ex.Message.Contains("Padding")) {
                    ShowErrorT("The decryption code was most likely invalid.");
                }
                else {
                    ShowErrorT(ex);
                }
                e.Result = false;
            }
        }

        private void decodeWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            UnlockWindow();

            if (!(bool)e.Result) {
                return;
            }

            if (fileRadioButton.Checked &&
                MessageBox.Show(this, "Decoding Completed!\r\nWould you like to open the file?",
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) {
                try {
                    Process.Start(msgSaveDialog.FileName);
                }
                catch (Exception ex) {
                    ShowError(ex);
                }
            }
        }

        private Trithemius MakeTrithemius()
        {
            Trithemius t = new Trithemius(OpenBitmap(pathTextbox.Text));
            t.Color = (PixelColor)(pixelValueComboBox.SelectedIndex + 1);
            t.InvertBits = invertBox.Checked;

            if (!seedBox.Text.Equals("")) {
                t.Seed = new TrithemiusSeed(seedBox.Text);
            }

            t.LeastSignificantBits = (int)bitsNumericUpDown.Value;

            return t;
        }

        private void SaveLog(int RGB, Dictionary<int, byte> changes, int size)
        {
            if (changesSaveDialog.ShowDialog() == DialogResult.OK)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("[PROPERTIES]");
                sb.AppendLine(string.Format("COLOR={0}", RGB));
                sb.AppendLine(string.Format("SIZE={0}", size));
                sb.AppendLine("\r\n[ORIGINAL]");
                foreach (KeyValuePair<int, byte> kv in changes)
                {
                    sb.AppendLine(string.Format("{0}={1}", kv.Key, kv.Value));
                }
                File.WriteAllText(changesSaveDialog.FileName, sb.ToString());
            }
        }

        private void browseButton_Click(object sender, EventArgs e)
        {
            if (imageOpenDialog.ShowDialog() == DialogResult.OK)
            {
                Image img = Image.FromFile(imageOpenDialog.FileName);
                maxSize = ((img.Height - 1) * (img.Width - 1)) / 8;
                availableSizeBox.Text = Program.SizeToString(maxSize * (int)bitsNumericUpDown.Value);
                pathTextbox.Text = imageOpenDialog.FileName;
                img.Dispose();
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            writeTextButton.Text = fileRadioButton.Checked ? "Select File" : "Write Text";
        }

        private void encodeWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            UnlockWindow();

            if ((bool)e.Result) {
                MessageBox.Show(this, "Encoding completed! You should spot no visible image differences.", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void writeTextButton_Click(object sender, EventArgs e)
        {
            if (textRadioButton.Checked) {
                Text t = new Text(this);
                t.ShowDialog();
            }
            else {
                msgOpenDialog.ShowDialog();
            }
            RefreshRequiredSize();
        }

        private void RefreshRequiredSize()
        {
            try {
                Trithemius t = new Trithemius(null);
                t.LeastSignificantBits = (int)bitsNumericUpDown.Value;

                byte[] msg;
                if (textRadioButton.Checked) {
                    if (message == null)
                        return;
                    msg = Encoding.ASCII.GetBytes(message);
                }
                else {
                    if (msgOpenDialog.FileName.Equals(""))
                        return;
                    msg = File.ReadAllBytes(msgOpenDialog.FileName);
                }

                if (!passwordBox.Text.Equals(""))
                    msg = Crypto.EncryptStringAES(msg, passwordBox.Text);

                int msgSize = t.GetRequiredSize(msg);

                requiredSizeBox.Text = Program.SizeToString(msgSize);
                if (msgSize > maxSize) {
                    requiredSizeLabel.ForeColor = Color.Red;
                }
                else {
                    requiredSizeLabel.ForeColor = Color.Black;
                }
            }
            catch (FileNotFoundException ex) {
                ShowError(ex);
            }
        }
        
        private void bitsNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            label10.Text = bitsNumericUpDown.Value == 1 ? "Bit" : "Bits";
            label6.Text = "Maximum Avalible Size: " + Program.SizeToString(maxSize
                * (int)bitsNumericUpDown.Value);
            RefreshRequiredSize();
        }

        private void seedBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsDigit(e.KeyChar) && e.KeyChar != '\b') {
                e.Handled = true;
            }
        }

        private void encodeRadio_CheckedChanged(object sender, EventArgs e)
        {
            if (encodeRadio.Checked) {
                encodeButton.Text = "&Encode";
                textRadioButton.Text = "Encode Text";
                fileRadioButton.Text = "Encode File";
                writeTextButton.Visible =  requiredSizeLabel.Visible = requiredSizeBox.Visible = true;
                encodeButton.Click -= decodeButton_Click;
                encodeButton.Click += encodeButton_Click;
            }
            else {
                encodeButton.Text = "D&ecode";
                textRadioButton.Text = "Decode Text";
                fileRadioButton.Text = "Decode File";
                writeTextButton.Visible = requiredSizeLabel.Visible = requiredSizeBox.Visible = false;
                encodeButton.Click -= encodeButton_Click;
                encodeButton.Click += decodeButton_Click;
            }
        }

        private void ErrorNoData()
        {
            ShowErrorT("No decodable data was detected.");
        }

        private void ShowErrorT(Exception ex)
        {
            ShowErrorT(ex.Message + "\n" + ex.TargetSite);
        }

        private void ShowErrorT(string message)
        {
            MessageBox.Show(message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void ShowError(Exception ex)
        {
            ShowError(ex.Message + "\n" + ex.TargetSite);
        }

        private void ShowError(string message)
        {
            MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

    }
}