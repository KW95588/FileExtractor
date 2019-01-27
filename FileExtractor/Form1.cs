using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileExtractor
{
    public partial class Form1 : Form
    {
        string fileName;
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Open File";
            openFileDialog.Filter = "Text Files (*.txt)|*.txt";

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                fileName = openFileDialog.FileName;
                MessageBox.Show("You have selected file: " + fileName);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            int fileCount = 0;
            string directory = Path.GetDirectoryName(fileName);
            List<string> fileContentList = new List<string>(); ;

            if (!String.IsNullOrEmpty(fileName) && (this.HasDirectoryAccess(FileSystemRights.Read, fileName)))
            {
                if (File.Exists(fileName))
                {
                    try
                    {
                        using (TextReader tr = new StreamReader(new FileStream(fileName, FileMode.Open), Encoding.UTF8))
                        {
                            string line;
                            List<string> invoiceContentList = new List<string>();
                            while ((line = tr.ReadLine()) != null)
                            {
                                fileContentList.Add(line);
                            }
                        }

                        //Prepare directory to store the extracted files
                        if (!Directory.Exists(directory + "\\ExtractedFiles"))
                        {
                            Directory.CreateDirectory(directory + "\\ExtractedFiles");
                        }
                        else
                        {
                            System.IO.DirectoryInfo di = new DirectoryInfo(directory + "\\ExtractedFiles");
                            foreach (FileInfo file in di.GetFiles())
                            {
                                file.Delete();
                            }
                            foreach (DirectoryInfo dir in di.GetDirectories())
                            {
                                dir.Delete(true);
                            }
                        }
                        
                        //a list to store data for each invoice
                        List<string> invoiceContents = new List<string>();

                        //get invoice numbee of the first invoice in the data file
                        int index = fileContentList.FindIndex(x => x.Contains("TAX INVOICE"));
                        string currentInvoiceNumber = fileContentList[index + 1].Trim();

                        string newFilePath = "";
                        int currentPageStartIndex = 0;
                        string invoiceNumber = "";

                        for (int i = 0; i < fileContentList.Count; i++)
                        {
                            invoiceNumber = "";
                            string currentLine = fileContentList[i];

                            if (currentLine!= string.Empty && currentLine.Contains('\u000c'))
                            {
                                currentPageStartIndex = i;
                                invoiceContents.Add(currentLine);
                            }
                            else if(currentLine != string.Empty && currentLine.Contains("TAX INVOICE"))
                            {
                                invoiceNumber = fileContentList[i + 1].Trim();
                                if(invoiceNumber == currentInvoiceNumber)
                                {
                                    invoiceContents.Add(currentLine);
                                }
                                else
                                {
                                    //before proceedsing to the next invoice, create a file to save the current invoice data 
                                    newFilePath = directory + "\\ExtractedFiles\\" + currentInvoiceNumber + ".txt";
                                    int count = 0;
                                    currentInvoiceNumber = invoiceNumber;
                                    using (TextWriter tw = new StreamWriter(newFilePath))
                                    {
                                        int differences = i - currentPageStartIndex;
                                        for (int j = 0; j < (invoiceContents.Count - differences) ; j++)
                                        {
                                            tw.WriteLine(invoiceContents[j]);
                                            count = j;
                                        }                                       
                                    }
                                    //clear the saved invoice data from the list
                                    invoiceContents.Clear();
                                    i = currentPageStartIndex-1; //adjust index position
                                    fileCount++;
                                }
                            }
                            else if (i == fileContentList.Count - 1)    //save the last invoice
                            {
                                newFilePath = directory + "\\ExtractedFiles\\" + currentInvoiceNumber + ".txt";
                                currentInvoiceNumber = invoiceNumber;
                                using (TextWriter tw = new StreamWriter(newFilePath))
                                {
                                    int differences = i - currentPageStartIndex;
                                    for (int j = 0; j < invoiceContents.Count; j++)
                                    {
                                        tw.WriteLine(invoiceContents[j]);
                                    }
                                }
                                invoiceContents.Clear();
                                fileCount++;
                            }
                            else
                            {
                                invoiceContents.Add(currentLine);
                            }
                        }

                        MessageBox.Show(fileCount + " files were created at " + directory);

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Exception message 1" + ex.Message);
                    }
                }
                else
                {
                    MessageBox.Show("Exception message 2");
                }
            }
            else
            {
                MessageBox.Show("Please select a valid file");
            }
        }


        //https://stackoverflow.com/questions/9503884/best-way-to-handle-errors-when-opening-file
        private bool HasDirectoryAccess(FileSystemRights fileSystemRights, string directoryPath)
        {
            DirectorySecurity directorySecurity = Directory.GetAccessControl(directoryPath);

            foreach (FileSystemAccessRule rule in directorySecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount)))
            {
                if ((rule.FileSystemRights & fileSystemRights) != 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
