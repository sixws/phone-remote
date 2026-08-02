using Microsoft.Extensions.DependencyInjection;

namespace PhoneRemoteApp;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new AppShell());

#if WINDOWS
		// Windows 调试：固定手机竖屏分辨率（400×860 逻辑像素），不用手动拉窗口
		window.Width = 400;
		window.Height = 860;
		window.MinimumWidth = 320;
		window.MinimumHeight = 640;
#endif

		return window;
	}
}