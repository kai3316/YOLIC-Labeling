using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenCvSharp;
using OpenCvSharp.ML;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
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
        Boolean PolygonMode = false;
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
        Color[] colorslist = new Color[]{Color.FromArgb(0,255,0), Color.FromArgb(138,244,123), Color.FromArgb(100,0,60),
                              Color.FromArgb(87,96,105), Color.FromArgb(220,87,18), Color.FromArgb(230,180,80),
                              Color.FromArgb(255,0,255), Color.FromArgb(10,255,105), Color.FromArgb(255,0,0),
                               Color.FromArgb(50,60,246), Color.FromArgb(243,10,100), Color.FromArgb(153, 163, 112),
                               Color.FromArgb(91, 97, 67), Color.FromArgb(210, 224, 155), Color.FromArgb(222, 237, 164),
                               Color.FromArgb(243,50,100), Color.FromArgb(112, 163, 153),Color.FromArgb(67, 97, 91),
                               Color.FromArgb(155, 224, 210), Color.FromArgb(164, 237, 222)};

        JArray[] COIList;
        
        float[] mImgEmbedding;
        int SAM_w = 0;
        int SAM_h = 0;
        float[] point_coords = null;
        float[] label = null;

        private int clickCount = 0;
        private Point startPoint;
        bool isRectangleMarked = true;
        System.Drawing.Rectangle rectangle;
        JArray marks = new JArray();
        JArray marksForsave = new JArray();
        private PointF[] PolygonPoints = new PointF[0];
        private PointF? highlightedPoint = null;
        int snapThreshold = 10;
        private PointF? temporaryPoint = null;

        
        
        
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
                        //((CheckBox)this.Controls.Find("checkBox" + j, true)[0]).ForeColor = colorslist[i];
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
                        if (coijsonObject["COIs"]["Width"] != null && coijsonObject["COIs"]["Height"] != null)
                        {
                            int img_width = (int)coijsonObject["COIs"]["Width"];
                            int img_height = (int)coijsonObject["COIs"]["Height"];
                            for (int idx = 1; idx < COIList[i - 1].Count; idx++)
                            {
                                if (COIList[i - 1][idx].Value<int>() > 1)
                                {
                                    if (idx % 2 == 1)
                                    {
                                        COIList[i - 1][idx] = COIList[i - 1][idx].Value<double>() / img_width;
                                    }
                                    else
                                    {
                                        COIList[i - 1][idx] = COIList[i - 1][idx].Value<double>() / img_height;
                                    }
                                }
                            }
                        }
                        
                        
                    }
                    //Console.WriteLine(COIList.ToString());

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
                        //((CheckBox)this.Controls.Find("checkBox" + j, true)[0]).ForeColor = colorslist[i];
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
                    list_Img = new List<string>(Directory.GetFiles(openFile_Img.SelectedPath, "*.*", SearchOption.AllDirectories).Where(s => s.EndsWith(".png") || s.EndsWith(".jpg") || s.EndsWith(".jpeg")));
                    label6.Text = list_Img.Count.ToString();
                    button16.Text = "Start";
                    if (list_Img.Count == 0)
                    {
                        MessageBox.Show("No images under folder!", "Notice", MessageBoxButtons.OK);
                        return;
                    }
                    dat.Clear();
                    dat2.Clear();
                    button16.Text = "Start";
                    CurrentIndex = 0;

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
                    list_depthImg = new List<string>(Directory.GetFiles(openFile_DepthImg.SelectedPath, "*.*", SearchOption.AllDirectories).Where(s => s.EndsWith(".png") || s.EndsWith(".jpg") || s.EndsWith(".jpeg")));
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
                    list_Img = new List<string>(Directory.GetFiles(openFile_Img.SelectedPath, "*.*", SearchOption.AllDirectories).Where(s => s.EndsWith(".png") || s.EndsWith(".jpg") || s.EndsWith(".jpeg")));
                    label7.Text = list_Img.Count.ToString();
                    if (list_Img.Count == 0)
                    {
                        MessageBox.Show("No images under this folder!", "Notice", MessageBoxButtons.OK);
                        return;
                    }
                    dat.Clear();
                    dat2.Clear();
                    button7.Text = "Start";
                    CurrentIndex = 0;
                    
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
                    rgb.DrawRectangle(new Pen(Color.LightGreen, 2), (float)COIList[i][1] * pictureBox2.Image.Width, (float)COIList[i][2] * pictureBox2.Image.Height, (float)COIList[i][3] * pictureBox2.Image.Width, (float)COIList[i][4] * pictureBox2.Image.Height);
                    depth.DrawRectangle(new Pen(Color.LightGreen, 2), (float)COIList[i][1] * pictureBox3.Image.Width, (float)COIList[i][2] * pictureBox3.Image.Height, (float)COIList[i][3] * pictureBox3.Image.Width, (float)COIList[i][4] * pictureBox3.Image.Height);
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
                    rgb.DrawPolygon(new Pen(Color.LightGreen, 2), points_rgb);
                    depth.DrawPolygon(new Pen(Color.LightGreen, 2), points_depth);
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
            if (CheckMode == true && auto == 1)
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
            if (COIList==null)
            {
                return;
            }
            if (button7.Text.Equals("Start") == true)
            {
                button3.Enabled = true;
                button4.Enabled = true;
                button28.Enabled = true;
                pictureBox1.Enabled = true;
                currentLabel = new string[COIList.Length * (LabelList.Count + 1)];
                for (int i = 0; i < currentLabel.Length; i++)
                {
                    currentLabel[i] = "0";
                }
                if (PolygonMode == false)
                {
                    label13.Text = "Annotation Mode: SAM";
                    this.Text = "Loading SAM Encoder, please wait...";
                    this.Cursor = Cursors.WaitCursor;
                    Application.DoEvents();
                    LoadSAM_Encode();
                    this.Text = "YOLIC Annotation Tool";
                    this.Cursor = Cursors.Default;

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
        private MaskData SAM_Decode(int orgHei, int orgWid, float[] point_coords, float[] label)
        {
            
            string exePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string decode_model_path = exePath + @"\decoder-quant.onnx";
            if (!File.Exists(decode_model_path))
            {
                MessageBox.Show(decode_model_path + " not exist!");
                return null;
            }
            var options = new SessionOptions();
            InferenceSession mDecoder = new InferenceSession(decode_model_path, options);
            var embedding_tensor = new DenseTensor<float>(mImgEmbedding, new[] { 1, 256, 64, 64 });
            float[] mask = new float[256 * 256];
            for (int i = 0; i < mask.Count(); i++)
            {
                mask[i] = 0;
            }
            var mask_tensor = new DenseTensor<float>(mask, new[] { 1, 1, 256, 256 });

            float[] hasMaskValues = new float[1] { 0 };
            var hasMaskValues_tensor = new DenseTensor<float>(hasMaskValues, new[] { 1 });

            float[] orig_im_size_values = { (float)orgHei, (float)orgWid };
            var orig_im_size_values_tensor = new DenseTensor<float>(orig_im_size_values, new[] { 2 });
            int pointCount = label.Length;
            var point_coords_tensor = new DenseTensor<float>(point_coords, new[] { 1, pointCount, 2 });

            var point_label_tensor = new DenseTensor<float>(label, new[] { 1, pointCount });

            var decode_inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("image_embeddings", embedding_tensor),
                NamedOnnxValue.CreateFromTensor("point_coords", point_coords_tensor),
                NamedOnnxValue.CreateFromTensor("point_labels", point_label_tensor),
                NamedOnnxValue.CreateFromTensor("mask_input", mask_tensor),
                NamedOnnxValue.CreateFromTensor("has_mask_input", hasMaskValues_tensor),
                NamedOnnxValue.CreateFromTensor("orig_im_size", orig_im_size_values_tensor)
            };
            MaskData result = new MaskData();
            var segmask = mDecoder.Run(decode_inputs).ToList();
            result.mMask = segmask[0].AsTensor<float>().ToArray().ToList();
            result.mShape = segmask[0].AsTensor<float>().Dimensions.ToArray();
            result.mIoU = segmask[1].AsTensor<float>().ToList();
            return result;
            
        }
        private void LoadSAM_Encode()
        {

            string exePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string encode_model_path = exePath + @"\encoder-quant.onnx";
            if (!File.Exists(encode_model_path))
            {
                MessageBox.Show(encode_model_path + " not exist!");
                return;
            }
            var options = new SessionOptions();
            InferenceSession mEncoder = new InferenceSession(encode_model_path, options);
            var inputMeta = mEncoder.InputMetadata;

            Mat color_image = Cv2.ImRead(list_Img[CurrentIndex], ImreadModes.Color);
            int orgh = color_image.Height;
            int orgw = color_image.Width;
            Mat resizedImage = new Mat();
            int model_size = 0;
            foreach (var name in inputMeta.Keys)
            {
                Console.WriteLine("Dimension Length: " + inputMeta[name].Dimensions[1]+" "+inputMeta[name].Dimensions[3]+" "+ inputMeta[name].Dimensions[2]);
                if (inputMeta[name].Dimensions[3]!= inputMeta[name].Dimensions[2])
                {
                    MessageBox.Show("Width != Height");
                    return;
                }
                else
                {
                    model_size = inputMeta[name].Dimensions[3];
                }
            }
            float scale = model_size * 1.0f / Math.Max(orgh, orgw);
            float newht = orgh * scale;
            float newwt = orgw * scale;

            int neww = (int)(newwt + 0.5);
            int newh = (int)(newht + 0.5);
            SAM_w = neww;
            SAM_h = newh;
            Cv2.Resize(color_image, resizedImage, new OpenCvSharp.Size(neww, newh));
            Mat floatImage = new Mat();
            resizedImage.ConvertTo(floatImage, MatType.CV_32FC3);

            // 计算均值和标准差
            Scalar mean, stddev;
            Cv2.MeanStdDev(floatImage, out mean, out stddev);

            // 标准化图像
            Mat normalizedImage = new Mat();
            Cv2.Subtract(floatImage, mean, normalizedImage);
            Cv2.Divide(normalizedImage, stddev, normalizedImage);
            float[] img = new float[3 * model_size * model_size];
            for (int i = 0; i < neww; i++)
            {
                for (int j = 0; j < newh; j++)
                {
                    int index = j * model_size + i;
                    img[index] = normalizedImage.At<Vec3f>(j, i)[0];
                    img[model_size * model_size + index] = normalizedImage.At<Vec3f>(j, i)[1];
                    img[2 * model_size * model_size + index] = normalizedImage.At<Vec3f>(j, i)[2];
                }
            }
            Tensor<float> inputdata = new DenseTensor<float>(img, new[] { 1, 3, model_size, model_size });

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("x", inputdata)
            };

            var results = mEncoder.Run(inputs);
            var embedding = results.First().AsTensor<float>().ToArray();
            mImgEmbedding = embedding;
            color_image.Dispose();
            resizedImage.Dispose();
            floatImage.Dispose();
            normalizedImage.Dispose();

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
                    rgb.DrawRectangle(new Pen(Color.Red, 2), (float)COIList[i][1] * pictureBox1.Image.Width, (float)COIList[i][2] * pictureBox1.Image.Height, (float)COIList[i][3] * pictureBox1.Image.Width, (float)COIList[i][4] * pictureBox1.Image.Height);

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
                    rgb.DrawPolygon(new Pen(Color.Red, 2), points);
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
            if (CheckMode == true && auto == 1)
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
                        StringFormat format = new StringFormat();
                        format.Alignment = StringAlignment.Center;
                        format.LineAlignment = StringAlignment.Center;
                        rgb.DrawString(classlabel, new Font("Arial", 8), new SolidBrush(Color.FromArgb(opacity, Color.Yellow)), rect.X + rect.Width / 2, rect.Y + rect.Height / 2, format);
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
                        PointF center = ComputePolygonCentroid(points);
                       
                        StringFormat format = new StringFormat();
                        format.Alignment = StringAlignment.Center;
                        format.LineAlignment = StringAlignment.Center;
                        rgb.DrawString(classlabel, new Font("Arial", 8), new SolidBrush(Color.FromArgb(opacity, Color.Yellow)), center, format);
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
                        StringFormat format = new StringFormat();
                        format.Alignment = StringAlignment.Center;
                        format.LineAlignment = StringAlignment.Center;
                        rgb.DrawString(classlabel, new Font("Arial", 8), new SolidBrush(Color.FromArgb(opacity, Color.Yellow)), rect.X + rect.Width / 2, rect.Y + rect.Height / 2, format);
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
                        PointF center = ComputePolygonCentroid(points);
                        StringFormat format = new StringFormat();
                        format.Alignment = StringAlignment.Center;
                        format.LineAlignment = StringAlignment.Center;
                        rgb.DrawString(classlabel, new Font("Arial", 8), new SolidBrush(Color.FromArgb(opacity, Color.Yellow)), center, format);
                    }
                }

            }
        }

        private PointF ComputePolygonCentroid(PointF[] points)
        {
            float centroidX = 0;
            float centroidY = 0;
            float area = 0;

            for (int i = 0; i < points.Length; i++)
            {
                float wi = points[i].X * points[(i + 1) % points.Length].Y - points[(i + 1) % points.Length].X * points[i].Y;
                area += wi;

                centroidX += (points[i].X + points[(i + 1) % points.Length].X) * wi;
                centroidY += (points[i].Y + points[(i + 1) % points.Length].Y) * wi;
            }

            area *= 0.5f;

            centroidX /= 6 * area;
            centroidY /= 6 * area;

            return new PointF(centroidX, centroidY);
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
            int originalHeight = this.pictureBox1.Image.Height;
            int originalWidth = this.pictureBox1.Image.Width;
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

            if (e.Button == MouseButtons.Left)
            {
                if (PolygonMode == true)
                {
                    
                    dat2.Add(new Point(Convert.ToInt32(original_x), Convert.ToInt32(original_y)));
                    dat.Add(new Point(e.X, e.Y));
                    pictureBox1.Invalidate();
                }
                else
                {
                    
                    double scale_x = (double)SAM_w / (double)originalWidth;
                    double scale_y = (double)SAM_h / (double)originalHeight;

                    double newX = original_x * scale_x;
                    double newY = original_y * scale_y;
                    AddPointCoords(newX, newY);
                    AddLabel();
                    MaskData result = SAM_Decode(originalHeight, originalWidth, point_coords, label);
                    if (result == null) return;
                    ShowMask(result.mMask.ToArray(), Color.FromArgb((byte)100, (byte)0, (byte)0, (byte)139));
                }
                
            }
            if (e.Button == MouseButtons.Right)
            {

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

                    }
                    DisplayRGB(CurrentIndex, 0);
                    RedrawR(pictureBox1.Image);
                }
            }

        }

        private void ShowMask(float[] floats, Color color)
        {
            if (pictureBox1.Image == null)
                return;

            int width = pictureBox1.Image.Width;
            int height = pictureBox1.Image.Height;

            // Create a new bitmap to draw the mask
            using (Bitmap maskBitmap = new Bitmap(width, height))
            {
                using (Graphics g = Graphics.FromImage(maskBitmap))
                {
                    // Draw the original image
                    g.DrawImage(pictureBox1.Image, 0, 0, width, height);

                    // Create a color matrix that sets the alpha to 50%
                    ColorMatrix cm = new ColorMatrix();
                    cm.Matrix33 = 0.5f; // 50% opacity

                    ImageAttributes ia = new ImageAttributes();
                    ia.SetColorMatrix(cm);

                    // Create the mask overlay
                    using (Bitmap overlay = new Bitmap(width, height))
                    {
                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                int index = y * width + x;
                                if (floats[index] > 0.5f) // Adjust threshold as needed
                                {
                                    overlay.SetPixel(x, y, color);
                                }
                            }
                        }

                        // Draw the overlay with transparency
                        g.DrawImage(overlay, new Rectangle(0, 0, width, height), 0, 0, width, height, GraphicsUnit.Pixel, ia);
                    }
                }

                // Display the result in the PictureBox
                if (pictureBox1.Image != null)
                {
                    pictureBox1.Invalidate();
                }
                pictureBox1.Image = new Bitmap(maskBitmap);
                ConvertMaskToPolygonAndMark(floats, width, height);
            }
        }

        private void ConvertMaskToPolygonAndMark(float[] maskData, int width, int height)
        {
            // Convert mask to OpenCV Mat
            Mat mask = new Mat(height, width, MatType.CV_8UC1);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    mask.Set(y, x, maskData[index] > 0.5f ? 255 : 0);
                }
            }
            
            // Find contours
            // Find contours
            OpenCvSharp.Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(mask, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            // Find the largest contour
            if (contours.Length == 0)
            {
                Console.WriteLine("No contours found.");
                return;
            }

            var largestContour = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();

            // Approximate contour to polygon
            OpenCvSharp.Point[] polygon = Cv2.ApproxPolyDP(largestContour, 3, true);

            // Convert OpenCV points to System.Drawing.PointF
            PointF[] polygonF = polygon.Select(p => new PointF((float)p.X / width, (float)p.Y / height)).ToArray();

            // Clear existing dat and dat2
            dat.Clear();
            dat2.Clear();

            // Get image position and scaling information
            int originalHeight = pictureBox1.Image.Height;
            int originalWidth = pictureBox1.Image.Width;
            PropertyInfo rectangleProperty = pictureBox1.GetType().GetProperty("ImageRectangle", BindingFlags.Instance | BindingFlags.NonPublic);
            Rectangle rectangle = (Rectangle)rectangleProperty.GetValue(pictureBox1, null);
            int currentWidth = rectangle.Width;
            int currentHeight = rectangle.Height;
            float rate = (float)currentHeight / (float)originalHeight;
            int black_left_width = (currentWidth == pictureBox1.Width) ? 0 : (pictureBox1.Width - currentWidth) / 2;
            int black_top_height = (currentHeight == pictureBox1.Height) ? 0 : (pictureBox1.Height - currentHeight) / 2;
            // Add points to dat and dat2
            foreach (var point in polygonF)
            {
                float zoom_x = point.X * pictureBox1.Image.Width * rate;
                float zoom_y = point.Y * pictureBox1.Image.Height * rate;
                int dat_x = (int)zoom_x + black_left_width;
                int dat_y = (int)zoom_y + black_top_height;
                dat.Add(new System.Drawing.Point(dat_x, dat_y));
                dat2.Add(new PointF(point.X * pictureBox1.Image.Width, point.Y * pictureBox1.Image.Height));
            }

        }

        private void AddLabel()
        {
            if (label == null)
            {
                label = new float[1];
                label[0] = 1;
                return;
            }
            int newSize = label.Length + 1;

            float[] newArray = new float[newSize];

            Array.Copy(label, newArray, label.Length);

            newArray[newSize - 1] = 1;

            label = newArray;
        }

        private void AddPointCoords(double newX, double newY)
        {
            if (point_coords == null)
            {
                point_coords = new float[2];
                point_coords[0] = (int)newX;
                point_coords[1] = (int)newY;
                return;
            }
            int newSize = point_coords.Length + 2;

            float[] newArray = new float[newSize];

            Array.Copy(point_coords, newArray, point_coords.Length);

            newArray[newSize - 2] = (int)newX;
            newArray[newSize - 1] = (int)newY;

            point_coords = newArray;


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
                    DisplayRGB(CurrentIndex,0);
                    RedrawR(pictureBox1.Image);
                    if (LastArea != -1 && LastArea != LabelArea)
                    {
                        DrawboxRGB(pictureBox1.Image, LastArea, Color.Black);
                    }
                    DrawboxRGB(pictureBox1.Image, LabelArea, Color.Black);
                    LastArea = -1;

                }
            }
            if(e.Button == MouseButtons.Left)
            {
                button27_Click(sender,e);
                
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (checkBox42.Checked == true)
            {
                SaveImage(CurrentIndex);
            }
            CurrentIndex++;
            if (PolygonMode == false)
            {
                LoadSAM_Encode();
                point_coords = null;
                label = null;
            }
            
            dat.Clear();
            dat2.Clear();
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
            if (PolygonMode == false)
            {
                LoadSAM_Encode();
                point_coords = null;
                label = null;
            }
            dat.Clear();
            dat2.Clear();
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
            if (list_Img == null) { return; }
            try
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
            catch (Exception)
            {
                this.BeginInvoke((Action)(() => MessageBox.Show("Failed to delete!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)));
            }
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
            if (PolygonMode == false)
            {
                LoadSAM_Encode();
                point_coords = null;
                label = null;
            }
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
                                    Pen p = new Pen(colorslist[ii], 2);
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
                                    Pen p = new Pen(colorslist[ii], 2);
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
                Display(CurrentIndex, 0);
                Redraw(pictureBox2.Image);
                pictureBox2.Invalidate();
                //for (int ii = 0, j = 21; ii < LabelList.Count; ii++, j++)
                //{
                //    if (((CheckBox)this.Controls.Find("checkBox" + j, true)[0]).Checked)
                //    {
                //        ((CheckBox)this.Controls.Find("checkBox" + j, true)[0]).Checked = false;
                //    }

                //}
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
                    Display(CurrentIndex, 0);
                    Redraw(pictureBox2.Image);


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
                    Display(CurrentIndex, 0);
                    Redraw(pictureBox2.Image);
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
                point_coords = null;
                label = null;
                DisplayRGB(CurrentIndex, 0);
                RedrawR(pictureBox1.Image);
                pictureBox1.Invalidate();
                System.Threading.Thread.Sleep(100);
                //for (int ii = 0, j = 1; ii < LabelList.Count; ii++, j++)
                //{
                //    if (((CheckBox)this.Controls.Find("checkBox" + j, true)[0]).Checked)
                //    {
                //        ((CheckBox)this.Controls.Find("checkBox" + j, true)[0]).Checked = false;
                //    }

                //}

            }
            catch (Exception)
            {
                this.BeginInvoke((Action)(() => MessageBox.Show("Failed to save the polygon! Please use the right click to mark the cell", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)));
            }
        }

        private void button26_Click(object sender, EventArgs e)
        {
            point_coords = null;
            label = null;
            DisplayRGB(CurrentIndex, 0);
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



        

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // if it is a hotkey, return true; otherwise, return false
            switch (keyData)
            {
                case Keys.Right:
                    //焦点定位到控件button_num_0上，即数字0键上
                    if (button3.Enabled)
                    {
                        button3.Focus();
                        //执行按钮点击操作
                        button3.PerformClick();
                        return true;
                    }
                    if (button11.Enabled)
                    {
                        button11.Focus();
                        //执行按钮点击操作
                        button11.PerformClick();
                        return true;
                    }
                    break;
                case Keys.Left:
                    if (button4.Enabled)
                    {
                        button4.Focus();
                        //执行按钮点击操作
                        button4.PerformClick();
                        return true;
                    }
                    if (button12.Enabled)
                    {
                        button12.Focus();
                        //执行按钮点击操作
                        button12.PerformClick();
                        return true;
                    }
                    break;
                //......
                default:
                    break;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void button28_Click(object sender, EventArgs e)
        {
            point_coords = null;
            label = null;
            dat.Clear();
            dat2.Clear();
            
            if (PolygonMode == true)
            {
                PolygonMode = false;
                label13.Text = "Annotation Mode: SAM";
                button27.Text = "Add/Save Mask";
                button26.Text = "Remove Mask";
                LoadSAM_Encode();
            }
            else
            {
                PolygonMode = true;
                label13.Text = "Annotation Mode: Polygon";
                button27.Text = "Add/Save Polygon";
                button26.Text = "Remove Polygon";

            }
        }

        private void button29_Click(object sender, EventArgs e)
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

        private void button30_Click(object sender, EventArgs e)
        {
            clickCount = 0;
            rectangle = new Rectangle();
            PolygonPoints = new PointF[0];
            marks = new JArray();
            marksForsave = new JArray();
            pictureBox4.Invalidate();
        }

        private void button31_Click(object sender, EventArgs e)
        {
            clickCount = 0;
            rectangle = new Rectangle();
            PolygonPoints = new PointF[0];
            pictureBox4.Invalidate();
        }

        private void button32_Click(object sender, EventArgs e)
        {
            if (this.pictureBox4.Image == null)
            {
                return;
            }
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
                if (original_y >= this.pictureBox4.Image.Height)
                {
                    original_y = this.pictureBox4.Image.Height;
                }

                float original_width = rectangle.Width / rate;
                float original_height = rectangle.Height / rate;
                // 将矩形标记信息添加到 JArray 中
                marksForsave.Add(new JArray("rectangle", original_x / originalWidth, original_y / originalHeight, original_width / originalWidth, original_height / originalHeight));
                marks.Add(new JArray("rectangle", rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height));
                pictureBox4.Invalidate();
                clickCount = 0;
            }
            // 如果当前是多边形标记状态
            else
            {
                if (PolygonPoints == null || PolygonPoints.Length < 3)
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

        private void button33_Click(object sender, EventArgs e)
        {
            if (isRectangleMarked)
            {
                // 将 isRectangleMarked 标志设置为 true
                isRectangleMarked = false;

                // 更新按钮文本
                button33.Text = "Polygon mode";
            }
            else
            {
                // 将 isRectangleMarked 标志设置为 true
                isRectangleMarked = true;
                // 更新按钮文本
                button33.Text = "Rectangle mode";
            }
        }

        private void button34_Click(object sender, EventArgs e)
        {
            JObject output = new JObject { { "Labels", new JObject { { "LabelList", new JArray("Class 1", "Class 2") }, { "LabelAbbreviation", new JArray("C1", "C2") }, { "LabelNumber", "2" } } }, { "COIs", new JObject { { "COINumber", marksForsave.Count.ToString() } } } };

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

        private bool IsNear(PointF p1, PointF p2, float threshold)
        {
            // 计算两点之间的距离
            float dist = (float)Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
            // 如果距离小于阈值，则返回 true
            return dist <= threshold;
        }

        private void pictureBox4_MouseClick(object sender, MouseEventArgs e)
        {

            // 吸附阈值，决定了点需要多近才会被吸附到现有顶点

            System.Console.WriteLine("11");
            Point newPoint = new Point(e.X, e.Y);

            // 遍历所有现有的顶点，如果新点离某个顶点很近（小于阈值），则将新点的位置设为该顶点的位置
            foreach (JArray mark in marks)
            {
                string shape = mark[0].ToString();
                if (shape == "rectangle")
                {
                    // 如果是矩形，检查矩形的四个顶点
                    Point topLeft = new Point(mark[1].ToObject<int>(), mark[2].ToObject<int>());
                    Point topRight = new Point(mark[1].ToObject<int>() + mark[3].ToObject<int>(), mark[2].ToObject<int>());
                    Point bottomLeft = new Point(mark[1].ToObject<int>(), mark[2].ToObject<int>() + mark[4].ToObject<int>());
                    Point bottomRight = new Point(mark[1].ToObject<int>() + mark[3].ToObject<int>(), mark[2].ToObject<int>() + mark[4].ToObject<int>());

                    if (IsNear(newPoint, topLeft, snapThreshold))
                        newPoint = topLeft;
                    else if (IsNear(newPoint, topRight, snapThreshold))
                        newPoint = topRight;
                    else if (IsNear(newPoint, bottomLeft, snapThreshold))
                        newPoint = bottomLeft;
                    else if (IsNear(newPoint, bottomRight, snapThreshold))
                        newPoint = bottomRight;
                }
                else if (shape == "polygon")
                {
                    // 如果是多边形，检查多边形的每个顶点
                    for (int i = 1; i < mark.Count(); i += 2)
                    {
                        Point point = new Point(mark[i].ToObject<int>(), mark[i + 1].ToObject<int>());
                        if (IsNear(newPoint, point, snapThreshold))
                        {
                            newPoint = point;
                            break;
                        }
                    }
                }
            }

            if (isRectangleMarked)
            {
                //Console.WriteLine(clickCount);
                // 增加计数器
                clickCount++;
                // 如果是第一次单击
                if (clickCount == 1)
                {
                    // 记录起点
                    startPoint = newPoint;
                }
                // 如果是第二次单击
                else if (clickCount == 2)
                {
                    // 计算矩形的位置和大小
                    int x = Math.Min(startPoint.X, newPoint.X);
                    int y = Math.Min(startPoint.Y, newPoint.Y);
                    int width = Math.Abs(startPoint.X - newPoint.X);
                    int height = Math.Abs(startPoint.Y - newPoint.Y);
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

                Point point = newPoint;

                // 将鼠标单击的位置添加到 PolygonPoints 数组中
                Array.Resize(ref PolygonPoints, PolygonPoints.Length + 1);
                PolygonPoints[PolygonPoints.Length - 1] = point;
                // 如果 PolygonPoints 数组中的点的数量大于等于 3 个，表示多边形已经被完整标记
                pictureBox4.Invalidate();

            }
        }

        private void pictureBox4_MouseEnter(object sender, EventArgs e)
        {
            
        }

        private void pictureBox4_MouseLeave(object sender, EventArgs e)
        {
            
            label14.Text = "X";
            label15.Text = "Y";
        }

        private void pictureBox4_MouseMove(object sender, MouseEventArgs e)
        {


            PointF newPoint = new PointF(e.X, e.Y);

            // 初始设置为 null，表示没有顶点被高亮
            highlightedPoint = null;

            // 和 MouseClick 方法类似，检查鼠标当前位置是否靠近任何顶点
            foreach (JArray mark in marks)
            {
                string shape = mark[0].ToString();
                if (shape == "rectangle")
                {
                    int x = mark[1].ToObject<int>();
                    int y = mark[2].ToObject<int>();
                    int width = mark[3].ToObject<int>();
                    int height = mark[4].ToObject<int>();

                    // Check the four corners of the rectangle
                    if (Math.Abs(newPoint.X - x) < snapThreshold && Math.Abs(newPoint.Y - y) < snapThreshold)
                    {
                        highlightedPoint = new PointF(x, y);
                        if (Cursor.Position != pictureBox4.PointToScreen(new Point(x, y)))
                        {
                            Cursor.Position = pictureBox4.PointToScreen(new Point(x, y));
                        }
                        break;
                    }

                    // 右上角
                    if (Math.Abs(newPoint.X - (x + width)) < snapThreshold && Math.Abs(newPoint.Y - y) < snapThreshold)
                    {
                        highlightedPoint = new PointF(x + width, y);
                        if (Cursor.Position != pictureBox4.PointToScreen(new Point(x + width, y)))
                        {
                            Cursor.Position = pictureBox4.PointToScreen(new Point(x + width, y));
                        }

                        break;
                    }

                    // 左下角
                    if (Math.Abs(newPoint.X - x) < snapThreshold && Math.Abs(newPoint.Y - (y + height)) < snapThreshold)
                    {
                        highlightedPoint = new PointF(x, y + height);
                        if (Cursor.Position != pictureBox4.PointToScreen(new Point(x, y + height)))
                        {
                            Cursor.Position = pictureBox4.PointToScreen(new Point(x, y + height));
                        }
                        break;
                    }

                    // 右下角
                    if (Math.Abs(newPoint.X - (x + width)) < snapThreshold && Math.Abs(newPoint.Y - (y + height)) < snapThreshold)
                    {
                        highlightedPoint = new PointF(x + width, y + height);
                        if (Cursor.Position != pictureBox4.PointToScreen(new Point(x + width, y + height)))
                        {
                            Cursor.Position = pictureBox4.PointToScreen(new Point(x + width, y + height));
                        }

                        break;
                    }
                }
                else if (shape == "polygon")
                {
                    for (int i = 1; i < mark.Count(); i += 2)
                    {
                        int x = mark[i].ToObject<int>();
                        int y = mark[i + 1].ToObject<int>();
                        if (Math.Abs(newPoint.X - x) < snapThreshold && Math.Abs(newPoint.Y - y) < snapThreshold)
                        {
                            highlightedPoint = new PointF(x, y);
                            if (Cursor.Position != pictureBox4.PointToScreen(new Point(x, y)))
                            {
                                Cursor.Position = pictureBox4.PointToScreen(new Point(x, y));
                            }

                            break;
                        }
                    }
                }
            }

            // 因为 highlightedPoint 已经改变，所以需要重新绘制 pictureBox
            pictureBox4.Invalidate();

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
            label14.Text = original_x.ToString();
            label15.Text = original_y.ToString();
            

            
            


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

            if (!isRectangleMarked && PolygonPoints.Length > 0)
            {
                temporaryPoint = e.Location;
                pictureBox4.Invalidate();
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
            if (highlightedPoint.HasValue)
            {
                e.Graphics.FillEllipse(Brushes.Green, highlightedPoint.Value.X - 5, highlightedPoint.Value.Y - 5, 10, 10);
            }


            if (temporaryPoint.HasValue && PolygonPoints.Length > 0)
            {
                List<PointF> temporaryPolygon = new List<PointF>(PolygonPoints);
                temporaryPolygon.Add(temporaryPoint.Value);

                // Draw the temporary polygon
                e.Graphics.DrawPolygon(Pens.Red, temporaryPolygon.ToArray());
            }
        }

       
    }
}
