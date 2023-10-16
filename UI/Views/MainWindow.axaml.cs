using Avalonia.Controls;
using System;

namespace UI.Views;

public partial class MainWindow : Window
{
  public MainWindow()
  {
	InitializeComponent();
  }


  public void OnSpin(object sender, SpinEventArgs e)
  {
	var spinner = (ButtonSpinner)sender;

	if (spinner.Content is TextBlock txtBox)
	{
	  //int value = Array.IndexOf(available_page_limits, Convert.ToInt32(txtBox.Text));
	  int value = Convert.ToInt32(txtBox.Text);

	  if (e.Direction == SpinDirection.Increase)
	  {
			value += 50;
	  }
	  else
	  {
		value = value > 50 ? value - 50 : 0;
	  }


	  txtBox.Text = value.ToString();
	}

  }

}