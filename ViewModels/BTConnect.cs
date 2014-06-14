using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InTheHand;
using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using System.Windows;
using System.Net.NetworkInformation;
using System.Collections.ObjectModel;
using Windows.Devices;
using Windows.Devices.Input;
using Windows.Devices.Portable;

namespace BTControler.ViewModels
{

  public class BTConnect
  {
    private string bt_mac = "5CAC4CE7439A";
    private BluetoothEndPoint localEndpoint;
    private BluetoothClient localClient;
    private BluetoothComponent localComponent;
    private List<BluetoothDeviceInfo> deviceList = new List<BluetoothDeviceInfo>();
    public ObservableCollection<BTData> Items { get; set; }

    public BTConnect()
    {
      Items = new ObservableCollection<BTData>();
      Items.Add(new BTData { Data = "Erste Zeile" });
      Items.Add(new BTData { Data = "Zweite Zeile" });
      Items.Add(new BTData { Data = "Dritte Zeile" });
    }

    private void scan()
    {
      BluetoothAddress mac = BluetoothAddress.Parse(bt_mac);
      // mac is mac address of local bluetooth device
      localEndpoint = new BluetoothEndPoint(mac, BluetoothService.SerialPort);
      // client is used to manage connections
      localClient = new BluetoothClient(localEndpoint);
      // component is used to manage device discovery
      localComponent = new BluetoothComponent(localClient);
      // async methods, can be done synchronously too
      localComponent.DiscoverDevicesAsync(255, true, true, true, true, null);
      localComponent.DiscoverDevicesProgress += new EventHandler<DiscoverDevicesEventArgs>(component_DiscoverDevicesProgress);
      localComponent.DiscoverDevicesComplete += new EventHandler<DiscoverDevicesEventArgs>(component_DiscoverDevicesComplete);
    }

    private void component_DiscoverDevicesProgress(object sender, DiscoverDevicesEventArgs e)
    {
      // log and save all found devices
      for (int i = 0; i < e.Devices.Length; i++)
      {
        if (e.Devices[i].Remembered)
        {
          MessageBox.Show(e.Devices[i].DeviceName + " (" + e.Devices[i].DeviceAddress + "): Device is known");
        }
        else
        {
          MessageBox.Show(e.Devices[i].DeviceName + " (" + e.Devices[i].DeviceAddress + "): Device is unknown");
        }
        this.deviceList.Add(e.Devices[i]);
      }
    }

    private void component_DiscoverDevicesComplete(object sender, DiscoverDevicesEventArgs e)
    {
      // log some stuff
      pairing();
    }

    private void pairing()
    {
      // get a list of all paired devices
      BluetoothDeviceInfo[] paired = localClient.DiscoverDevices(255, false, true, false, false);
      // check every discovered device if it is already paired 
      foreach (BluetoothDeviceInfo device in this.deviceList)
      {
        bool isPaired = false;
        for (int i = 0; i < paired.Length; i++)
        {
          if (device.Equals(paired[i]))
          {
            isPaired = true;
            break;
          }
        }

        // if the device is not paired, pair it!
        if (!isPaired)
        {
          // replace DEVICE_PIN here, synchronous method, but fast
          isPaired = BluetoothSecurity.PairRequest(device.DeviceAddress, "1234");
          if (isPaired)
          {
            // now it is paired
            MessageBox.Show("now it is paired");
          }
          else
          {
            // pairing failed
            MessageBox.Show("pairing failed");
          }
        }
      }
    }

    private string getMACAddress()
    {
      NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
      String sMacAddress = string.Empty;

      foreach (NetworkInterface adapter in nics)
      {
        if (sMacAddress == String.Empty)// only return MAC Address from first card  
        {
          IPInterfaceProperties properties = adapter.GetIPProperties();
          sMacAddress = adapter.GetPhysicalAddress().ToString();
        }
      }

      return sMacAddress;
    }

  }
}
