using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using BTController.ViewModels;

namespace BTController
{
  public partial class App : Application
  {
    private static BTModel viewModel = null;

    public static BTModel ViewModel
    {
      get
      {
        if (viewModel == null)
        {
          viewModel = new BTModel();
          viewModel.LoadData();
        }
        return viewModel;
      }
    }
  }
}
