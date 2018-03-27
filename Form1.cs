using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.ML;
using Emgu.CV.Structure;
using System.Runtime.InteropServices;
using Emgu.CV.Util;
using System.Diagnostics;
using Emgu.CV.CvEnum;
using ThridLibray;//使用第三方库调用摄像机
using System.IO;

namespace WindowsFormsApp4
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
		}
	     public void Sort(int[,] Array,int count)
		 {
			 for (int i = 0; i < count; i++)
			 {
				 for (int j = i + 1; j < count; j++)
				 {
					 if (Array[1, i] > Array[1, j])
					 {
						int x = Array[0, i];
						int y = Array[1, i];
						Array[0, i] = Array[0, j];
						Array[1, i] = Array[1, j];
						Array[0, j] = x;
						Array[1, j] = y;
					 }
				 }
			 }
		 }
		public void Sort(double[,] Array, int count)
		{
			for (int i = 0; i < count; i++)
			{
				for (int j = i + 1; j < count; j++)
				{
					if (Array[1, i] > Array[1, j])
					{
						double x = Array[0, i];
						double y = Array[1, i];
						Array[0, i] = Array[0, j];
						Array[1, i] = Array[1, j];
						Array[0, j] = x;
						Array[1, j] = y;
					}
				}
			}
		}
		//函数功能：当轮廓上出现多个坐标的Y坐标相等时，取平均值
		public int AverageValue(int[,] Inputarray, double[,] Outputarray, int number)
		{
			int contourinit = Inputarray[1, 0];
			double sum = 0;
			int cnt = 0;
			int centercontourindex = 0;
			for (int i = 0; i < number; i++)
			{
				if ((i + 1) == number || Inputarray[1, i + 1] != contourinit)
				{
					Outputarray[0, centercontourindex] = sum / cnt;
					Outputarray[1, centercontourindex] = Inputarray[1, i];
					centercontourindex++;
					cnt = 0; sum = 0;
					if (i + 1 != number)
					{
						contourinit = Inputarray[1, i + 1];
					}
				}
				else
				{
					sum += Inputarray[0, i];
					cnt++;
				}
			}
			return centercontourindex;

		}
		Image<Bgr,byte> _gimage_Source1,_gimage_Source2,_gimage_Source3;//全局变量
		int LENGTH = 1500;//给定基准物体距离摄像机的水平距离为1500个像素点长度
	    //计算中间值距离图像中心距离distancefromcenter
		double[] _gdistancefromcenter = new double[960];
		double[] _galpha_d = new double[960];//图片1测试过后的参考系数供图片2使用
		private void button1_Click_1(object sender, EventArgs e)
		{
			//OpenFileDialog Openfile = new OpenFileDialog();
			//if (Openfile.ShowDialog() == DialogResult.OK)
			//{
				//Image<Bgr, byte> Src_Image = new Image<Bgr, byte>(Openfile.FileName);
				Image<Bgr,byte> Src_Image=_gimage_Source2;
				pictureBox1.Image = Src_Image.ToBitmap(); // 1、pictureBox1 显示原图
				VectorOfMat vm = new VectorOfMat();
				//测试时间：原图加载过后经过一系列处理最终显示在窗口上的时间
				//Stopwatch sw = new Stopwatch();
				//sw.Start();
			    CvInvoke.Split(Src_Image, vm);//分割图像B,G,R 
			    var vms = vm.GetOutputArray();
				Mat one_channel1 = vms.GetMat(0);
				Mat one_channel2 = vms.GetMat(1);
				Mat one_channel3 = vms.GetMat(2);
				//R-B
				Image<Bgr, byte> sub_channel = (one_channel3.ToImage<Bgr, byte>()).Sub(one_channel1.ToImage<Bgr, byte>());
				//高斯滤波
				CvInvoke.GaussianBlur(sub_channel, sub_channel, new System.Drawing.Size(9, 9), 0, 0);
				//Otsu二值化
				Image<Gray, byte> gray_image = sub_channel.Convert<Gray, byte>();
			    var gthreshImage = gray_image.CopyBlank();
				//第一个参数必须是单通道灰度图，第二个参数是单通道黑白图
				CvInvoke.Threshold(gray_image, gthreshImage, 0, 255, Emgu.CV.CvEnum.ThresholdType.Otsu);
				//矩形腐蚀
				Mat element = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Rectangle, new Size(21, 1), new Point(-1, -1));
				CvInvoke.Erode(gthreshImage, gthreshImage, element, new Point(-1, -1), 1, Emgu.CV.CvEnum.BorderType.Default, new Emgu.CV.Structure.MCvScalar(0));
				//sw.Stop();
				//Console.WriteLine("花费时间：" + sw.ElapsedMilliseconds + "ms");//毫秒
				//对二值化图像，对不为0的区域进行检测
				VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
				Mat b1 = new Mat();
				Image<Gray, byte> res = new Image<Gray, byte>(gthreshImage.Width, gthreshImage.Height, new Gray(0));
				Image<Bgr, byte> disp = new Image<Bgr, byte>(gthreshImage.Width, gthreshImage.Height);
				Image<Bgr, byte> edges = new Image<Bgr, byte>(gthreshImage.Width, gthreshImage.Height);
				CvInvoke.FindContours(gthreshImage, contours, b1, Emgu.CV.CvEnum.RetrType.Ccomp, ChainApproxMethod.ChainApproxNone);
				//方法一：
				for (int i = 0; i < contours.Size; i++)
				{
					CvInvoke.DrawContours(disp, contours, i, new MCvScalar(255,255,255), 1);//白色轮廓
				}
				//方法二：
				for (int i = 0; i < contours.Size; i++)
				{
					for (int j = 0; j < contours[i].Size; j++)
					{
						res.Data[contours[i][j].Y, contours[i][j].X, 0] = 255;
					}
				}
				pictureBox2.Image = disp.ToBitmap();
				//描述矩形边界框，返回一个包围轮廓的CvRect,CvRect只能表示一个方正的长方形
				//此功能暂时不用
				Image<Bgr, byte> draw = disp.CopyBlank();
				for (int i = 0; i < contours.Size; i++)
				{
					using (VectorOfPoint contour = contours[i])
					{
						Rectangle BoundingBox = CvInvoke.BoundingRectangle(contour);
						CvInvoke.Rectangle(draw, BoundingBox, new MCvScalar(255, 0, 255, 255), 3);
					}
				}
				//获取轮廓所有坐标值
				int Contours_Sizeof = 2000;//轮廓坐标点总数
				int[,] contourArray = new int[2, Contours_Sizeof];
				int count = 0;//计算坐标点总数
				int[] assign = new int[Contours_Sizeof];//存储每个纵坐标值出现的次数
				double[,] centercontourArray = new double[2, Contours_Sizeof];//两个轮廓点中心坐标点数组
				//Console.WriteLine("size：" + contours.Size);
				for ( int i = 0 ;i  < contours.Size ; i++)
				{
					for (int j = 0; j < contours[i].Size; j++)
					{
						contourArray[0, j] = contours[i][j].Y;//短
						contourArray[1, j] = contours[i][j].X;//长
						++count;
						//Console.WriteLine(contourArray[0, j] + "," + contourArray[1, j]);							
					}
				}
				Console.WriteLine("*********测试图片1数据********");
				Console.WriteLine("轮廓总数 : "+contours.Size);
				Console.WriteLine("坐标点总数 : " + count);
				//对轮廓坐标进行排序
				Sort(contourArray,count);			
				//对排序后的数组进行中值计算
				AverageValue(contourArray,centercontourArray,count);
				//数组_galpha_d存放系数
				for(int i=0;i<960;i++)
				{
					_gdistancefromcenter[i] = 300 - centercontourArray[0, i];
					_galpha_d[i] = _gdistancefromcenter[i] / LENGTH;
					Console.WriteLine("第 "+ i+  "个轮廓点距离图像中心距离为 : "+_gdistancefromcenter[i]+"     "+ "参考系数 : " + _galpha_d[i]);
				}
				//sw.Stop();
				//Console.WriteLine("加载图片过后程序总共花费时间：" + sw.ElapsedMilliseconds + "ms");//毫秒				
				/*******************************测试时间结束Stop****************************************************/
			//}
		}
		private void button2_Click(object sender, EventArgs e)
		{
			//OpenFileDialog Openfile = new OpenFileDialog();
			//if (Openfile.ShowDialog() == DialogResult.OK)
			//{
				//Image<Bgr, byte> Src_Image = new Image<Bgr, byte>(Openfile.FileName);
				Image<Bgr,byte> Src_Image=_gimage_Source3;
				pictureBox3.Image = Src_Image.ToBitmap(); // 1、pictureBox3 显示原图
				VectorOfMat vm = new VectorOfMat();
				CvInvoke.Split(Src_Image, vm);//分割图像B,G,R 
				var vms = vm.GetOutputArray();
				Mat one_channel1 = vms.GetMat(0);
				Mat one_channel2 = vms.GetMat(1);
				Mat one_channel3 = vms.GetMat(2);
				//R-B
				Image<Bgr, byte> sub_channel = (one_channel3.ToImage<Bgr, byte>()).Sub(one_channel1.ToImage<Bgr, byte>());
				//高斯滤波
				CvInvoke.GaussianBlur(sub_channel, sub_channel, new System.Drawing.Size(9, 9), 0, 0);
				//Otsu二值化
				Image<Gray, byte> gray_image = sub_channel.Convert<Gray, byte>();
				var gthreshImage = gray_image.CopyBlank();
				//第一个参数必须是单通道灰度图，第二个参数是单通道黑白图
				CvInvoke.Threshold(gray_image, gthreshImage, 0, 255, Emgu.CV.CvEnum.ThresholdType.Otsu);
				//矩形腐蚀
				Mat element = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Rectangle, new Size(21, 1), new Point(-1, -1));
				CvInvoke.Erode(gthreshImage, gthreshImage, element, new Point(-1, -1), 1, Emgu.CV.CvEnum.BorderType.Default, new Emgu.CV.Structure.MCvScalar(0));
				//对二值化图像，对不为0的区域进行检测
				VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
				Mat b1 = new Mat();
				Image<Gray, byte> res = new Image<Gray, byte>(gthreshImage.Width, gthreshImage.Height, new Gray(0));
				Image<Bgr, byte> disp = new Image<Bgr, byte>(gthreshImage.Width, gthreshImage.Height);
				Image<Bgr, byte> edges = new Image<Bgr, byte>(gthreshImage.Width, gthreshImage.Height);
				CvInvoke.FindContours(gthreshImage, contours, b1, Emgu.CV.CvEnum.RetrType.Ccomp, ChainApproxMethod.ChainApproxNone);
				//画轮廓：
				for (int i = 0; i < contours.Size; i++)
				{
					CvInvoke.DrawContours(disp, contours, i, new MCvScalar(255, 255, 255), 1);//白色轮廓
				}
				pictureBox4.Image = disp.ToBitmap();
				//获取轮廓所有坐标值
				int Contours_Sizeof = 2000;//轮廓点总数
				int[,] contourArray = new int[2, Contours_Sizeof];
				int count = 0;//计算坐标点总数
				int[] assign = new int[Contours_Sizeof];//存储每个纵坐标值出现的次数
				double[,] centercontourArray = new double[2, Contours_Sizeof];
				int kk = 0;
				for (int i = 0; i < contours.Size; i++)
				{
					for (int j = 0; j < contours[i].Size; j++)
					{
						contourArray[0, kk] = contours[i][j].Y;//短
						contourArray[1, kk] = contours[i][j].X;//长
						++count;
						kk++;
					}
				}
				Console.WriteLine("*********测试图片2数据********");
				Console.WriteLine("坐标点总数" + count);
				//对轮廓坐标进行排序
				Sort(contourArray,count);
				//对数组进行差值，补全断层处缺失的Y值
				int index = AverageValue(contourArray, centercontourArray, count);
				int centercontourSize = index;
				for (int i=0;i < centercontourSize - 1; i++)
				{
					if((centercontourArray[1,i]+1)!=centercontourArray[1,i+1])
					{
						for(int j=(int)centercontourArray[1,i]+1;j<centercontourArray[1,i+1];j++)
						{
							centercontourArray[1, index] = j;//插值在数组末尾
							centercontourArray[0, index] = 10000000;//将断层处的X值设为一个极大值，方便判断
							index++;
						}
					}
				}
				centercontourSize = index;
				//对插值过后的数组进行重新排序
				Sort(centercontourArray,centercontourSize);
				double[] distancefromcenter = new double[960];
				//数组alpha_d存放系数
				double[] length_d = new double[960];//存放图像中每个点距离摄像机的水平距离
				double[] pic1subpic2distance = new double[960];//两个图像的水平距离差值
			    int JS = 0;
				for (int i = 0; i < 960; i++)
				{
					distancefromcenter[i] = 300 - centercontourArray[0, i];
				    length_d[i] = (distancefromcenter[i]*LENGTH) / _gdistancefromcenter[i];//先乘后除，提升精度
				    Console.WriteLine("第 " + i + "对轮廓点中心距离图像中心距离为 : "+distancefromcenter[i]+ "     "+"根据参考系统得出图片距离摄像机水平距离为 : " + length_d[i]);
					pic1subpic2distance[i] = LENGTH > length_d[i] ? (LENGTH-length_d[i]) : (length_d[i]-LENGTH);
				}
				for(int i=0;i<960;i++)
				{
				   if (pic1subpic2distance[i] > 1000000)
				   {
					   Console.WriteLine(@"出现断层\________________________/此处有凹陷！！！");
				   }
				   else
				   {
					   Console.WriteLine("两幅图像距离摄像机水平距离差值为 : " + pic1subpic2distance[i]);
				   }
				}
			//}
		}
		private void pictureBox1_Click(object sender, EventArgs e)
		{

		}
		// 使用EmguCV的双三次插值算法缩放图像。
		//从摄像机读取出的图片大小为1920x1200，需要将图片缩小为960x600
		public static Image<Bgr,byte> ResizeUsingEmguCV(Image<Bgr,byte> original, int newWidth, int newHeight)
		{
			try
			{
				Image<Bgr, byte> image =original;
				Image<Bgr, byte> newImage = image.Resize(newWidth, newHeight,Inter.Cubic);
				return newImage;
			}
			catch
			{
				return null;
			}
		}
		//打开大华摄像机读取视频流数据
		private ThridLibray.IDevice CameraOne;
		private void Form1_Load(object sender, EventArgs e)
		{
			this.button3.Tag = false;
		}
		private void button3_Click(object sender, EventArgs e)
		{
			List<IDeviceInfo> Devices = Enumerator.EnumerateDevices();
			if(Devices.Count>0)
			{
				if((bool)this.button3.Tag)
				{
					CloseCamera();
				}
				else
				{
					InitCamera();//初始化相机1
				}
			}
		}
		private void InitCamera()
		{
			CameraOne = Enumerator.GetDeviceByIndex(0);
			if (CameraOne == null) { MessageBox.Show(0 + "号相机未工作", "温馨提示", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
			CameraOne.CameraOpened += OnCameraOpen;         //打开相机回调函数
			CameraOne.ConnectionLost += OnConnectLoss;      //相机丢失回调函数
			CameraOne.CameraClosed += OnCameraClose;        //关闭相机回调函数
			if (!CameraOne.Open())
			{
				MessageBox.Show(@"相机连接失败");
				return;
			}

			this.CameraSet(CameraOne);
			CameraOne.StreamGrabber.ImageGrabbed += OnImageGrabbedOne;
			if (!CameraOne.GrabUsingGrabLoopThread())
			{
				MessageBox.Show("相机：0" + @"开启码流失败");
				return;
			}
			CameraOne.TriggerSet.Close();   //AED
		}
		private void CameraSet(IDevice deviceProp)
		{
			//int camera = (int)cameraConfig.CameraType;

			if (!deviceProp.IsOpen)
			{
				// MessageBox.Show(@"连接相机失败");
				return;
			}

			using (IFloatParameter Exposuretime = deviceProp.ParameterCollection[new FloatName("ExposureTime")])
			{
				Exposuretime.SetValue(0);//曝光时间
			}

			using (IFloatParameter FrameRate = deviceProp.ParameterCollection[new FloatName("AcquisitionFrameRate")])
			{
				FrameRate.SetValue(5);//帧率(最高60帧)
			}
			using (IBooleanParameter FrameRateEnable = deviceProp.ParameterCollection[new BooleanName("AcquisitionFrameRateEnable")])
			{
				FrameRateEnable.SetValue(true);//是否使用设置帧率
			}

			using (IIntegraParameter Height = deviceProp.ParameterCollection[new IntegerName("Height")])
			{

				Height.SetValue(1200);//图像高
			}
			using (IIntegraParameter Width = deviceProp.ParameterCollection[new IntegerName("Width")])
			{
				Width.SetValue(1920);//图像宽
			}
			using (IIntegraParameter OffsetX = deviceProp.ParameterCollection[new IntegerName("OffsetX")])
			{
				OffsetX.SetValue(0);//X轴偏移
			}
			using (IIntegraParameter OffsetY = deviceProp.ParameterCollection[new IntegerName("OffsetY")])
			{
				OffsetY.SetValue(0);//Y轴偏移
			}
			using (IEnumParameter ImageFormat = deviceProp.ParameterCollection[ParametrizeNameSet.ImagePixelFormat])
			{
				ImageFormat.SetValue("color");//Mono8 Mono10 Mono16图像格式
			}
		}
		private void OnCameraOpen(object sender, EventArgs e)
		{
			//connectCamera = true;
			IDevice device = (IDevice)sender;
			this.Invoke(new Action(() =>
			{
				this.button3.Text = "停止";
				this.button3.Tag = true;
			}));
		}
		#region 相机关闭回调
		// 相机关闭回调
		private void OnCameraClose(object sender, EventArgs e)
		{
			IDevice device = (IDevice)sender;
			this.Invoke(new Action(() =>
			{
				this.button3.Text = "停止";
				this.button3.Tag = false;
			}));
		}
		#endregion

		#region 相机丢失回调
		/// <summary>
		/// 相机丢失回调
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnConnectLoss(object sender, EventArgs e)
		{
			IDevice device = (IDevice)sender;
			if (device == CameraOne)
			{
				CameraOne.ShutdownGrab();
				CameraOne.Dispose();
				CameraOne = null;
				//MessageBox.Show("相机1丢失","温馨提示",MessageBoxButtons.OK,MessageBoxIcon.Information);
			}
		}
		#endregion

		#region 相机1显示图像回调函数
		/// <summary>
		/// 相机1显示图像回调函数
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnImageGrabbedOne(Object sender, GrabbedEventArgs e)
		{
			try
			{
				if (!(bool)this.button3.Tag) return;
				var bitmap = e.GrabResult.ToBitmap(false);
				_gimage_Source1 = new Image<Bgr, byte>(bitmap);
				// 显示图片数据
				if (InvokeRequired)
				{
					//Invoke(new MethodInvoker(() =>
					BeginInvoke(new MethodInvoker(() =>
					{
						try
						{
							if (this.pictureBox1.Image != null)
							{
								this.pictureBox1.Image.Dispose();
							}
							//将大华摄像机读取的图片缩小为960x600
							_gimage_Source2= ResizeUsingEmguCV(_gimage_Source1, 960, 600);
							this.pictureBox1.Image = _gimage_Source2.ToBitmap();
						}
						catch (Exception exception)
						{

							Catcher.Show(exception);
						}
					}));
				}

				bitmap.Dispose();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}

		private void button5_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void button4_Click(object sender, EventArgs e)
		{
			_gimage_Source3 = _gimage_Source2;
			pictureBox3.Image = _gimage_Source3.ToBitmap();
		}
		#endregion

		#region 关闭相机
		/// <summary>
		/// 关闭相机
		/// </summary>
		private void CloseCamera()
		{
			try
			{
				this.button3.Tag = false;
				Thread.Sleep(1000);
				if (CameraOne != null)
				{
					CameraOne.CameraOpened -= OnCameraOpen;       //打开相机回调函数
					CameraOne.ConnectionLost -= OnConnectLoss;
					CameraOne.CameraClosed -= OnCameraClose;        //关闭相机回调函数
					CameraOne.StreamGrabber.ImageGrabbed -= OnImageGrabbedOne;
					CameraOne.ShutdownGrab();
					CameraOne.Close();
					pictureBox1.Refresh();
				}
				if (CameraOne == null)
				{
					MessageBox.Show("0号相机Device is invalid", "温馨提示", MessageBoxButtons.OK, MessageBoxIcon.Information);

				}
			}
			catch (Exception exception)
			{
				Catcher.Show(exception);
			}


		}

		#endregion
	}
}
