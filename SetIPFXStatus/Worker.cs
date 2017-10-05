using IPFX.UC.Messaging;
using IPFX.UC.Messaging.Messages;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SetIPFXStatus
{
	public class Worker
	{
		private UCConnection connection;

		private bool keepRunning;

		public bool waitingForReply;

		private Worker.psLocations currentLocation;

		public string Extension
		{
			get;
			set;
		}

		public string IPFXServerName
		{
			get;
			set;
		}

		public string IPFXIPAddress
		{
			get;
			set;
		}

		public Worker()
		{
			this.keepRunning = true;
		}

		public void ChangeState(SessionSwitchReason reason)
		{
			if (this.connection != null && this.connection.IsConnected)
			{
				if (reason == SessionSwitchReason.SessionUnlock)
				{
					if (this.GetCurrentStatus() == Worker.psLocations.psLocationDND)
					{
						this.connection.SendMessage(new RequestSetLocation(this.IPFXServerName, this.Extension, 0, 0));
						return;
					}
				}
				else
				{
					if (this.GetCurrentStatus() == Worker.psLocations.psLocationOffice)
					{
						this.connection.SendMessage(new RequestSetLocation(this.IPFXServerName, this.Extension, 2, 0, new DateTime?(DateTime.MinValue)));
						return;
					}
				}
			}
		}

		public void Close()
		{
			this.keepRunning = false;
		}

		private void connection_MessageReceived(IMessage message)
		{
			ResponseGetExtensionBasic m = new ResponseGetExtensionBasic(new MessageDataReader(message.ToString()));
			this.currentLocation = (Worker.psLocations)int.Parse(m.Rows[0][0]);
			this.waitingForReply = false;
		}

		public void DoWork()
		{
			this.connection = new UCConnection("SetIPFXStatus", this.IPFXServerName, this.IPFXIPAddress, 100, true);
			try
			{
				try
				{
					this.connection.Start();
					if (this.connection.Started)
					{
						while (this.connection.State == ConnectionStateType.Connecting)
						{
							Thread.Sleep(1);
						}
					}
					if (this.connection.State == ConnectionStateType.Connected)
					{
						this.connection.MessageReceived += new MessageReceivedEventHandler(this.connection_MessageReceived);
						this.GetCurrentStatus();
						while (this.keepRunning)
						{
							Thread.Sleep(1);
						}
					}
				}
				catch (Exception exception)
				{
					Console.WriteLine(exception.Message);
				}
			}
			finally
			{
				if (this.connection != null && this.connection.State == ConnectionStateType.Connected)
				{
					this.connection.Stop(false);
				}
			}
		}

		public Worker.psLocations GetCurrentStatus()
		{
			if (this.connection.State == ConnectionStateType.Connected)
			{
				this.waitingForReply = true;
				List<string> strs = new List<string>();
				strs.Add(this.Extension);
				this.connection.SendMessage(new RequestGetExtensionBasic(this.IPFXServerName, strs, false, 0, true, "Location"));
				while (this.waitingForReply)
				{
					Thread.Sleep(1);
				}
			}
			return this.currentLocation;
		}

		private void Worker_Close()
		{
			this.keepRunning = false;
		}

		public enum psLocations
		{
			psLocationWorkTime = -1000,
			psLocationQueue = -1,
			psLocationOffice = 0,
			psLocationMeeting = 1,
			psLocationDND = 2,
			psLocationGoneOut = 3,
			psLocationGFTD = 4,
			psLocationHoliday = 5,
			psLocationSick = 6,
			psLocationBreak = 7,
			psLocationCustom = 8
		}
	}
}