using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sharp7;

namespace DataBlockReadWrite
{
    
    public partial class Form1 : Form
    {

        #region variable

        private S7Client Client;
        private byte[] Buffer = new byte[65536];
        private byte[] DB_A = new byte[1024];
        private byte[] DB_B = new byte[1024];
        private byte[] DB_C = new byte[1024];

        #endregion



        public Form1()
        {
            InitializeComponent();
            this.Text = "DB READ/WRITE, PPE690 tool (Inpro Electric - Spisiak)";
            Client = new S7Client();

            // zatial nieje potreba mat variabilne
            TxtSize.Enabled = false;
        }


        private void ShowResult(int Result)
        {
            // This function returns a textual explaination of the error code
            TextError.Text = Client.ErrorText(Result);
            if (Result == 0)
                TextError.Text = TextError.Text + " (" + Client.ExecutionTime.ToString() + " ms)";
        }


        private void HexDump(TextBox DumpBox, byte[] bytes, int Size)
        {
            if (bytes == null)
                return;
            int bytesLength = Size;
            int bytesPerLine = 16;

            char[] HexChars = "0123456789ABCDEF".ToCharArray();

            int firstHexColumn =
                  8                   // 8 characters for the address
                + 3;                  // 3 spaces

            int firstCharColumn = firstHexColumn
                + bytesPerLine * 3       // - 2 digit for the hexadecimal value and 1 space
                + (bytesPerLine - 1) / 8 // - 1 extra space every 8 characters from the 9th
                + 2;                  // 2 spaces 

            int lineLength = firstCharColumn
                + bytesPerLine           // - characters to show the ascii value
                + Environment.NewLine.Length; // Carriage return and line feed (should normally be 2)

            char[] line = (new String(' ', lineLength - 2) + Environment.NewLine).ToCharArray();
            int expectedLines = (bytesLength + bytesPerLine - 1) / bytesPerLine;
            StringBuilder result = new StringBuilder(expectedLines * lineLength);

            for (int i = 0; i < bytesLength; i += bytesPerLine)
            {
                line[0] = HexChars[(i >> 28) & 0xF];
                line[1] = HexChars[(i >> 24) & 0xF];
                line[2] = HexChars[(i >> 20) & 0xF];
                line[3] = HexChars[(i >> 16) & 0xF];
                line[4] = HexChars[(i >> 12) & 0xF];
                line[5] = HexChars[(i >> 8) & 0xF];
                line[6] = HexChars[(i >> 4) & 0xF];
                line[7] = HexChars[(i >> 0) & 0xF];

                int hexColumn = firstHexColumn;
                int charColumn = firstCharColumn;

                for (int j = 0; j < bytesPerLine; j++)
                {
                    if (j > 0 && (j & 7) == 0) hexColumn++;
                    if (i + j >= bytesLength)
                    {
                        line[hexColumn] = ' ';
                        line[hexColumn + 1] = ' ';
                        line[charColumn] = ' ';
                    }
                    else
                    {
                        byte b = bytes[i + j];
                        line[hexColumn] = HexChars[(b >> 4) & 0xF];
                        line[hexColumn + 1] = HexChars[b & 0xF];
                        line[charColumn] = (b < 32 ? '·' : (char)b);
                    }
                    hexColumn += 3;
                    charColumn++;
                }
                result.Append(line);
            }
            DumpBox.Text = result.ToString();
        }


        private void ReadArea()
        {
            // Declaration separated from the code for readability
            int DBNumber;
            int Amount;
            int SizeRead = 0;
            int Result;
            int[] Area =
            {
                S7Consts.S7AreaPE,
                S7Consts.S7AreaPA,
                S7Consts.S7AreaMK,
                S7Consts.S7AreaDB,
                S7Consts.S7AreaCT,
                S7Consts.S7AreaTM
            };
            int[] WordLen =
            {
                S7Consts.S7WLBit,
                S7Consts.S7WLByte,
                S7Consts.S7WLChar,
                S7Consts.S7WLWord,
                S7Consts.S7WLInt,
                S7Consts.S7WLDWord,
                S7Consts.S7WLDInt,
                S7Consts.S7WLReal,
                S7Consts.S7WLCounter,
                S7Consts.S7WLTimer
            };

            TxtDump.Text = "";

            DBNumber = System.Convert.ToInt32(TxtDB.Text);
            Amount = System.Convert.ToInt32(TxtSize.Text);
            Result = Client.ReadArea(S7Consts.S7AreaDB, DBNumber, 0, Amount, S7Consts.S7WLByte, Buffer, ref SizeRead);


            ShowResult(Result);
            if (Result == 0)
            {
                HexDump(TxtDump, Buffer, SizeRead);


                bin.data_read = Buffer;

                MessageBox.Show("Nacitane " + " (" + Client.ExecutionTime.ToString() + " ms)", "PLC Read", MessageBoxButtons.OK);

            }


        }


        private void ConnectBtn_Click(object sender, EventArgs e)
        {
            int Result;
            int Rack = System.Convert.ToInt32(TxtRack.Text);
            int Slot = System.Convert.ToInt32(TxtSlot.Text);
            Result = Client.ConnectTo(TxtIP.Text, Rack, Slot);
            ShowResult(Result);
            if (Result == 0)
            {
                TextError.Text = TextError.Text + " PDU Negotiated : " + Client.PduSizeNegotiated.ToString();
                TxtIP.Enabled = false;
                TxtRack.Enabled = false;
                TxtSlot.Enabled = false;
                ConnectBtn.Enabled = false;
                DisconnectBtn.Enabled = true;
                button4.Enabled = true;
                ReadBtn.Enabled = true;
            }
        }

        private void DisconnectBtn_Click(object sender, EventArgs e)
        {
            Client.Disconnect();
            TextError.Text = "Disconnected";
            TxtIP.Enabled = true;
            TxtRack.Enabled = true;
            TxtSlot.Enabled = true;
            ConnectBtn.Enabled = true;
            DisconnectBtn.Enabled = false;
            button4.Enabled = false;
            ReadBtn.Enabled = false;
        }

        private void ReadBtn_Click(object sender, EventArgs e)
        {
            ReadArea();

            label40.Text = "--";
            label40.BackColor = SystemColors.Control;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //int Result = Client.WriteArea(S7Consts.S7AreaCT, 0, 1, 3, S7Consts.S7WLCounter, bin.data);
            int DBNumber;
            int Amount;

            DBNumber = System.Convert.ToInt32(TxtDB.Text);
            Amount = System.Convert.ToInt32(TxtSize.Text);

            int Result = Client.WriteArea(S7Consts.S7AreaDB, DBNumber, 0, Amount, S7Consts.S7WLByte, bin.data_write);
            ShowResult(Result);
            if (Result == 0)
            {

                MessageBox.Show("Zapisane " + " (" + Client.ExecutionTime.ToString() + " ms)", "PLC Write", MessageBoxButtons.OK);

            }

            label40.Text = "--";
            label40.BackColor = SystemColors.Control;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.InitialDirectory = "This PC\\Documents";
            openFileDialog1.Filter = "BIN Files (*.bin)|*.bin";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = true;
            openFileDialog1.Title = "Open a file";



            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if (openFileDialog1.CheckPathExists)
                {

                    using (BinaryReader BinaryRead = new BinaryReader(File.Open(openFileDialog1.FileName, FileMode.Open)))
                    {

                        bin.data_write = new byte[BinaryRead.BaseStream.Length];
                        for (int pos = 0; pos < BinaryRead.BaseStream.Length; pos++)
                        {
                            bin.data_write[pos] = (byte)BinaryRead.BaseStream.ReadByte();
                            //if (pos > 100 && pos < 200)
                            //    richTextBox1.Text = richTextBox1.Text + " " + (bin.data[pos].ToString());
                        }

                        BinaryRead.Close();

                        HexDump(TxtDump1, bin.data_write, 32000);

                        label38.Text = "Nacitane : " + openFileDialog1.FileName;
                    }

                }
            }


            
        }

        private void button6_Click(object sender, EventArgs e)
        {
            byte[] null_array = new byte[32000];

            bin.data_write = null_array;

            HexDump(TxtDump1, bin.data_write, 32000);

            label38.Text = "Write buffer je prazdny. Nacitaj BIN alebo zapis prazdne. ";
        }

        private void button9_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            saveFileDialog1.Filter = "Bin file (*.bin)|*.bin";
            saveFileDialog1.FilterIndex = 1;
            saveFileDialog1.RestoreDirectory = true;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                using (BinaryWriter BinaryWrite = new BinaryWriter(File.Open(saveFileDialog1.FileName, FileMode.Create)))
                {
                    BinaryWrite.Write(bin.data_write);

                    BinaryWrite.Close();

                }
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            saveFileDialog1.Filter = "Bin file (*.bin)|*.bin";
            saveFileDialog1.FilterIndex = 1;
            saveFileDialog1.RestoreDirectory = true;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                using (BinaryWriter BinaryWrite = new BinaryWriter(File.Open(saveFileDialog1.FileName, FileMode.Create)))
                {
                    BinaryWrite.Write(bin.data_read);

                    BinaryWrite.Close();

                }
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Array.Resize(ref bin.data_read, 32000);
            Array.Resize(ref bin.data_write, 32000);
            byte[] null_array = new byte[32000];

            var comparsion = bin.data_write.SequenceEqual(bin.data_read);

            var null_read_buffer = bin.data_read.SequenceEqual(null_array);
            var null_write_buffer = bin.data_write.SequenceEqual(null_array);


            if (null_read_buffer || null_write_buffer)
            {
                label40.Text = "Read alebo Write buffer je prazdny.";
                label40.BackColor = Color.Yellow;
            }

            if (!null_read_buffer && !null_write_buffer)
            {
                if (comparsion == true)
                {
                    label40.Text = "Porovnanie OK.";
                    label40.BackColor = Color.Green;
                }

                else
                {
                    label40.Text = "Porovnanie NOK.";
                    label40.BackColor = Color.Red;
                }
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {

            PID.update = "";

            if (checkBox1.Checked == true || checkBox2.Checked == true)
            {

                OpenFileDialog openFileDialog1 = new OpenFileDialog();
                openFileDialog1.InitialDirectory = "This PC\\Documents";
                openFileDialog1.Filter = "BIN Files (*.bin)|*.bin";
                openFileDialog1.FilterIndex = 1;
                openFileDialog1.RestoreDirectory = true;
                openFileDialog1.Title = "Open a file";


                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    if (openFileDialog1.CheckPathExists)
                    {

                        using (BinaryReader BinaryRead = new BinaryReader(File.Open(openFileDialog1.FileName, FileMode.Open)))
                        {

                            bin.data_update = new byte[BinaryRead.BaseStream.Length];
                            for (int pos = 0; pos < BinaryRead.BaseStream.Length; pos++)
                            {
                                bin.data_update[pos] = (byte)BinaryRead.BaseStream.ReadByte();

                            }

                            PID.update = System.IO.Path.GetFileName(openFileDialog1.FileName);

                            BinaryRead.Close();
                        }
                    }
                }
                
                


            }


            for (int pos = 0; pos <= 32000; pos++)
            {
                // Update Teile NR.
                if (checkBox1.Checked == true)
                {
                    if (pos >= 250 && pos <= 3199)
                        bin.data_write[pos] = bin.data_update[pos];

                }

                // Update Recepty
                if (checkBox2.Checked == true)
                {
                    if (pos >= 3600 && pos <= 7298)
                        bin.data_write[pos] = bin.data_update[pos];

                }


                // Zmazat oblast
                if (checkBox3.Checked == true)
                {

                    int Start_A;
                    int End_A;

                    Start_A = System.Convert.ToInt32(textBox1.Text);
                    End_A = System.Convert.ToInt32(textBox2.Text);

                    if (End_A != 0 && Start_A < End_A && pos >= Start_A && pos <= End_A)
                        bin.data_write[pos] = 0;

                }

            }



            if ((checkBox1.Checked == true || checkBox2.Checked == true && PID.update != "") || checkBox3.Checked == true)
            {
                HexDump(TxtDump1, bin.data_write, 32000);

                label38.Text = "Recepty aktualizovane : " + PID.update;
                MessageBox.Show("Recepty aktualizovane ", "Update", MessageBoxButtons.OK);

            }
            else
            {
                MessageBox.Show("Ziadny vyber k aktualizacii ", "Update", MessageBoxButtons.OK);
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {


            if (textBox2.Text != "")
            {
                var regex = new Regex(@"\d");

                if (regex.IsMatch(textBox2.Text))
                {
                    int boxInput = 0;
                    Int32.TryParse(textBox2.Text, out boxInput);

                    if (boxInput > 32000)
                    {
                        textBox2.BackColor = Color.Red;
                    }
                    else
                    {
                        textBox2.BackColor = SystemColors.Window;
                    }
                }
                else
                {
                    textBox2.BackColor = Color.Red;
                }
            }
            else
            {
                textBox2.BackColor = SystemColors.Window;
            }

            
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (textBox1.Text != "")
            {
                var regex = new Regex(@"\d");

                if (regex.IsMatch(textBox1.Text))
                {
                    int boxInput = 0;
                    Int32.TryParse(textBox1.Text, out boxInput);

                    if (boxInput > 32000)
                    {
                        textBox1.BackColor = Color.Red;
                    }
                    else
                    {
                        textBox1.BackColor = SystemColors.Window;
                    }
                }
                else
                {
                    textBox1.BackColor = Color.Red;
                }
            }
            else
            {
                textBox1.BackColor = SystemColors.Window;
            }

            //TODO: start byte nesmie byt vacsi ako end byte
        }
    }
    
}
