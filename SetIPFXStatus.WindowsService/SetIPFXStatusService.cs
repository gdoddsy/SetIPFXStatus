using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using System.Windows.Forms;

namespace SetIPFXStatus.WindowsService
{
	public class SetIPFXStatusService : ServiceBase
	{
		private Worker worker;

		private Thread workerThread;

		private bool isRemoteConnection;

		private IContainer components;

		public SetIPFXStatusService()
		{
			this.worker = new Worker();
			this.InitializeComponent();
			base.CanHandleSessionChangeEvent = true;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && this.components != null)
			{
				this.components.Dispose();
			}
			base.Dispose(disposing);
		}

		internal void Init()
		{
			this.workerThread = new Thread(new ThreadStart(this.worker.DoWork));
			this.worker.Extension = ConfigurationManager.AppSettings["Ext"];
			this.worker.IPFXServerName = ConfigurationManager.AppSettings["IPFXServerName"];
			this.worker.IPFXIPAddress = ConfigurationManager.AppSettings["IPFXIPAddress"];
			this.workerThread.Start();
		}

		private void InitializeComponent()
		{
			this.components = new Container();
			base.ServiceName = "SetIPFXStatus";
		}

		protected override void OnSessionChange(SessionChangeDescription changeDescription)
		{
			base.OnSessionChange(changeDescription);
			SessionChangeReason reason = changeDescription.Reason;
			switch (reason)
			{
				case SessionChangeReason.RemoteConnect:
				{
					this.isRemoteConnection = true;
					return;
				}
				case SessionChangeReason.RemoteDisconnect:
				{
					this.isRemoteConnection = false;
					return;
				}
				case SessionChangeReason.SessionLogon:
				case SessionChangeReason.SessionLogoff:
				{
					return;
				}
				case SessionChangeReason.SessionLock:
				{
					if (!this.isRemoteConnection)
					{
						DateTime now = DateTime.Now;
						Console.WriteLine(string.Concat("Locked at ", now.ToString()));
						this.worker.ChangeState(SessionSwitchReason.SessionLock);
						return;
					}
					else
					{
						return;
					}
				}
				case SessionChangeReason.SessionUnlock:
				{
					if (!this.isRemoteConnection)
					{
						DateTime dateTime = DateTime.Now;
						Console.WriteLine(string.Concat("Unlocked at ", dateTime.ToString()));
						this.worker.ChangeState(SessionSwitchReason.SessionUnlock);
						return;
					}
					else
					{
						return;
					}
				}
				default:
				{
					return;
				}
			}
		}

		protected override void OnStart(string[] args)
		{
			this.Init();
		}

		protected override void OnStop()
		{
			this.Quit();
		}

		internal void Quit()
		{
			this.worker.Close();
			this.workerThread.Join();
			Application.Exit();
		}

		private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
		{
			SessionSwitchReason reason = e.Reason;
			if (reason == SessionSwitchReason.SessionLock)
			{
				DateTime now = DateTime.Now;
				Console.WriteLine(string.Concat("Locked at ", now.ToString()));
				this.worker.ChangeState(SessionSwitchReason.SessionLock);
			}
			else
			{
				if (reason == SessionSwitchReason.SessionUnlock)
				{
					DateTime dateTime = DateTime.Now;
					Console.WriteLine(string.Concat("Unlocked at ", dateTime.ToString()));
					this.worker.ChangeState(SessionSwitchReason.SessionUnlock);
				}
			}
			using (TextWriter tw = new StreamWriter("c:\\temp\\IPFXStatus.txt"))
			{
				SessionSwitchReason sessionSwitchReason = e.Reason;
				tw.WriteLine(sessionSwitchReason.ToString());
				tw.Close();
			}
		}
	}
}