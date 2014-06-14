﻿using BTControler.ViewModels;
using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using InTheHand.Windows.Forms;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;

namespace BTControler
{
  public class BTService : BTBase
  {
    readonly Guid OurServiceClassId = new Guid("{29913A2D-EB93-40cf-BBB8-DEEE26452197}");
    readonly string OurServiceName = "32feet.NET Chat2";

    public ObservableCollection<BTData> Items { get; set; }
    private object _listLock = new object();

    public BlockingCollection<BTData> Datas { get; set; }

    private string sendValue = "";
    public string SendValue { 
      get { return sendValue; }
      set
      {
        if (value != sendValue)
        {
          sendValue = value;
          Datas.Add(new BTData { Data = value });
          AddMessage(MessageSource.Info, value);
          NotifyPropertyChanged();
        }
      }
    }

    private Point sendPoint = new Point();
    public Point SendPoint
    {
      get { return sendPoint; }
      set
      {
        if (value != sendPoint)
        {
          sendPoint = value;
          string result = "P:" + convertPoint(value);
          Datas.Add(new BTData { Data = result });
          AddMessage(MessageSource.Info, result);
          NotifyPropertyChanged();
        }
      }
    }

    private Point sendTranslation = new Point();
    public Point SendTranslation
    {
      get { return sendTranslation; }
      set
      {
        if (value != sendTranslation)
        {
          sendTranslation = value;
          string result = "V:" + convertPoint(value);
          Datas.Add(new BTData { Data = result });
          AddMessage(MessageSource.Info, result);
          NotifyPropertyChanged();
        }
      }
    }

    //private double canvasWidth;
    //public double CanvasWidth { 
    //  get { return canvasWidth; }
    //  set
    //  {
    //    if (value != canvasWidth)
    //    {
    //      canvasWidth = value;
    //    }
    //  }
    //}

    //private double canvasHeight;
    //public double CanvasHeight
    //{
    //  get { return canvasHeight; }
    //  set
    //  {
    //    if (value != canvasHeight)
    //    {
    //      canvasHeight = value;
    //    }
    //  }
    //}

    volatile bool _closing;
    TextWriter _connWtr;
    BluetoothListener _lsnr;

    public BTService()
    {
      Datas = new BlockingCollection<BTData>();

      Items = new ObservableCollection<BTData>();
      BindingOperations.EnableCollectionSynchronization(Items, _listLock);

      StartBluetooth();
      AddMessage(MessageSource.Info, "Connect to another remote device!");
    }

    #region Bluetooth start/Connect/Listen

    private void StartBluetooth()
    {
      try
      {
        new BluetoothClient();
      }
      catch (Exception ex)
      {
        var msg = "Bluetooth init failed: " + ex;
        System.Windows.MessageBox.Show(msg);
        throw new InvalidOperationException(msg, ex);
      }
      // TODO Check radio?
      //
      // Always run server?
      StartListener();
    }

    private BluetoothAddress BluetoothSelect()
    {
      var dlg = new SelectBluetoothDeviceDialog();
      var rslt = dlg.ShowDialog();
      if (rslt != DialogResult.OK)
      {
        AddMessage(MessageSource.Info, "Cancelled select device.");
        return null;
      }
      var addr = dlg.SelectedDevice.DeviceAddress;
      return addr;
    }

    private void BluetoothConnect(BluetoothAddress addr)
    {
      var cli = new BluetoothClient();
      try
      {
        cli.Connect(addr, OurServiceClassId);
        var peer = cli.GetStream();
        SetConnection(peer, true, cli.RemoteEndPoint);
        ThreadPool.QueueUserWorkItem(ReadMessagesToEnd_Runner, peer);
      }
      catch (SocketException ex)
      {
        // Try to give a explanation reason by checking what error-code.
        // http://32feet.codeplex.com/wikipage?title=Errors
        // Note the error codes used on MSFT+WM are not the same as on
        // MSFT+Win32 so don't expect much there, we try to use the
        // same error codes on the other platforms where possible.
        // e.g. Widcomm doesn't match well, Bluetopia does.
        // http://32feet.codeplex.com/wikipage?title=Feature%20support%20table
        string reason;
        switch (ex.ErrorCode)
        {
          case 10048: // SocketError.AddressAlreadyInUse
            // RFCOMM only allow _one_ connection to a remote service from each device.
            reason = "There is an existing connection to the remote Chat2 Service";
            break;
          case 10049: // SocketError.AddressNotAvailable
            reason = "Chat2 Service not running on remote device";
            break;
          case 10064: // SocketError.HostDown
            reason = "Chat2 Service not using RFCOMM (huh!!!)";
            break;
          case 10013: // SocketError.AccessDenied:
            reason = "Authentication required";
            break;
          case 10060: // SocketError.TimedOut:
            reason = "Timed-out";
            break;
          default:
            reason = null;
            break;
        }
        reason += " (" + ex.ErrorCode.ToString() + ") -- ";
        //
        var msg = "Bluetooth connection failed: " + ex;
        msg = reason + msg;
        AddMessage(MessageSource.Error, msg);
        System.Windows.MessageBox.Show(msg);
      }
      catch (Exception ex)
      {
        var msg = "Bluetooth connection failed: " + ex;
        AddMessage(MessageSource.Error, msg);
        System.Windows.MessageBox.Show(msg);
      }
    }

    private void StartSending()
    {
      ThreadPool.QueueUserWorkItem(Sending_Runner);
    }

    private void Sending_Runner(object state)
    {
      while (true)
      {
        if (Send(Datas.Take().Data))
        {
          //AddMessage(MessageSource.Info, "Sending...");
        }
      }
    }

    private void StartListener()
    {
      var lsnr = new BluetoothListener(OurServiceClassId);
      lsnr.ServiceName = OurServiceName;
      lsnr.Start();
      _lsnr = lsnr;
      ThreadPool.QueueUserWorkItem(ListenerAccept_Runner, lsnr);
    }

    void ListenerAccept_Runner(object state)
    {
      var lsnr = (BluetoothListener)_lsnr;
      // We will accept only one incoming connection at a time. So just
      // accept the connection and loop until it closes.
      // To handle multiple connections we would need one threads for
      // each or async code.
      while (true)
      {
        var conn = lsnr.AcceptBluetoothClient();
        var peer = conn.GetStream();
        SetConnection(peer, false, conn.RemoteEndPoint);
        ReadMessagesToEnd(peer);
      }
    }
    #endregion

    #region Connection Set/Close

    private void SetConnection(Stream peerStream, bool outbound, BluetoothEndPoint remoteEndPoint)
    {
      if (_connWtr != null)
      {
        AddMessage(MessageSource.Error, "Already Connected!");
        return;
      }
      _closing = false;
      var connWtr = new StreamWriter(peerStream);
      connWtr.NewLine = "\r\n"; // Want CR+LF even on UNIX/Mac etc.
      _connWtr = connWtr;
      AddMessage(MessageSource.Info,
          (outbound ? "Connected to " : "Connection from ")
        // Can't guarantee that the Port is set, so just print the address.
        // For more info see the docs on BluetoothClient.RemoteEndPoint.
          + remoteEndPoint.Address);
      StartSending();
    }

    private void ConnectionCleanup()
    {
      _closing = true;
      var wtr = _connWtr;
      //_connStrm = null;
      _connWtr = null;
      if (wtr != null)
      {
        try
        {
          wtr.Close();
        }
        catch (Exception ex)
        {
          Debug.WriteLine("ConnectionCleanup close ex: " + ex);
        }
      }
    }

    public void BluetoothDisconnect()
    {
      AddMessage(MessageSource.Info, "Disconnecting");
      ConnectionCleanup();
    }
    
    #endregion

    #region Connection I/O
    private bool Send(string message)
    {
      if (_connWtr == null)
      {
        System.Windows.MessageBox.Show("No connection.");
        return false;
      }
      try
      {
        _connWtr.WriteLine(message);
        _connWtr.Flush();
        return true;
      }
      catch (Exception ex)
      {
        System.Windows.MessageBox.Show("Connection lost! (" + ex + ")");
        ConnectionCleanup();
        return false;
      }
    }

    private void ReadMessagesToEnd_Runner(object state)
    {
      Stream peer = (Stream)state;
      ReadMessagesToEnd(peer);
    }

    private void ReadMessagesToEnd(Stream peer)
    {
      var rdr = new StreamReader(peer);
      while (true)
      {
        string line;
        try
        {
          line = rdr.ReadLine();
        }
        catch (IOException ioex)
        {
          if (_closing)
          {
            // Ignore the error that occurs when we're in a Read
            // and _we_ close the connection.
          }
          else
          {
            AddMessage(MessageSource.Error, "Connection was closed hard (read).  " + ioex);
          }
          break;
        }
        if (line == null)
        {
          AddMessage(MessageSource.Info, "Connection was closed (read).");
          break;
        }
        AddMessage(MessageSource.Remote, line);
      }//while
      ConnectionCleanup();
    }
    #endregion

    #region Radio
    public void SetRadioMode(RadioMode mode)
    {
      try
      {
        BluetoothRadio.PrimaryRadio.Mode = mode;
      }
      catch (NotSupportedException)
      {
        System.Windows.MessageBox.Show("Setting Radio.Mode not supported on this Bluetooth stack.");
      }
    }

    static void DisplayPrimaryBluetoothRadio(TextWriter wtr)
    {
      var myRadio = BluetoothRadio.PrimaryRadio;
      if (myRadio == null)
      {
        wtr.WriteLine("No radio hardware or unsupported software stack");
        return;
      }
      var mode = myRadio.Mode;
      // Warning: LocalAddress is null if the radio is powered-off.
      wtr.WriteLine("* Radio, address: {0:C}", myRadio.LocalAddress);
      wtr.WriteLine("Mode: " + mode.ToString());
      wtr.WriteLine("Name: " + myRadio.Name);
      wtr.WriteLine("HCI Version: " + myRadio.HciVersion
          + ", Revision: " + myRadio.HciRevision);
      wtr.WriteLine("LMP Version: " + myRadio.LmpVersion
          + ", Subversion: " + myRadio.LmpSubversion);
      wtr.WriteLine("ClassOfDevice: " + myRadio.ClassOfDevice
          + ", device: " + myRadio.ClassOfDevice.Device
          + " / service: " + myRadio.ClassOfDevice.Service);
      wtr.WriteLine("S/W Manuf: " + myRadio.SoftwareManufacturer);
      wtr.WriteLine("H/W Manuf: " + myRadio.Manufacturer);
    }
    #endregion

    #region Menu items etc

    public void ConnectBySelect()
    {
      var addr = BluetoothSelect();
      if (addr == null)
      {
        return;
      }
      BluetoothConnect(addr);
    }

    public void ConnectByAddress()
    {
      var addr = BluetoothAddress.Parse("002233445566");
      var line = Microsoft.VisualBasic.Interaction.InputBox("Target Address", "Chat2", null, -1, -1);
      if (string.IsNullOrEmpty(line))
      {
        return;
      }
      line = line.Trim();
      if (!BluetoothAddress.TryParse(line, out addr))
      {
        System.Windows.MessageBox.Show("Invalid address.");
        return;
      }
      BluetoothConnect(addr);
    }

    //public void SendMessage(string message)
    //{
    //  if (Send(message))
    //  {
    //    AddMessage(MessageSource.Local, message);
    //  }
    //}

    //public void SendPosition(Point p)
    //{
    //  string r = convertPosition(p);
    //  if (Send(r))
    //  {
    //    AddMessage(MessageSource.Local, r);
    //  }
    //}

    //private string convertPosition(Point p)
    //{
    //  Point r = new Point(p.X / canvasWidth, p.Y / canvasHeight);
    //  return r.ToString();
    //}

    private string convertPoint(Point p)
    {
      return 
        p.X.ToString("0.0000", CultureInfo.InvariantCulture) + ";" + 
        p.Y.ToString("0.0000", CultureInfo.InvariantCulture);
    }

    //--
    private void wtbModeDiscoverable_Click(object sender, EventArgs e)
    {
      SetRadioMode(RadioMode.Discoverable);
    }

    private void wtbModeConnectable_Click(object sender, EventArgs e)
    {
      SetRadioMode(RadioMode.Connectable);
    }

    private void wtbModeNeither_Click(object sender, EventArgs e)
    {
      SetRadioMode(RadioMode.PowerOff);
    }

    public void ShowRadioInfo()
    {
      using (var wtr = new StringWriter())
      {
        DisplayPrimaryBluetoothRadio(wtr);
        AddMessage(MessageSource.Info, wtr.ToString());
      }
    }
    #endregion

    #region Chat Log
    enum MessageSource
    {
      Local,
      Remote,
      Info,
      Error,
    }

    void AddMessage(MessageSource source, string message)
    {
      string prefix;
      string result;
      switch (source)
      {
        case MessageSource.Local:
          prefix = "Me: ";
          break;
        case MessageSource.Remote:
          prefix = "You: ";
          break;
        case MessageSource.Info:
          prefix = "Info: ";
          break;
        case MessageSource.Error:
          prefix = "Error: ";
          break;
        default:
          prefix = "???:";
          break;
      }
      result = prefix + message;
      lock (_listLock)
      {
        Items.Insert(0, new BTData { Data = result });
        if (500 < Items.Count)
        {
          Items.RemoveAt(500);
        }
      }
    }

    #endregion

  }
}
