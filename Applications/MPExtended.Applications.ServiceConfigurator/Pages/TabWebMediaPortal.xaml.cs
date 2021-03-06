﻿#region Copyright (C) 2012-2013 MPExtended
// Copyright (C) 2012-2013 MPExtended Developers, http://www.mpextended.com/
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
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MPExtended.Applications.ServiceConfigurator.Code;
using MPExtended.Libraries.Service;
using MPExtended.Libraries.Service.Strings;

namespace MPExtended.Applications.ServiceConfigurator.Pages
{
    /// <summary>
    /// Interaction logic for TabWebMediaPortal.xaml
    /// </summary>
    public partial class TabWebMediaPortal : Page, ITabCloseCallback
    {
        private ServiceControlInterface sci;

        public TabWebMediaPortal()
        {
            InitializeComponent();

            txtPort.Text = Configuration.WebMediaPortalHosting.Port.ToString();
            txtHTTPSPort.Text = Configuration.WebMediaPortalHosting.PortTLS.ToString();
            cbHTTPS.IsChecked = Configuration.WebMediaPortalHosting.EnableTLS;
            txtHTTPSPort.IsEnabled = Configuration.WebMediaPortalHosting.EnableTLS;

            sci = new ServiceControlInterface("MPExtended WebMediaPortal", lblStatusInfo, btnStartStop);
            if (sci.IsServiceAvailable)
                sci.StartServiceWatcher();
        }

        public void TabClosed()
        {
            bool hasChanged = Configuration.WebMediaPortalHosting.Port != Int32.Parse(txtPort.Text) ||
                Configuration.WebMediaPortalHosting.EnableTLS != cbHTTPS.IsChecked.GetValueOrDefault(false) ||
                Configuration.WebMediaPortalHosting.PortTLS != Int32.Parse(txtHTTPSPort.Text);
            if (!hasChanged)
                return;

            int port;
            if (!Int32.TryParse(txtPort.Text, out port))
                port = 8080;

            Configuration.WebMediaPortalHosting.Port = port;
            Configuration.WebMediaPortalHosting.EnableTLS = cbHTTPS.IsChecked.GetValueOrDefault(false);
            Configuration.WebMediaPortalHosting.PortTLS = Int32.Parse(txtHTTPSPort.Text);
            Configuration.Save();

            if (!sci.IsServiceAvailable)
                return;
            sci.RestartService();
        }

        private void btnStartStop_Click(object sender, RoutedEventArgs e)
        {
            sci.TriggerButtonClick();
        }

        private void ChangeHTTPSCheckbox(object sender, RoutedEventArgs e)
        {
            txtHTTPSPort.IsEnabled = cbHTTPS.IsChecked.GetValueOrDefault(false);
        }

        private void txtHTTPSPort_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!cbHTTPS.IsChecked.GetValueOrDefault(false))
                return;

            int port;
            if (!Int32.TryParse(txtHTTPSPort.Text, out port) || port < 44300 || port > 44399)
            {
                MessageBox.Show(UI.HTTPSInvalidPort, "MPExtended", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnResetSkin_Click(object sender, RoutedEventArgs e)
        {
            Configuration.WebMediaPortal.Skin = "default";
            Configuration.Save();
            MessageBox.Show(UI.WebMediaPortalResetSkinSuccess, "MPExtended", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
