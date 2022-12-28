using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Contexts;
using System.Windows;
using System.Windows.Forms;
using MessageBox = System.Windows.Forms.MessageBox;
using Path = System.IO.Path;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;

namespace YOLIC
{
    public partial class Form1 : Form
    {
        Boolean KeepHistory = false;
        Boolean SemiAutomatic = false;
        Boolean CheckMode = false;
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
        JArray LabelAbbreviation;
        int COInumber;
        int Labelnumber;
        int CurrentIndex = 0;
        int LastArea = -1;
        int fullrgb = 0;
        Color[] colorslist = new Color[]{Color.FromArgb(0,255,0), Color.FromArgb(138,244,123), Color.FromArgb(220,0,30),
                              Color.FromArgb(87,96,105), Color.FromArgb(220,87,18), Color.FromArgb(230,180,80),
                              Color.FromArgb(255,0,255), Color.FromArgb(10,255,105), Color.FromArgb(255,0,0),
                               Color.FromArgb(50,60,246), Color.FromArgb(243,10,100), Color.FromArgb(153, 163, 112),
                               Color.FromArgb(91, 97, 67), Color.FromArgb(210, 224, 155), Color.FromArgb(222, 237, 164),
                               Color.FromArgb(243,50,100), Color.FromArgb(112, 163, 153),Color.FromArgb(67, 97, 91),
                               Color.FromArgb(155, 224, 210), Color.FromArgb(164, 237, 222)};

        JArray[] COIList;
        private int clickCount = 0;
        private Point startPoint;
        bool isRectangleMarked = true;
        System.Drawing.Rectangle rectangle;
        JArray marks = new JArray();
        JArray marksForsave = new JArray();
        private PointF[] PolygonPoints = new PointF[0];

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
            if (list_Img == null && list_depthImg == null)
            {
                MessageBox.Show("Import RGB images and Depth images first!", "Notice", MessageBoxButtons.OK);
                return;
            }
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
                    LabelAbbreviation = (JArray)coijsonObject["Labels"]["LabelAbbreviation"];
                    Labelnumber = LabelList.Count;
                    if (LabelList.Count > 20)
                    {
                        MessageBox.Show("Up to 20 labels!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    for (int i = 0, j = 21; i < LabelList.Count; i++, j++)
                    {
                        ((CheckBox)this.Controls.Find("checkBox" + j, true)[0]).Visible = true;
                        ((CheckBox)this.Controls.Find("checkBox" + j, true)[0]).Text = LabelList[i].ToString();
                        ((CheckBox)this.Controls.Find("checkBox" + j, true)[0]).ForeColor = colorslist[i];
                    }

                    button16.Enabled = true;
                    button17.Enabled = true;
                    button22.Enabled = true;
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
                    LabelAbbreviation = (JArray)coijsonObject["Labels"]["LabelAbbreviation"];
                    Labelnumber = LabelList.Count;
                    if (LabelList.Count > 20)
                    {
                        MessageBox.Show("Up to 20 labels!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    for (int i = 0, j = 1; i < LabelList.Count; i++, j++)
                    {
                        ((CheckBox)this.Controls.Find("checkBox" + j, true)[0]).Visible = true;
                        ((CheckBox)this.Controls.Find("checkBox" + j, true)[0]).Text = LabelList[i].ToString();
                        ((CheckBox)this.Controls.Find("checkBox" + j, true)[0]).ForeColor = colorslist[i];
                    }

                    button7.Enabled = true;
                    button25.Enabled = true;
                    button15.Enabled = true;
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
                    label6.Text = list_Img.Count.ToString();
                    button16.Text = "Start";
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
                        MessageBox.Show("No images under this folder!", "Notice", MessageBoxButtons.OK);
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
                Console.WriteLine(saveFile.SelectedPath);
            }
        }
        static void PrintInputMetadata(IReadOnlyDictionary<string, NodeMetadata> inputMeta)
        {
            foreach (var name in inputMeta.Keys)
            {
                Console.WriteLine(name);
                Console.WriteLine("Dimension Length: " + inputMeta[name].Dimensions.Length);
                for (int i = 0; i < inputMeta[name].Dimensions.Length; ++i)
                {
                    Console.WriteLine(inputMeta[name].Dimensions[i]);
                }
                Console.WriteLine(inputMeta[name].ElementType.ToString());
                Console.WriteLine(inputMeta[name].IsTensor.ToString());
                Console.WriteLine(inputMeta[name].OnnxValueType.ToString());
                Console.WriteLine(inputMeta[name].SymbolicDimensions.ToString());
            }

        }

        public Tensor<float> ConvertImageToFloatTensor(Mat image, int mode)
        {
            if (mode == 0)
            {
                Tensor<float> data = new DenseTensor<float>(new[] { 1, 4, image.Height, image.Width });
                Bitmap bitimg = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(image);

                for (int x = 0; x < bitimg.Height; x++)
                {
                    for (int y = 0; y < bitimg.Width; y++)
                    {

                        Color color = bitimg.GetPixel(y, x);

                        data[0, 0, x, y] = color.R / (float)255.0;

                        data[0, 1, x, y] = color.G / (float)255.0;

                        data[0, 2, x, y] = color.B / (float)255.0;

                        data[0, 3, x, y] = color.A / (float)255.0;

                        //if (x == 110 & y == 140)
                        //{
                        //    Console.WriteLine(color.B);
                        //    Console.WriteLine(color.G);
                        //    Console.WriteLine(color.R);
                        //    Console.WriteLine(color.A);
                        //}
                    }
                }
                return data;
            }
            else
            {
                Tensor<float> data = new DenseTensor<float>(new[] { 1, 3, image.Height, image.Width });
                Bitmap bitimg = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(image);

                for (int x = 0; x < bitimg.Height; x++)
                {
                    for (int y = 0; y < bitimg.Width; y++)
                    {

                        Color color = bitimg.GetPixel(y, x);

                        data[0, 0, x, y] = color.R / (float)255.0;

                        data[0, 1, x, y] = color.G / (float)255.0;

                        data[0, 2, x, y] = color.B / (float)255.0;


                        //if (x == 110 & y == 140)
                        //{
                        //    Console.WriteLine(color.B);
                        //    Console.WriteLine(color.G);
                        //    Console.WriteLine(color.R);
                        //    Console.WriteLine(color.A);
                        //}
                    }
                }
                return data;
            }

        }
        private void Display(int currentIndex, int auto = 1)
        {

            string Imagename = Path.GetFileName(list_Img[currentIndex]);
            string DImagename = Path.GetFileName(list_depthImg[currentIndex]);
            label8.Text = Imagename;
            label9.Text = DImagename;
            if (!Imagename.Equals(DImagename))
            {
                MessageBox.Show("Unable find the corresponding rgb or depth images!", "Notice", MessageBoxButtons.OK);
                return;
            }
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
            for (int i = 0; i < COIList.Length; i++)
            {
                if (COIList[i][0].ToString().Equals("rectangle"))
                {
                    rgb.DrawRectangle(new Pen(Color.Black, 3), (float)COIList[i][1] * pictureBox2.Image.Width, (float)COIList[i][2] * pictureBox2.Image.Height, (float)COIList[i][3] * pictureBox2.Image.Width, (float)COIList[i][4] * pictureBox2.Image.Height);
                    depth.DrawRectangle(new Pen(Color.Black, 3), (float)COIList[i][1] * pictureBox3.Image.Width, (float)COIList[i][2] * pictureBox3.Image.Height, (float)COIList[i][3] * pictureBox3.Image.Width, (float)COIList[i][4] * pictureBox3.Image.Height);
                }
                if (COIList[i][0].ToString().Equals("polygon"))
                {
                    int COI_count = COIList[i].Count;
                    List<PointF> polygonListRgb = new List<PointF>();
                    List<PointF> polygonListDepth = new List<PointF>();
                    for (int index = 1; index < COI_count; index = index + 2)
                    {
                        polygonListRgb.Add(new PointF((float)COIList[i][index] * pictureBox2.Image.Width, (float)COIList[i][index + 1] * pictureBox2.Image.Height));
                        polygonListDepth.Add(new PointF((float)COIList[i][index] * pictureBox3.Image.Width, (float)COIList[i][index + 1] * pictureBox3.Image.Height));
                    }
                    PointF[] points_rgb = polygonListRgb.ToArray();
                    PointF[] points_depth = polygonListDepth.ToArray();
                    rgb.DrawPolygon(new Pen(Color.Black, 3), points_rgb);
                    depth.DrawPolygon(new Pen(Color.Black, 3), points_depth);
                }

            }

            if (SemiAutomatic == true && auto == 1)
            {
                Mat color_image = Cv2.ImRead(list_Img[CurrentIndex], ImreadModes.Color);
                Mat depth_image = Cv2.ImRead(list_depthImg[CurrentIndex], ImreadModes.Color);
                Mat[] cvd = Cv2.Split(depth_image);
                Mat[] cvrgb = Cv2.Split(color_image);
                Mat merged = new Mat();
                Cv2.Merge(new Mat[] { cvrgb[0], cvrgb[1], cvrgb[2], cvd[0] }, merged);
                Mat outimg = new Mat();

                //Console.WriteLine(outimg.Channels());
                //Console.WriteLine(outimg.Get<Vec4b>(110, 140));
                string ModelName = OpenOnnx.FileName;
                using (var session = new InferenceSession(ModelName))
                {

                    var inputMeta = session.InputMetadata;
                    var container = new List<NamedOnnxValue>();
                    //PrintInputMetadata(inputMeta);

                    foreach (var name in inputMeta.Keys)
                    {
                        Console.WriteLine("Dimension Length: " + inputMeta[name].Dimensions[1]);
                        if (inputMeta[name].Dimensions[1] != 4)
                        {
                            MessageBox.Show("Unable match the RGBD 4 channel!", "Notice", MessageBoxButtons.OK);
                            return;
                        }
                        Cv2.Resize(merged, outimg, new OpenCvSharp.Size(inputMeta[name].Dimensions[3], inputMeta[name].Dimensions[2]));
                        Tensor<float> inputdata = ConvertImageToFloatTensor(outimg, 0);
                        container.Add(NamedOnnxValue.CreateFromTensor<float>(name, inputdata));
                    }

                    using (var results = session.Run(container))  // results is an IDisposableReadOnlyCollection<DisposableNamedOnnxValue> container
                    {
                        // Get the results
                        foreach (var r in results)
                        {
                            //Console.WriteLine("Output Name: {0}", r.Name);
                            int[] prediction = sigmoidup(r.AsTensor<float>());
                            int numcell = prediction.Length / (Labelnumber + 1);
                            //Console.WriteLine(numcell);
                            if (numcell != COInumber)
                            {
                                this.BeginInvoke((Action)(() => MessageBox.Show("Failed to get output from the model!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)));
                                return;
                            }
                            for (int i = 0; i < currentLabel.Length; i++)
                            {
                                currentLabel[i] = prediction[i].ToString();
                            }
                            Redraw(pictureBox2.Image);
                            //for (int i = 1, j = 0; i <= prediction.Length; i = i + Labelnumber + 1, j++)
                            //{
                            //    int[] cell = new int[Labelnumber+1];
                            //    Array.Copy(prediction, i-1, cell, 0, Labelnumber + 1);
                            //    if (cell[cell.Length-1] == 1) { continue; }

                            //    //for(int j = 0; j < cell.Length; j++)
                            //    //{
                            //    //    Console.Write(cell[j]);
                            //    //}
                            //    //Console.WriteLine();

                            //    //Console.Write(prediction[i - 1]);
                            //    //Console.Write(" ");
                            //    //if (i % 12 == 0)
                            //    //{
                            //    //    Console.WriteLine(" ");
                            //    //}
                            //}
                        }
                    }


                }


            }
            if (CheckMode == true)
            {
                string NameWithoutExtension = Path.GetFileNameWithoutExtension(list_Img[currentIndex]);
                //Console.WriteLine(Path.Combine(saveFile.SelectedPath,NameWithoutExtension + ".txt"));
                if (File.Exists(Path.Combine(saveFile.SelectedPath, NameWithoutExtension + ".txt")) == true)
                {

                    StreamReader rd = File.OpenText(Path.Combine(saveFile.SelectedPath, NameWithoutExtension + ".txt"));
                    string s = rd.ReadLine();
                    string[] currentLabelFormTxt = s.Split(' ');
                    //Console.WriteLine(currentLabelFormTxt.Length);
                    //Console.WriteLine(currentLabel.Length);
                    rd.Close();
                    try
                    {
                        for (int i = 0; i < currentLabel.Length; i++)
                        {
                            currentLabel[i] = currentLabelFormTxt[i];
                        }
                        //Console.WriteLine(currentLabel.Length);
                        Redraw(pictureBox2.Image);


                    }
                    catch (Exception)
                    {
                        Console.WriteLine("init label");
                        for (int i = 0; i < currentLabel.Length; i++)
                        {
                            currentLabel[i] = "0";

                        }
                    }

                }

            }
        }

        private int[] sigmoidup(Tensor<float> tensors)
        {

            int[] output = new int[tensors.Length];
            for (int i = 0; i < tensors.Length; ++i)
            {
                float prob = tensors.GetValue(i);
                if (prob <= 0)
                {
                    output[i] = 0;
                }
                else
                {
                    output[i] = 1;
                }

            }
            return output;
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
                button15.Enabled = true;
            }
        }

        private void DisplayRGB(int currentIndex, int auto = 1)
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
                    rgb.DrawRectangle(new Pen(Color.Black, 3), (float)COIList[i][1] * pictureBox1.Image.Width, (float)COIList[i][2] * pictureBox1.Image.Height, (float)COIList[i][3] * pictureBox1.Image.Width, (float)COIList[i][4] * pictureBox1.Image.Height);

                }
                if (COIList[i][0].ToString().Equals("polygon"))
                {
                    int COI_count = COIList[i].Count;
                    List<PointF> polygonList = new List<PointF>();
                    for (int index = 1; index < COI_count; index = index + 2)
                    {
                        polygonList.Add(new PointF((float)COIList[i][index] * pictureBox1.Image.Width, (float)COIList[i][index + 1] * pictureBox1.Image.Height));
                    }
                    PointF[] points = polygonList.ToArray();
                    rgb.DrawPolygon(new Pen(Color.Black, 3), points);
                }

            }
            if (SemiAutomatic == true && auto == 1)
            {
                Mat color_image = Cv2.ImRead(list_Img[CurrentIndex], ImreadModes.Color);
                //Console.WriteLine(outimg.Channels());
                //Console.WriteLine(outimg.Get<Vec4b>(110, 140));
                string ModelName = OpenOnnx.FileName;
                using (var session = new InferenceSession(ModelName))
                {

                    var inputMeta = session.InputMetadata;
                    var container = new List<NamedOnnxValue>();
                    Mat[] cvrgb = Cv2.Split(color_image);
                    Mat merged = new Mat();
                    Cv2.Merge(new Mat[] { cvrgb[0], cvrgb[1], cvrgb[2] }, merged);
                    Mat outimg = new Mat();

                    PrintInputMetadata(inputMeta);

                    foreach (var name in inputMeta.Keys)
                    {
                        //Console.WriteLine(": " + inputMeta[name].Dimensions.Length);
                        if (inputMeta[name].Dimensions[1] != 3)
                        {
                            MessageBox.Show("Unable match the RGB 3 channel!", "Notice", MessageBoxButtons.OK);
                            return;
                        }
                        Cv2.Resize(merged, outimg, new OpenCvSharp.Size(inputMeta[name].Dimensions[3], inputMeta[name].Dimensions[2])); // resize(w,h)
                        Tensor<float> inputdata = ConvertImageToFloatTensor(outimg, 1);
                        container.Add(NamedOnnxValue.CreateFromTensor<float>(name, inputdata));
                    }

                    using (var results = session.Run(container))  // results is an IDisposableReadOnlyCollection<DisposableNamedOnnxValue> container
                    {
                        // Get the results
                        foreach (var r in results)
                        {
                            //Console.WriteLine("Output Name: {0}", r.Name);
                            int[] prediction = sigmoidup(r.AsTensor<float>());
                            int numcell = prediction.Length / (Labelnumber + 1);
                            //Console.WriteLine(numcell);
                            //Console.WriteLine(COInumber);
                            if (numcell != COInumber)
                            {
                                this.BeginInvoke((Action)(() => MessageBox.Show("Failed to get output from the model!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)));
                                return;
                            }
                            for (int i = 0; i < currentLabel.Length; i++)
                            {
                                currentLabel[i] = prediction[i].ToString();
                            }
                            RedrawR(pictureBox1.Image);
                            //for (int i = 1, j = 0; i <= prediction.Length; i = i + Labelnumber + 1, j++)
                            //{
                            //    int[] cell = new int[Labelnumber+1];
                            //    Array.Copy(prediction, i-1, cell, 0, Labelnumber + 1);
                            //    if (cell[cell.Length-1] == 1) { continue; }

                            //    //for(int j = 0; j < cell.Length; j++)
                            //    //{
                            //    //    Console.Write(cell[j]);
                            //    //}
                            //    //Console.WriteLine();

                            //    //Console.Write(prediction[i - 1]);
                            //    //Console.Write(" ");
                            //    //if (i % 12 == 0)
                            //    //{
                            //    //    Console.WriteLine(" ");
                            //    //}
                            //}
                        }
                    }


                }


            }
            if (CheckMode == true)
            {
                string NameWithoutExtension = Path.GetFileNameWithoutExtension(list_Img[currentIndex]);
                //Console.WriteLine(Path.Combine(saveFile.SelectedPath,NameWithoutExtension + ".txt"));
                if (File.Exists(Path.Combine(saveFile.SelectedPath, NameWithoutExtension + ".txt")) == true)
                {

                    StreamReader rd = File.OpenText(Path.Combine(saveFile.SelectedPath, NameWithoutExtension + ".txt"));
                    string s = rd.ReadLine();
                    string[] currentLabelFormTxt = s.Split(' ');
                    //Console.WriteLine(currentLabelFormTxt.Length);
                    //Console.WriteLine(currentLabel.Length);
                    rd.Close();
                    try
                    {
                        for (int i = 0; i < currentLabel.Length; i++)
                        {
                            currentLabel[i] = currentLabelFormTxt[i];
                        }
                        //Console.WriteLine(currentLabel.Length);
                        RedrawR(pictureBox1.Image);


                    }
                    catch (Exception)
                    {
                        Console.WriteLine("init label");
                        for (int i = 0; i < currentLabel.Length; i++)
                        {
                            currentLabel[i] = "0";

                        }
                    }


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
                for (int j = 0; j < LabelList.Count; j++)
                {
                    if (currentLabel[(i * (LabelList.Count + 1)) + j].Equals("1"))
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
            if (COIList[labelArea][0].ToString().Equals("polygon"))
            {
                int COI_count = COIList[labelArea].Count;
                List<PointF> polygonList = new List<PointF>();
                for (int index = 1; index < COI_count; index = index + 2)
                {
                    polygonList.Add(new PointF((float)COIList[labelArea][index] * pictureBox2.Image.Width, (float)COIList[labelArea][index + 1] * pictureBox2.Image.Height));
                }
                PointF[] points = polygonList.ToArray();
                rgb.DrawPolygon(new Pen(c, 2), points);
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
            if (COIList[labelArea][0].ToString().Equals("polygon"))
            {
                int COI_count = COIList[labelArea].Count;
                List<PointF> polygonList = new List<PointF>();
                for (int index = 1; index < COI_count; index = index + 2)
                {
                    polygonList.Add(new PointF((float)COIList[labelArea][index] * pictureBox1.Image.Width, (float)COIList[labelArea][index + 1] * pictureBox1.Image.Height));
                }
                PointF[] points = polygonList.ToArray();
                rgb.DrawPolygon(new Pen(c, 2), points);
            }
        }
        private int JudgeArea(Point point)
        {
            int area = -1;
            for (int i = 0; i < COIList.Length; i++)
            {
                //Console.WriteLine(pictureBox2.Image.Width);
                //Console.WriteLine(new Point((int)((double)COIList[0][1] * pictureBox2.Image.Width), (int)((double)COIList[0][2] * pictureBox2.Image.Height)));
                if (COIList[i][0].ToString().Equals("rectangle"))
                {
                    if (IfInside(point, new Point[]{ new Point((int)((double)COIList[i][1] * pictureBox2.Image.Width), (int)((double)COIList[i][2] * pictureBox2.Image.Height)),
                    new Point((int)(((double)COIList[i][1] + (double)COIList[i][3]) * pictureBox2.Image.Width), (int)((double)COIList[i][2] * pictureBox2.Image.Height)),
                    new Point((int)(((double)COIList[i][1] + (double)COIList[i][3]) * pictureBox2.Image.Width), (int)(((double)COIList[i][2] + (double)COIList[i][4]) * pictureBox2.Image.Height)),
                    new Point((int)((double)COIList[i][1] * pictureBox2.Image.Width), (int)(((double)COIList[i][2] + (double)COIList[i][4]) * pictureBox2.Image.Height)) }))
                    {

                        area = i;
                    }
                }
                if (COIList[i][0].ToString().Equals("polygon"))
                {
                    int COI_count = COIList[i].Count;
                    List<PointF> polygonList = new List<PointF>();
                    for (int index = 1; index < COI_count; index = index + 2)
                    {
                        polygonList.Add(new PointF((float)COIList[i][index] * pictureBox2.Image.Width, (float)COIList[i][index + 1] * pictureBox2.Image.Height));
                    }
                    PointF[] points = polygonList.ToArray();
                    if (IfInside(point, points))
                    {
                        area = i;
                    }
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
                if (COIList[i][0].ToString().Equals("rectangle")){
                    if (IfInside(point, new Point[]{ new Point((int)((double)COIList[i][1] * pictureBox1.Image.Width), (int)((double)COIList[i][2] * pictureBox1.Image.Height)),
                    new Point((int)(((double)COIList[i][1] + (double)COIList[i][3]) * pictureBox1.Image.Width), (int)((double)COIList[i][2] * pictureBox1.Image.Height)),
                    new Point((int)(((double)COIList[i][1] + (double)COIList[i][3]) * pictureBox1.Image.Width), (int)(((double)COIList[i][2] + (double)COIList[i][4]) * pictureBox1.Image.Height)),
                    new Point((int)((double)COIList[i][1] * pictureBox1.Image.Width), (int)(((double)COIList[i][2] + (double)COIList[i][4]) * pictureBox1.Image.Height)) }))
                    {

                        area = i;
                    }
                }
                if (COIList[i][0].ToString().Equals("polygon"))
                {
                    int COI_count = COIList[i].Count;
                    List<PointF> polygonList = new List<PointF>();
                    for (int index = 1; index < COI_count; index = index + 2)
                    {
                        polygonList.Add(new PointF((float)COIList[i][index] * pictureBox1.Image.Width, (float)COIList[i][index + 1] * pictureBox1.Image.Height));
                    }
                    PointF[] points = polygonList.ToArray();
                    if (IfInside(point, points))
                    {
                        area = i;
                    }
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
        private bool IfInside(Point checkPoint, PointF[] point)
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
            LastArea = -1;
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
            if (checkBox41.Checked == true)
            {
                SaveImage(CurrentIndex);
            }

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
                //for (int i = 0, j = 21; i < LabelList.Count; i++, j++)
                //{
                //    ((CheckBox)this.Controls.Find("checkBox" + j, true)[0]).Checked = false;

                //}
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
            int opacity = 255; // 50% opaque (0 = invisible, 255 = fully opaque)
            for (int i = 0; i < COIList.Length; i++)
            {
                int colorindex = 0;
                string normal = "1";
                string classlabel = "";
                for (int j = 0; j < LabelList.Count; j++)
                {
                    if (currentLabel[(i * (LabelList.Count + 1)) + j].Equals("1"))
                    {
                        normal = "0";
                        colorindex = j;
                        classlabel += LabelAbbreviation[j].ToString() + " ";

                    }
                }
                if (normal.Equals("0"))
                {
                    if (COIList[i][0].ToString().Equals("rectangle"))
                    {
                        rgb.DrawRectangle(new Pen(colorslist[colorindex], 2), (float)COIList[i][1] * pictureBox2.Image.Width, (float)COIList[i][2] * pictureBox2.Image.Height, (float)COIList[i][3] * pictureBox2.Image.Width, (float)COIList[i][4] * pictureBox2.Image.Height);
                        Rectangle rect = new Rectangle((int)((float)COIList[i][1] * pictureBox2.Image.Width), (int)((float)COIList[i][2] * pictureBox2.Image.Height), (int)((float)COIList[i][3] * pictureBox2.Image.Width), (int)((float)COIList[i][4] * pictureBox2.Image.Height));
                        rgb.DrawString(classlabel, new Font("Arial", 9), new SolidBrush(Color.FromArgb(opacity, Color.Yellow)), rect);
                    }
                    if (COIList[i][0].ToString().Equals("polygon"))
                    {
                        int COI_count = COIList[i].Count;
                        List<PointF> polygonList = new List<PointF>();
                        for (int index = 1; index < COI_count; index = index + 2)
                        {
                            polygonList.Add(new PointF((float)COIList[i][index] * pictureBox2.Image.Width, (float)COIList[i][index + 1] * pictureBox2.Image.Height));
                        }
                        PointF[] points = polygonList.ToArray();
                        rgb.DrawPolygon(new Pen(colorslist[colorindex], 2), points);
                        rgb.DrawString(classlabel, new Font("Arial", 9), new SolidBrush(Color.FromArgb(opacity, Color.Yellow)), new PointF((float)COIList[i][1] * pictureBox2.Image.Width, (float)COIList[i][2] * pictureBox2.Image.Height));
                    }
                }

            }
        }
        private void RedrawR(Image g)
        {
            pictureBox1.Image = g;
            System.Drawing.Graphics rgb = Graphics.FromImage(pictureBox1.Image);
            int opacity = 255; // 50% opaque (0 = invisible, 255 = fully opaque)
            for (int i = 0; i < COIList.Length; i++)
            {
                int colorindex = 0;
                string normal = "1";
                string classlabel = "";
                for (int j = 0; j < LabelList.Count; j++)
                {
                    if (currentLabel[(i * (LabelList.Count + 1)) + j].Equals("1"))
                    {
                        normal = "0";
                        colorindex = j;
                        classlabel += LabelAbbreviation[j].ToString() + " ";
                    }
                }
                if (normal.Equals("0"))
                {
                    if (COIList[i][0].ToString().Equals("rectangle"))
                    {
                        rgb.DrawRectangle(new Pen(colorslist[colorindex], 2), (float)COIList[i][1] * pictureBox1.Image.Width, (float)COIList[i][2] * pictureBox1.Image.Height, (float)COIList[i][3] * pictureBox1.Image.Width, (float)COIList[i][4] * pictureBox1.Image.Height);
                        Rectangle rect = new Rectangle((int)((float)COIList[i][1] * pictureBox1.Image.Width), (int)((float)COIList[i][2] * pictureBox1.Image.Height), (int)((float)COIList[i][3] * pictureBox1.Image.Width), (int)((float)COIList[i][4] * pictureBox1.Image.Height));
                        rgb.DrawString(classlabel, new Font("Arial", 9), new SolidBrush(Color.FromArgb(opacity, Color.Yellow)), rect);
                    }
                    if (COIList[i][0].ToString().Equals("polygon"))
                    {
                        int COI_count = COIList[i].Count;
                        List<PointF> polygonList = new List<PointF>();
                        for (int index = 1; index < COI_count; index = index + 2)
                        {
                            polygonList.Add(new PointF((float)COIList[i][index] * pictureBox1.Image.Width, (float)COIList[i][index + 1] * pictureBox1.Image.Height));
                        }
                        PointF[] points = polygonList.ToArray();
                        rgb.DrawPolygon(new Pen(colorslist[colorindex], 2), points);
                        rgb.DrawString(classlabel, new Font("Arial", 9), new SolidBrush(Color.FromArgb(opacity, Color.Yellow)), new PointF((float)COIList[i][1] * pictureBox1.Image.Width, (float)COIList[i][2] * pictureBox1.Image.Height));
                    }
                }

            }
        }

        private void button17_Click(object sender, EventArgs e)
        {
            if (File.Exists(Path.Combine(saveFile.SelectedPath, Path.GetFileNameWithoutExtension(list_Img[CurrentIndex]) + ".txt")) == true)
            {
                string annotationpath = Path.Combine(saveFile.SelectedPath, Path.GetFileNameWithoutExtension(list_Img[CurrentIndex]) + ".txt");
                FileInfo fi = new FileInfo(annotationpath);
                fi.Delete();
            }

            for (int i = 0; i < currentLabel.Length; i++)
            {
                currentLabel[i] = "0";
            }
            Display(CurrentIndex, 0);
            LastArea = -1;


        }

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
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
                dat2.Add(new Point(Convert.ToInt32(original_x), Convert.ToInt32(original_y)));
                dat.Add(new Point(e.X, e.Y));
                pictureBox1.Invalidate();
            }
            if (e.Button == MouseButtons.Right)
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
                            if (LastArea != -1 && LastArea != LabelArea)
                            {
                                DrawboxRGB(pictureBox1.Image, LastArea, Color.Blue);
                            }
                            DrawboxRGB(pictureBox1.Image, LabelArea, Color.White);
                            LastArea = LabelArea;
                        }
                        else
                        {
                            currentLabel[(LabelArea * (LabelList.Count + 1)) + i] = "0";
                        }

                    }
                    //if (LastArea != -1 && LastArea != LabelArea)
                    //{
                    //    DrawboxRGB(pictureBox1.Image, LastArea, Color.Blue);
                    //}
                    //DrawboxRGB(pictureBox1.Image, LabelArea, Color.White);
                    //LastArea = LabelArea;
                }
            }

        }

        private void pictureBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
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
                        DrawboxRGB(pictureBox1.Image, LastArea, Color.Black);
                    }
                    DrawboxRGB(pictureBox1.Image, LabelArea, Color.Black);
                    LastArea = -1;

                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (checkBox42.Checked == true)
            {
                SaveImage(CurrentIndex);
            }
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

                for (int i = 0; i < currentLabel.Length; i++)
                {
                    currentLabel[i] = "0";
                }
                DisplayRGB(CurrentIndex);
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
            //for (int i = 0, j = 1; i < LabelList.Count; i++, j++)
            //{
            //    ((CheckBox)this.Controls.Find("checkBox" + j, true)[0]).Checked = false;

            //}
            for (int i = 0; i < currentLabel.Length; i++)
            {
                currentLabel[i] = "0";
            }

            DisplayRGB(CurrentIndex);
        }

        private void button15_Click(object sender, EventArgs e)
        {
            if (File.Exists(Path.Combine(saveFile.SelectedPath, Path.GetFileNameWithoutExtension(list_Img[CurrentIndex]) + ".txt")) == true)
            {
                string annotationpath = Path.Combine(saveFile.SelectedPath, Path.GetFileNameWithoutExtension(list_Img[CurrentIndex]) + ".txt");
                FileInfo fi = new FileInfo(annotationpath);
                fi.Delete();
            }

            for (int i = 0; i < currentLabel.Length; i++)
            {
                currentLabel[i] = "0";
            }
            DisplayRGB(CurrentIndex, 0);
            LastArea = -1;
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
            CurrentIndex = int.Parse("0" + textBox2.Text);
            LastArea = -1;

            if (CurrentIndex >= list_Img.Count)
            {
                MessageBox.Show("The specified image is complete!", "Notice", MessageBoxButtons.OK);
                CurrentIndex = temp;
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

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!(char.IsNumber(e.KeyChar)) && e.KeyChar != (char)8)
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
                button19.Text = "Label History OFF";
            }
            else
            {
                KeepHistory = true;
                button19.Text = "Label History ON";
            }
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
                                    Pen p = new Pen(colorslist[ii], 3);
                                    g.DrawPolygon(p, dat2.ToArray());
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
                    if (COIList[i][0].ToString().Equals("polygon"))
                    {
                        int COI_count = COIList[i].Count;
                        List<PointF> polygonList = new List<PointF>();
                        for (int index = 1; index < COI_count; index = index + 2)
                        {
                            polygonList.Add(new PointF((float)COIList[i][index] * pictureBox2.Image.Width, (float)COIList[i][index + 1] * pictureBox2.Image.Height));
                        }
                        PointF[] points = polygonList.ToArray();
                        if (IfPolygonInside(points, dat2.ToArray(), g))
                        {
                            //System.Console.WriteLine(i);
                            for (int ii = 0, j = 21; ii < LabelList.Count; ii++, j++)
                            {
                                if (((CheckBox)this.Controls.Find("checkBox" + j, true)[0]).Checked)
                                {
                                    Pen p = new Pen(colorslist[ii], 3);
                                    g.DrawPolygon(p, dat2.ToArray());
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
            bool a = myRegion.IsVisible(x, y, width, height);
            myRegion.Dispose();
            return a;
        }

        private bool IfPolygonInside(PointF[] points1, PointF[] points2, Graphics g)
        {
            // 创建两个 Region 对象
            byte[] types1 = new byte[points1.Length];
            for (int i = 0; i < types1.Length; i++)
            {
                types1[i] = (byte)PathPointType.Line;
            }
            Region region1 = new Region(new GraphicsPath(points1, types1));

            byte[] types2 = new byte[points2.Length];
            for (int i = 0; i < types2.Length; i++)
            {
                types2[i] = (byte)PathPointType.Line;
            }
            Region region2 = new Region(new GraphicsPath(points2, types2));
            region1.Intersect(region2);
            if (!region1.IsEmpty(g))
            {
                //Console.WriteLine("The two polygons intersect.");
                return true;
            }
            else
            {
                return false;
            }

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

                            if (LastArea != -1 && LastArea != LabelArea)
                            {
                                Drawbox(pictureBox2.Image, LastArea, Color.Blue);
                            }
                            Drawbox(pictureBox2.Image, LabelArea, Color.White);
                            LastArea = LabelArea;
                        }

                    }


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
                        else
                        {
                            currentLabel[(LabelArea * (LabelList.Count + 1)) + i] = "0";
                        }


                    }

                    if (LastArea != -1 && LastArea != LabelArea)
                    {
                        Drawbox(pictureBox2.Image, LastArea, Color.Black);
                    }
                    Drawbox(pictureBox2.Image, LabelArea, Color.Black);
                    LastArea = -1;

                }
            }

        }

        private void button22_Click(object sender, EventArgs e)
        {
            OpenOnnx.InitialDirectory = logPath;
            OpenOnnx.Title = "Select a trained model";
            OpenOnnx.Filter = "ONNX files (*.onnx)|*.onnx";
            OpenOnnx.RestoreDirectory = true;
            OpenOnnx.FilterIndex = 1;

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
                        SemiAutomatic = true;
                        button22.Text = "Semi-automatic Mode ON";
                    }
                }
                catch (Exception)
                {
                    this.BeginInvoke((Action)(() => MessageBox.Show("Failed to read the ONNX model file!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)));
                }

            }
        }

        private void pictureBox3_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (fullrgb == 0)
            {
                pictureBox2.Size = new System.Drawing.Size(pictureBox2.Size.Width, pictureBox2.Size.Height + pictureBox3.Size.Height / 2);
                fullrgb = 1;
            }
            else
            {
                pictureBox2.Size = new System.Drawing.Size(pictureBox2.Size.Width, pictureBox3.Size.Height);
                fullrgb = 0;
            }



        }

        private void button23_Click(object sender, EventArgs e)
        {
            if (CheckMode == true)
            {
                CheckMode = false;
                button23.Text = "Check Mode OFF";
            }
            else
            {
                CheckMode = true;
                button23.Text = "Check Mode ON";
            }
        }

        private void label11_DoubleClick(object sender, EventArgs e)
        {
            if (LabelList != null)
            {
                for (int i = 0, j = 21; i < LabelList.Count; i++, j++)
                {
                    ((CheckBox)this.Controls.Find("checkBox" + j, true)[0]).Checked = false;

                }
            }

        }

        private void button25_Click(object sender, EventArgs e)
        {
            OpenOnnx.InitialDirectory = logPath;
            OpenOnnx.Title = "Select a trained model";
            OpenOnnx.Filter = "ONNX files (*.onnx)|*.onnx";
            OpenOnnx.RestoreDirectory = true;
            OpenOnnx.FilterIndex = 1;

            if (SemiAutomatic == true)
            {
                SemiAutomatic = false;
                button25.Text = "Semi-automatic Mode OFF";
            }
            else
            {
                try
                {
                    if (OpenOnnx.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        SemiAutomatic = true;
                        button25.Text = "Semi-automatic Mode ON";
                    }
                }
                catch (Exception)
                {
                    this.BeginInvoke((Action)(() => MessageBox.Show("Failed to read the ONNX model file!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)));
                }

            }
        }

        private void button24_Click(object sender, EventArgs e)
        {
            if (CheckMode == true)
            {
                CheckMode = false;
                button24.Text = "Check Mode OFF";
            }
            else
            {
                CheckMode = true;
                button24.Text = "Check Mode ON";
            }
        }

        private void label12_DoubleClick(object sender, EventArgs e)
        {
            if (LabelList != null)
            {
                for (int i = 0, j = 1; i < LabelList.Count; i++, j++)
                {
                    ((CheckBox)this.Controls.Find("checkBox" + j, true)[0]).Checked = false;

                }
            }
        }

        private void button27_Click(object sender, EventArgs e)
        {
            try
            {
                System.Drawing.Graphics g = Graphics.FromImage(pictureBox1.Image);
                //var g = e.Graphics;


                for (int i = 0; i < COIList.Length; i++)
                {
                    if (COIList[i][0].ToString().Equals("rectangle"))
                    {
                        if (IfPolygonInside((float)COIList[i][1] * pictureBox1.Image.Width, (float)COIList[i][2] * pictureBox1.Image.Height, (float)COIList[i][3] * pictureBox1.Image.Width, (float)COIList[i][4] * pictureBox1.Image.Height, dat2.ToArray()))
                        {
                            //System.Console.WriteLine(i);
                            for (int ii = 0, j = 1; ii < LabelList.Count; ii++, j++)
                            {
                                if (((CheckBox)this.Controls.Find("checkBox" + j, true)[0]).Checked)
                                {
                                    Pen p = new Pen(colorslist[ii], 3);
                                    g.DrawPolygon(p, dat2.ToArray());
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
                    if (COIList[i][0].ToString().Equals("polygon"))
                    {
                        int COI_count = COIList[i].Count;
                        List<PointF> polygonList = new List<PointF>();
                        for (int index = 1; index < COI_count; index = index + 2)
                        {
                            polygonList.Add(new PointF((float)COIList[i][index] * pictureBox1.Image.Width, (float)COIList[i][index + 1] * pictureBox1.Image.Height));
                        }
                        PointF[] points = polygonList.ToArray();

                        if (IfPolygonInside(points, dat2.ToArray(), g))
                        {
                            //System.Console.WriteLine(i);
                            for (int ii = 0, j = 1; ii < LabelList.Count; ii++, j++)
                            {
                                if (((CheckBox)this.Controls.Find("checkBox" + j, true)[0]).Checked)
                                {
                                    Pen p = new Pen(colorslist[ii], 3);
                                    g.DrawPolygon(p, dat2.ToArray());
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
                pictureBox1.Invalidate();
            }
            catch (Exception)
            {
                this.BeginInvoke((Action)(() => MessageBox.Show("Failed to save the polygon! Please use the right click to mark the cell", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)));
            }
        }

        private void button26_Click(object sender, EventArgs e)
        {
            dat.Clear();
            dat2.Clear();
            pictureBox1.Invalidate();
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
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

        private void button28_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.jpe, *.jfif, *.png) | *.jpg; *.jpeg; *.jpe; *.jfif; *.png";
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string fileName = openFileDialog.FileName;
                    pictureBox4.Load(fileName);
                    pictureBox4.Enabled = true;
                }
            }
        }

        private void pictureBox4_Paint(object sender, PaintEventArgs e)
        {
            // 获取 PictureBox 的画布
            Graphics g = e.Graphics;

            // 遍历 marks 数组
            foreach (JArray mark in marks)
            {
                // 获取标记形状
                string shape = mark[0].ToString();
                // 如果是矩形
                if (shape == "rectangle")
                {
                    // 获取矩形的位置和大小
                    int x = mark[1].ToObject<int>();
                    int y = mark[2].ToObject<int>();
                    int width = mark[3].ToObject<int>();
                    int height = mark[4].ToObject<int>();
                    // 画出矩形
                    g.DrawRectangle(Pens.Red, new Rectangle(x, y, width, height));
                }
                // 如果是多边形
                else if (shape == "polygon")
                {
                    // 获取多边形的顶点坐标
                    float[] listp = new float[mark.Count() - 1];

                    for (int i = 1; i < mark.Count(); i++)
                    {
                        listp[i - 1] = float.Parse(mark[i].ToString());
                    }
                    
                    PointF[] points = new PointF[listp.Length / 2];
                    for (int i = 0; i < listp.Length; i += 2)
                    {
                        points[i / 2] = new PointF((float)listp[i], (float)listp[i + 1]);
                    }
                    // 画出多边形
                    g.DrawPolygon(Pens.Red, points);
                }
            }


            g.DrawRectangle(Pens.Red, rectangle);
            if (PolygonPoints != null)
            {
                if (PolygonPoints.Length < 3)
                {
                    // 遍历 points 数组
                    foreach (PointF point in PolygonPoints)
                    {
                        // 在 PictureBox 中显示点的位置
                        g.FillEllipse(Brushes.Red, point.X - 2, point.Y - 2, 4, 4);
                    }
                }
                // 如果 points 数组中的点的数量大于等于 3
                else
                {
                    // 画出多边形
                    g.DrawPolygon(Pens.Red, PolygonPoints);
                }
            }

        }


        private void button29_Click(object sender, EventArgs e)
        {
            if (isRectangleMarked)
            {
                // 将 isRectangleMarked 标志设置为 true
                isRectangleMarked = false;

                // 更新按钮文本
                button29.Text = "Polygon Mark";
            }
            else
            {
                // 将 isRectangleMarked 标志设置为 true
                isRectangleMarked = true;
                // 更新按钮文本
                button29.Text = "Rectangle Mark";
            }



        }

        private void pictureBox4_MouseClick(object sender, MouseEventArgs e)
        {

            if (isRectangleMarked)
            {
                //Console.WriteLine(clickCount);
                // 增加计数器
                clickCount++;
                // 如果是第一次单击
                if (clickCount == 1)
                {
                    // 记录起点
                    startPoint = e.Location;
                }
                // 如果是第二次单击
                else if (clickCount == 2)
                {
                    // 计算矩形的位置和大小
                    int x = Math.Min(startPoint.X, e.X);
                    int y = Math.Min(startPoint.Y, e.Y);
                    int width = Math.Abs(startPoint.X - e.X);
                    int height = Math.Abs(startPoint.Y - e.Y);
                    // 更新 rectangle 变量
                    rectangle = new System.Drawing.Rectangle(x, y, width, height);
                    // 触发 PictureBox 的重绘事件
                    pictureBox4.Invalidate();
                    // 重置计数器
                    clickCount = 0;
                }
            }
            else
            {

                Point point = new Point(e.X, e.Y);

                // 将鼠标单击的位置添加到 PolygonPoints 数组中
                Array.Resize(ref PolygonPoints, PolygonPoints.Length + 1);
                PolygonPoints[PolygonPoints.Length - 1] = point;
                // 如果 PolygonPoints 数组中的点的数量大于等于 3 个，表示多边形已经被完整标记
                pictureBox4.Invalidate();

            }
        }

        private void pictureBox4_MouseMove(object sender, MouseEventArgs e)
        {
            int originalHeight = this.pictureBox4.Image.Height;

            PropertyInfo rectangleProperty = this.pictureBox4.GetType().GetProperty("ImageRectangle", BindingFlags.Instance | BindingFlags.NonPublic);
            Rectangle picrectangle = (Rectangle)rectangleProperty.GetValue(this.pictureBox4, null);

            int currentWidth = picrectangle.Width;
            int currentHeight = picrectangle.Height;

            double rate = (double)currentHeight / (double)originalHeight;

            int black_left_width = (currentWidth == this.pictureBox4.Width) ? 0 : (this.pictureBox4.Width - currentWidth) / 2;
            int black_top_height = (currentHeight == this.pictureBox4.Height) ? 0 : (this.pictureBox4.Height - currentHeight) / 2;
            int zoom_x = e.X - black_left_width;
            int zoom_y = e.Y - black_top_height;

            int original_x = (int)(zoom_x / rate);
            int original_y = (int)(zoom_y / rate);
            int width_g = 100; // 放大的宽度
            int height_g = 100; // 放大的高度

            Bitmap bmp = new Bitmap(width_g, height_g);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.DrawImage(pictureBox4.Image, new Rectangle(0, 0, width_g*2, height_g*2), new Rectangle(original_x-25, original_y-25, width_g, height_g), GraphicsUnit.Pixel);
                g.FillEllipse(Brushes.Yellow, width_g/2 -2, height_g/2 -2, 4, 4);
            }
            pictureBox5.Image = bmp;
            Point pt = new Point();
            pt.X = Cursor.Position.X - this.Location.X- 55;
            pt.Y = Cursor.Position.Y - this.Location.Y-200;
            pictureBox5.Location = pt;

            if (clickCount == 1)
            {
                // 计算矩形的位置和大小
                int x = Math.Min(startPoint.X, e.X);
                int y = Math.Min(startPoint.Y, e.Y);
                int width = Math.Abs(startPoint.X - e.X);
                int height = Math.Abs(startPoint.Y - e.Y);
                // 更新 rectangle 变量
                rectangle = new Rectangle(x, y, width, height);
                // 触发 PictureBox 的重绘事件
                pictureBox4.Invalidate();
            }
        }

        private void button31_Click(object sender, EventArgs e)
        {
            int originalHeight = this.pictureBox4.Image.Height;
            int originalWidth = this.pictureBox4.Image.Width;

            PropertyInfo rectangleProperty = this.pictureBox4.GetType().GetProperty("ImageRectangle", BindingFlags.Instance | BindingFlags.NonPublic);
            Rectangle picturerectangle = (Rectangle)rectangleProperty.GetValue(this.pictureBox4, null);

            int currentWidth = picturerectangle.Width;
            int currentHeight = picturerectangle.Height;

            float rate = (float)currentHeight / (float)originalHeight;

            int black_left_width = (currentWidth == this.pictureBox4.Width) ? 0 : (this.pictureBox4.Width - currentWidth) / 2;
            int black_top_height = (currentHeight == this.pictureBox4.Height) ? 0 : (this.pictureBox4.Height - currentHeight) / 2;

            
            if (isRectangleMarked)
            {
                int zoom_x = rectangle.X - black_left_width;
                int zoom_y = rectangle.Y - black_top_height;

                float original_x = (float)zoom_x / rate;
                float original_y = (float)zoom_y / rate;
                if (original_x<0)
                {
                    original_x = 0;
                    
                }
                if (original_y < 0)
                {
                    original_y = 0;
                }

                if(original_x > this.pictureBox4.Image.Width )
                {
                    original_x = this.pictureBox4.Image.Width;
                    
                }
                if(original_y >= this.pictureBox4.Image.Height)
                {
                    original_y = this.pictureBox4.Image.Height;
                }
                
                float original_width = rectangle.Width / rate;
                float original_height = rectangle.Height / rate;
                // 将矩形标记信息添加到 JArray 中
                marksForsave.Add(new JArray("rectangle", original_x/ originalWidth, original_y/ originalHeight, original_width/ originalWidth, original_height/ originalHeight));
                marks.Add(new JArray("rectangle", rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height));
                pictureBox4.Invalidate();
                clickCount = 0;
            }
            // 如果当前是多边形标记状态
            else
            {
                if (PolygonPoints==null || PolygonPoints.Length < 3)
                {
                    return;
                }
                JArray pointArray = new JArray("polygon");
                JArray pointArraySave = new JArray("polygon");
                foreach (PointF point in PolygonPoints)
                {
                    pointArray.Add(point.X);
                    pointArray.Add(point.Y);
                    float zoom_x = point.X - black_left_width;
                    float zoom_y = point.Y - black_top_height;

                    float original_x = (float)zoom_x / rate;
                    float original_y = (float)zoom_y / rate;
                    if (original_x < 0)
                    {
                        original_x = 0;

                    }
                    if (original_y < 0)
                    {
                        original_y = 0;
                    }

                    if (original_x > this.pictureBox4.Image.Width)
                    {
                        original_x = this.pictureBox4.Image.Width;

                    }
                    if (original_y > this.pictureBox4.Image.Height)
                    {
                        original_y = this.pictureBox4.Image.Height;
                    }
                    pointArraySave.Add(original_x / originalWidth);
                    pointArraySave.Add(original_y / originalHeight);
                }
                // 将多边形标记信息添加到 JArray 中
                marks.Add(pointArray);
                marksForsave.Add(pointArraySave);
                pictureBox4.Invalidate();
                PolygonPoints = new PointF[0];
            }
        }

        private void button30_Click(object sender, EventArgs e)
        {
            clickCount = 0;
            rectangle = new Rectangle();
            PolygonPoints = new PointF[0];
            pictureBox4.Invalidate();
        }

        private void button32_Click(object sender, EventArgs e)
        {
            JObject output = new JObject { { "COIs", new JObject { { "COINumber", marksForsave.Count.ToString() } } } };

            // 遍历JArray数组，将每个数组元素添加到JSON对象中
            for (int i = 0; i < marksForsave.Count; i++)
            {
                output["COIs"][(i + 1).ToString()] = marksForsave[i];
            }

            // 将JSON对象转换为字符串
            string outputStr = output.ToString();
            SaveFileDialog saveDialog = new SaveFileDialog();

            // 设置文件保存位置（这里设置为桌面）
            saveDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            saveDialog.FileName = "new configuration.json";
            saveDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";

            // 设置默认文件保存类型为JSON文件
            saveDialog.DefaultExt = "json";
            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                // 使用File类的WriteAllText方法将JSON字符串保存到文件中
                File.WriteAllText(saveDialog.FileName, outputStr);
            }

            Console.WriteLine(outputStr);
        }

        private void pictureBox4_MouseEnter(object sender, EventArgs e)
        {
            // 设置 picturebox2 的位置为鼠标的当前位置
            pictureBox5.Enabled = true;
            
            Point pt = new Point();
            pt.X = Cursor.Position.X - this.Location.X;
            pt.Y = Cursor.Position.Y - this.Location.Y;
            pictureBox5.Location = pt;
            // 设置 picturebox2 的可见性为 true
            pictureBox5.Visible = true;

        }

        private void pictureBox4_MouseLeave(object sender, EventArgs e)
        {
            pictureBox5.Visible = false;
            pictureBox5.Enabled = false;
        }

        private void button33_Click(object sender, EventArgs e)
        {
            clickCount = 0;
            rectangle = new Rectangle();
            PolygonPoints = new PointF[0];
            marks = new JArray();
            marksForsave = new JArray();
            pictureBox4.Invalidate();
        }
    }
}
