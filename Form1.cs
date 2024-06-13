using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Lab_27_Danylko
{
    public partial class Form1 : Form
    {
        private ImageList imageList;

        public Form1()
        {
            InitializeComponent();
            InitializeImageList();
            LoadDrives();
            comboBox1.SelectedIndexChanged += ComboBox1_SelectedIndexChanged;
        }

        private void InitializeImageList()
        {
            imageList = new ImageList();
            imageList.ImageSize = new Size(16, 16);

            try
            {
                // Завантаження іконок
                imageList.Images.Add("drive", ShellIcon.GetSmallIcon("shell32.dll", 8).ToBitmap());
                imageList.Images.Add("folder", ShellIcon.GetSmallIcon("shell32.dll", 3).ToBitmap());
                imageList.Images.Add("file", ShellIcon.GetSmallIcon("shell32.dll", 0).ToBitmap()); // Іконка для невідомих файлів
                imageList.Images.Add("text", Image.FromFile("Icons/text.png")); // Іконка для текстових файлів
                imageList.Images.Add("image", Image.FromFile("Icons/image.png")); // Іконка для зображень
            }
            catch (FileNotFoundException ex)
            {
                MessageBox.Show($"Icon file not found: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while loading icons: {ex.Message}");
            }

            treeView1.ImageList = imageList;
            listView1.SmallImageList = imageList;
        }

        private void LoadDrives()
        {
            try
            {
                foreach (var drive in DriveInfo.GetDrives())
                {
                    TreeNode node = new TreeNode(drive.Name, 0, 0) { Tag = drive };
                    treeView1.Nodes.Add(node);
                    if (drive.IsReady)
                        node.Nodes.Add(new TreeNode()); // Додаємо пустий вузол як маркер
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while loading drives: {ex.Message}");
            }
        }

        private void LoadDirectoriesAndFiles(TreeNode node)
        {
            DirectoryInfo dir = node.Tag as DirectoryInfo ?? (node.Tag as DriveInfo)?.RootDirectory;
            if (dir == null) return;

            try
            {
                node.Nodes.Clear(); // Очищуємо існуючі вузли

                foreach (var subDir in dir.GetDirectories())
                {
                    try
                    {
                        TreeNode subNode = new TreeNode(subDir.Name, 1, 1) { Tag = subDir };
                        subNode.Nodes.Add(new TreeNode()); // Додаємо пустий вузол як маркер для можливості розгортання
                        node.Nodes.Add(subNode);
                    }
                    catch (UnauthorizedAccessException) { }
                    catch (DirectoryNotFoundException) { }
                }

                foreach (var file in dir.GetFiles())
                {
                    try
                    {
                        string imageKey = GetFileIconKey(file.Extension);
                        TreeNode fileNode = new TreeNode(file.Name, imageKey == "text" ? 3 : imageKey == "image" ? 4 : 2, imageKey == "text" ? 3 : imageKey == "image" ? 4 : 2) { Tag = file };
                        node.Nodes.Add(fileNode);
                    }
                    catch (UnauthorizedAccessException) { }
                    catch (FileNotFoundException) { }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while loading directories and files: {ex.Message}");
            }
        }

        private string GetFileIconKey(string extension)
        {
            switch (extension.ToLower())
            {
                case ".txt":
                    return "text";
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".bmp":
                case ".gif":
                    return "image";
                default:
                    return "file";
            }
        }

        private void treeView1_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            TreeNode node = e.Node;
            if (node.Nodes.Count == 1 && node.Nodes[0].Tag == null)
            {
                LoadDirectoriesAndFiles(node);
            }
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            TreeNode selectedNode = e.Node;
            propertyGrid1.SelectedObject = selectedNode.Tag;
            listView1.Items.Clear();

            if (selectedNode.Tag is DriveInfo drive)
            {
                if (drive.IsReady)
                    LoadFilesAndDirectories(drive.RootDirectory);
            }
            else if (selectedNode.Tag is DirectoryInfo dir)
            {
                LoadFilesAndDirectories(dir);
            }
            else if (selectedNode.Tag is FileInfo file)
            {
                LoadFileContent(file);
            }
        }

        private void LoadFilesAndDirectories(DirectoryInfo dir)
        {
            try
            {
                foreach (var subDir in dir.GetDirectories())
                {
                    try
                    {
                        ListViewItem item = new ListViewItem(subDir.Name, "folder") { Tag = subDir };
                        item.SubItems.Add("Directory");
                        listView1.Items.Add(item);
                    }
                    catch (UnauthorizedAccessException) { }
                    catch (DirectoryNotFoundException) { }
                }

                foreach (var file in dir.GetFiles())
                {
                    try
                    {
                        string imageKey = GetFileIconKey(file.Extension);
                        ListViewItem item = new ListViewItem(file.Name, imageKey) { Tag = file };
                        item.SubItems.Add("File");
                        listView1.Items.Add(item);
                    }
                    catch (UnauthorizedAccessException) { }
                    catch (FileNotFoundException) { }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while loading files and directories: {ex.Message}");
            }
        }

        private void listView1_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (e.IsSelected)
            {
                propertyGrid1.SelectedObject = e.Item.Tag;
                if (e.Item.Tag is FileInfo file)
                {
                    LoadFileContent(file);
                }
            }
        }

        private void LoadFileContent(FileInfo file)
        {
            try
            {
                if (file.Extension == ".txt")
                {
                    textBox1.Text = File.ReadAllText(file.FullName);
                    textBox1.Visible = true;
                    pictureBox1.Image = null;
                    pictureBox1.Visible = false;
                }
                else if (file.Extension == ".jpg" || file.Extension == ".jpeg" || file.Extension == ".png" || file.Extension == ".bmp" || file.Extension == ".gif")
                {
                    pictureBox1.Image = Image.FromFile(file.FullName);
                    pictureBox1.Visible = true;
                    textBox1.Clear();
                    textBox1.Visible = false;
                }
                else
                {
                    textBox1.Clear();
                    textBox1.Visible = false;
                    pictureBox1.Image = null;
                    pictureBox1.Visible = false;
                }
            }
            catch (FileNotFoundException ex)
            {
                MessageBox.Show($"File not found: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show($"Access denied to file: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while loading file content: {ex.Message}");
            }
        }

        private void ComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string filter = comboBox1.SelectedItem.ToString().ToLower();
            foreach (ListViewItem item in listView1.Items)
            {
                if (filter == "all" || item.SubItems[1].Text.ToLower().Contains(filter))
                {
                    item.ForeColor = SystemColors.WindowText; // Відображати елемент
                }
                else
                {
                    item.ForeColor = SystemColors.GrayText; // Приховати елемент через зміну кольору тексту
                }
            }
        }
    }

    public static class ShellIcon
    {
        [DllImport("Shell32.dll")]
        public static extern int ExtractIconEx(string file, int index, IntPtr[] largeIcon, IntPtr[] smallIcon, int icons);

        public static Icon GetSmallIcon(string file, int index)
        {
            IntPtr[] smallIcon = new IntPtr[1];
            ExtractIconEx(file, index, null, smallIcon, 1);
            return Icon.FromHandle(smallIcon[0]);
        }
    }
}