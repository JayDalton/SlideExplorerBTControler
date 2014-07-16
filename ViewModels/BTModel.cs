using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using InTheHand;
using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using System.Windows;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Threading;
using System.Threading;

namespace BTController.ViewModels
{
  public class BTModel
  {
    public BTService BTService { get; set; }

    public void LoadData()
    {
      createBTConnect();
    }

    private void createBTConnect()
    {
      BTService = new BTService();
    }
  }

}
