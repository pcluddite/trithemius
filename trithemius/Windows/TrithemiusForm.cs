// =====
//
// Copyright (c) 2013-2026 Timothy Baxendale
//
// =====
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using Monk.Imaging;
using Monk.Memory.Bittwiddling;

namespace Trithemius.Windows
{
    public partial class TrithemiusForm : Form
    {
        private readonly TextForm _inputTextForm;

        public TrithemiusForm()
        {
            InitializeComponent();
            _inputTextForm = new TextForm();
        }

        private void TrithemiusForm_Load(object sender, EventArgs e)
        {
            comboBoxEndian.SelectedIndex = 0;
            comboBoxVersions.SelectedIndex = 0;
        }

        private void SetEnabled(bool enabled)
        {
            buttonEncode.Enabled = buttonDecode.Enabled = groupBoxPath.Enabled = 
                groupBoxImage.Enabled = groupBoxEncode.Enabled = enabled;
        }

        private Process TryStart(ProcessStartInfo startInfo, bool displayErrors = true)
        {
            try {
                return Process.Start(startInfo);
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is IOException || ex is Win32Exception) {
                if (displayErrors) ShowError(ex.Message);
                return null;
            }
        }

        private static Image LoadImage(string filename)
        {
            Stream stream = new MemoryStream(File.ReadAllBytes(filename));
            return Bitmap.FromStream(stream);
        }

        private static Image CopyImage(Image image)
        {
            Stream stream = new MemoryStream();
            image.Save(stream, image.RawFormat);
            return Bitmap.FromStream(stream);
        }

        private DialogResult ShowError(string message)
        {
            return ShowError(message, MessageBoxButtons.OK);
        }

        private DialogResult ShowError(string message, MessageBoxButtons buttons)
        {
            return MessageBox.Show(this, message, Text, buttons, MessageBoxIcon.Warning);
        }

        private DialogResult ShowInfo(string message)
        {
            return ShowInfo(message, MessageBoxButtons.OK);
        }

        private DialogResult ShowInfo(string message, MessageBoxButtons buttons)
        {
            return MessageBox.Show(this, message, Text, buttons, MessageBoxIcon.Information);
        }

        private DialogResult ShowQuestion(string message)
        {
            return ShowQuestion(message, MessageBoxButtons.YesNo);
        }

        private DialogResult ShowQuestion(string message, MessageBoxButtons buttons)
        {
            return MessageBox.Show(this, message, Text, buttons, MessageBoxIcon.Question);
        }

        private bool IsLegacyMode()
        {
            return comboBoxVersions.SelectedIndex > 0;
        }

        private SteganographyInfo CreateTrithemius()
        {
            try {
                SteganographyInfo trithemius;
                
                if (comboBoxVersions.SelectedIndex == 1) {
                    trithemius = SteganographyInfo.PresetsA0001;
                } else if (comboBoxVersions.SelectedIndex == 2) {
                    trithemius = SteganographyInfo.PresetsB0003;
                } else {
                    trithemius = new SteganographyInfo()
                    {
                        Offset               = (int)numericUpDownOffset.Value - 1,
                        LeastSignificantBits = (int)numericUpDownLsb.Value,
                        InvertDataBits       = checkBoxInvertData.Checked,
                        InvertPrefixBits     = checkBoxInvertPrefix.Checked,
                        Endianness           = comboBoxEndian.SelectedIndex == 0 ? EndianMode.LittleEndian : EndianMode.BigEndian,
                        ZeroBasedSize        = checkBoxZeroBased.Checked
                    };
                }

                if (!string.IsNullOrEmpty(textBoxSeed.Text)) {
                    string seedText = textBoxSeed.Text;
                    ushort[] digits = new ushort[seedText.Length];
                    for (int idx = 0; idx < seedText.Length; ++idx) {
                        char c = seedText[idx];
                        if (char.IsDigit(c)) {
                            digits[idx] = (ushort)(c - '0');
                        }
                        else {
                            textBoxSeed.Select(idx, 1);
                            textBoxSeed.Focus();
                            throw new ArgumentException("All characters in a seed must be a number");
                        }
                    }
                    trithemius.Seed = digits;
                }

                trithemius.Colors = PixelColor.None;
                if (checkAlpha.Checked && checkAlpha.Enabled) trithemius.Colors |= PixelColor.Alpha;
                if (checkRed.Checked && checkRed.Enabled) trithemius.Colors |= PixelColor.Red;
                if (checkGreen.Checked && checkGreen.Enabled) trithemius.Colors |= PixelColor.Green;
                if (checkBlue.Checked && checkBlue.Enabled) trithemius.Colors |= PixelColor.Blue;
                return trithemius;
            }
            catch (ArgumentException ex) {
                ShowError(ex.Message);
                return null;
            }
        }
    }
}
