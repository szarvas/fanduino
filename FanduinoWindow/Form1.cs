/*
Copyright 2016 Attila Szarvas

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using System.IO;

namespace FanduinoWindow
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            if (Fanduino.Config.startMinimized)
            {
                WindowState = FormWindowState.Minimized;
                Form1_Resize(this, null);
            }
        }

        delegate void SetTextCallback(string text);

        public void SetText(String msg)
        {
            if (this.label1.InvokeRequired)
            {
                this.Invoke(new SetTextCallback(SetText), new object[] { msg });
            }
            else
            {
                this.label1.Text = msg;
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            Show();
            notifyIcon1.Visible = false;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                notifyIcon1.Visible = true;
                Hide();
                this.ShowInTaskbar = false;
            }
        }
    }
}
