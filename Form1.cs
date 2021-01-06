using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace YOLIC
{
    public partial class Form1 : Form
    {
        Boolean KeepHistory = false;
        Boolean SemiAutomatic = false;
        string ImageExtension = "png";
        private List<string> list_Img;
        private List<string> list_depthImg;
        OpenFileDialog OpenJson = new OpenFileDialog();
        OpenFileDialog OpenOnnx = new OpenFileDialog();
        List<PointF> dat = new List<PointF>();
        List<PointF> dat2 = new List<PointF>();
        FolderBrowserDialog openFile_Img = new FolderBrowserDialog();
        FolderBrowserDialog openFile_DepthImg = new FolderBrowserDialog();
        FolderBrowserDialog saveFile = new FolderBrowserDialog();
        private readonly string logPath;
        string[] currentLabel;
        JArray LabelList;
        int COInumber;
        int CurrentIndex = 0;
        int LastArea = -1;

        JArray[] COIList;


        public Form1()
        {
            InitializeComponent();
            logPath = System.Environment.CurrentDirectory.ToString();
            Console.WriteLine(logPath);
            changeSize();
        }
        private void changeSize()
        {
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true); // 禁止擦除背景.
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true); // 双缓冲DoubleBuffer

            x = this.Width;
            y = this.Height;
            setTag(this);
            //if (this.WindowState == FormWindowState.Maximized)
            //{
            //    this.WindowState = FormWindowState.Normal;
            //}
            //else
            //{
            //    this.FormBorderStyle = FormBorderStyle.None;
            //    this.WindowState = FormWindowState.Maximized;
            //}

        }
        private float x;//定义当前窗体的宽度
        private float y;//定义当前窗体的高度
        private void setTag(Control cons)
        {
            foreach (Control con in cons.Controls)
            {
                con.Tag = con.Width + ";" + con.Height + ";" + con.Left + ";" + con.Top + ";" + con.Font.Size;
                if (con.Controls.Count > 0)
                {
                    setTag(con);
                }
            }
        }
        //设置双缓冲区、解决闪屏问题
        public static void SetDouble(Control cc)
        {

            cc.GetType().GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance |
                         System.Reflection.BindingFlags.NonPublic).SetValue(cc, true, null);

        }
        private void setControls(float newx, float newy, Control cons)
        {
            //遍历窗体中的控件，重新设置控件的值
            foreach (Control con in cons.Controls)
            {
                ////获取控件的Tag属性值，并分割后存储字符串数组
                SetDouble(this);
                SetDouble(con);
                if (con.Tag != null)
                {

                    string[] mytag = con.Tag.ToString().Split(new char[] { ';' });
                    //根据窗体缩放的比例确定控件的值
                    con.Width = Convert.ToInt32(System.Convert.ToSingle(mytag[0]) * newx);//宽度
                    con.Height = Convert.ToInt32(System.Convert.ToSingle(mytag[1]) * newy);//高度
                    con.Left = Convert.ToInt32(System.Convert.ToSingle(mytag[2]) * newx);//左边距
                    con.Top = Convert.ToInt32(System.Convert.ToSingle(mytag[3]) * newy);//顶边距
                    Single currentSize = System.Convert.ToSingle(mytag[4]) * newy;//字体大小
                    con.Font = new Font(con.Font.Name, currentSize, con.Font.Style, con.Font.Unit);
                    if (con.Controls.Count > 0)
                    {
                        setControls(newx, newy, con);
                    }
                }


            }
        }

        //在窗体的的时间中添加Resize事件
        private void Login_Resize(object sender, EventArgs e)
        {
            float newx = (this.Width) / x;
            float newy = (this.Height) / y;
            setControls(newx, newy, this);
        }

        private void button13_Click(object sender, EventArgs e)
        {
            OpenJson.InitialDirectory = logPath;
            OpenJson.Title = "Set COI Data";
            OpenJson.Filter = "Json files (*.json)|*.json";
            OpenJson.RestoreDirectory = true;
            OpenJson.FilterIndex = 1;
            try
            {
                if (OpenJson.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string JsonName = OpenJson.FileName;
                    StreamReader COIjson = File.OpenText(JsonName);
                    JsonTextReader labeljsonreader = new JsonTextReader(COIjson);
                    JObject coijsonObject = (JObject)JToken.ReadFrom(labeljsonreader);
                    COInumber = (int)coijsonObject["COIs"]["COINumber"];
                    COIList = new JArray[COInumber];
                    for (int i = 1; i <= COInumber; i++)
                    {
                        COIList[i - 1] = (JArray)coijsonObject["COIs"][i.ToString()];
                    }

                    LabelList = (JArray)coijsonObject["Labels"]["LabelList"];
                    if (LabelList.Count > 20)
                    {
                        MessageBox.Show("Up to 20 labels!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    for (int i = 0, j = 21; i < LabelList.Count; i++, j++)
                    {
                        ((CheckBox)this.Controls.Find("checkBox" + j, true)[0]).Visible = true;
                        ((CheckBox)this.Controls.Find("checkBox" + j, true)[0]).Text = LabelList[i].ToString();
                    }

                    button16.Enabled = true;
                }
            }
            catch (Exception)
            {
                this.BeginInvoke((Action)(() => MessageBox.Show("Failed to read the configuration file, please check the Json file!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)));
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            OpenJson.InitialDirectory = logPath;
            OpenJson.Title = "Set COI Data";
            OpenJson.Filter = "Json files (*.json)|*.json";
            OpenJson.RestoreDirectory = true;
            OpenJson.FilterIndex = 1;
            try
            {
                if (OpenJson.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string JsonName = OpenJson.FileName;
                    StreamReader COIjson = File.OpenText(JsonName);
                    JsonTextReader labeljsonreader = new JsonTextReader(COIjson);
                    JObject coijsonObject = (JObject)JToken.ReadFrom(labeljsonreader);
                    COInumber = (int)coijsonObject["COIs"]["COINumber"];
                    COIList = new JArray[COInumber];
                    for (int i = 1; i <= COInumber; i++)
                    {
                        COIList[i - 1] = (JArray)coijsonObject["COIs"][i.ToString()];
                    }

                    LabelList = (JArray)coijsonObject["Labels"]["LabelList"];
                    if (LabelList.Count > 20)
                    {
                        MessageBox.Show("Up to 20 labels!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    for (int i = 0, j = 1; i < LabelList.Count; i++, j++)
                    {
                        ((CheckBox)this.Controls.Find("checkBox" + j, true)[0]).Visible = true;
                        ((CheckBox)this.Controls.Find("checkBox" + j, true)[0]).Text = LabelList[i].ToString();
                    }

                    button7.Enabled = true;
                }
            }
            catch (Exception)
            {
                this.BeginInvoke((Action)(() => MessageBox.Show("Failed to read the file, please check the COI Jaon file!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)));
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            try
            {
                
                if (openFile_Img.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    list_Img = new List<string>(Directory.GetFiles(openFile_Img.SelectedPath, "*." + ImageExtension));
                    label6.Text =  list_Img.Count.ToString();
                    if (list_Img.Count == 0)
                    {
                        MessageBox.Show("No images under folder!", "Notice", MessageBoxButtons.OK);
                        return;
                    }
                    
                }
            }
            catch (Exception)
            {
                this.BeginInvoke((Action)(() => MessageBox.Show("Failed to read the RGB image, please check the image path!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)));
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            try
            {
                if (openFile_DepthImg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    list_depthImg = new List<string>(Directory.GetFiles(openFile_DepthImg.SelectedPath, "*." + ImageExtension));
                    if (list_depthImg.Count == 0)
                    {
                        MessageBox.Show("No images under folder!", "Notice", MessageBoxButtons.OK);
                        return;
                    }

                }
            }
            catch (Exception)
            {
                this.BeginInvoke((Action)(() => MessageBox.Show("Failed to read the RGB image, please check the image path!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)));
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            try
            {

                if (openFile_Img.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    list_Img = new List<string>(Directory.GetFiles(openFile_Img.SelectedPath, "*." + ImageExtension));
                    label7.Text = list_Img.Count.ToString();
                    if (list_Img.Count == 0)
                    {
                        MessageBox.Show("No images under folder!", "Notice", MessageBoxButtons.OK);
                        return;
                    }

                }
            }
            catch (Exception)
            {
                this.BeginInvoke((Action)(() => MessageBox.Show("Failed to read the RGB image, please check the image path!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)));
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (saveFile.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                button7.Enabled = true;
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            if (saveFile.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                button16.Enabled = true;
            }
        }

        private void Display(int currentIndex)
        {

            string Imagename = Path.GetFileName(list_Img[currentIndex]);
            string DImagename = Path.GetFileName(list_depthImg[currentIndex]);
            label8.Text = Imagename;
            label9.Text = DImagename;
            
            
            Image OriginalImage = Image.FromFile(list_Img[CurrentIndex]);
            Image OriginalDapthImage = Image.FromFile(list_depthImg[CurrentIndex]);

            Image DetectedImage = cutImage(OriginalImage, new Point(0, 0), OriginalImage.Width, OriginalImage.Height);
            Image DetectedDapthImage = cutImage(OriginalDapthImage, new Point(0, 0), OriginalDapthImage.Width, OriginalDapthImage.Height);
            OriginalImage.Dispose();
            OriginalDapthImage.Dispose();
            pictureBox2.Image = DetectedImage;
            pictureBox3.Image = DetectedDapthImage;

         
            System.Drawing.Graphics rgb = Graphics.FromImage(pictureBox2.Image);
            System.Drawing.Graphics depth = Graphics.FromImage(pictureBox3.Image);
            //Console.WriteLine(COIList.Length);
            for (int i =0; i< COIList.Length; i++)
            {
                if (COIList[i][0].ToString().Equals("rectangle"))
                {
                    rgb.DrawRectangle(new Pen(Color.Red, 3), (float)COIList[i][1] * pictureBox2.Image.Width, (float)COIList[i][2] * pictureBox2.Image.Height, (float)COIList[i][3] * pictureBox2.Image.Width, (float)COIList[i][4] * pictureBox2.Image.Height);
                    depth.DrawRectangle(new Pen(Color.Red, 3), (float)COIList[i][1] * pictureBox3.Image.Width, (float)COIList[i][2] * pictureBox3.Image.Height, (float)COIList[i][3] * pictureBox3.Image.Width, (float)COIList[i][4] * pictureBox3.Image.Height);
                }
                
            }
        }
        private Image cutImage(Image SrcImage, Point pos, int cutWidth, int cutHeight)
        {

            Image cutedImage = null;

            //先初始化一个位图对象，来存储截取后的图像
            Bitmap bmpDest = new Bitmap(cutWidth, cutHeight, PixelFormat.Format32bppRgb);
            Graphics g = Graphics.FromImage(bmpDest);

            //矩形定义,将要在被截取的图像上要截取的图像区域的左顶点位置和截取的大小
            Rectangle rectSource = new Rectangle(pos.X, pos.Y, cutWidth, cutHeight);


            //矩形定义,将要把 截取的图像区域 绘制到初始化的位图的位置和大小
            //rectDest说明，将把截取的区域，从位图左顶点开始绘制，绘制截取的区域原来大小
            Rectangle rectDest = new Rectangle(0, 0, cutWidth, cutHeight);

            //第一个参数就是加载你要截取的图像对象，第二个和第三个参数及如上所说定义截取和绘制图像过程中的相关属性，第四个属性定义了属性值所使用的度量单位
            g.DrawImage(SrcImage, rectDest, rectSource, GraphicsUnit.Pixel);

            //在GUI上显示被截取的图像
            cutedImage = (Image)bmpDest;

            g.Dispose();

            return cutedImage;

        }
        private void button7_Click(object sender, EventArgs e)
        {
            if (button7.Text.Equals("Start") == true)
            {
                button3.Enabled = true;
                button4.Enabled = true;
                pictureBox1.Enabled = true;
                currentLabel = new string[COIList.Length * (LabelList.Count + 1)];
                for (int i = 0; i < currentLabel.Length; i++)
                {
                    currentLabel[i] = "0";
                }

                DisplayRGB(CurrentIndex);
                button7.Text = "Save";
            }
            else
            {
                SaveImage(CurrentIndex);

            }
        }

        private void DisplayRGB(int currentIndex)
        {
            string Imagename = Path.GetFileName(list_Img[currentIndex]);
            
            label10.Text = Imagename;
            


            Image OriginalImage = Image.FromFile(list_Img[CurrentIndex]);
            

            Image DetectedImage = cutImage(OriginalImage, new Point(0, 0), OriginalImage.Width, OriginalImage.Height);
            
            OriginalImage.Dispose();
            
            pictureBox1.Image = DetectedImage;

            System.Drawing.Graphics rgb = Graphics.FromImage(pictureBox1.Image);
            
            //Console.WriteLine(COIList.Length);
            for (int i = 0; i < COIList.Length; i++)
            {
                if (COIList[i][0].ToString().Equals("rectangle"))
                {
                    rgb.DrawRectangle(new Pen(Color.Red, 3), (float)COIList[i][1] * pictureBox1.Image.Width, (float)COIList[i][2] * pictureBox1.Image.Height, (float)COIList[i][3] * pictureBox1.Image.Width, (float)COIList[i][4] * pictureBox1.Image.Height);
                    
                }

            }
        }

        private void button16_Click(object sender, EventArgs e)
        {
            if (button16.Text.Equals("Start") == true)
            {
                button11.Enabled = true;
                button12.Enabled = true;
                pictureBox2.Enabled = true;
                currentLabel = new string[COIList.Length * (LabelList.Count + 1)];
                for (int i = 0; i < currentLabel.Length; i++)
                {
                    currentLabel[i] = "0";
                }

                Display(CurrentIndex);
                button16.Text = "Save Annotation";
            }
            else
            {
                SaveImage(CurrentIndex);
                //CurrentIndex++;
                //label6.Text = "Total: " + (CurrentIndex + 1).ToString() + " / " + list_Img.Count.ToString();
                //if (CurrentIndex == list_Img.Count)
                //{
                //    MessageBox.Show("The specified image is complete!", "Notice", MessageBoxButtons.OK);
                //    button16.Enabled = false;
                //    return;
                //}
                //Display(CurrentIndex);
            }

            
        }

        private void SaveImage(int currentIndex)
        {

            for (int i = 0; i < COIList.Length; i++)
            {
                string normal = "1";
                for(int j = 0; j < LabelList.Count; j++)
                {
                    if(currentLabel[(i * (LabelList.Count + 1)) + j].Equals("1"))
                    {
                        normal = "0";
                    }
                }
                currentLabel[(i * (LabelList.Count + 1)) + LabelList.Count] = normal;
            }

            //List<string> list = new List<string>(currentLabel);
            //Console.WriteLine(list[11]);
            //list.RemoveAt(11);
            //Console.WriteLine(list[11]);

            //list.RemoveRange(1057, 10);
            //string[] currentLabel1 = list.ToArray();
            string annotationpath = Path.Combine(saveFile.SelectedPath, Path.GetFileNameWithoutExtension(list_Img[currentIndex]) + ".txt");
            StreamWriter save = new StreamWriter(annotationpath, false, System.Text.Encoding.Default);

            for (int i = 0; i < currentLabel.Length; i++)
            {
                save.Write(currentLabel[i]);
                save.Write(" ");
            }
            //System.Console.WriteLine(currentLabel);


            save.Flush();
            save.Close();
        }

        private void Drawbox(Image g, int labelArea, Color c)
        {
            pictureBox2.Image = g;
            System.Drawing.Graphics rgb = Graphics.FromImage(pictureBox2.Image);
            
            if (COIList[labelArea][0].ToString().Equals("rectangle"))
            {
                rgb.DrawRectangle(new Pen(c, 2), (float)COIList[labelArea][1] * pictureBox2.Image.Width, (float)COIList[labelArea][2] * pictureBox2.Image.Height, (float)COIList[labelArea][3] * pictureBox2.Image.Width, (float)COIList[labelArea][4] * pictureBox2.Image.Height);
                
            }
        }
        private void DrawboxRGB(Image g, int labelArea, Color c)
        {
            pictureBox1.Image = g;
            System.Drawing.Graphics rgb = Graphics.FromImage(pictureBox1.Image);

            if (COIList[labelArea][0].ToString().Equals("rectangle"))
            {
                rgb.DrawRectangle(new Pen(c, 2), (float)COIList[labelArea][1] * pictureBox1.Image.Width, (float)COIList[labelArea][2] * pictureBox1.Image.Height, (float)COIList[labelArea][3] * pictureBox1.Image.Width, (float)COIList[labelArea][4] * pictureBox1.Image.Height);

            }
        }
        private int JudgeArea(Point point)
        {
            int area = -1;
            for (int i = 0; i < COIList.Length; i++)
            {
                //Console.WriteLine(pictureBox2.Image.Width);
                //Console.WriteLine(new Point((int)((double)COIList[0][1] * pictureBox2.Image.Width), (int)((double)COIList[0][2] * pictureBox2.Image.Height)));
                if (IfInside(point, new Point[]{ new Point((int)((double)COIList[i][1] * pictureBox2.Image.Width), (int)((double)COIList[i][2] * pictureBox2.Image.Height)),
                    new Point((int)(((double)COIList[i][1] + (double)COIList[i][3]) * pictureBox2.Image.Width), (int)((double)COIList[i][2] * pictureBox2.Image.Height)),
                    new Point((int)(((double)COIList[i][1] + (double)COIList[i][3]) * pictureBox2.Image.Width), (int)(((double)COIList[i][2] + (double)COIList[i][4]) * pictureBox2.Image.Height)),
                    new Point((int)((double)COIList[i][1] * pictureBox2.Image.Width), (int)(((double)COIList[i][2] + (double)COIList[i][4]) * pictureBox2.Image.Height)) })){

                    area = i ;
                }

            }
            return area;
        }
        private int JudgeAreaRGB(Point point)
        {
            int area = -1;
            for (int i = 0; i < COIList.Length; i++)
            {
                //Console.WriteLine(pictureBox2.Image.Width);
                //Console.WriteLine(new Point((int)((double)COIList[0][1] * pictureBox2.Image.Width), (int)((double)COIList[0][2] * pictureBox2.Image.Height)));
                if (IfInside(point, new Point[]{ new Point((int)((double)COIList[i][1] * pictureBox1.Image.Width), (int)((double)COIList[i][2] * pictureBox1.Image.Height)),
                    new Point((int)(((double)COIList[i][1] + (double)COIList[i][3]) * pictureBox1.Image.Width), (int)((double)COIList[i][2] * pictureBox1.Image.Height)),
                    new Point((int)(((double)COIList[i][1] + (double)COIList[i][3]) * pictureBox1.Image.Width), (int)(((double)COIList[i][2] + (double)COIList[i][4]) * pictureBox1.Image.Height)),
                    new Point((int)((double)COIList[i][1] * pictureBox1.Image.Width), (int)(((double)COIList[i][2] + (double)COIList[i][4]) * pictureBox1.Image.Height)) }))
                {

                    area = i;
                }

            }
            return area;
        }
        private bool IfInside(Point checkPoint, Point[] point)
        {
            GraphicsPath myGraphicsPath = new GraphicsPath();
            Region myRegion = new Region();
            myGraphicsPath.Reset();
            myGraphicsPath.AddPolygon(point);
            myRegion.MakeEmpty();
            myRegion.Union(myGraphicsPath);
           
            bool a = myRegion.IsVisible(checkPoint);
            myRegion.Dispose();
            return a;//返回判断点是否在多边形里
        }

        private void button12_Click(object sender, EventArgs e)
        {

            CurrentIndex--;
            if (CurrentIndex < 0)
            {
                MessageBox.Show("No previous image!", "Notice", MessageBoxButtons.OK);
                CurrentIndex++;
                return;
            }
            label6.Text = (CurrentIndex + 1).ToString() + " / " + list_Img.Count.ToString();
            for (int i = 0, j = 21; i < LabelList.Count; i++, j++)
            {
                ((CheckBox)this.Controls.Find("checkBox" + j, true)[0]).Checked = false;

            }
            for (int i = 0; i < currentLabel.Length; i++)
            {
                currentLabel[i] = "0";
            }

            Display(CurrentIndex);
           
            
        }

        private void button11_Click(object sender, EventArgs e)
        {
            CurrentIndex++;
            LastArea = -1;

            if (CurrentIndex == list_Img.Count)
            {
                MessageBox.Show("The specified image is complete!", "Notice", MessageBoxButtons.OK);
                CurrentIndex--;
                return;
            }
            label6.Text = (CurrentIndex + 1).ToString() + " / " + list_Img.Count.ToString();
            if (KeepHistory == true)
            {
                Display(CurrentIndex);
                Redraw(pictureBox2.Image);

            }
            else
            {
                for (int i = 0, j = 21; i < LabelList.Count; i++, j++)
                {
                    ((CheckBox)this.Controls.Find("checkBox" + j, true)[0]).Checked = false;

                }
                for (int i = 0; i < currentLabel.Length; i++)
                {
                    currentLabel[i] = "0";
                }
                Display(CurrentIndex);
            }
            //for (int i = 0, j = 21; i < LabelList.Count; i++, j++)
            //{
            //    ((CheckBox)this.Controls.Find("checkBox" + j, true)[0]).Checked = false;

            //}
            //for (int i = 0; i < currentLabel.Length; i++)
            //{
            //    currentLabel[i] = "0";
            //}
            
        }

        private void Redraw(Image g)
        {
            pictureBox2.Image = g;
            System.Drawing.Graphics rgb = Graphics.FromImage(pictureBox2.Image);
            for (int i = 0; i < COIList.Length; i++)
            {
                string normal = "1";
                for (int j = 0; j < LabelList.Count; j++)
                {
                    if (currentLabel[(i * (LabelList.Count + 1)) + j].Equals("1"))
                    {
                        normal = "0";
                    }
                }
                if (normal.Equals("0"))
                {
                    if (COIList[i][0].ToString().Equals("rectangle"))
                    {
                        rgb.DrawRectangle(new Pen(Color.Blue, 1), (float)COIList[i][1] * pictureBox2.Image.Width, (float)COIList[i][2] * pictureBox2.Image.Height, (float)COIList[i][3] * pictureBox2.Image.Width, (float)COIList[i][4] * pictureBox2.Image.Height);
                        
                    }
                }
                
            }
        }
        private void RedrawR(Image g)
        {
            pictureBox1.Image = g;
            System.Drawing.Graphics rgb = Graphics.FromImage(pictureBox1.Image);
            for (int i = 0; i < COIList.Length; i++)
            {
                string normal = "1";
                for (int j = 0; j < LabelList.Count; j++)
                {
                    if (currentLabel[(i * (LabelList.Count + 1)) + j].Equals("1"))
                    {
                        normal = "0";
                    }
                }
                if (normal.Equals("0"))
                {
                    if (COIList[i][0].ToString().Equals("rectangle"))
                    {
                        rgb.DrawRectangle(new Pen(Color.Blue, 1), (float)COIList[i][1] * pictureBox1.Image.Width, (float)COIList[i][2] * pictureBox1.Image.Height, (float)COIList[i][3] * pictureBox1.Image.Width, (float)COIList[i][4] * pictureBox1.Image.Height);

                    }
                }

            }
        }

        private void button17_Click(object sender, EventArgs e)
        {
            string annotationpath = Path.Combine(saveFile.SelectedPath, Path.GetFileNameWithoutExtension(list_Img[CurrentIndex]) + ".txt");
            FileInfo fi = new FileInfo(annotationpath);
            fi.Delete();
        }

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {


            int originalHeight = this.pictureBox1.Image.Height;

            PropertyInfo rectangleProperty = this.pictureBox1.GetType().GetProperty("ImageRectangle", BindingFlags.Instance | BindingFlags.NonPublic);
            Rectangle rectangle = (Rectangle)rectangleProperty.GetValue(this.pictureBox1, null);

            int currentWidth = rectangle.Width;

            int currentHeight = rectangle.Height;

            double rate = (double)currentHeight / (double)originalHeight;

            int black_left_width = (currentWidth == this.pictureBox1.Width) ? 0 : (this.pictureBox1.Width - currentWidth) / 2;
            int black_top_height = (currentHeight == this.pictureBox1.Height) ? 0 : (this.pictureBox1.Height - currentHeight) / 2;

            int zoom_x = e.X - black_left_width;
            int zoom_y = e.Y - black_top_height;

            double original_x = (double)zoom_x / rate;
            double original_y = (double)zoom_y / rate;
            //Console.WriteLine(original_x.ToString());
            //Console.WriteLine(original_y.ToString());
            int LabelArea = JudgeAreaRGB(new Point((int)original_x, (int)original_y));
            //Console.WriteLine(LabelArea.ToString());
            if (LabelArea != -1)
            {

                for (int i = 0, j = 1; i < LabelList.Count; i++, j++)
                {
                    if (((CheckBox)this.Controls.Find("checkBox" + j, true)[0]).Checked)
                    {
                        //Console.WriteLine(i);
                        currentLabel[(LabelArea * (LabelList.Count + 1)) + i] = "1";
                    }
                    else
                    {
                        currentLabel[(LabelArea * (LabelList.Count + 1)) + i] = "0";
                    }

                }
                if (LastArea != -1 && LastArea != LabelArea)
                {
                    DrawboxRGB(pictureBox1.Image, LastArea, Color.Blue);
                }
                DrawboxRGB(pictureBox1.Image, LabelArea, Color.White);
                LastArea = LabelArea;
            }

        }

        private void pictureBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            int originalHeight = this.pictureBox1.Image.Height;

            PropertyInfo rectangleProperty = this.pictureBox1.GetType().GetProperty("ImageRectangle", BindingFlags.Instance | BindingFlags.NonPublic);
            Rectangle rectangle = (Rectangle)rectangleProperty.GetValue(this.pictureBox1, null);

            int currentWidth = rectangle.Width;
            int currentHeight = rectangle.Height;

            double rate = (double)currentHeight / (double)originalHeight;

            int black_left_width = (currentWidth == this.pictureBox1.Width) ? 0 : (this.pictureBox1.Width - currentWidth) / 2;
            int black_top_height = (currentHeight == this.pictureBox1.Height) ? 0 : (this.pictureBox1.Height - currentHeight) / 2;

            int zoom_x = e.X - black_left_width;
            int zoom_y = e.Y - black_top_height;

            double original_x = (double)zoom_x / rate;
            double original_y = (double)zoom_y / rate;

            int LabelArea = JudgeAreaRGB(new Point((int)original_x, (int)original_y));

            if (LabelArea != -1)
            {

                for (int i = 0, j = 1; i < LabelList.Count; i++, j++)
                {
                    if (((CheckBox)this.Controls.Find("checkBox" + j, true)[0]).Checked)
                    {
                        //Console.WriteLine(i);
                        currentLabel[(LabelArea * (LabelList.Count + 1)) + i] = "0";
                    }
                    else
                    {
                        currentLabel[(LabelArea * (LabelList.Count + 1)) + i] = "0";
                    }

                }

                if (LastArea != -1 && LastArea != LabelArea)
                {
                    DrawboxRGB(pictureBox1.Image, LastArea, Color.Red);
                }
                DrawboxRGB(pictureBox1.Image, LabelArea, Color.Red);
                LastArea = -1;

            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            CurrentIndex++;
            LastArea = -1;

            if (CurrentIndex == list_Img.Count)
            {
                MessageBox.Show("The specified image is complete!", "Notice", MessageBoxButtons.OK);
                CurrentIndex--;
                return;
            }
            label7.Text = (CurrentIndex + 1).ToString() + " / " + list_Img.Count.ToString();
            if (KeepHistory == true)
            {
                DisplayRGB(CurrentIndex);
                RedrawR(pictureBox1.Image);

            }
            else
            {
                for (int i = 0, j = 21; i < LabelList.Count; i++, j++)
                {
                    ((CheckBox)this.Controls.Find("checkBox" + j, true)[0]).Checked = false;

                }
                for (int i = 0; i < currentLabel.Length; i++)
                {
                    currentLabel[i] = "0";
                }
                Display(CurrentIndex);
            }
            //for (int i = 0, j = 1; i < LabelList.Count; i++, j++)
            //{
            //    ((CheckBox)this.Controls.Find("checkBox" + j, true)[0]).Checked = false;

            //}
            //for (int i = 0; i < currentLabel.Length; i++)
            //{
            //    currentLabel[i] = "0";
            //}
            //DisplayRGB(CurrentIndex);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            CurrentIndex--;
            if (CurrentIndex < 0)
            {
                MessageBox.Show("No previous image!", "Notice", MessageBoxButtons.OK);
                CurrentIndex++;
                return;
            }
            label7.Text = (CurrentIndex + 1).ToString() + " / " + list_Img.Count.ToString();
            for (int i = 0, j = 1; i < LabelList.Count; i++, j++)
            {
                ((CheckBox)this.Controls.Find("checkBox" + j, true)[0]).Checked = false;

            }
            for (int i = 0; i < currentLabel.Length; i++)
            {
                currentLabel[i] = "0";
            }

            DisplayRGB(CurrentIndex);
        }

        private void button15_Click(object sender, EventArgs e)
        {
            string annotationpath = Path.Combine(saveFile.SelectedPath, Path.GetFileNameWithoutExtension(list_Img[CurrentIndex]) + ".txt");
            FileInfo fi = new FileInfo(annotationpath);
            fi.Delete();
        }



        private void button6_Click(object sender, EventArgs e)
        {
            try
            {
                int temp = CurrentIndex;
                CurrentIndex = int.Parse("0" + textBox1.Text);
                LastArea = -1;

                if (CurrentIndex >= list_Img.Count)
                {
                    MessageBox.Show("The specified image is complete!", "Notice", MessageBoxButtons.OK);
                    CurrentIndex = temp;
                    return;
                }
                label6.Text = (CurrentIndex + 1).ToString() + " / " + list_Img.Count.ToString();
                for (int i = 0, j = 21; i < LabelList.Count; i++, j++)
                {
                    ((CheckBox)this.Controls.Find("checkBox" + j, true)[0]).Checked = false;

                }
                for (int i = 0; i < currentLabel.Length; i++)
                {
                    currentLabel[i] = "0";
                }
                Display(CurrentIndex);
            }
            catch (Exception)
            {
                this.BeginInvoke((Action)(() => MessageBox.Show("Failed to jump!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)));
            }
        }

        private void button14_Click(object sender, EventArgs e)
        {
            int temp = CurrentIndex;
            CurrentIndex = int.Parse("0"+textBox2.Text);
            LastArea = -1;

            if (CurrentIndex >= list_Img.Count)
            {
                MessageBox.Show("The specified image is complete!", "Notice", MessageBoxButtons.OK);
                CurrentIndex = temp;
                return;
            }
            label6.Text = (CurrentIndex + 1).ToString() + " / " + list_Img.Count.ToString();
            for (int i = 0, j = 21; i < LabelList.Count; i++, j++)
            {
                ((CheckBox)this.Controls.Find("checkBox" + j, true)[0]).Checked = false;

            }
            for (int i = 0; i < currentLabel.Length; i++)
            {
                currentLabel[i] = "0";
            }
            Display(CurrentIndex);
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!(char.IsNumber(e.KeyChar))&& e.KeyChar != (char)8)
            {
                e.Handled = true;
                
            }
            button14.Enabled = true;
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!(char.IsNumber(e.KeyChar)) && e.KeyChar != (char)8)
            {
                e.Handled = true;
                
            }
            button6.Enabled = true;
        }

        private void button18_Click(object sender, EventArgs e)
        {
            if (KeepHistory == true)
            {
                KeepHistory = false;
                button18.Text = "Label History OFF";
            }
            else
            {
                KeepHistory = true;
                button18.Text = "Label History ON";
            }
        }

        private void button19_Click_1(object sender, EventArgs e)
        {
            if (KeepHistory == true)
            {
                KeepHistory = false;
                button18.Text = "Label History OFF";
            }
            else
            {
                KeepHistory = true;
                button18.Text = "Label History ON";
            }
        }

        private void pictureBox2_MouseDown(object sender, MouseEventArgs e)
        {




        }

        private void pictureBox2_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;

            if (dat.Count > 2)
            {
                g.DrawPolygon(Pens.Black, dat.ToArray());
                
            }
            else
            {
                foreach (var p in dat)
                {
                    g.DrawArc(Pens.Black, new RectangleF(p.X - 2, p.Y - 2, 5, 5), 0, 360);
                }
            }
        }

        private void button20_Click(object sender, EventArgs e)
        {
            try
            {
                System.Drawing.Graphics g = Graphics.FromImage(pictureBox2.Image);
                //var g = e.Graphics;
                g.DrawPolygon(Pens.White, dat2.ToArray());

                for (int i = 0; i < COIList.Length; i++)
                {
                    if (COIList[i][0].ToString().Equals("rectangle"))
                    {
                        if (IfPolygonInside((float)COIList[i][1] * pictureBox2.Image.Width, (float)COIList[i][2] * pictureBox2.Image.Height, (float)COIList[i][3] * pictureBox2.Image.Width, (float)COIList[i][4] * pictureBox2.Image.Height, dat2.ToArray()))
                        {
                            //System.Console.WriteLine(i);
                            for (int ii = 0, j = 21; ii < LabelList.Count; ii++, j++)
                            {
                                if (((CheckBox)this.Controls.Find("checkBox" + j, true)[0]).Checked)
                                {
                                    //Console.WriteLine(i);
                                    currentLabel[(i * (LabelList.Count + 1)) + ii] = "1";
                                }
                                //else
                                //{
                                //    currentLabel[(i * (LabelList.Count + 1)) + ii] = "0";
                                //}

                            }
                        }

                    }

                }
                dat.Clear();
                dat2.Clear();
                pictureBox2.Invalidate();
            }
            catch (Exception)
            {
                this.BeginInvoke((Action)(() => MessageBox.Show("Failed to save the polygon!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)));
            }
            
        }

        private bool IfPolygonInside(float x, float y, float width, float height, PointF[] point)
        {
            GraphicsPath myGraphicsPath = new GraphicsPath();
            Region myRegion = new Region();
            myGraphicsPath.Reset();
            myGraphicsPath.AddPolygon(point);
            myRegion.MakeEmpty();
            myRegion.Union(myGraphicsPath);
            bool a = myRegion.IsVisible( x, y, width, height);
            myRegion.Dispose();
            return a;
        }

        private void button21_Click(object sender, EventArgs e)
        {
            dat.Clear();
            dat2.Clear();
            pictureBox2.Invalidate();
        }

        private void pictureBox2_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int originalHeight = this.pictureBox2.Image.Height;

                PropertyInfo rectangleProperty = this.pictureBox2.GetType().GetProperty("ImageRectangle", BindingFlags.Instance | BindingFlags.NonPublic);
                Rectangle rectangle = (Rectangle)rectangleProperty.GetValue(this.pictureBox2, null);

                int currentWidth = rectangle.Width;

                int currentHeight = rectangle.Height;

                double rate = (double)currentHeight / (double)originalHeight;

                int black_left_width = (currentWidth == this.pictureBox2.Width) ? 0 : (this.pictureBox2.Width - currentWidth) / 2;
                int black_top_height = (currentHeight == this.pictureBox2.Height) ? 0 : (this.pictureBox2.Height - currentHeight) / 2;

                int zoom_x = e.X - black_left_width;
                int zoom_y = e.Y - black_top_height;

                double original_x = (double)zoom_x / rate;
                double original_y = (double)zoom_y / rate;
                dat2.Add(new Point(Convert.ToInt32(original_x), Convert.ToInt32(original_y)));
                dat.Add(new Point(e.X, e.Y));
                pictureBox2.Invalidate();
            }
            if (e.Button == MouseButtons.Right)
            {
                int originalHeight = this.pictureBox2.Image.Height;

                PropertyInfo rectangleProperty = this.pictureBox2.GetType().GetProperty("ImageRectangle", BindingFlags.Instance | BindingFlags.NonPublic);
                Rectangle rectangle = (Rectangle)rectangleProperty.GetValue(this.pictureBox2, null);

                int currentWidth = rectangle.Width;

                int currentHeight = rectangle.Height;

                double rate = (double)currentHeight / (double)originalHeight;

                int black_left_width = (currentWidth == this.pictureBox2.Width) ? 0 : (this.pictureBox2.Width - currentWidth) / 2;
                int black_top_height = (currentHeight == this.pictureBox2.Height) ? 0 : (this.pictureBox2.Height - currentHeight) / 2;

                int zoom_x = e.X - black_left_width;
                int zoom_y = e.Y - black_top_height;

                double original_x = (double)zoom_x / rate;
                double original_y = (double)zoom_y / rate;
                //Console.WriteLine(original_x.ToString());
                //Console.WriteLine(original_y.ToString());
                int LabelArea = JudgeArea(new Point((int)original_x, (int)original_y));
                //Console.WriteLine(LabelArea.ToString());
                if (LabelArea != -1)
                {

                    for (int i = 0, j = 21; i < LabelList.Count; i++, j++)
                    {
                        if (((CheckBox)this.Controls.Find("checkBox" + j, true)[0]).Checked)
                        {
                            //Console.WriteLine(i);
                            currentLabel[(LabelArea * (LabelList.Count + 1)) + i] = "1";
                        }

                    }
                    if (LastArea != -1 && LastArea != LabelArea)
                    {
                        Drawbox(pictureBox2.Image, LastArea, Color.Blue);
                    }
                    Drawbox(pictureBox2.Image, LabelArea, Color.White);
                    LastArea = LabelArea;
                }
            }
        }

        private void pictureBox2_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                int originalHeight = this.pictureBox2.Image.Height;

                PropertyInfo rectangleProperty = this.pictureBox2.GetType().GetProperty("ImageRectangle", BindingFlags.Instance | BindingFlags.NonPublic);
                Rectangle rectangle = (Rectangle)rectangleProperty.GetValue(this.pictureBox2, null);

                int currentWidth = rectangle.Width;
                int currentHeight = rectangle.Height;

                double rate = (double)currentHeight / (double)originalHeight;

                int black_left_width = (currentWidth == this.pictureBox2.Width) ? 0 : (this.pictureBox2.Width - currentWidth) / 2;
                int black_top_height = (currentHeight == this.pictureBox2.Height) ? 0 : (this.pictureBox2.Height - currentHeight) / 2;

                int zoom_x = e.X - black_left_width;
                int zoom_y = e.Y - black_top_height;

                double original_x = (double)zoom_x / rate;
                double original_y = (double)zoom_y / rate;

                int LabelArea = JudgeArea(new Point((int)original_x, (int)original_y));

                if (LabelArea != -1)
                {

                    for (int i = 0, j = 21; i < LabelList.Count; i++, j++)
                    {
                        if (((CheckBox)this.Controls.Find("checkBox" + j, true)[0]).Checked)
                        {
                            //Console.WriteLine(i);
                            currentLabel[(LabelArea * (LabelList.Count + 1)) + i] = "0";
                        }
                        

                    }

                    if (LastArea != -1 && LastArea != LabelArea)
                    {
                        Drawbox(pictureBox2.Image, LastArea, Color.Red);
                    }
                    Drawbox(pictureBox2.Image, LabelArea, Color.Red);
                    LastArea = -1;

                }
            }

        }

        private void button22_Click(object sender, EventArgs e)
        {
            OpenJson.InitialDirectory = logPath;
            OpenJson.Title = "Select a trained model";
            OpenJson.Filter = "ONNX files (*.onnx)|*.onnx";
            OpenJson.RestoreDirectory = true;
            OpenJson.FilterIndex = 1;
            if (SemiAutomatic == true)
            {
                SemiAutomatic = false;
                button22.Text = "Semi-automatic Mode OFF";
            }
            else
            {
                try
                {
                    if (OpenOnnx.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                       
                    }
                }
                catch (Exception)
                {
                    this.BeginInvoke((Action)(() => MessageBox.Show("Failed to read the ONNX model file!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)));
                }
                SemiAutomatic = true;
                button22.Text = "Semi-automatic Mode ON";
            }
        }
    }
}
