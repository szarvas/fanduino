﻿/*
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
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using log4net;

namespace FanduinoWindow
{
    static class Program
    {
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
        static void Main()
        {
            log4net.Config.XmlConfigurator.Configure();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                Form1 form = new Form1();
                form.SetText("Connecting on port " + Fanduino.Config.port);
                Converter<string, Microsoft.FSharp.Core.Unit> hook = s =>
                {
                    String sCopy = s;
                    form.SetText(sCopy);
                    return null;
                };
            
                    using (Fanduino.Main.program(hook))
                        Application.Run(form);
            }
            catch(Exception e)
            {
                MessageBox.Show(e.InnerException.Message, "Fanduino",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
