using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using InTheHand.Windows.Forms;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BTControler
{
  public partial class MainWindow : Window
  {

    private uint moveCounter;
    private bool isMouseDown;
    private bool isBTConnect;
    private Point LastPosition;

    public MainWindow()
    {
      InitializeComponent();
      DataContext = App.ViewModel;
    }

    #region InputControl

    #region Mouse

    private void canvMain_MouseDown(object sender, MouseButtonEventArgs e)
    {
      moveCounter = 0;
      isMouseDown = true;
      try
      {
        Point p = e.GetPosition(this);
        App.ViewModel.BTService.SendValue = "DOWN";
        AddEllipseAt(canvMain, p, Brushes.Red);
        LastPosition = p;
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.ToString());
      }
    }

    // Länge eines Vectors: Wurzel aus skalarProdukt
    private float lenVec(Point v)
    {
      return (float)Math.Sqrt(v.X * v.X + v.Y * v.Y);
    }

    // Vector aus zwei Punkten
    private Point getVector(Point p1, Point p2)
    {
      return new Point(p2.X - p1.X, p2.Y - p1.Y);
    }

    private Point convertRelative(Point p)
    {
      return new Point(p.X / canvMain.Width, p.Y / canvMain.Height);
    }

    private void canvMain_MouseMove(object sender, MouseEventArgs e)
    {
      if (isMouseDown)
      {
        moveCounter++;
        Point p = e.GetPosition(this);
        Point v = getVector(LastPosition, p);
        App.ViewModel.BTService.SendTranslation = v;
        AddLineFromTo(canvMain, LastPosition, p, Brushes.Black);
        LastPosition = p;
      }
    }

    private void canvMain_MouseUp(object sender, MouseButtonEventArgs e)
    {
      isMouseDown = false;
      Point p = e.GetPosition(this);
      App.ViewModel.BTService.SendValue = "UP";
      AddEllipseAt(canvMain, p, Brushes.Blue);
      //MessageBox.Show("MovePositions: " + moveCounter);
    }

    private void canvMain_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ClickCount == 2)
      {
        MessageBox.Show("Double Click");
      }
    }
    
    #endregion

    #region Touch
    private void Canvas_TouchDown(object sender, TouchEventArgs e)
    {
      isMouseDown = true;
      moveCounter = 0;
      try
      {
        TouchPoint tp = e.GetTouchPoint(null);
        AddEllipseAt(canvMain, tp.Position, Brushes.Red);
        LastPosition = tp.Position;
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.ToString());
      }
    }

    private void Canvas_TouchMove(object sender, TouchEventArgs e)
    {
      moveCounter++;
      TouchPoint tp = e.GetTouchPoint(null);
      App.ViewModel.BTService.SendTranslation = getVector(LastPosition, tp.Position);
      AddLineFromTo(canvMain, LastPosition, tp.Position, Brushes.Black);
      LastPosition = tp.Position;
    }

    private void Canvas_TouchUp(object sender, TouchEventArgs e)
    {
      isMouseDown = false;
      TouchPoint tp = e.GetTouchPoint(null);
      App.ViewModel.BTService.SendValue = "UP";
      AddEllipseAt(canvMain, tp.Position, Brushes.Blue);
      //MessageBox.Show("MovePositions: " + moveCounter);
    }

    #endregion

    #endregion

    #region VisualMethods

    private const double CircleWidth = 10;

    private void AddEllipseAt(Canvas canv, Point pt, Brush brush)
    {
      Ellipse el = new Ellipse();
      el.Stroke = brush;
      el.Fill = brush;
      el.Width = CircleWidth;
      el.Height = CircleWidth;

      Canvas.SetLeft(el, pt.X - (CircleWidth / 2));
      Canvas.SetTop(el, pt.Y - (CircleWidth / 2));

      canv.Children.Add(el);
    }

    private void AddLineFromTo(Canvas canv, Point from, Point to, Brush brush)
    {
      Line l = new Line();
      l.Stroke = brush;
      l.X1 = from.X;
      l.Y1 = from.Y;
      l.X2 = to.X;
      l.Y2 = to.Y;
      l.StrokeThickness = 2;

      canv.Children.Add(l);
    }

    #endregion

    #region BTControl

    private void ConnectBySelect_Click(object sender, RoutedEventArgs e)
    {
      App.ViewModel.BTService.ConnectBySelect();
    }

    private void RadioInformations_Click(object sender, RoutedEventArgs e)
    {
      App.ViewModel.BTService.ShowRadioInfo();
      canvMain.Children.Clear();
    }

    #endregion

    #region Test

    private void test()
    {
      var client = new BluetoothClient();
      var dlg = new SelectBluetoothDeviceDialog();
      var result = dlg.ShowDialog();
      if (result != System.Windows.Forms.DialogResult.OK)
      {
        return;
      }
      BluetoothDeviceInfo device = dlg.SelectedDevice;
      BluetoothAddress addr = device.DeviceAddress;
      Console.WriteLine(device.DeviceName);
      BluetoothSecurity.PairRequest(addr, "Whatever pin");
      device.SetServiceState(BluetoothService.HumanInterfaceDevice, true);
      Thread.Sleep(1000); // Just in case
      if (device.InstalledServices.Length == 0)
      {
        // I wouldn't know why it doesn't install the service
        int x = 0;
      }
      Guid[] i = device.InstalledServices;
      MessageBox.Show(string.Join("\n", i));
      client.Connect(addr, BluetoothService.HumanInterfaceDevice);
    }
    #endregion

    private void canvMain_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
    {
      Vector vec = e.DeltaManipulation.Scale;
      MessageBox.Show("ManiDelatVec: " + vec.ToString());


      //// Get the image that's being manipulated.
      //UIElement element = (UIElement)e.Source;

      //// Use the matrix of the transform to manipulate the element's appearance.
      //Matrix matrix = ((MatrixTransform)element.RenderTransform).Matrix;
      
      //// Get the ManipulationDelta object.
      //ManipulationDelta deltaManipulation = e.DeltaManipulation;
      //Size s = element.RenderSize;
      
      //// Find the old center, and apply any previous manipulations.
      //Point center = new Point(element.ActualWidth / 2, element.ActualHeight / 2);
      //center = matrix.Transform(center);
      
      //// Apply new zoom manipulation (if it exists).
      //matrix.ScaleAt(deltaManipulation.Scale.X, deltaManipulation.Scale.Y,
      //center.X, center.Y);
      
      //// Apply new rotation manipulation (if it exists).
      //matrix.RotateAt(e.DeltaManipulation.Rotation, center.X, center.Y);
      
      //// Apply new panning manipulation (if it exists).
      //matrix.Translate(e.DeltaManipulation.Translation.X,
      //e.DeltaManipulation.Translation.Y);
    }

  }
}
