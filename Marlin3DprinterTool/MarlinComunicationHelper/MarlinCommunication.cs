﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Marlin3DprinterToolConfiguration;
using ZylSoft.Serial;

namespace MarlinComunicationHelper
{
    /// <summary>
    /// Class that do all communication thru ZylSerial 
    /// </summary>
    public sealed class MarlinCommunication
    {

        
        private readonly SerialPort _serialPort = new SerialPort();
        private FrmShowCommunication _showcom;

        private string linuxNewline = "\n";
        private bool _kill;

        #region Properties

        /// <summary>
        /// Used to communicate the ongoing feature. 
        /// Use done when the feature is finished
        /// </summary>
        public enum Feature
        {
            /// <summary>
            /// Done = The feature is Done
            /// </summary>
            Done = 0,
            /// <summary>
            /// The Feature is testing Endstops
            /// </summary>
            EndStop = 1,
            /// <summary>
            /// Feature is trying to do a Bedlevel
            /// </summary>
            Bedlevel = 2,
            /// <summary>
            /// Doing Surface Scan
            /// </summary>
            SurfaceScan = 3,
            /// <summary>
            /// Process that do AutoBedLevel
            /// </summary>
            AutoBedLevel = 4,
            /// <summary>
            /// Search for where the Probe touch the bed
            /// </summary>
            DockZprobe = 5,
            
            /// <summary>
            /// Auto Tune PID for Exctruder
            /// </summary>
            AutoPidTuneExtruder = 6,
            /// <summary>
            /// Auto Tune PID for Bed
            /// </summary>
            AutoPidTuneBed = 7,
            /// <summary>
            /// Start of extruder Calibration = Heating up
            /// </summary>
            ExtruderCalibration = 8,
            /// <summary>
            /// Start of test extrudsion of 100mm filament
            /// </summary>
            ExtruderCalibrationTest = 9
        }
        


        /// <summary>
        /// If the communication is Sending and/or Receiving data
        /// </summary>
        public bool IsReceivingOrSending { get; set; }
        

        /// <summary>
        /// The ZprobeOffset that is found in M851
        /// </summary>
        public decimal M851ZprobeOffset { get; set; }

        /// <summary>
        /// The Steps per Unit X that is found during INIT
        /// </summary>
        public string StepsPerUnitX { internal get; set; }

        /// <summary>
        /// The Steps per Unit Y that is found during INIT
        /// </summary>
        public string StepsPerUnitY { internal get; set; }

        /// <summary>
        /// The Steps per Unit Z that is found during INIT
        /// </summary>
        public string StepsPerUnitZ { internal get; set; }

        /// <summary>
        /// The Steps per Unit E that is found during INIT
        /// </summary>
        public string StepsPerUnitE { get; set; }

        /// <summary>
        /// The PidExtruder Kp that is found during INIT
        /// </summary>
        public string PidExtruderKp { internal get; set; }

        /// <summary>
        /// The PidExtruder Ki that is found during INIT
        /// </summary>
        public string PidExtruderKi { internal get; set; }

        /// <summary>
        /// The PidExtruder Kd that is found during INIT
        /// </summary>
        public string PidExtruderKd { internal get; set; }


        /// <summary>
        /// The PidBed Kp that is found during INIT
        /// </summary>
        public string PidBedKp { internal get; set; }

        /// <summary>
        /// The PidBed Ki that is found during INIT
        /// </summary>
        public string PidBedKi { internal get; set; }

        /// <summary>
        /// The PidBed Kd that is found during INIT
        /// </summary>
        public string PidBedKd { internal get; set; }

        /// <summary>
        /// The mean value of a M48
        /// </summary>
        public string ProbeM48MeanValue { get; internal set; }

        /// <summary>
        /// The feature that is processing
        /// </summary>
        public Feature Status { get; set; }

        /// <summary>
        /// The current position that are parsed from many GCODE
        /// </summary>
        public Position CurrentPosition { get; internal set; }

        /// <summary>
        /// The staus of all endstops
        /// </summary>
        public EndStop EndStopStatus { get; set; }


        /// <summary>
        ///     Kill the commandstream at next command
        /// </summary>
        public bool Kill
        {
            private get { return _kill; }
            set
            {
                _kill = value;
                if (_kill)
                {
                    //TODO: TypeOfCursor cursorType = new TypeOfCursor(Cursors.Default);
                    //TODO: OnSending(cursorType);
                    SendCommand("M P50"); // Send a short wait    
                }
                
            }

        }

        /// <summary>
        ///     The Gcode without argument
        /// </summary>
        public string Gcode { internal get; set; }

        /// <summary>
        ///     Baudrate
        /// </summary>
        public string BaudRate { get; set; }

        /// <summary>
        ///     Set the serial com port from a string like COM10
        /// </summary>
        public string Port
        {
            get { return _serialPort.SerialCommPortToString(_serialPort.Port); }
            set { _serialPort.Port = SerialPort.StringToSerialCommPort(value); }
        }


        /// <summary>
        /// All the proberesponces from a features ( many G30 )
        /// </summary>
        public List<Position> ProbeResponceList { get; set; }

        /// <summary>
        ///     If the serial port is open
        /// </summary>
        public bool IsPortOpen { get; private set; }



        /// <summary>
        /// Show a form of the communication and allow send commands
        /// </summary>
        public FrmShowCommunication Showform
        {
            get { return _showcom; }
            set
            {
                _showcom = value;
                _showcom = FrmShowCommunication.InstanceFrmShowForm;

                _showcom.Show();
            }
        }

        #endregion




        



        #region Parse Received Data

        private void ParseInit(string dataReceived)
        {
            IsPortOpen = true;

            // Create INIT responce Event
            var responce = new ResponceData(dataReceived);

            ParseM851(dataReceived);


            OnInit(responce);

            //Delete the responce from the received bytes
            dataReceived = string.Empty;


            
        }

        private void ParseG28(string dataReceived)
        {
            // Return if The _dataReceived not contains ok\n
            //if (WaitForOkAndNewLineToBeReceived() == false) return;


            // Get all responces
            //var responces = GetAllResponces();

            //TODO: Get the current position???

           


        }


        private void ParseG29(string dataReceived)
        {
            

            // Get all responces
            var responces = dataReceived.Split('\n');


            // X:0.00 Y:0.00 Z:5.00 E:0.00 Count X: 0 Y:0 Z:16000
            foreach (var row in responces)
            {
                var positionPattern =
                    @"X:[0-9]*\.[0-9]*\s*Y:[0-9]*\.[0-9]*\s*Z:[0-9]*\.[0-9]*\s*E:[0-9]*\.[0-9]*\s*Count\s*X:\s*";
                var positionMatch = Regex.Match(row, positionPattern);
                if (positionMatch.Success)
                {
                    var x =
                        (double) Convert.ToDecimal(Regex.Match(row, @"(?<=X:)[0-9]*\.[0-9]*").Value.Replace('.', ','));
                    var y =
                        (double) Convert.ToDecimal(Regex.Match(row, @"(?<=Y:)[0-9]*\.[0-9]*").Value.Replace('.', ','));
                    var z =
                        (double) Convert.ToDecimal(Regex.Match(row, @"(?<=Z:)[0-9]*\.[0-9]*").Value.Replace('.', ','));
                    ProbeResponceList.Add(new Position {X = x, Y = y, Z = z});
                }
            }


            // Create G29 responce Event
            var eventG29Resonce = new Responce(new List<string>());
            eventG29Resonce.ResponsRowList.AddRange(responces);

            OnG29Responce(eventG29Resonce);


            
            
        }

        private void ParseG30(string dataReceived)
        {
           
            // Get all responces
            var responces = dataReceived.Split('\n');



            var probePosition = new Position();
            string xstring = null;
            string ystring = null;
            string zstring = null;

            // Create G30 responce Event
            foreach (var responce in responces)
            {
                if (responce.Contains("Bed"))
                {
                    
                    try
                    {
                        // Bed X: 170.00 Y: 180.00 Z: -0.98
                        // (?: Z:\s[-] *[0 - 9] *.[0 - 9] *\s.*X:)([0 - 9] *.[0 - 9] *)


                        xstring =
                            Regex.Match(responce, @"(?:X:\s)([-]*[0-9]*.[0-9]*)", RegexOptions.CultureInvariant).Groups[
                                1].Value;
                        ystring =
                            Regex.Match(responce, @"(?:Y:\s)([-]*[0-9]*.[0-9]*)", RegexOptions.CultureInvariant).Groups[
                                1].Value;
                        zstring =
                            Regex.Match(responce, @"(?:Z:\s)([-]*[0-9]*.[0-9]*)", RegexOptions.CultureInvariant).Groups[
                                1].Value;
                    }
                    catch (Exception parsException)
                    {
                        MessageBox.Show($"Error in parser of G30. Error: {parsException.Message}");
                    }

                    
                    if (zstring != null)
                    {
                        probePosition.Zstring = zstring;
                    }
                    if (ystring != null)
                    {
                        probePosition.Ystring = ystring;
                    }
                    if (xstring != null)
                    {
                        probePosition.Xstring = xstring;
                    }
                    
                }
                

                
            }

            ProbeResponceList.Add(probePosition);

            // Create G30 responce Event
            OnG30ProbeResponce(ProbeResponceList);

            
            
        }

        private void ParseM48(string dataReceived)
        {
           
            // Get all responces
            var responces = dataReceived.Split('\n');


            //ok
            //ok
            //ok
            //M48 Z - Probe Repeatability test.   Version 2.00
            //Full support at: http://3dprintboard.com/forum.php
            //Positioning probe for the test.
            //1 of 4   z: 8.705000 mean: 8.705000   sigma: 0.000000
            //2 of 4   z: 8.706875 mean: 8.705938   sigma: 0.000937
            //3 of 4   z: 8.705313 mean: 8.705730   sigma: 0.000820
            //4 of 4   z: 8.707813 mean: 8.706251   sigma: 0.001148
            //Mean: 8.706251
            //Standard Deviation: 0.001148
            //            ok
            //            echo:endstops hit:  Z:8.71
            //            ok

            // The M48 ends with a calculation of all probes test.
            // The row begins with Mean: and then the calculated value 
            // (?<=^Mean:\s*)([-,0-9]*\.[0-9]*)

            try
            {
                var meanProbeValue =
                    Regex.Match(dataReceived, @"(?<=Mean:\s*)([-,0-9]*\.[0-9]*)", RegexOptions.CultureInvariant).Groups[1]
                        .Value;
                ProbeM48MeanValue = meanProbeValue;
            }
            catch (Exception parsException)
            {
                MessageBox.Show($"Error in parser of M48. Error: {parsException.Message}");
            }

            // Create M48 responce Event
            var eventM48Resonce = new Responce(new List<string>());
            eventM48Resonce.ResponsRowList.AddRange(responces);

            OnM48ProbeStatus(eventM48Resonce);

            
            
        }

        private void ParseM114(string dataReceived)
        {
            
            // Get all responces 
            var responces = dataReceived.Split('\n');

            // Create M114 responce Event
            foreach (var responce in responces)
            {
                // X:0.00 Y:0.00 Z:5.00 E:0.00 Count X: 0 Y:0 Z:4000
                if (Regex.Match(responce, @"\AX:[0-9,-,.]*\s*Y:[0-9,-,.]*\s*Z:[0-9,-,.]*\s*E:[0-9,-,.]*\s*Count X:\s*[0-9,-]*\s*Y:[0-9,-]*\s*Z:[0-9,-]*").Success)
                {
                    var xstring =
                        Regex.Match(responce, @"(?:X:)([-,0-9]*.[0-9]*)", RegexOptions.CultureInvariant).Groups[1].Value;
                    var ystring =
                        Regex.Match(responce, @"(?:Y:)([-,0-9]*.[0-9]*)", RegexOptions.CultureInvariant).Groups[1].Value;
                    var zstring =
                        Regex.Match(responce, @"(?:Z:)([-,0-9]*.[0-9]*)", RegexOptions.CultureInvariant).Groups[1].Value;
                    //var estring = Regex.Match(responce, @"(?:E:)([0-9]*.[0-9]*)", RegexOptions.CultureInvariant).Groups[1].Value; // Not Needed (yet)
                    var currentPosition = new CurrentPosition(xstring, ystring, zstring);
                    CurrentPosition.Xstring = xstring;
                    CurrentPosition.Ystring = ystring;
                    CurrentPosition.Zstring = zstring;
                    OnM114GetCurrentPosition(currentPosition);
                }


                
               
            }
        }

        private void ParseM119(string dataReceived)
        {
            
            // Get all responces 
            var responces = dataReceived.Split('\n');



            foreach (var responce in responces)
            {
                if (responce.Contains("x_min")) EndStopStatus.Xmin = responce.ToLower().Contains("triggered");
                if (responce.Contains("x_max")) EndStopStatus.Xmax = responce.ToLower().Contains("triggered");
                if (responce.Contains("y_min")) EndStopStatus.Ymin = responce.ToLower().Contains("triggered");
                if (responce.Contains("y_max")) EndStopStatus.Ymax = responce.ToLower().Contains("triggered");
                if (responce.Contains("z_min")) EndStopStatus.Zmin = responce.ToLower().Contains("triggered");
                if (responce.Contains("z_max")) EndStopStatus.Zmax = responce.ToLower().Contains("triggered");
            }







            
            OnM119EndStopStatus(EndStopStatus);

            
            
        }

        private void ParseM301(string dataReceived)
        {
            
            var responceData = new ResponceData(dataReceived);
            OnM301Responce(responceData);

            
        }

        private void ParseM303(string dataReceived)
        {
            var responceData = new ResponceData(dataReceived);
            OnM303Responce(responceData);

           
            
        }

        private void ParseM304(string dataReceived)
        {
            
            var responceData = new ResponceData(dataReceived);
            OnM304Responce(responceData);

            
        }

        private void ParseM500(string dataReceived)
        {
            var responceData = new ResponceData(dataReceived);
            OnM500Responce(responceData);


            
        }

        private void ParseM501(string dataReceived)
        {
            ParseInit(dataReceived);
            var responceData = new ResponceData(dataReceived);

            OnM501Responce(responceData);

            
        }

        private void ParseM851(string dataReceived)
        {
            var m851Pattern = @"(?<=M851\sZ)[-|.|0-9]*";

            var m851Match = Regex.Match(dataReceived, m851Pattern);
            if (m851Match.Success)
            {
                M851ZprobeOffset = Convert.ToDecimal(m851Match.Value.Replace('.', ','));
                OnM851Responce(new ResponceData(m851Match.Value.Replace(',', '.')));
            }

            
        }

        private void ParseDefault(string dataReceived)
        {
            
            
        }

       

       

        

        #endregion


        #region Parse Temperature / Event on Temperatures

        private bool ParseTemperatures(string temperatureLine)
        {
            var pattern = @"\A\s*T:[0-9,-,.]*\s*/[0-9,.]*\s*B:[0-9,.]*\s/[0-9,.]*\s@:[0-9,.]*\s*B@:[0-9,.]*";

            if (Regex.Match(temperatureLine, pattern).Success)
            {
                var extruderString =
                    Regex.Match(temperatureLine, @"(?:T:)([0-9]*.[0-9]*)", RegexOptions.CultureInvariant).Groups[1]
                        .Value
                        .Replace(".", ",");
                if (!extruderString.Contains(","))
                {
                    extruderString += ",0";
                }
                var setExtruderString =
                    Regex.Match(temperatureLine, @"(?:T:[0-9]*.[0-9]*\s\/)([0-9]*.[0-9]*)",
                        RegexOptions.CultureInvariant)
                        .Groups[1].Value.Replace(".", ",");
                if (!setExtruderString.Contains(","))
                {
                    setExtruderString += ",0";
                }
                var heatbedTempString =
                    Regex.Match(temperatureLine, @"(?:B:)([0-9]*.[0-9]*)", RegexOptions.CultureInvariant).Groups[1]
                        .Value
                        .Replace(".", ",");
                if (!heatbedTempString.Contains(","))
                {
                    heatbedTempString += ",0";
                }
                var heatbedSetString =
                    Regex.Match(temperatureLine, @"(?:B:[0-9]*.[0-9]*\s\/)([0-9]*.[0-9]*)",
                        RegexOptions.CultureInvariant)
                        .Groups[1].Value.Replace(".", ",");
                if (!heatbedSetString.Contains(","))
                {
                    heatbedSetString += ",0";
                }
                var extruder2TempString =
                    Regex.Match(temperatureLine, @"(?:T0:)([0-9]*.[0-9]*)", RegexOptions.CultureInvariant).Groups[1]
                        .Value
                        .Replace(".", ",");
                if (!extruder2TempString.Contains(","))
                {
                    extruder2TempString += ",0";
                }
                var extruder2SetString =
                    Regex.Match(temperatureLine, @"(?:T0:[0-9]*.[0-9]*\s\/)([0-9]*.[0-9]*)",
                        RegexOptions.CultureInvariant)
                        .Groups[1].Value.Replace(".", ",");
                if (!extruder2SetString.Contains(","))
                {
                    extruder2SetString += ",0";
                }

                Temperatures eventTemperatures = new Temperatures(
                    Convert.ToDouble(extruderString),
                    Convert.ToDouble(setExtruderString),
                    Convert.ToDouble(heatbedTempString),
                    Convert.ToDouble(heatbedSetString),
                    Convert.ToDouble(extruder2TempString),
                    Convert.ToDouble(extruder2SetString)
                    );



                OnTemperature(eventTemperatures);


                return true;
            }
            else return false;
        }

        
       

        /// <summary>
        ///     Eventhandler for Tempertatures
        /// </summary>
        public event EventHandler<Temperatures> Temperatures;

        private void OnTemperature(Temperatures temperatures)
        {
            var handler = Temperatures;
            handler?.Invoke(this, temperatures);
        }
        #endregion

        #region Connect / DisConnect Serialport


        /// <summary>
        /// Connect and get the INIT without events..
        /// Timeout on first responce is 10s
        /// The following has 4s (Test of SD is slow)
        /// 
        /// </summary>
        public void Connect()
        {
            if (_serialPort.ConnectedTo.ToString() == "None")
            {
                try
                {
                    //TODO: TypeOfCursor cursorType = new TypeOfCursor(Cursors.WaitCursor);
                    //TODO: OnSending(cursorType);
                    IsPortOpen = false;
                    _serialPort.Disconnected += _serialPort_Disconnected;
                    _serialPort.Connected += _serialPort_Connected;


                    _serialPort.CustomBaudRate = Convert.ToUInt32(BaudRate);
                    _serialPort.BaudRate = SerialBaudRate.Custom;


                    // 8 batabits No parity 1 Stop Bit
                    _serialPort.DataWidth = SerialDataWidth.Dw8Bits;
                    _serialPort.ParityBits = SerialParityBits.None;
                    _serialPort.StopBits = SerialStopBits.Sb1Bit;
                    _serialPort.AutoReceive = false;

                    CurrentPosition = new Position();
                    EndStopStatus = new EndStop();
                    ProbeResponceList = new List<Position>();

                    // Activate a true licence
                    var config = new Configuration();

                    if (!string.IsNullOrEmpty(Configuration.Decrypt(config.LicenseKey)))
                    {
                        _serialPort.UnlockKey = "FA3450FEA2344897EFC34325BA391072";
                    }


                    _serialPort.NewLine = "\n";
                    _serialPort.AutoReceive = false;
                    _serialPort.Open();

                    string dataReceived = "";
                    var rec = _serialPort.ReadLine(10); //TODO: Config of initial timeout
                    while (true)
                    {
                        var lastreceived = _serialPort.LastTimeReceived;

                        dataReceived += rec + Environment.NewLine;

                        var timeDiff = DateTime.Now - lastreceived;
                        if (timeDiff.Seconds >= 4) break; //TODO: Config of timeout
                        rec = _serialPort.ReadLine(4);
                    }

                    if (!_serialPort.IsConnected()) return;
                    ParseInit(dataReceived);
                    //TODO: cursorType = new TypeOfCursor(Cursors.Default);
                    //TODO: OnSending(cursorType);


                }
                catch (Exception serialOpenException)
                {
                    MessageBox.Show($"The serialport {_serialPort.Port} can´t be opend!\n\n" +
                                    $"({serialOpenException.Message})"
                        , @"Cant open the Serial port", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }


        /// <summary>
        /// Close the Comport and wait for it to be done
        /// </summary>
        public void DisConnect()
        {
            if (IsPortOpen)
            {
                _serialPort.Close();

                for (var i = 0; i < 10; i++)
                {
                    if (IsPortOpen) return;
                    Thread.Sleep(100);
                }
            }
        }

        private void _serialPort_Disconnected(object sender, ConnectionEventArgs e)
        {
            IsPortOpen = false;

            OnDisConnect(e);
        }

        private void _serialPort_Connected(object sender, ConnectionEventArgs e)
        {
            Gcode = "INIT";

            _serialPort.ClearInputBuffer();

            
            OnConnected(e);
        }


        /// <summary>
        /// Get the existing Comports
        /// </summary>
        /// <returns></returns>
        public string[] GetExistingSerialPorts()
        {
            return SerialPort.GetExistingCommPortNames();
        }


        /// <summary>
        /// Clear the communication
        /// </summary>
        public void ClearCommunication()
        {
            if (IsPortOpen)
            {
                _serialPort.ClearInputBuffer();
                _serialPort.ClearOutputBuffer();
            }
        }
        #endregion

        #region SendCommand

        /// <summary>
        /// Send a single GCODE. Check for each command if Kill signal is received
        /// </summary>
        /// <param name="command"></param>
        public void SendCommand(string command)
        {

            List<string> commands = new List<string>();
            commands.Add(command);
            SendCommand(commands);
        }

       




        /// <summary>
        /// Send a list of commands . Each command uses SendCommand with the single GCODE
        /// </summary>
        /// <param name="commands"></param>
        public void SendCommand(List<string> commands)
        {


            if (!IsReceivingOrSending)
            {
                BackgroundWorker serialBackgroundWorker = new BackgroundWorker();
                serialBackgroundWorker.DoWork += SerialBackgroundWorker_DoWork;
                serialBackgroundWorker.WorkerSupportsCancellation = true;
                serialBackgroundWorker.RunWorkerAsync(commands);
            }


        }

        private void SerialBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            //TODO: TypeOfCursor cursorType = new TypeOfCursor(Cursors.WaitCursor);


            if (Status != Feature.EndStop) // Reuce flicker cursor
            {
                //TODO: OnSending(cursorType);
            }
            

            IsReceivingOrSending = true;
            List<string> commands = (List<string>)e.Argument;


            foreach (string command in commands)
            {
                // For the technically minded, Gcode line endings are Unix Line Endings (\n), 
                // but will accept Windows Line Endings (\r\n), so you should not need to worry
                // about converting between the two, but it is best practice to use Unix Line Endings where possible.
                if (IsPortOpen)
                {



                    // Stop executing commands on backgrounworkerCancel or KILL
                    if (Kill || (worker != null && worker.CancellationPending))
                    {
                        // Kill the printer
                        _serialPort.SendAsciiString("M112" + linuxNewline);
                        Thread.Sleep(3000);
                        //Dissconnect
                        _serialPort.Close();
                        break;
                    }



                    if (Showform != null)
                    {
                        _showcom.AddSend = command;
                    }
                    // Extract the GCODE command without parameters
                    Gcode = command.IndexOf(" ", StringComparison.Ordinal) > 0
                        ? command.Substring(0, command.IndexOf(" ", StringComparison.Ordinal)).Trim()
                        : command;


                    // Send the command
                    //TODO: _serialPort.SendAsciiStringLine()
                    _serialPort.SendAsciiString(command + linuxNewline);





                    switch (Gcode)
                    {
                        case "G28":

                            ParseG28(ReceiveDataUntilOk(30));
                            break;
                        case "G29":
                            ParseG29(ReceiveDataUntilOk(30));
                            break;
                        case "G30":
                            ParseG30(ReceiveDataUntilOk(30));
                            break;
                        case "M114":
                            ParseM114(ReceiveDataUntilOk(30));
                            break;

                        case "M119":
                            Thread.Sleep(100);
                            ParseM119(ReceiveDataUntilOk(30));
                            break;

                        case "M48":
                            ParseM48(ReceiveDataUntilOk(30));
                            break;

                        case "M301":
                            ParseM301(ReceiveDataUntilOk(30));
                            break;


                        case "M303":
                            ParseM303(ReceiveDataUntilOk(30));
                            break;

                        case "M304":
                            ParseM304(ReceiveDataUntilOk(30));
                            break;

                        case "M500":
                            ParseM500(ReceiveDataUntilOk(30));
                            break;

                        case "M501":
                            ParseM501(ReceiveDataUntilOk(30));
                            break;

                        default:
                            ParseDefault(ReceiveDataUntilOk(30));
                            break;
                    }





                }
            }

            IsReceivingOrSending = false;


            OnCommandSequenceeDone(EventArgs.Empty);

            //TODO: cursorType = new TypeOfCursor(Cursors.Default);
            //TODO: OnSending(cursorType);
            

        }

        private string ReceiveDataUntilOk(int timeout)
        {
            // Read all bytes in the buffer


            string dataReceived = null;
           
            //string dataReceived = _serialPort.ReadStringUpToEndChars("ok\n", timeout);

            while (true)
            {
                string line = _serialPort.ReadLine(timeout); //Delete all "Busy ..."

                if (line.ToLower().Contains("busy"))
                {
                    continue;
                }

                if (ParseTemperatures(line)) continue; // Delete all temperatures

                dataReceived += line + linuxNewline;
                
                ParseM114(line);

                switch (Gcode)
                {
                    case "M303":
                        var responceData = new ResponceData(dataReceived);
                        OnM303Responce(responceData);
                        break;
                    case "G30":
                        OnG30ProbeResponce(ProbeResponceList);
                        break;
                }

                
                if (dataReceived.ToLower().Contains("ok")) break;
            }



            if (Showform != null)
            {
                _showcom.AddReceived = dataReceived;
            }


            return dataReceived;
        }
        #endregion


        private double CalculateExtrusionSteps(int extrude, double oldSteps, int meassuredUsedFilament)
        {
            // Extruder steps/mm = ( Testextruded length * old extruder steps/mm ) / used length on filament
            return Math.Round(extrude*oldSteps/meassuredUsedFilament, 3);
        }


        /// <summary>
        ///     Update the uploaded Firmware with new calibrated value
        /// </summary>
        /// <param name="extrude">Amount of extruded filament in the test</param>
        /// <param name="oldSteps">Value for Steps-per-mm before the test</param>
        /// <param name="meassuredUsedFilament">Meassured filament that is used during the test</param>
        public void ExtrudeCalculationUpdate(int extrude, string oldSteps, int meassuredUsedFilament)
        {

            double currentStepsPermm = Convert.ToDouble(oldSteps.Replace(".", ","));

            var commands = new List<string>
            {
                $"M92 E{CalculateExtrusionSteps(extrude, currentStepsPermm, meassuredUsedFilament)}" + Environment.NewLine
            };


            DialogResult result =
                MessageBox.Show(@"The old Steps-Per-mm.....: " + oldSteps + Environment.NewLine +
                                @"Extruded filament........: " + extrude + Environment.NewLine +
                                @"Meassured filament.......: " + meassuredUsedFilament + Environment.NewLine +
                                @"Recalculated Steps-per-mm: " + CalculateExtrusionSteps(extrude, currentStepsPermm, meassuredUsedFilament) + Environment.NewLine +
                                Environment.NewLine +
                                @"Do you want to update EEPROM?",@"ReCalculation of Steps-per-mm",MessageBoxButtons.YesNoCancel,MessageBoxIcon.Question,MessageBoxDefaultButton.Button1);
                
            if (result == DialogResult.Cancel) return;
            if (result == DialogResult.Yes)
            {
                commands.Add("M500");
                commands.Add("M501");
            }

            SendCommand(commands);
        }

        

        #region Connected
        /// <summary>
        /// Event when the ZylSerial is connected
        /// </summary>
        public event EventHandler Connected;

        private void OnConnected(ConnectionEventArgs e)
        {
            var handler = Connected;
            handler?.Invoke(this, e);
        }


        /// <summary>
        /// Event when ZylSerial is disConnected
        /// </summary>
        public event EventHandler DisConnected;

        private void OnDisConnect(ConnectionEventArgs e)
        {
            var handler = DisConnected;
            handler?.Invoke(this, e);
        }

        #endregion

        #region INIT
        /// <summary>
        /// Handle Init event that rise after startup of Marlin FW
        /// </summary>
        public event EventHandler<ResponceData> Init;

        private void OnInit(ResponceData responce)
        {
            var handler = Init;
            handler?.Invoke(this, responce);
        }

        #endregion

        #region G29Responce
        /// <summary>
        /// Handle event that rise after G29
        /// </summary>
        public event EventHandler<Responce> G29Responce;

        /// <summary>
        /// Handle event that rise after G29
        /// </summary>
        private void OnG29Responce(Responce responce)
        {
            var handler = G29Responce;
            handler?.Invoke(this, responce);
        }

        #endregion

        #region G30ProbeResponce 
        /// <summary>
        /// Handle event that rise after G30
        /// </summary>
        public event EventHandler<List<Position>> G30ProbeResponce;

        private void OnG30ProbeResponce(List<Position> probeResponce)
        {
            var handler = G30ProbeResponce;
            handler?.Invoke(this, probeResponce);
        }

        #endregion

        #region M48ProbeStatus
        /// <summary>
        /// Handle event that rise after M48
        /// </summary>
        public event EventHandler<Responce> M48ProbeStatus;

        /// <summary>
        /// Handle event that rise after M48
        /// </summary>
        private void OnM48ProbeStatus(Responce probeResponceList)
        {
            var handler = M48ProbeStatus;
            handler?.Invoke(this, probeResponceList);
        }

        #endregion

        #region M114 Get current position
        /// <summary>
        /// Handle event that rise after M114
        /// </summary>
        public event EventHandler<CurrentPosition> M114GetCurrentPosition;

        /// <summary>
        /// Handle event that rise after M114
        /// </summary>
        private void OnM114GetCurrentPosition(CurrentPosition currentPosition)
        {
            var handler = M114GetCurrentPosition;
            handler?.Invoke(this, currentPosition);
        }

        #endregion

        #region M119EndstopStatus

        /// <summary>
        ///     Eventhandler for M119 EndstopStatus
        /// </summary>
        /// TODO: EndstopStatusList -> EndstopStatus
        public event EventHandler<EndStop> M119EndStopStatus;

        /// <summary>
        ///     Report result of M119 EndstopStatus
        /// </summary>
        /// <param name="endstopStatusList"></param>
        /// TODO: EndstopStatusList -> EndstopStatus
        private void OnM119EndStopStatus(EndStop endstopStatusList)
        {
            var handler = M119EndStopStatus;
            handler?.Invoke(this, endstopStatusList);
        }

        #endregion

        #region M301Responce

        /// <summary>
        ///     Eventhandler for M301
        /// </summary>
        public event EventHandler<ResponceData> M301Responce;

        private void OnM301Responce(ResponceData responce)
        {
            var handler = M301Responce;
            handler?.Invoke(this, responce);
        }

        #endregion

        #region M303Responce

        /// <summary>
        ///     Eventhandler for M303Responce
        /// </summary>
        public event EventHandler<ResponceData> M303Responce;

        private void OnM303Responce(ResponceData responce)
        {
            var handler = M303Responce;
            handler?.Invoke(this, responce);
        }

        #endregion

        #region M304Responce

        /// <summary>
        ///     Eventhandler for M304
        /// </summary>
        public event EventHandler<ResponceData> M304Responce;

        private void OnM304Responce(ResponceData responce)
        {
            var handler = M304Responce;
            handler?.Invoke(this, responce);
        }

        #endregion

        #region M500Responce

        /// <summary>
        ///     Eventhandler for M500
        /// </summary>
        public event EventHandler<ResponceData> M500Responce;

        private void OnM500Responce(ResponceData responce)
        {
            var handler = M500Responce;
            handler?.Invoke(this, responce);
        }

        #endregion

        #region M501Responce

        /// <summary>
        ///     Eventhandler for M501
        /// </summary>
        public event EventHandler<ResponceData> M501Responce;

        private void OnM501Responce(ResponceData responce)
        {
            var handler = M501Responce;
            handler?.Invoke(this, responce);
        }



        #endregion

        #region M851Responce

        /// <summary>
        ///     Eventhandler for M501
        /// </summary>
        public event EventHandler<ResponceData> M851Responce;

        private void OnM851Responce(ResponceData responce)
        {
            var handler = M851Responce;
            handler?.Invoke(this, responce);
        }



        #endregion




        /// <summary>
        /// 
        /// </summary>
        public event EventHandler CommandSequenceeDone;

        private void OnCommandSequenceeDone(EventArgs e)
        {
            var handler = CommandSequenceeDone;
            handler?.Invoke(this, e);
        }


       
        //TODO: Cursor on Sending
        ///// <summary>
        ///// 
        ///// </summary>
        //public event EventHandler Sending;

        //private void OnSending(EventArgs e)
        //{
        //    var handler = Sending;
        //    handler?.Invoke(this, e);
        //}











}


    //TODO: Cursor 
    ///// <summary>
    ///// Generic responcedata
    ///// </summary>
    //public class TypeOfCursor : EventArgs
    //{
    //    /// <summary>
    //    /// Cursor during operations
    //    /// </summary>
    //    /// <param name="cursorType"></param>
    //    public TypeOfCursor(Cursor cursorType)
    //    {
    //        CursorType = cursorType;
    //    }

    //    /// <summary>
    //    /// Generic Responce Data
    //    /// </summary>
    //    public Cursor CursorType { get; }
    //}



    #region EndStop/EndstopStatus
    /// <summary>
    ///     Endstop Argument
    /// </summary>
    public class EndstopStatus : EventArgs
    {
        /// <summary>
        /// Endstop Status
        /// </summary>
        /// <param name="endstop"></param>
        public EndstopStatus(EndStop endstop)
        {
            Status = endstop;
        }

        /// <summary>
        /// The status for all endstops
        /// </summary>
        private EndStop Status { get; }
    }

    /// <summary>
    /// Endstop with the status of all endstops
    /// </summary>
    public class EndStop
    {
        /// <summary>
        /// status for Xmin
        /// </summary>
        public bool Xmin { get; set; }

        /// <summary>
        /// status for Xmax
        /// </summary>
        public bool Xmax { get; set; }

        /// <summary>
        /// status for Ymin
        /// </summary>
        public bool Ymin { get; set; }

        /// <summary>
        /// status for Ymax
        /// </summary>
        public bool Ymax { get; set; }

        /// <summary>
        /// status for Zmin
        /// </summary>
        public bool Zmin { get; set; }

        /// <summary>
        /// status for Zmax
        /// </summary>
        public bool Zmax { get; set; }



    }
    #endregion
    
    #region Temperature
    /// <summary>
    ///     Temperature
    /// </summary>
    public class Temperatures : EventArgs
    {
        /// <summary>
        /// Temperature divided to there source
        /// </summary>
        /// <param name="extruder"></param>
        /// <param name="setExtruder"></param>
        /// <param name="heatbed"></param>
        /// <param name="setHeatbed"></param>
        /// <param name="extruder2"></param>
        /// <param name="extruder2Set"></param>
        public Temperatures(double extruder, double setExtruder, double heatbed, double setHeatbed, double extruder2,
            double extruder2Set)
        {
            Extruder = extruder;
            SetExtruder = setExtruder;
            Heatbed = heatbed;
            SetHeatbed = setHeatbed;
            Extruder2 = extruder2;
            SetExtruder2 = extruder2Set;
        }

        /// <summary>
        /// Extruder temp
        /// </summary>
        public double Extruder { get; }
        /// <summary>
        /// Set extruder Temp
        /// </summary>
        public double SetExtruder { get; }

        /// <summary>
        /// Heatbed temp
        /// </summary>
        public double Heatbed { get; }
        /// <summary>
        /// Set heatbed Temp
        /// </summary>
        public double SetHeatbed { get; }

        /// <summary>
        /// Extruder2 Temp
        /// </summary>
        public double Extruder2 { get; }
        /// <summary>
        /// Set extruder2 Temp
        /// </summary>
        public double SetExtruder2 { get; }
    }
    #endregion
    
    #region ResponceData / generic responce
    /// <summary>
    /// Generic responcedata
    /// </summary>
    public class ResponceData : EventArgs
    {
        /// <summary>
        /// Generic Responce Data
        /// </summary>
        /// <param name="data"></param>
        public ResponceData(string data)
        {
            Data = data;
        }

        /// <summary>
        /// Generic Responce Data
        /// </summary>
        public string Data { get; }
    }


    /// <summary>
    /// Responce with many lines
    /// </summary>
    public class Responce : EventArgs
    {
        /// <summary>
        /// List of responce rows
        /// </summary>
        /// <param name="responceRowList"></param>
        public Responce(List<string> responceRowList)
        {
            ResponsRowList = responceRowList;
        }

        /// <summary>
        /// Responce list of rows
        /// </summary>
        public List<string> ResponsRowList { get; }
    }

    #endregion

    #region Current Position
    /// <summary>
    /// Current Position
    /// </summary>
    public class CurrentPosition : EventArgs
    {
        /// <summary>
        /// Current position where X/Y/Z is strings
        /// </summary>
        /// <param name="xposition"></param>
        /// <param name="yposition"></param>
        /// <param name="zposition"></param>
        public CurrentPosition(string xposition, string yposition, string zposition)
        {
            Xdouble = Convert.ToDouble(xposition.Replace(".", ","));
            Ydouble = Convert.ToDouble(yposition.Replace(".", ","));
            Zdouble = Convert.ToDouble(zposition.Replace(".", ","));
        }

        /// <summary>
        /// X as a Double
        /// </summary>
        public double Xdouble { get; }
        /// <summary>
        /// Y as a double
        /// </summary>
        public double Ydouble { get; }
        /// <summary>
        /// Z as a double
        /// </summary>
        public double Zdouble { get; }
    }
    #endregion
}