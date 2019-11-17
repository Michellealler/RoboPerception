﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;//用于优化绘制的结果
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.ML.OnnxRuntime;
using System.Numerics.Tensors;
using System.Media;
using System.IO;

namespace MNIST
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            

            

        }

        private Bitmap digitImage;//用来保存手写数字
        private Point startPoint;//用于绘制线段，作为线段的初始端点坐标
        //private Mnist model;//用于识别手写数字
        private const int MnistImageSize = 28;//Mnist模型所需的输入图片大小



        private void Form1_Load(object sender, EventArgs e)
        {

            skinEngine1.SkinFile = @"C:\Users\87059\source\repos\MNIST\MNIST\bin\Debug\Skins\DeepCyan.ssk";
            digitImage = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            Graphics g = Graphics.FromImage(digitImage);
            g.Clear(Color.White);
            pictureBox1.Image = digitImage;
           

        }
       
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            //当鼠标左键被按下时，记录下需要绘制的线段的起始坐标
            startPoint = (e.Button == MouseButtons.Left) ? e.Location : startPoint;
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            //当鼠标在移动，且当前处于绘制状态时，根据鼠标的实时位置与记录的起始坐标绘制线段，同时更新需要绘制的线段的起始坐标
            if (e.Button == MouseButtons.Left)
            {
                Graphics g = Graphics.FromImage(digitImage);
                Pen myPen = new Pen(Color.Black, 40);
                myPen.StartCap = LineCap.Round;
                myPen.EndCap = LineCap.Round;
                g.DrawLine(myPen, startPoint, e.Location);
                pictureBox1.Image = digitImage;
                g.Dispose();
                startPoint = e.Location;
            }
        }

        public class ButtonX : Button
        {


            protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
            {

                base.OnPaint(e);
                System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
                path.AddEllipse(0, 0, this.Width, this.Height);
                this.Region = new Region(path);

            }

            protected override void OnMouseEnter(EventArgs e)
            {
                Graphics g = this.CreateGraphics();
                g.DrawEllipse(new Pen(Color.Blue), 0, 0, this.Width, this.Height);
                g.Dispose();
            }

        }


        private void button1_Click(object sender, EventArgs e)
        {

            //当点击清除时，重新绘制一个白色方框，同时清除label1显示的文本
            digitImage = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            Graphics g = Graphics.FromImage(digitImage);
            g.Clear(Color.White);
            pictureBox1.Image = digitImage;
            label1.Text = "";
            this.Opacity = 0.5;

        }
        private void button2_Click(object sender, EventArgs e)
        {
            System.Media.SoundPlayer player = new System.Media.SoundPlayer();
            player.SoundLocation = @"C:\Users\87059\source\repos\MNIST\MNIST\DJ Blyatman,Russian Village Boys - Cyka Blyat.wav";
            player.Load();
            player.Play();

            
        }
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            //当鼠标左键释放时
            //开始处理图片进行推理
            if (e.Button == MouseButtons.Left)
            {
                Bitmap digitTmp = (Bitmap)digitImage.Clone();//复制digitImage
                                                             //调整图片大小为Mnist模型可接收的大小：28×28
                using (Graphics g = Graphics.FromImage(digitTmp))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(digitTmp, 0, 0, MnistImageSize, MnistImageSize);
                }

                //将图片转为灰阶图，并将图片的像素信息保存在list中
                float[] imageArray = new float[MnistImageSize * MnistImageSize];
                for (int y = 0; y < MnistImageSize; y++)
                {
                    for (int x = 0; x < MnistImageSize; x++)
                    {
                        var color = digitTmp.GetPixel(x, y);
                        var a = (float)(0.5 - (color.R + color.G + color.B) / (3.0 * 255));

                        imageArray[y * MnistImageSize + x] = a;

                    }
                }

                // 设置要加载的模型的路径，跟据需要改为你的模型名称
                string modelPath = AppDomain.CurrentDomain.BaseDirectory + "mnist.onnx";

                using (var session = new InferenceSession(modelPath))
                {                    var inputMeta = session.InputMetadata;
                    var container = new List<NamedOnnxValue>();


                    // 用Netron看到需要的输入类型是float32[1, 1, 28, 28]
                    // 第一维None表示可以传入多张图片进行推理
                    // 这里只使用一张图片，所以使用的输入数据尺寸为[1, 1, 28, 28]
                    var shape = new int[] { 1, 1, MnistImageSize, MnistImageSize };
                    var tensor = new DenseTensor<float>(imageArray, shape);

                    // 支持多个输入，对于mnist模型，只需要一个输入，输入的名称是input3
                    container.Add(NamedOnnxValue.CreateFromTensor<float>("Input3", tensor));

                    // 推理
                    var results = session.Run(container);

                    // 输出结果: Plus214_Output_0
                    IList<float> imageList = results.FirstOrDefault(item => item.Name == "Plus214_Output_0").AsTensor<float>().ToList();

                    // Query to check for highest probability digit
                    var maxIndex = imageList.IndexOf(imageList.Max());

                    // Display the results
                    label1.Text = maxIndex.ToString();


                }



            }

        }
        private void skinEngine1_CurrentSkinChanged(object sender, Sunisoft.IrisSkin.SkinChangedEventArgs e)
        {
            
        }
        //外部定义一个累加器
        int i = 0;

        /// <summary>
        /// 点击更换皮肤
        /// </summary>
        private void btn_ChangeSkin_Click(object sender, EventArgs e)
        {
            //读取所有的皮肤文件
            //获取皮肤文件夹中所有皮肤文件的全路径，存储到SkinPath数组中
            //引入命名空间：using system.IO;
            //Directory.GetFiles:用于获取，文件夹下，所有文件的全路径。
            string[] SkinPath = Directory.GetFiles(@"C:\Users\87059\source\repos\MNIST\MNIST\bin\Debug\Skins");

            //每点击一次，i加一
            i++;

            //当i == 最后一个皮肤文件时候，重新设置i= 0，让其返回到第一个皮肤文件
            if (i == SkinPath.Length)
            {
                i = 0;
            }

            //把文件路径赋给，skinFile，使用皮肤。
            skinEngine1.SkinFile = SkinPath[i];
        }
    }
}