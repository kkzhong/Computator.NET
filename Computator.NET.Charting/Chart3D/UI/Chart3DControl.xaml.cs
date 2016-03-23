﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Computator.NET.Charting.Chart3D.Splines;
using Computator.NET.DataTypes;

namespace Computator.NET.Charting.Chart3D
{
    /// <summary>
    ///     Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class Chart3DControl : UserControl, IChart
    {
        private readonly List<Function> functions;
        private Color _axesColor;
        private double _dotSize;
        private bool _equalAxes;
        private double _scale;

        private bool _visibilityAxes;
        private AxisLabels axisLabels;
        private Chart3D m_3dChart; // data for 3d chart
        public int m_nChartModelIndex = -1; // model index in the Viewport3d
        public TransformMatrix m_transformMatrix = new TransformMatrix();
        private Chart3DMode mode;
        private double N;
        private double xmax;
        private double xmin;
        private double ymax;
        private double ymin;

        public Chart3DControl()
        {
            InitializeComponent();

            axisLabels = new AxisLabels(canvasOn3D);
            functions = new List<Function>();

            _visibilityAxes = _equalAxes = true;
            Focusable = true;
            _axesColor = Colors.MediumSlateBlue;

            _scale = 0.5;
            _dotSize = 0.02;
            xmax = ymax = 5;
            xmin = ymin = -5;
            N = 100;

            Mode = Chart3DMode.Surface;

            MouseDown += OnViewportMouseDown;
            MouseMove += OnViewportMouseMove;
            MouseUp += OnViewportMouseUp;
            // this.MouseEnter += new MouseEventHandler(Chart3DControl_MouseEnter);
            MouseWheel += OnViewportMouseWheel;
            KeyDown += OnKeyDown;
        }

        public double Scale
        {
            get { return _scale; }
            set
            {
                _scale = value;
                RescaleProjectionMatrix();
            }
        }

        ///
        public double DotSize
        {
            get { return _dotSize*0.1*0.5*(Math.Abs(xmax - xmin) + Math.Abs(ymax - ymin)); }
            set
            {
                _dotSize = value;
                ReloadPoints();
            }
        }

        ///
        public Color AxesColor
        {
            get { return _axesColor; }
            set
            {
                _axesColor = value;
                ReloadPoints();
            }
        }

        public bool EqualAxes
        {
            get { return _equalAxes; }
            set
            {
                _equalAxes = value;
                RescaleProjectionMatrix();
            }
        }

        ///
        public bool VisibilityAxes
        {
            get { return _visibilityAxes; }
            set
            {
                _visibilityAxes = value;
                if (m_3dChart != null) m_3dChart.UseAxes = value;
                ReloadPoints();
            }
        }

        public AxisLabels AxisLabels
        {
            get { return axisLabels; }
            set
            {
                axisLabels = value;
                TransformChart();
            }
        }

        public double XMin
        {
            get { return xmin; }
            set
            {
                if (value != xmin)
                {
                    xmin = value;
                    Redraw();
                }
            }
        }



        public double XMax
        {
            get { return xmax; }
            set
            {
                if (value != xmax)
                {
                    xmax = value;
                    Redraw();
                }
            }
        }

        public double YMin
        {
            get { return ymin; }
            set
            {
                if (value != ymin)
                {
                    ymin = value;
                    Redraw();
                }
            }
        }

        public double YMax
        {
            get { return ymax; }
            set
            {
                if (value != ymax)
                {
                    ymax = value;
                    Redraw();
                }
            }
        }

        public double Quality
        {
            set
            {
                if (value <= 100.0 && value >= 0.0)
                {
                    calculateN(value);
                    Redraw();
                }
            }
        }

        public Chart3DMode Mode
        {
            get { return mode; }
            set
            {
                if (value != mode)
                {
                    mode = value;
                    if (value == Chart3DMode.Points)
                        m_3dChart = new ScatterChart3D();//TestScatterPlot(1);
                    else if (value == Chart3DMode.Surface)
                        m_3dChart = new UniformSurfaceChart3D();//TestSurfacePlot(1);


                    //TransformChart();
                    Redraw();
                }
            }
        }

        private void Chart3DControl_MouseEnter(object sender, MouseEventArgs e)
        {
            Focus();
        }

        private void calculateN(double value)
        {
            if (value <= 50.0)
            {
                N = (int) (1.5*value + 25);
            }
            else
                N = 100 + (int) value;

            if (mode == Chart3DMode.Surface)
                N += 25;
        }

        public void Clear()
        {
            functions.Clear();

            if (mode == Chart3DMode.Points)
                m_3dChart = new ScatterChart3D();//TestScatterPlot(1);
            else if (mode == Chart3DMode.Surface)
                m_3dChart = new UniformSurfaceChart3D();//TestSurfacePlot(1);

            ReloadPoints();

            axisLabels.Remove();
            TransformChart();
        }

        public void addFx(Function fxy)
        {
            functions.Add(fxy);
            Redraw();
        }

        public void addFx(Func<double,double,double> fxy, double n)
        {
            N = n;
            functions.Add(new Function(fxy,"function1","function1"));
            Redraw();
        }

        public void Print()
        {
            throw new NotImplementedException();
        }

        public void PrintPreview()
        {
            throw new NotImplementedException();
        }

        public void SetChartAreaValues(double x0, double xn, double y0, double yn)
        {
            xmax = xn;
            xmin = x0;
            ymax = yn;
            ymin = y0;
            Redraw();
        }

        public void SaveImage(string path, ImageFormat imageFormat)
        {
            BitmapEncoder encoder;

            if(Equals(imageFormat, ImageFormat.Png))
                encoder= new PngBitmapEncoder();
            else if(Equals(imageFormat, ImageFormat.Bmp))
                encoder = new BmpBitmapEncoder();
            else if (Equals(imageFormat, ImageFormat.Gif))
                encoder = new GifBitmapEncoder();
            else if (Equals(imageFormat, ImageFormat.Jpeg))
                encoder = new JpegBitmapEncoder();
            else if (Equals(imageFormat, ImageFormat.Tiff))
                encoder = new TiffBitmapEncoder();
            else if (Equals(imageFormat, ImageFormat.Wmf))
                encoder = new WmpBitmapEncoder();
            else
                encoder = new PngBitmapEncoder();


            RenderTargetBitmap bitmap = new RenderTargetBitmap((int)this.ActualWidth, (int)this.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(this);
            BitmapFrame frame = BitmapFrame.Create(bitmap);
            encoder.Frames.Add(frame);

            using (var stream = File.Create(path))
            {
                encoder.Save(stream);
            }
        }
        
        public void Redraw()
        {
            foreach (var f in functions)
                DrawFunction((x, y) => f.Evaluate(x, y));
        }

        private void DrawFunction(Func<double, double, double> fxy)
        {
            if (mode == Chart3DMode.Surface)
            {
                AddSurface(fxy);
                return;
            }

            var spline3d = CalculateSpline3D(fxy);

            var rnd = new Random();

                AddPoints(spline3d.getPoints(),
                    new Color {R = (byte) rnd.Next(0, 256), G = (byte) rnd.Next(0, 256), B = (byte) rnd.Next(0, 256)});
        }

        private Spline3D CalculateSpline3D(Func<double, double, double> fxy)
        {
            double dx = (XMax - XMin)/N, dy = (YMax - YMin)/N;
            double x, y, z;
            var spline3d = new Spline3D();
            for (var ix = 0; ix <= N; ix++)
                for (var iy = 0; iy <= N; iy++)
                {
                    x = XMin + ix*dx;
                    y = YMin + iy*dy;
                    z = fxy(x, y);
                    if (!double.IsNaN(z) && !double.IsInfinity(z))
                        spline3d.addPoint(new Point3D(x, y, z));
                }
            return spline3d;
        }

        private void ReloadPoints() //changeDotSize, change
        {
            if (m_3dChart == null)
                return;

            if (mode == Chart3DMode.Points)
            {
                // 2. set the properties of each dot
                for (var i = 0; i < m_3dChart.GetDataNo(); i++)
                {
                    var plotItem = ((ScatterChart3D) m_3dChart).GetVertex(i);

                    plotItem.w = (float) DotSize; //size of plotItem
                    plotItem.h = (float) DotSize; //size of plotItem

                    ((ScatterChart3D) m_3dChart).SetVertex(i, plotItem);
                }
            }

            UpdateChart();
        }

        private DiffuseMaterial backMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.DimGray));

        private void AddSurface(Func<double, double, double> fxy)
        {
            ((UniformSurfaceChart3D) m_3dChart).SetGrid((int) N, (int) N, (float) XMin, (float) XMax, (float) YMin,
                (float) YMax);

            var oldSize = 0;
            var nVertNo = (int) (N*N);

            for (var i = 0; i < nVertNo; i++)
            {
                var vert = m_3dChart[oldSize + i];

                var z = fxy(vert.x, vert.y);
                //if (!double.IsNaN(z) && !double.IsInfinity(z))
                m_3dChart[oldSize + i].z = (float) z;
            }
            m_3dChart.GetDataRange();

            // 3. set the surface chart color according to z vaule
            double zMin = m_3dChart.ZMin();
            double zMax = m_3dChart.ZMax();
            for (var i = 0; i < nVertNo; i++)
            {
                var vert = m_3dChart[oldSize + i];
                var h = (vert.z - zMin)/(zMax - zMin);
                var color = TextureMapping.PseudoColor(h);
                m_3dChart[oldSize + i].color = color;

                if (double.IsInfinity(vert.z) || double.IsNaN(vert.z))
                {
                    vert.z = 0;
                }
            }

            UpdateChart();
        }

        public void AddPoints(IList<Point3D> points, Color color)
        {
            var oldSize = m_3dChart.GetDataNo();
            m_3dChart.IncreaseDataSize(points.Count);

            // 2. set the properties of each dot
            for (var i = 0; i < points.Count; i++)
            {
                var plotItem = new ScatterPlotItem
                {
                    w = (float)DotSize,//size of plotItem
                    h = (float)DotSize,//size of plotItem
                    x = (float)points[i].X,
                    y = (float)points[i].Y,
                    z = (float)points[i].Z,
                    shape = (int)Chart3D.SHAPE.ELLIPSE,
                    color = color
                };

                ((ScatterChart3D)m_3dChart).SetVertex(oldSize + i, plotItem);
            }

            UpdateChart();
        }









        private void UpdateChart()
        {
            m_3dChart.UseAxes = VisibilityAxes;
            m_3dChart.SetAxesColor(AxesColor);
            m_3dChart.GetDataRange();
            m_3dChart.SetAxes();

            ArrayList meshs = null;
            if (mode == Chart3DMode.Points)
                meshs = ((ScatterChart3D)m_3dChart).GetMeshes();
            else if (mode == Chart3DMode.Surface)
                meshs = ((UniformSurfaceChart3D)m_3dChart).GetMeshes();

            UpdateChartLabels(meshs);

            m_nChartModelIndex = (new Model3D()).UpdateModel(meshs, (m_3dChart is UniformSurfaceChart3D) ? backMaterial : null, m_nChartModelIndex, mainViewport);

            RescaleProjectionMatrix();
          //  TransformChart();
        }




        public void RescaleProjectionMatrix()
        {
            if (EqualAxes)
                m_transformMatrix.CalculateProjectionMatrix(Math.Min(m_3dChart.XMin(), Math.Min(m_3dChart.YMin(), m_3dChart.ZMin())),
                    Math.Max(m_3dChart.XMax(),Math.Max(m_3dChart.YMax(),m_3dChart.ZMax())), Scale);
            else
                m_transformMatrix.CalculateProjectionMatrix(m_3dChart.XMin(), m_3dChart.XMax(), m_3dChart.YMin(),
                    m_3dChart.YMax(), m_3dChart.ZMin(), m_3dChart.ZMax(), Scale);
            TransformChart();
        }

        public void OnViewportMouseDown(object sender, MouseButtonEventArgs args)
        {
            var pt = args.GetPosition(mainViewport);

            switch (args.ChangedButton)
            {
                case MouseButton.Left:
                    m_transformMatrix.OnLBtnDown(pt); // rotate 3d model
                    break;

                case MouseButton.Right:
                    m_transformMatrix.OnRBtnDown(pt); //drag 3d model
                    break;

                case MouseButton.Middle:
                    m_transformMatrix.OnMBtnDown();
                    TransformChart();
                    break;

                case MouseButton.XButton1:
                case MouseButton.XButton2:
                    //m_selectRect.OnMouseDown(pt, mainViewport, m_nRectModelIndex);// select rect
                    break;
            }
        }

        public void OnViewportMouseMove(object sender, MouseEventArgs args)
        {
            var pt = args.GetPosition(mainViewport);


            if (args.LeftButton == MouseButtonState.Pressed || args.RightButton == MouseButtonState.Pressed)
                // rotate or drag 3d model
            {
                m_transformMatrix.OnMouseMove(pt, mainViewport);

                TransformChart();
            }
            else if (args.XButton1 == MouseButtonState.Pressed || args.XButton2 == MouseButtonState.Pressed)
                // select rect
            {
                //m_selectRect.OnMouseMove(pt, mainViewport, m_nRectModelIndex);
                /*
                String s1;
                Point pt2 = m_transformMatrix.VertexToScreenPt(new Point3D(0.5, 0.5, 0.3), mainViewport);
                s1 = string.Format("Screen:({0:d},{1:d}), Predicated: ({2:d}, H:{3:d})", 
                    (int)pt.X, (int)pt.Y, (int)pt2.X, (int)pt2.Y);
                this.statusPane.Text = s1;
                */
            }
        }

        public void OnViewportMouseUp(object sender, MouseButtonEventArgs args)
        {
            var pt = args.GetPosition(mainViewport);
            if (args.ChangedButton == MouseButton.Left)
            {
                m_transformMatrix.OnLBtnUp();
            }

            else if (args.ChangedButton == MouseButton.Right)
            {
                m_transformMatrix.OnRBtnUp();
            }
            else if (args.ChangedButton == MouseButton.XButton1 || args.ChangedButton == MouseButton.XButton2)
            {
                /*if (m_nChartModelIndex == -1) return;
                // 1. get the mesh structure related to the selection rect
                MeshGeometry3D meshGeometry = Computator.NET.Charting.Chart3D.Model3D.GetGeometry(mainViewport, m_nChartModelIndex);
                if (meshGeometry == null) return;

                // 2. set selection in 3d chart
                m_3dChart.Select(m_selectRect, m_transformMatrix, mainViewport);

                // 3. update selection display
                m_3dChart.HighlightSelection(meshGeometry, Color.FromRgb(200, 200, 200));*/
            }
        }

        // zoom in 3d display
        public void OnViewportMouseWheel(object sender, MouseWheelEventArgs e)
        {
            m_transformMatrix.OnMouseWheel(e);
            TransformChart();
        }

        // zoom in 3d display
        public void OnKeyDown(object sender, KeyEventArgs args)
        {
            m_transformMatrix.OnKeyDown(args);
            TransformChart();
        }

        private void UpdateChartLabels(ArrayList meshs)
        {
            if (meshs == null)
                return;

            var whichTimeCone3dAppear = 0;

            for (var i = 0; i < meshs.Count; i++)
            {
                if (meshs[i].GetType() == typeof (Cone3D))
                {
                    whichTimeCone3dAppear++;
                    if (whichTimeCone3dAppear == 1)
                    {
                        axisLabels.x3D = ((Cone3D) meshs[i]).GetLastPoint();
                    }
                    else if (whichTimeCone3dAppear == 2)
                    {
                        axisLabels.y3D = ((Cone3D) meshs[i]).GetLastPoint();
                    }
                    else if (whichTimeCone3dAppear == 3)
                    {
                        axisLabels.z3D = ((Cone3D) meshs[i]).GetLastPoint();
                    }
                }
            }
        }

        // this function is used to rotate, drag and zoom the 3d chart
        private void TransformChart()
        {
            if (m_nChartModelIndex == -1) return;
            var visual3d = (ModelVisual3D) (mainViewport.Children[m_nChartModelIndex]);

            if (visual3d.Content == null) return;
            var group1 = visual3d.Content.Transform as Transform3DGroup;
            group1.Children.Clear();
            group1.Children.Add(new MatrixTransform3D(m_transformMatrix.m_totalMatrix));

            if (axisLabels.ActiveLabels && functions != null && functions.Count > 0)
            {
                var x = axisLabels.x3D;
                var y = axisLabels.y3D;
                var z = axisLabels.z3D;

                axisLabels.Reload(m_transformMatrix.VertexToScreenPt(x, mainViewport),
                    m_transformMatrix.VertexToScreenPt(y, mainViewport),
                    m_transformMatrix.VertexToScreenPt(z, mainViewport));
            }
        }
    }
}