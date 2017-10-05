using System;
using System.ServiceProcess;

namespace SetIPFXStatus.WindowsService
{
	internal static class Program
	{
		private static void Main()
		{
			ServiceBase[] setIPFXStatusService = new ServiceBase[1];
			setIPFXStatusService[0] = new SetIPFXStatusService();
			ServiceBase.Run(setIPFXStatusService);
		}
	}
}