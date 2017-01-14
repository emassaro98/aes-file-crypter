using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AesCrypter
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        public static byte[] GenerateRandomSalt()
        {
            //Source: http://www.dotnetperls.com/rngcryptoserviceprovider
            byte[] data = new byte[32];

            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                // Ten iterations.
                for (int i = 0; i < 10; i++)
                {
                    // Fill buffer.
                    rng.GetBytes(data);
                }
            }
            return data;
        }

        //Encrypt
        private void AES_Encrypt(string inputFile, string password)
        {
            //http://stackoverflow.com/questions/27645527/aes-encryption-on-large-files

            //generate random salt
            byte[] salt = GenerateRandomSalt();

            //create output file name
            FileStream fsCrypt = new FileStream(inputFile + ".encrypted", FileMode.Create);

            //convert password string to byte arrray
            byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);

            //Set Rijndael symmetric encryption algorithm
            RijndaelManaged AES = new RijndaelManaged();
            AES.KeySize = 256;
            AES.BlockSize = 128;
            AES.Padding = PaddingMode.PKCS7;

            //http://stackoverflow.com/questions/2659214/why-do-i-need-to-use-the-rfc2898derivebytes-class-in-net-instead-of-directly
            //"What it does is repeatedly hash the user password along with the salt." High iteration counts.
            var key = new Rfc2898DeriveBytes(passwordBytes, salt, 50000);
            AES.Key = key.GetBytes(AES.KeySize / 8);
            AES.IV = key.GetBytes(AES.BlockSize / 8);

            //Cipher modes: http://security.stackexchange.com/questions/52665/which-is-the-best-cipher-mode-and-padding-mode-for-aes-encryption
            AES.Mode = CipherMode.CFB;

            //write salt to the begining of the output file, so in this case can be random every time
            fsCrypt.Write(salt, 0, salt.Length);

            CryptoStream cs = new CryptoStream(fsCrypt, AES.CreateEncryptor(), CryptoStreamMode.Write);

            FileStream fsIn = new FileStream(inputFile, FileMode.Open);

            //create a buffer (1mb) so only this amount will allocate in the memory and not the whole file
            byte[] buffer = new byte[1048576];
            int read;

            try
            {
                while ((read = fsIn.Read(buffer, 0, buffer.Length)) > 0)
                {
                    Application.DoEvents(); // -> for responsive GUI, using Task will be better!
                    cs.Write(buffer, 0, read);
                }

                //close up
                fsIn.Close();
                labelStato.Text = "Successful! ";

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error: " + ex.Message);
                labelStato.Text = "Error, try again! ";
            }
            finally
            {
                cs.Close();
                fsCrypt.Close();
            }
        }

        //Decrypt
        private void AES_Decrypt(string inputFile, string password)
        {
            //todo:
            // - create error message on wrong password
            // - on cancel: close and delete file
            // - on wrong password: close and delete file!
            // - create a better filen name
            // - could be check md5 hash on the files but it make this slow

            byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
            byte[] salt = new byte[32];

            FileStream fsCrypt = new FileStream(inputFile, FileMode.Open);
            fsCrypt.Read(salt, 0, salt.Length);

            RijndaelManaged AES = new RijndaelManaged();
            AES.KeySize = 256;
            AES.BlockSize = 128;
            var key = new Rfc2898DeriveBytes(passwordBytes, salt, 50000);
            AES.Key = key.GetBytes(AES.KeySize / 8);
            AES.IV = key.GetBytes(AES.BlockSize / 8);
            AES.Padding = PaddingMode.PKCS7;
            AES.Mode = CipherMode.CFB;

            CryptoStream cs = new CryptoStream(fsCrypt, AES.CreateDecryptor(), CryptoStreamMode.Read);

            FileStream fsOut = new FileStream(inputFile + ".decrypted", FileMode.Create);

            int read;
            byte[] buffer = new byte[1048576];

            try
            {
                while ((read = cs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    Application.DoEvents();
                    fsOut.Write(buffer, 0, read);
                }
            }
            catch (System.Security.Cryptography.CryptographicException ex_CryptographicException)
            {
                Debug.WriteLine("CryptographicException error: " + ex_CryptographicException.Message);
                labelStato.Text = "Error, try again! ";
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error: " + ex.Message);
                labelStato.Text = "Error, try again! ";
            }

            try
            {
                labelStato.Text = "Successful! ";
                cs.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error by closing CryptoStream: " + ex.Message);
                labelStato.Text = "Error, try again! ";
            }
            finally
            {
                fsOut.Close();
                fsCrypt.Close();
            }
        }

        private void buttonOpen_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                labelFile.Text = "Selected file:" + openFileDialog1.FileName;
                labelStato.Visible = true;
                labelStato.Text = "Preparing file...";
                if (txtPassword.Text != null & txtPassword.Text != " " & txtPassword.TextLength >= 8)
                {
                    AES_Encrypt(openFileDialog1.FileName, txtPassword.Text);

                }
                else {
                    MessageBox.Show("Password not valid or not enough length! The min length must be 8 character", "Alert");
                }
                
            }
        }

        private void buttonDecrypt_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                labelFile.Text = "Selected file:" + openFileDialog1.FileName;
                labelStato.Visible = true;
                labelStato.Text = "Preparing file...";
                if (txtPassword.Text != null & txtPassword.Text != " " & txtPassword.TextLength >= 8)
                {
                    AES_Decrypt(openFileDialog1.FileName, txtPassword.Text);

                }
                else
                {
                    MessageBox.Show("Password not valid or not enough length! The min length must be 8 character", "Alert");
                }

            }
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }
    }
}
