using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BTController.ViewModels
{
  public class BTBase : INotifyPropertyChanged
  {
    public string Title { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
    {
      if (PropertyChanged != null)
      {
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
      }
    }

  }
}
