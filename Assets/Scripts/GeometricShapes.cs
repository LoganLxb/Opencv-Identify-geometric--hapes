using System.Collections;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using UnityEngine;
using UnityEngine.UI;
public class GeometricShapes : MonoBehaviour
{
    public RawImage image;
    /// <summary>
    /// Mat格式存放处理的图片
    /// </summary>
    private Mat _scrMat;
    /// <summary>
    /// Mat格式存放处理的图片
    /// </summary>
    private Mat _dstMat;

    private Texture2D _texture;

    private List<MatOfPoint> _srcContours = new List<MatOfPoint>();

    private Mat _srcHierarchy = new Mat();

    private string _deviceName;

    private WebCamTexture _webCamTexture;
    private void Awake()
    {
        //打开摄像头
        //OpenCamera();
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //导入图片
            ImprotImage();
            LookForOutline();
        }
    }
    private void OpenCamera()
    {
        WebCamDevice[] _device = WebCamTexture.devices;
        _deviceName = _device[0].name;
        //获取图像
        _webCamTexture = new WebCamTexture(_deviceName, 1920, 1080, 50);
            _webCamTexture.Play();
    }

    /// <summary>
    /// 导入图片
    /// </summary>
    private void ImprotImage()
    {
        //image.texture

        Texture2D srcTexture = image.mainTexture as Texture2D;
        _scrMat = new Mat(srcTexture.height, srcTexture.width, CvType.CV_8UC1);
        Utils.texture2DToMat(srcTexture, _scrMat);

        //读取图片
        //_scrMat = Imgcodecs.imread(Application.streamingAssetsPath + "/test.jpeg", 1);

        //_scrMat = new Mat(_webCamTexture.height, _webCamTexture.width, CvType.CV_8UC4);
        //Utils.webCamTextureToMat(_webCamTexture, _scrMat);

        //定义Texture2D设置其宽高随_scrMat材质颜色模式为RGB32
        _texture = new Texture2D(_scrMat.cols(), _scrMat.rows(), TextureFormat.ARGB32, false);
        //对图片进行处理
        ImageDeal();
        //ImageDraw();

    }
    /// <summary>
    /// 图片处理
    /// </summary>
    private void ImageDeal()
    {
        //图片颜色模式转换
        Imgproc.cvtColor(_scrMat, _scrMat, Imgproc.COLOR_BGR2GRAY);
        //图片高斯模糊处理
        Imgproc.GaussianBlur(_scrMat, _scrMat, new Size(5, 5), 0);
        //图片二值化处理
        Imgproc.threshold(_scrMat, _scrMat, 128, 255, Imgproc.THRESH_BINARY);
        //把Mat格式转换成texture格式
        Utils.matToTexture2D(_scrMat, _texture);
    }

    /// <summary>
    /// 寻找轮廓
    /// </summary>
    private void LookForOutline()
    {
        //寻找轮廓
        Imgproc.findContours(_scrMat, _srcContours, _srcHierarchy, Imgproc.RETR_CCOMP, Imgproc.CHAIN_APPROX_NONE);
        for (int i = 0; i < _srcContours.Count; i++)
        {

            //轮廓描边
            Imgproc.drawContours(_scrMat, _srcContours, i, new Scalar(255, 255, 255), 2, 8, _srcHierarchy, 0, new Point());
            Point _tempPoint = new Point();
            float[] _radius = new float[1];
            //获取点集最小外接圆点
            Imgproc.minEnclosingCircle(new MatOfPoint2f(_srcContours[i].toArray()), _tempPoint, _radius);
            //在圆点位置绘制圆形
            Imgproc.circle(_scrMat, _tempPoint, 7, new Scalar(0, 0, 255), -1);
            MatOfPoint2f _newMatOfPoit2f = new MatOfPoint2f(_srcContours[i].toArray());
            string _tempShape = CompareGraph(_srcContours[i], _newMatOfPoit2f);
            Debug.Log(_tempShape);
            Imgproc.putText(_scrMat, _tempShape, new Point(_tempPoint.x - 20, _tempPoint.y - 20), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar(255, 0, 0), 2, Imgproc.LINE_AA, false);
        }

        Texture2D texture = new Texture2D(_scrMat.cols(), _scrMat.rows(), TextureFormat.RGBA32, false);

        Utils.matToTexture2D(_scrMat, texture);

        image.texture = texture;
    }

    /// <summary>
    /// 比较图形
    /// </summary>
    private string CompareGraph(MatOfPoint _matOfPoint, MatOfPoint2f _mp2f)
    {
        string _shape = "unidentified";
        double _peri;
        //主要是计算图像轮廓的周长
        _peri = Imgproc.arcLength(_mp2f, true);
        //对图像轮廓点进行多变形拟合
        MatOfPoint2f _polyShape = new MatOfPoint2f();
        Imgproc.approxPolyDP(_mp2f, _polyShape, 0.04f * _peri, true);
        int _shapeLen = _polyShape.toArray().Length;


        //根据轮廓凸点拟合结果，判断属于哪个形状
        switch (_shapeLen)
        {
            case 3:
                _shape = "triangle";
                break;
            case 4:
                OpenCVForUnity.CoreModule.Rect _rect = Imgproc.boundingRect(_matOfPoint);
                float _width = _rect.width;
                float _height = _rect.height;
                float _ar = _width / _height;
                //计算宽高比，判断是矩形还是正方形
                if (_ar >= 0.95f && _ar <= 1.05f)
                {
                    _shape = "square";
                }
                else
                {
                    _shape = "rectangle";
                }
                break;
            case 5:
                _shape = "pentagon";
                break;
            default:
                _shape = "circle";
                break;
        }
        return _shape;
    }
}
