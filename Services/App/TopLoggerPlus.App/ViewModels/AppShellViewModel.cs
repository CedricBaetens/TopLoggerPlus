using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TopLoggerPlus.App.ViewModels;

public class AppShellViewModel : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler PropertyChanged;
	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChangedEventHandler handler = PropertyChanged;
		if (handler != null)
			handler(this, new PropertyChangedEventArgs(propertyName));
	}
}
