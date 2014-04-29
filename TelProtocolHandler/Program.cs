﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using System.Threading;
using System.Windows.Forms;
using JulMar.Tapi3;

namespace TelProtocolHandler {
    internal class Program {

        private static string Protocol = "tel:";
        private static XElement xmlFile;

        private static void ConfigTAPILine () {
            Application.Run(new SelectTAPIForm());
            xmlFile = XElement.Load(@"Config.xml");
            return;
        }

        [STAThread]
        private static void Main (string[] args) {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length < 1) {
                Debug.WriteLine("No arguments given.");
                if (MessageBox.Show("No arguments given.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error) == DialogResult.OK) { }
                return;
            }
            else {
                string phoneNumberInput = args[0];

                if (!phoneNumberInput.StartsWith(Protocol)) {
                    Debug.WriteLine(String.Format("Unexpected input. Expected argument to start with '{0}'.", Protocol));
                    if (MessageBox.Show(String.Format("Unexpected input. Expected argumet to start with '{0}'", Protocol), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error) == DialogResult.OK) { }
                    return;
                }

                
                string phoneNumber = phoneNumberInput.Substring(Protocol.Length);

                // Set line configuration value
                TTapi tapi = new TTapi();
                tapi.Initialize();   

                xmlFile = XElement.Load(@"Config.xml");
                if (xmlFile.Element("lineToUse").Value == "") {     // Select line configuration value if none is set
                    Debug.WriteLine("No line configuration value set. Starting settings application...");
                    ConfigTAPILine();
                }

            AfterConfig:
                string lineToUse = xmlFile.Element("lineToUse").Value;
                Debug.WriteLine(String.Format("Line configuration value is '{0}'.", lineToUse));

                if (string.IsNullOrEmpty(lineToUse)) {
                    Debug.WriteLine("No TAPI line selected!");
                    if (MessageBox.Show("No TAPI line selected!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error) == DialogResult.OK) { }
                    return;
                }  

                // Replace + prefix with a 00
                if (phoneNumber.StartsWith("+")) {
                    phoneNumber = "00" + phoneNumber.Substring(1);
                }

                TAddress line = tapi.Addresses.SingleOrDefault(a => a.AddressName == lineToUse);
                if (null == line) {
                    Debug.WriteLine(String.Format("Unable to find TAPI line with name '{0}'!", lineToUse));
                    if (MessageBox.Show(String.Format("Unable to find TAPI line with name '{0}'!\nDo you wish to select another TAPI line?", lineToUse), "Error", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes) {
                        ConfigTAPILine();
                        goto AfterConfig;
                    }
                    return;
                }

                // Always assumes 0 prefix is needed to dial out.
                TCall call = line.CreateCall("0" + phoneNumber, LINEADDRESSTYPES.PhoneNumber, TAPIMEDIATYPES.AUDIO);
                try {
                    call.Connect(false);
                }
                catch (TapiException ex) {
                    Debug.WriteLine(ex.Message);
                    return;
                }

                Debug.WriteLine(String.Format("Calling '{0}'...", phoneNumber));
            }            
        }
    }
}