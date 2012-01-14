﻿#region Copyright (C) 2011-2012 MPExtended
// Copyright (C) 2011-2012 MPExtended Developers, http://mpextended.github.com/
// 
// MPExtended is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MPExtended is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MPExtended. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MPExtended.Libraries.Service.Hosting
{
    public class MPExtendedHost
    {
        private WCFHost wcf;
        private Zeroconf zeroconf;

        public MPExtendedHost()
        {
            Thread.CurrentThread.Name = "HostThread";
            wcf = new WCFHost();
            zeroconf = new Zeroconf();
        }

        public bool Open()
        {
            try
            {
                // rotate log files if possible
                LogRotation rotation = new LogRotation();
                rotation.Rotate();

                Log.Debug("Opening MPExtended ServiceHost version {0} (build {1})", VersionUtil.GetVersionName(), VersionUtil.GetBuildVersion());

                // always log uncaught exceptions
                AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
                {
                    Log.Error("Unhandled exception", (Exception)e.ExceptionObject);
                    if (e.IsTerminating)
                    {
                        Log.Fatal("Terminating because of previous exception");
                    }
                };

                // start the WCF services
                wcf.Start(Installation.GetAvailableServices().Where(x => x.WCFType != null));

                // start the background threads
                var services = Installation.GetAvailableServices().Where(x => x.BackgroundThread != null);
                foreach (var service in services)
                {
                    string name = service.Service.ToString() + "Background";
                    ThreadManager.Start(name, delegate()
                    {
                        service.BackgroundThread.Invoke();
                    });
                }

                // do the zeroconf publish
                ThreadManager.Start("Zeroconf", delegate()
                {
                    try
                    {
                        zeroconf.PublishServices(Installation.GetInstalledServices());
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Zeroconf publish failed", ex);
                    }
                });
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Failed to open MPExtended ServiceHost", ex);
                return false;
            }
        }

        public bool Close()
        {
            try
            {
                Log.Debug("Closing MPExtended ServiceHost...");
                wcf.Stop();
                ThreadManager.AbortAll();
                Log.Flush();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Failed to close MPExtended ServiceHost", ex);
                return false;
            }
        }
    }
}
