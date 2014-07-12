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

    public MainWindow()
    {
      InitializeComponent();
      DataContext = App.ViewModel;
    }

    private void canvMain_ManipulationStarted(object sender, ManipulationStartedEventArgs e)
    {
      Manipulation.SetManipulationMode(sender as UIElement, ManipulationModes.All);
    }

    private void canvMain_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
    {
      UIElement element = e.Source as UIElement;

      ManipulationDelta deltaManipulation = e.DeltaManipulation;

      if (1 != deltaManipulation.Scale.X && 1 != deltaManipulation.Scale.Y)
      {
        App.ViewModel.BTService.SendMText = deltaManipulation.Scale.Length.ToString();
        App.ViewModel.BTService.SendPoint = e.ManipulationOrigin;
        App.ViewModel.BTService.SendScale = deltaManipulation.Scale;
      }

      if (0 < deltaManipulation.Translation.Length)
      {
        App.ViewModel.BTService.SendTrans = deltaManipulation.Translation;
      }
      
    }

    private void ConnectBySelect_Click(object sender, RoutedEventArgs e)
    {
      App.ViewModel.BTService.ConnectBySelect();
    }

    private void RadioInformations_Click(object sender, RoutedEventArgs e)
    {
      canvMain.Children.Clear();
      App.ViewModel.BTService.BluetoothDisconnect();
    }
  }
}
