﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;
using PCEArgs = System.ComponentModel.PropertyChangedEventArgs;
using PCEHandler = System.ComponentModel.PropertyChangedEventHandler;


namespace register_programmer
{
    public partial class Form1 : Form
    {
        static private string ChooseFileCertainly(string initialDirectory, string errorMessage
            , string errorCaption, MessageBoxIcon icon, bool startWithErrorMessage
            , DialogResult buttonToExit, Func<string, bool> condition)
        {
            if (startWithErrorMessage)
                if (MessageBox.Show(errorMessage, errorCaption, MessageBoxButtons.YesNo, icon) 
                    == buttonToExit)
                    return null;

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = initialDirectory;

            while(true)
            {
                openFileDialog.ShowDialog();
                string filePath = openFileDialog.FileName;

                if (string.IsNullOrEmpty(filePath) || string.IsNullOrWhiteSpace(filePath))
                    if (MessageBox.Show(errorMessage, errorCaption, MessageBoxButtons.YesNo, icon)
                    == buttonToExit)
                        return null;
                    else
                        continue;

                string fileName = Path.GetFileName(filePath);

                if (string.IsNullOrEmpty(fileName) || string.IsNullOrWhiteSpace(fileName))
                    if (MessageBox.Show(errorMessage, errorCaption, MessageBoxButtons.YesNo, icon)
                    == buttonToExit)
                        return null;
                    else
                        continue;

                bool c = condition(fileName);
                if (!c)
                    if (MessageBox.Show(errorMessage, errorCaption, MessageBoxButtons.YesNo, icon)
                    == buttonToExit)
                        return null;
                    else
                        continue;

                return filePath;
            }
        }
        static private string GetArduinoPathFromEnvironment()
        {
            string pathVar =  System.Environment.GetEnvironmentVariable(ENV_VAR_NAME
                , EnvironmentVariableTarget.User);

            if (string.IsNullOrEmpty(pathVar) || string.IsNullOrWhiteSpace(pathVar))
            {
                string tempmsg, tempcap;
                if (pathVar is null)
                {
                    tempmsg = "There is no environment variable named " + ENV_VAR_NAME +" in your system. To use "
                        + PROGRAM_NAME + " you need to create an environment variable named " + ENV_VAR_NAME
                        + " and then add the path of your arduino.exe or arduino_debug.exe to the " + ENV_VAR_NAME +"."
                        + " Do you want to create one?";
                    tempcap = "No " + ENV_VAR_NAME +"!";
                }
                else
                {
                    tempmsg = "Your " + ENV_VAR_NAME + " environment variable is empty. You need to add the"
                        + " path of your arduino.exe or arduino_debug.exe to the " + ENV_VAR_NAME +". Do you"
                        + " want to add?";
                    tempcap = ENV_VAR_NAME + " is empty!";
                }

                if (MessageBox.Show(tempmsg, tempcap
                    , MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes)
                {
                    string filePath = ChooseFileCertainly(
                        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "You have not"
                        + " choosen any file. You should choose the arduino.exe file in your system. If you"
                        + " do not have Arduino IDE, install Arduino IDE and try again. You can do this"
                        + " later but we recommend you to do now. Click Yes if you want to choose now."
                        , "Choose arduino.exe", MessageBoxIcon.Error, false, DialogResult.No, (string name)
                        => true);
                    if (string.IsNullOrEmpty(filePath))
                        return null;

                    string fileName = Path.GetFileName(filePath);
                    bool wantsToContinue = false;

                    if (fileName != "arduino.exe" && fileName != "arduino_debug.exe")
                    {
                        string temp = ChooseFileCertainly(Path.GetDirectoryName(filePath), "Your file's name (" + fileName
                            + ") is not arduino.exe or arduino_debug.exe (arduino_debug.exe is"
                            + " strongly recommended for more functionality.). Do you want to still continue?"
                            + " Click No to replace the file.", "The file has an unexpected name"
                            , MessageBoxIcon.Warning, false, DialogResult.Yes, (string name)
                            => name == "arduino.exe" || name == "arduino_debug.exe");

                        if (!string.IsNullOrEmpty(temp))
                        {
                            filePath = temp;
                            fileName = Path.GetFileName(filePath);
                            wantsToContinue = true;
                        }
                    }

                    if (fileName == "arduino.exe" && !wantsToContinue)
                    {
                        string temp = ChooseFileCertainly(Path.GetDirectoryName(filePath), "arduino_debug.exe is strongly"
                            + " recommended for more functionality. Do you want to still continue? Click No"
                            + " to replace the file.", "arduino_debug.exe is recommended"
                            , MessageBoxIcon.Warning, true, DialogResult.Yes, (string name)
                            => name == "arduino_debug.exe" );

                        if (!string.IsNullOrEmpty(temp))
                        {
                            filePath = temp;
                            fileName = Path.GetFileName(filePath);
                        }
                    }

                    //filePath is ok
                    System.Environment.SetEnvironmentVariable("Path", filePath
                        , EnvironmentVariableTarget.User);
                    return filePath;
                }
                else
                    return null;
            }
            else
            {
                string candidate1 = null, candidate2 = null, candidate3 = null;
                foreach(string path in pathVar.Split(';'))
                {
                    string fileName = Path.GetFileName(path);

                    if (Directory.Exists(path))
                    {
                        foreach(string memberFile in Directory.EnumerateFiles(path))
                        {
                            if (fileName == "arduino_debug.exe")
                                candidate2 = path;
                            else if (fileName == "arduino.exe")
                                candidate3 = path;
                        }
                    }
                    else
                    {
                        if (fileName == "arduino_debug.exe")
                            return path;

                        if (fileName == "arduino.exe")
                            candidate1 = path;
                    }
                }
                if (!string.IsNullOrEmpty(candidate1)) return candidate1;
                if (!string.IsNullOrEmpty(candidate2)) return candidate2;
                if (!string.IsNullOrEmpty(candidate3)) return candidate3;

                //no arduino open a file dialog
                string filePath = null;
                if (MessageBox.Show("We could not locate arduino_debug.exe or arduino.exe in your " 
                    + ENV_VAR_NAME + ". Do you want to add it now?", "No arduino_debug.exe or arduino.exe in "
                    + ENV_VAR_NAME, MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes)
                    filePath = ChooseFileCertainly(
                        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "You have not"
                        + " choosen any file. You should choose the arduino.exe file in your system. If you"
                        + " do not have Arduino IDE, install Arduino IDE and try again. You can do this"
                        + " later but we recommend you to do now. Click Yes if you want to choose now."
                        , "Choose arduino.exe", MessageBoxIcon.Error, false, DialogResult.No, (string name)
                        => true);
                if (string.IsNullOrEmpty(filePath))
                    return null;

                string _fileName = Path.GetFileName(filePath);
                bool wantsToContinue = false;

                if (_fileName != "arduino.exe" && _fileName != "arduino_debug.exe")
                {
                    string temp = ChooseFileCertainly(Environment.GetFolderPath(
                        Environment.SpecialFolder.ProgramFiles), "Your file's name (" + _fileName
                        + ") is not arduino.exe or arduino_debug.exe (arduino_debug.exe is"
                        + " strongly recommended for more functionality.). Do you want to still continue?"
                        + " Click No to replace the file.", "The file has an unexpected name"
                        , MessageBoxIcon.Warning, true, DialogResult.Yes, (string name)
                        => name == "arduino.exe" || name == "arduino_debug.exe");

                    if (!string.IsNullOrEmpty(temp))
                    {
                        filePath = temp;
                        _fileName = Path.GetFileName(filePath);
                        wantsToContinue = true;
                    }
                }

                if (_fileName == "arduino.exe" && !wantsToContinue)
                {
                    string temp = ChooseFileCertainly(Path.GetDirectoryName(filePath), "arduino_debug.exe is strongly"
                        + " recommended for more functionality. Do you want to still continue? Click No"
                        + " to replace the file.", "arduino_debug.exe is recommended"
                        , MessageBoxIcon.Warning, true, DialogResult.Yes, (string name)
                        => name == "arduino_debug.exe");

                    if (!string.IsNullOrEmpty(temp))
                    {
                        filePath = temp;
                        _fileName = Path.GetFileName(filePath);
                    }
                }

                Environment.SetEnvironmentVariable("Path", pathVar + ";" + filePath
                    , EnvironmentVariableTarget.User);
                return filePath;
            }
        }
        static private bool CheckFolders()
        {
            bool wasThereProblems = false;

            if (!Directory.Exists(PROGRAM_APPDATA_PATH))
            {
                Directory.CreateDirectory(PROGRAM_APPDATA_PATH);
                wasThereProblems = true;
            }

            if (!Directory.Exists(SETTINGS_DIR))
            {
                Directory.CreateDirectory(SETTINGS_DIR);
                wasThereProblems = true;
            }

            if (!File.Exists(DEFAULT_SETTINGS_FILE))
            {
                #region settingsTree
                XElement settingsTree =
                    new XElement("Settings",
                        new XElement("Inputs",
                            new XElement("ReferenceInput", 122.8M.ToString()), 
                            new XElement("Divider", 1M.ToString()), 
                            new XElement("DivideBy2", true.ToString()), 
                            new XElement("Doubler", false.ToString()),
                            new XElement("ChannelSpacing", 1000M.ToString()),
                            new XElement("VCOOutput", 3600M.ToString()),
                            new XElement("OutputDividerIndex", 0.ToString())),
                        new XElement("Register0",
                            new XElement("AutoCalibration", true.ToString()),
                            new XElement("Prescaler", 0.ToString())),
                        new XElement("Register3",
                            new XElement("SDLoadReset", true.ToString()),
                            new XElement("PhaseResync", false.ToString()),
                            new XElement("PhaseAdjustment", false.ToString()),
                            new XElement("Phase", 0M.ToString())),
                        new XElement("Register4",
                            new XElement("MuxOutputIndex", 5.ToString()),
                            new XElement("DoubleBuffer", false.ToString()),
                            new XElement("CPCurrentIndex", 2.ToString()),
                            new XElement("ReferenceModeIsSingle", false.ToString()),
                            new XElement("MuxLevelIs18", false.ToString()),
                            new XElement("PDPolarityIsNegative", false.ToString()),
                            new XElement("PowerDown", false.ToString()), 
                            new XElement("CPThreeState", false.ToString()), 
                            new XElement("CounterReset", false.ToString())),
                        new XElement("Register6",
                            new XElement("FeedbackIndex", 1.ToString()),
                            new XElement("CPBleedCurrentInt", 36.ToString()),
                            new XElement("MuteTillLockDetect", false.ToString()), 
                            new XElement("AuxOutputEnable", false.ToString()),
                            new XElement("AuxOutputPower", (-1M).ToString()),
                            new XElement("RfOutputEnable", true.ToString()),
                            new XElement("RfOutputPower", 5M.ToString()),
                            new XElement("NegativeBleed", true.ToString()), 
                            new XElement("GatedBleed", false.ToString())),
                        new XElement("Register7",
                            new XElement("LESync", false.ToString()),
                            new XElement("LDCycleCountInt", 2.ToString()),
                            new XElement("LOLMode", true.ToString()),
                            new XElement("FracNPrecision", 3.ToString()),
                            new XElement("LDModeInt", 0.ToString())),
                        new XElement("Register9",
                            new XElement("AutosetFastestCalibration", false.ToString()),
                            new XElement("VCOBandDivisonInt", 52.ToString()),
                            new XElement("TimeoutInt", 25.ToString()),
                            new XElement("SynthLockTimeoutInt", 25.ToString()),
                            new XElement("TotalCalculatedTime", (148.958M).ToString())),
                        new XElement("Register10",
                            new XElement("ADCClockDividerInt", 154.ToString()), 
                            new XElement("ADCConversion", true.ToString()), 
                            new XElement("ADCEnable", true.ToString()), 
                            new XElement("ADCClockDividerAutoset", false.ToString())),
                        new XElement("Register12",
                            new XElement("ResyncClockInt", 1.ToString()),
                            new XElement("ResyncClockTimeout", (0.016M).ToString())), 
                        new XElement("EnvironmentVaribleName", "Path"));
                #endregion

                settingsTree.Save(DEFAULT_SETTINGS_FILE);

                wasThereProblems = true;
            }

            if (!Directory.Exists(INO_DIR))
            {
                Directory.CreateDirectory(INO_DIR);
                //wasThereProblems = true;
            }

            return wasThereProblems;
        }
        static private bool CheckFolders(string errorMessage, string caption)
        {
            bool wasThereProblems = CheckFolders();

            if (wasThereProblems)
                MessageBox.Show(errorMessage, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);

            return wasThereProblems;
        }
        private void LoadSettings()
        {
            XElement settingsTree = XElement.Load(DEFAULT_SETTINGS_FILE);
            XElement inputTree = settingsTree.Element("Inputs");
            XElement register0Tree = settingsTree.Element("Register0");
            XElement register3Tree = settingsTree.Element("Register3");
            XElement register4Tree = settingsTree.Element("Register4");
            XElement register6Tree = settingsTree.Element("Register6");
            XElement register9Tree = settingsTree.Element("Register9");
            XElement register10Tree = settingsTree.Element("Register10");
            XElement register12Tree = settingsTree.Element("Register12");
            XElement register7Tree = settingsTree.Element("Register7");

            #region other
            ENV_VAR_NAME = settingsTree.Element("EnvironmentVaribleName").Value;
            #endregion

            #region inputs
            this.Divider = decimal.Parse(inputTree.Element("Divider").Value);
            this.ReferenceInput = decimal.Parse(inputTree.Element("ReferenceInput").Value);
            this.Divideby2 = bool.Parse(inputTree.Element("DivideBy2").Value);
            this.Doubler = bool.Parse(inputTree.Element("Doubler").Value);
            this.Fchsp = decimal.Parse(inputTree.Element("ChannelSpacing").Value);
            this.Vco = decimal.Parse(inputTree.Element("VCOOutput").Value);
            this.OutputDividerIndex = int.Parse(inputTree.Element("OutputDividerIndex").Value);
            #endregion

            #region register 0
            this.Autocal = bool.Parse(register0Tree.Element("AutoCalibration").Value);
            this.Prescaler = int.Parse(register0Tree.Element("Prescaler").Value);
            #endregion
            #region register 3
            this.SDLoadReset = bool.Parse(register3Tree.Element("SDLoadReset").Value);
            this.PhaseResync = bool.Parse(register3Tree.Element("PhaseResync").Value);
            this.PhaseAdjustment = bool.Parse(register3Tree.Element("PhaseAdjustment").Value);
            this.Phase = decimal.Parse(register3Tree.Element("Phase").Value);
            #endregion
            #region register 4
            this.MuxOutIndex = int.Parse(register4Tree.Element("MuxOutputIndex").Value);
            this.DoubleBuffer = bool.Parse(register4Tree.Element("DoubleBuffer").Value);
            this.CPCurrentIndex = int.Parse(register4Tree.Element("CPCurrentIndex").Value);
            this.ReferenceModeIsSingle = bool.Parse(register4Tree.Element("ReferenceModeIsSingle").Value);
            this.MuxLevelIs18 = bool.Parse(register4Tree.Element("MuxLevelIs18").Value);
            this.PDPolarityIsNegative = bool.Parse(register4Tree.Element("PDPolarityIsNegative").Value);
            this.PowerDown = bool.Parse(register4Tree.Element("PowerDown").Value);
            this.CPThreeState = bool.Parse(register4Tree.Element("CPThreeState").Value);
            this.CounterReset = bool.Parse(register4Tree.Element("CounterReset").Value);
            #endregion
            #region register 6
            this.FeedBack = int.Parse(register6Tree.Element("FeedbackIndex").Value);
            this.CPBleedCurrentInt = int.Parse(register6Tree.Element("CPBleedCurrentInt").Value);
            this.MuteTillLockDetect = bool.Parse(register6Tree.Element("MuteTillLockDetect").Value);
            this.AuxOutEnable = bool.Parse(register6Tree.Element("AuxOutputEnable").Value);
            this.AuxPowerValue = int.Parse(register6Tree.Element("AuxOutputPower").Value);
            this.RfOutEnable = bool.Parse(register6Tree.Element("RfOutputEnable").Value);
            this.RfOutPowerValue = int.Parse(register6Tree.Element("RfOutputPower").Value);
            this.NegativeBleed = bool.Parse(register6Tree.Element("NegativeBleed").Value);
            this.GatedBleed = bool.Parse(register6Tree.Element("GatedBleed").Value);
            #endregion
            #region register 7
            this.LESync = bool.Parse(register7Tree.Element("LESync").Value);
            this.LDCycleCountInt = int.Parse(register7Tree.Element("LDCycleCountInt").Value);
            this.LOLMode = bool.Parse(register7Tree.Element("LOLMode").Value);
            this.FracNPrecisionInt = int.Parse(register7Tree.Element("FracNPrecision").Value);
            this.LDModeInt = int.Parse(register7Tree.Element("LDModeInt").Value);
            #endregion
            #region register 9
            this.VCOBandDivisonInt = int.Parse(register9Tree.Element("VCOBandDivisonInt").Value);
            this.TimeoutInt = int.Parse(register9Tree.Element("TimeoutInt").Value);
            this.SynthLockTimeoutInt = int.Parse(register9Tree.Element("SynthLockTimeoutInt").Value);
            this.TotalCalculatedTime = decimal.Parse(register9Tree.Element("TotalCalculatedTime").Value);
            this.FastestCalibration = bool.Parse(register9Tree.Element("AutosetFastestCalibration").Value);
            #endregion
            #region register 10
            this.ADCClockDividerInt = int.Parse(register10Tree.Element("ADCClockDividerInt").Value);
            this.ADCConversion = bool.Parse(register10Tree.Element("ADCConversion").Value);
            this.ADCEnable = bool.Parse(register10Tree.Element("ADCEnable").Value);
            this.ADCClockDividerAutoset = bool.Parse(register10Tree.Element("ADCClockDividerAutoset").Value);
            #endregion
            #region register 12
            this.ResyncClockInt = int.Parse(register12Tree.Element("ResyncClockInt").Value);
            this.ResyncClockTimeout = decimal.Parse(register12Tree.Element("ResyncClockTimeout").Value);
            #endregion

        }
        private void SaveSettings(string path)
        {
            #region settingsTree
            XElement settingsTree =
                new XElement("Settings",
                    new XElement("Inputs",
                        new XElement("ReferenceInput", this.ReferenceInput.ToString()),
                        new XElement("Divider", this.Divider.ToString()),
                        new XElement("DivideBy2", this.Divideby2.ToString()),
                        new XElement("Doubler", this.Doubler.ToString()),
                        new XElement("ChannelSpacing", this.Fchsp.ToString()),
                        new XElement("VCOOutput", this.Vco.ToString()),
                        new XElement("OutputDividerIndex", this.OutputDividerIndex.ToString())),
                    new XElement("Register0",
                        new XElement("AutoCalibration", this.Autocal.ToString()),
                        new XElement("Prescaler", this.Prescaler.ToString())),
                    new XElement("Register3",
                        new XElement("SDLoadReset", this.SDLoadReset.ToString()),
                        new XElement("PhaseResync", this.PhaseResync.ToString()),
                        new XElement("PhaseAdjustment", this.PhaseAdjustment.ToString()),
                        new XElement("Phase", this.Phase.ToString())),
                    new XElement("Register4",
                        new XElement("MuxOutputIndex", this.MuxOutIndex.ToString()),
                        new XElement("DoubleBuffer", this.DoubleBuffer.ToString()),
                        new XElement("CPCurrentIndex", this.CPCurrentIndex.ToString()),
                        new XElement("ReferenceModeIsSingle", this.ReferenceModeIsSingle.ToString()),
                        new XElement("MuxLevelIs18", this.MuxLevelIs18.ToString()),
                        new XElement("PDPolarityIsNegative", this.PDPolarityIsNegative.ToString()),
                        new XElement("PowerDown", this.PowerDown.ToString()),
                        new XElement("CPThreeState", this.CPThreeState.ToString()),
                        new XElement("CounterReset", this.CounterReset.ToString())),
                    new XElement("Register6",
                        new XElement("FeedbackIndex", this.FeedBack.ToString()),
                        new XElement("CPBleedCurrentInt", this.CPBleedCurrentInt.ToString()),
                        new XElement("MuteTillLockDetect", this.MuteTillLockDetect.ToString()),
                        new XElement("AuxOutputEnable", this.AuxOutEnable.ToString()),
                        new XElement("AuxOutputPower", this.AuxPowerValue.ToString()),
                        new XElement("RfOutputEnable", this.RfOutEnable.ToString()),
                        new XElement("RfOutputPower", this.RfOutPowerValue.ToString()),
                        new XElement("NegativeBleed", this.NegativeBleed.ToString()),
                        new XElement("GatedBleed", this.GatedBleed.ToString())),
                    new XElement("Register7",
                        new XElement("LESync", this.LESync.ToString()),
                        new XElement("LDCycleCountInt", this.LDCycleCountInt.ToString()),
                        new XElement("LOLMode", this.LOLMode.ToString()),
                        new XElement("FracNPrecision", this.FracNPrecisionInt.ToString()),
                        new XElement("LDModeInt", this.LDModeInt.ToString())),
                    new XElement("Register9",
                        new XElement("AutosetFastestCalibration", this.FastestCalibration.ToString()),
                        new XElement("VCOBandDivisonInt", this.VCOBandDivisonInt.ToString()),
                        new XElement("TimeoutInt", this.TimeoutInt.ToString()),
                        new XElement("SynthLockTimeoutInt", this.SynthLockTimeoutInt.ToString()),
                        new XElement("TotalCalculatedTime", this.TotalCalculatedTime.ToString())),
                    new XElement("Register10",
                        new XElement("ADCClockDividerInt", this.ADCClockDividerInt.ToString()),
                        new XElement("ADCConversion", this.ADCConversion.ToString()),
                        new XElement("ADCEnable", this.ADCEnable.ToString()),
                        new XElement("ADCClockDividerAutoset", this.ADCClockDividerAutoset.ToString())),
                    new XElement("Register12",
                        new XElement("ResyncClockInt", this.ResyncClockInt.ToString()),
                        new XElement("ResyncClockTimeout", this.ResyncClockTimeout.ToString())),
                    new XElement("EnvironmentVaribleName", ENV_VAR_NAME));
            #endregion

            string dir = Path.GetDirectoryName(path);

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            settingsTree.Save(path);
        }

        readonly static private string PROGRAM_NAME = "ADF4355-3 Programmer";
        readonly static private string PROGRAM_APPDATA_PATH = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), PROGRAM_NAME);
        readonly static private string SETTINGS_DIR = Path.Combine(PROGRAM_APPDATA_PATH, "Settings");
        readonly static private string DEFAULT_SETTINGS_FILE = Path.Combine(SETTINGS_DIR
            , "Default Settings.xml");
        readonly static private string INO_DIR = Path.Combine(PROGRAM_APPDATA_PATH, "Ino files");
        readonly static private string TEMP_INO_NAME = "temp";
        static private string ENV_VAR_NAME = "Path";
        static private Process ARDUINO_PROCESS = new Process();

        static private bool SettingsAreSaved = false;

        #region constants

        #region macros
        private static readonly decimal KILO = 1000;
        private static readonly decimal MEGA = 1000000;
        private static readonly decimal GIGA = 1000000000;
        private static readonly bool T = true;
        private static readonly bool F = false;
        private static readonly int MOD1 = 16777216;
        private static readonly DataSourceUpdateMode RIGHTNOW = DataSourceUpdateMode.OnPropertyChanged;
        private static readonly bool DEBUG = true;
        private static readonly string V = "Value";
        private static readonly string SI = "SelectedIndex";
        private static readonly string C = "Checked";
        private static readonly string TX = "Text";
        #endregion
        #region other
        private static readonly List<char> HEX = new List<char> { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'
            , 'A', 'B', 'C', 'D', 'E', 'F'};
        private static readonly Dictionary<string, string> HEX_TO_BIN = new Dictionary<string, string>
        {
            {"0", "0000"},
            {"1", "0001"},
            {"2", "0010"},
            {"3", "0011"},
            {"4", "0100"},
            {"5", "0101"},
            {"6", "0110"},
            {"7", "0111"},
            {"8", "1000"},
            {"9", "1001"},
            {"A", "1010"},
            {"B", "1011"},
            {"C", "1100"},
            {"D", "1101"},
            {"E", "1110"},
            {"F", "1111"}
        };
        private static readonly List<string> OUTPUT_DIVIDER_TEXT = new List<string> { "/1", "/2", "/4", "/8"
            , "/16", "/32", "/64" };
        private static readonly List<decimal> OUTPUT_DIVIDER = new List<decimal> { 1, 2, 4, 8, 16, 32, 64 };
        private static readonly List<string> PRESCALER_TEXT = new List<string> { "4/5", "8/9" };
        private static readonly List<Tuple<string, int>> MUX_OUT_TEXT = new List<Tuple<string, int>> {
            new Tuple<string, int>("3-state output", 0),
            new Tuple<string, int>("DVdd", 1),
            new Tuple<string, int>("DGND", 2),
            new Tuple<string, int>("R divider output", 3),
            new Tuple<string, int>("N divider output", 4),
            new Tuple<string, int>("Digital lock detect", 6)
        };
        /*private static readonly decimal[] CP_CURRENTS = new decimal[] {0.300M, 0.600M, 0.900M, 1.200M, 1.500M
            , 1.800M, 2.100M, 2.400M, 2.700M, 3.000M, 3.300M, 3.600M, 3.900M, 4.200M, 4.500M, 4.800M };*/
        private static readonly decimal[] CP_CURRENTS = new decimal[] {0.31M, 0.63M, 0.94M, 1.25M, 1.56M
            , 1.88M, 2.19M, 2.50M, 2.81M, 3.13M, 3.44M, 3.75M, 4.06M, 4.38M, 4.69M, 5.00M };
        private static readonly string[] CP_CURRENTS_TEXT = MODIFY_ARRAY(CP_CURRENTS, "", " mA");
        private static readonly string[] FEEDBACK_TEXT = new string[] { "Divided", "Fundamental" };
        private static readonly decimal[] LD_CYCLE_COUNTS = new decimal[] { 1024, 2048, 4096, 8192 };
        private static readonly decimal[] FRAC_N_LD_PRECISIONS = new decimal[] { 5.0M, 6.0M, 8.0M, 12.0M };
        private static readonly string[] FRAC_N_LD_PRECISIONS_TEXT = MODIFY_ARRAY(FRAC_N_LD_PRECISIONS, ""
            , " ns");
        private static readonly string[] LD_MODE_TEXT = new string[] { "Fractional-N", "Integer-N (2.9 ns)" };
        //private static readonly decimal[] BLEED_CURRENTS = CREATE_ARRAY(1, 255, 3.75M, 0);
        //private static readonly string[] BLEED_CURRENTS_TEXT = MODIFY_ARRAY(BLEED_CURRENTS, "", " dBm");
        //private static readonly decimal[] AUX_POWERS = new decimal[] { -4, -1, 2, 5 };
        //private static readonly string[] AUX_POWERS_TEXT = MODIFY_ARRAY(AUX_POWERS, "", " dBm");
        #endregion
        #region upper and lower limits
        private static readonly decimal VCO_UPPER_LIMIT = 6400;
        private static readonly decimal VCO_LOWER_LIMIT = 3200;
        private static readonly decimal INT45_UPPER_LIMIT = 32767;
        private static readonly decimal INT45_LOWER_LIMIT = 23;
        private static readonly decimal INT98_UPPER_LIMIT = 65535;
        private static readonly decimal INT98_LOWER_LIMIT = 65;
        private static readonly decimal FRAC1_UPPER_LIMIT = 16777215;
        private static readonly decimal FRAC1_LOWER_LIMIT = 0;
        private static readonly decimal MOD2_UPPER_LIMIT = 16383;
        private static readonly decimal MOD2_LOWER_LIMIT = 1;
        private static readonly decimal FRAC2_UPPER_LIMIT = 16383;
        private static readonly decimal FRAC2_LOWER_LIMIT = 0;
        #endregion

        #region error messages
        private static string ERROR_INT = "Int must be in between 23 and 65535.";
        private static string ERROR_MOD2 = "Mod2 must be in between 1 and 16383.";
        private static string ERROR_VCO = "VCO must be in between 3400 and 6800.";
        private static string WARNING_PHASE_RESYNC = "Do not forget the ";
        #endregion

        #region tooltip messages
        private static string AUTOCAL_HELP = "Check the check box for autocalibration. This is the recommended" +
            " mode of operation.";
        #endregion

        #region some useful functions
        private static decimal[] CREATE_ARRAY(decimal start_n, decimal stop_n, decimal a, decimal b)
        {
            int len = (int)(stop_n - start_n + 1);
            decimal[] arr = new decimal[len];


            for(decimal i = start_n; i <= stop_n; i++)
            {
                int index = (int)(i - start_n);
                arr[index] = a * i + b;
            }

            return arr;
        }
        private static string[] MODIFY_ARRAY(decimal[] arr, string starts, string ends)
        {
            string[] strArr = new string[arr.Length];

            for(int i = 0; i < arr.Length; i++)
            {
                strArr[i] = starts + arr[i].ToString() + ends;
            }

            return strArr;
        }
        private static decimal GCD(decimal a, decimal b)
        {
            while (b != 0)
            {
                decimal _a = a;
                a = b;
                b = _a % b;
            }
            return a;
        }
        private static int GCD(int a, int b)
        {
            while (b != 0)
            {
                int _a = a;
                a = b;
                b = _a % b;
            }
            return a;
        }
        private static decimal MAX(params decimal[] nums)
        {
            decimal _max = nums[0];

            foreach(decimal num in nums)
                if (_max < num)
                    _max = num;

            return _max;
        }
        private static int MAX(params int[] nums)
        {
            int _max = nums[0];

            foreach (int num in nums)
                if (_max < num)
                    _max = num;

            return _max;
        }
        private static decimal MIN(params decimal[] nums)
        {
            decimal _min = nums[0];

            foreach (decimal num in nums)
                if (_min > num)
                    _min = num;

            return _min;
        }
        private static int MIN(params int[] nums)
        {
            int _min = nums[0];

            foreach (int num in nums)
                if (_min > num)
                    _min = num;

            return _min;
        }
        private static string NUM_TO_HEX_STR(decimal num, decimal places)
        {
            string res = "";

            for(int i = 0; i < places; i++)
            {
                res = HEX[(int)(num % 16)] + res;
                num = decimal.Floor(num / 16);
            }

            return res;
        }
        private static string NUM_TO_BIN_STR(decimal num, decimal places)
        {
            string res = "";

            for (int i = 0; i < places; i++)
            {
                res = (num % 2).ToString() + res;
                num = decimal.Floor(num / 2);
            }

            return res;
        }
        private static decimal BIN_STR_TO_NUM(string binstr)
        {
            decimal temp = 1;
            decimal res = 0;
            foreach(char c in binstr.Reverse<char>())
            {
                res += c == '1' ? temp : 0;
                temp *= 2;
            }
            return res;
        }
        private static decimal REGISTER_VAL(params decimal[] args)
        {
            if (DEBUG)
            {
                decimal numOfElements = 0;
                for (int i = 0; i < args.Length; i += 2)
                    numOfElements += args[i + 1];

                if (numOfElements != 32)
                    MessageBox.Show("not 32");
            }

            string res = "";
            for(int i = 0; i < args.Length; i += 2)
            {
                res += NUM_TO_BIN_STR(args[i], args[i + 1]);
            }
            return BIN_STR_TO_NUM(res);
        }
        private static void CONTROL_BIND(object control, object variable)
        {
            NumericUpDown num = control as NumericUpDown;
            CheckBox check = control as CheckBox;
            ComboBox combo = control as ComboBox;
            TextBox text = control as TextBox;

            if(num != null)
            {
                try
                {
                    num.DataBindings.Add(V, variable as ActiveVar<decimal>, V, true, RIGHTNOW);
                }
                catch
                {
                    num.DataBindings.Add(V, variable as ActiveVar<int>, V, true, RIGHTNOW);
                }
            }
            else if(check != null)
            {
                check.DataBindings.Add(C, variable as ActiveVar<bool>, V, true, RIGHTNOW);
            }
            else if(combo != null)
            {
                combo.DataBindings.Add(SI, variable as ActiveVar<int>, V, true, RIGHTNOW);
            }
            else if(text != null)
            {
                text.DataBindings.Add(TX, variable as ActiveVar<decimal>, V, true, RIGHTNOW);
            }
        }
        /*
        private static decimal BIN_STR_TO_DEC(string bin)
        {

            decimal num = 0;
            decimal temp = 1;

            foreach(char digit in bin.Reverse<char>())
            {
                num += digit == '1' ? temp : 0;

                temp *= 2;
            }

            return num;
        }
        private static decimal HEX_STR_TO_DEC(string hex)
        {

            decimal num = 0;
            decimal temp = 1;

            foreach (char digit in hex.Reverse<char>())
            {
                num += HEX.IndexOf(digit) * temp;

                temp *= 16;
            }

            return num;
        }
        private static string HEX_STR_TO_BIN_STR(string hex)
        {
            string res = "";
            foreach(char c in hex)
            {
                res += HEX_TO_BIN[c.ToString()];
            }
            return res;
        }
        private static string BIN_STR_TO_HEX_STR(string bin)
        {
            string _bin = (string)bin.Reverse<char>();
            string res = "";

            for(int i = 0; i < _bin.Length; i += 4)
            {
                string quad;
                try { quad = _bin.Substring(i, 4); }
                catch { 
                    quad = _bin.Substring(i);
                    while (quad.Length < 4)
                        quad = "0" + quad;
                }
                
                res = quad + res;
            }

            return res;
        }
        private static string REG_NUM_BIN_STR(params decimal[] args)
        {
            if (DEBUG)
            {
                decimal numOfElements = 0;
                for (int i = 0; i < args.Length; i += 2)
                    numOfElements += args[i + 1];

                if (numOfElements != 32)
                    MessageBox.Show("not 32");
            }

            string binStr = "";

            for (int i = 0; i < args.Length; i += 2)
            {
                decimal num = args[i];
                int numOfBits = (int)args[i + 1];

                binStr += NUM_TO_BIN_STR(num, numOfBits);
            }

            return binStr;
        }
        private static string REG_NUM_BIN_STR(params int[] args)
        {
            if (DEBUG)
            {
                int numOfElements = 0;
                for (int i = 0; i < args.Length; i += 2)
                    numOfElements += args[i + 1];

                if (numOfElements != 32)
                    MessageBox.Show("not 32");
            }

            string binStr = "";

            for (int i = 0; i < args.Length; i += 2)
            {
                int num = args[i];
                int numOfBits = args[i + 1];

                binStr += NUM_TO_BIN_STR(num, numOfBits);
            }

            return binStr;
        }
        private static string REG_NUM_HEX_STR(params decimal[] args)
        {
            return BIN_STR_TO_HEX_STR(REG_NUM_BIN_STR(args));
        }
        private static string REG_NUM_HEX_STR(params int[] args)
        {
            return BIN_STR_TO_HEX_STR(REG_NUM_BIN_STR(args));
        }
        private static decimal REG_NUM_DEC(params int[] args)
        {
            return BIN_STR_TO_DEC(REG_NUM_BIN_STR(args));
        }
        private static decimal REG_NUM_DEC(params decimal[] args)
        {
            return BIN_STR_TO_DEC(REG_NUM_BIN_STR(args));
        }
        */

        #endregion

        #endregion

        
        public Form1()
        {
            InitializeComponent();
            //folder specific checks
            CheckFolders("Your default settings file could not found! So we created one."
                , "No Default Settings File");

            //arduino process created
            ARDUINO_PROCESS.StartInfo.FileName = GetArduinoPathFromEnvironment();
            ARDUINO_PROCESS.StartInfo.UseShellExecute = false;
            ARDUINO_PROCESS.StartInfo.RedirectStandardOutput = true;
            ARDUINO_PROCESS.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            ARDUINO_PROCESS.StartInfo.CreateNoWindow = true;
            ARDUINO_PROCESS.EnableRaisingEvents = true;


            #region active variable initializations
            this._referenceInput = new ActiveVar<decimal>();
            this._divider = new ActiveVar<decimal>();
            this._doubler = new ActiveVar<bool>();
            this._divideby2 = new ActiveVar<bool>();
            this._vco = new ActiveVar<decimal>();
            this._fchsp = new ActiveVar<decimal>();
            this._outputDividerIndex = new ActiveVar<int>();

            this._fpfd = new ActiveVar<decimal>();
            this._n = new ActiveVar<decimal>();
            this._int = new ActiveVar<decimal>();
            this._frac1 = new ActiveVar<decimal>();
            this._mod2 = new ActiveVar<decimal>();
            this._frac2 = new ActiveVar<decimal>();
            this._refOut = new ActiveVar<decimal>();

            this._intMode = new ActiveVar<bool>();

            #region registers
            #region register 0
            this._autocal = new ActiveVar<bool>();
            this._prescaler = new ActiveVar<int>();
            this._reg0 = new ActiveVar<decimal>();
            #endregion
            #region register 1
            this._reg1 = new ActiveVar<decimal>();
            #endregion
            #region register 2
            this._reg2 = new ActiveVar<decimal>();
            #endregion
            #region register 3
            this._sdLoadReset = new ActiveVar<bool>();
            this._phaseResync = new ActiveVar<bool>();
            this._phaseAdjustment = new ActiveVar<bool>();
            this._phase = new ActiveVar<decimal>();
            this._reg3 = new ActiveVar<decimal>();
            #endregion
            #region register 4
            this._muxOutIndex = new ActiveVar<int>();
            this._doubleBuffer = new ActiveVar<bool>();
            this._cpCurrentIndex = new ActiveVar<int>();
            this._referenceModeIsSingle = new ActiveVar<bool>();
            this._muxLevelIs18 = new ActiveVar<bool>();
            this._pdPolarityIsNegative = new ActiveVar<bool>();
            this._powerDown = new ActiveVar<bool>();
            this._cpThreeState = new ActiveVar<bool>();
            this._counterReset = new ActiveVar<bool>();
            this._reg4 = new ActiveVar<decimal>();
            #endregion
            #region register 5*
            this._reg5 = new ActiveVar<decimal>();
            #endregion
            #region register 6
            this._gatedBleed = new ActiveVar<bool>();
            this._negativeBleed = new ActiveVar<bool>();
            this._feedback = new ActiveVar<int>();
            this._cpBleedCurrentValue = new ActiveVar<decimal>();
            this._cpBleedCurrentInt = new ActiveVar<int>();
            this._muteTillLockDetect = new ActiveVar<bool>();
            this._auxOutEnable = new ActiveVar<bool>();
            this._auxPowerValue = new ActiveVar<int>();
            this._rfOutEnable = new ActiveVar<bool>();
            this._rfOutPowerValue = new ActiveVar<int>();
            this._reg6 = new ActiveVar<decimal>();
            #endregion
            #region register 7 
            this._leSync = new ActiveVar<bool>();
            this._ldCycleCountInt = new ActiveVar<int>();
            this._lolMode = new ActiveVar<bool>();
            this._fracNPrecisionInt = new ActiveVar<int>();
            this._ldModeInt = new ActiveVar<int>();
            this._reg7 = new ActiveVar<decimal>();
            #endregion
            #region register 8*
            this._reg8 = new ActiveVar<decimal>();
            #endregion
            #region register 9
            this._vcoBandDivisionInt = new ActiveVar<int>();
            this._timeoutInt = new ActiveVar<int>();
            this._synthLockTimeoutInt = new ActiveVar<int>();
            this._fastestCalibration = new ActiveVar<bool>();
            this._reg9 = new ActiveVar<decimal>();
            this._totalCalculatedTime = new ActiveVar<decimal>();
            #endregion
            #region register 10
            this._adcClockDividerInt = new ActiveVar<int>();
            this._adcConversion = new ActiveVar<bool>();
            this._adcEnable = new ActiveVar<bool>();
            this._reg10 = new ActiveVar<decimal>();
            this._frequency = new ActiveVar<decimal>();
            this._adcClockDividerAutoset = new ActiveVar<bool>();
            #endregion
            #region register 11*
            this._reg11 = new ActiveVar<decimal>();
            #endregion
            #region register 12
            this._resyncClockInt = new ActiveVar<int>();
            this._resyncClockTimeout = new ActiveVar<decimal>();
            this._reg12 = new ActiveVar<decimal>();
            #endregion
            #endregion

            #endregion

            #region calculation method bindings

            #region other
            this._referenceInput.PropertyChanged += this.calculateFpfd;
            this._divider.PropertyChanged += this.calculateFpfd;
            this._doubler.PropertyChanged += this.calculateFpfd;
            this._divideby2.PropertyChanged += this.calculateFpfd;

            this._vco.PropertyChanged += this.calculateN;
            this._fpfd.PropertyChanged += this.calculateN;

            this._n.PropertyChanged += this.calculateInt;

            this._n.PropertyChanged += this.calculateFrac1;
            this._int.PropertyChanged += this.calculateFrac1;

            this._fpfd.PropertyChanged += this.calculateMod2;
            this._fchsp.PropertyChanged += this.calculateMod2;

            this._n.PropertyChanged += this.calculateFrac2;
            this._int.PropertyChanged += this.calculateFrac2;
            this._frac1.PropertyChanged += this.calculateFrac2;
            this._mod2.PropertyChanged += this.calculateFrac2;

            this._vco.PropertyChanged += this.calculateRefOut;
            this._outputDividerIndex.PropertyChanged += this.calculateRefOut;

            this._frac1.PropertyChanged += this.calculateIntMode;
            this._frac2.PropertyChanged += this.calculateIntMode;

            #endregion

            #region registers
            #region register 0
            this._autocal.PropertyChanged += this.calculateRegister0;
            this._prescaler.PropertyChanged += this.calculateRegister0;
            this._int.PropertyChanged += this.calculateRegister0;
            this._int.PropertyChanged += (o, e) =>
            {
                if (this.Int >= INT45_LOWER_LIMIT && this.Int <= INT98_LOWER_LIMIT)
                    this._prescaler.Value = 0;
                else if (this.Int >= INT45_UPPER_LIMIT && this.Int <= INT98_UPPER_LIMIT)
                    this._prescaler.Value = 1;
            };
            #endregion
            #region register 1
            this._frac1.PropertyChanged += this.calculateRegister1;
            #endregion
            #region register 2
            this._frac2.PropertyChanged += this.calculateRegister2;
            this._mod2.PropertyChanged += this.calculateRegister2;
            #endregion
            #region register 3
            this._sdLoadReset.PropertyChanged += this.calculateRegister3;
            this._phaseResync.PropertyChanged += this.calculateRegister3;
            this._phaseAdjustment.PropertyChanged += this.calculateRegister3;
            this._phase.PropertyChanged += this.calculateRegister3;
            #endregion
            #region register 4
            this._muxOutIndex.PropertyChanged += this.calculateRegister4;
            this._doubleBuffer.PropertyChanged += this.calculateRegister4;
            this._cpCurrentIndex.PropertyChanged += this.calculateRegister4;
            this._referenceModeIsSingle.PropertyChanged += this.calculateRegister4;
            this._muxLevelIs18.PropertyChanged += this.calculateRegister4;
            this._pdPolarityIsNegative.PropertyChanged += this.calculateRegister4;
            this._powerDown.PropertyChanged += this.calculateRegister4;
            this._cpThreeState.PropertyChanged += this.calculateRegister4;
            this._counterReset.PropertyChanged += this.calculateRegister4;
            this._divider.PropertyChanged += this.calculateRegister4;
            this._doubler.PropertyChanged += this.calculateRegister4;
            this._divideby2.PropertyChanged += this.calculateRegister4;
            #endregion
            #region register 6
            this._cpBleedCurrentInt.PropertyChanged += (o, e) => { 
                this._cpBleedCurrentValue.Value = this._cpBleedCurrentInt.Value * 3.75M; 
            };
            this._gatedBleed.PropertyChanged += this.calculateRegister6;
            this._negativeBleed.PropertyChanged += this.calculateRegister6;
            this._feedback.PropertyChanged += this.calculateRegister6;
            this._outputDividerIndex.PropertyChanged += this.calculateRegister6;
            this._cpBleedCurrentInt.PropertyChanged += this.calculateRegister6;
            this._muteTillLockDetect.PropertyChanged += this.calculateRegister6;
            this._auxOutEnable.PropertyChanged += this.calculateRegister6;
            this._auxPowerValue.PropertyChanged += this.calculateRegister6;
            this._rfOutEnable.PropertyChanged += this.calculateRegister6;
            this._rfOutPowerValue.PropertyChanged += this.calculateRegister6;
            #endregion
            #region register 7
            this._leSync.PropertyChanged += this.calculateRegister7;
            this._ldCycleCountInt.PropertyChanged += this.calculateRegister7;
            this._lolMode.PropertyChanged += this.calculateRegister7;
            this._fracNPrecisionInt.PropertyChanged += this.calculateRegister7;
            this._ldModeInt.PropertyChanged += this.calculateRegister7;
            #endregion
            #region register 9
            this._vcoBandDivisionInt.PropertyChanged += this.calculateRegister9;
            this._timeoutInt.PropertyChanged += this.calculateRegister9;
            this._synthLockTimeoutInt.PropertyChanged += this.calculateRegister9;
            this._fastestCalibration.PropertyChanged += this.calculateAutosetReg9;
            this._vcoBandDivisionInt.PropertyChanged += this.calculateTotalTime;
            this._fpfd.PropertyChanged += this.calculateTotalTime;
            this._fpfd.PropertyChanged += this.calculateAutosetReg9;
            this._timeoutInt.PropertyChanged += this.calculateAutosetReg9;
            #endregion
            #region register 10
            this._adcClockDividerInt.PropertyChanged += this.calculateRegister10;
            this._adcConversion.PropertyChanged += this.calculateRegister10;
            this._adcEnable.PropertyChanged += this.calculateRegister10;
            this._adcClockDividerAutoset.PropertyChanged += this.calculateAutosetReg10;
            this._adcClockDividerInt.PropertyChanged += this.calculateFrequency;
            this._fpfd.PropertyChanged += this.calculateFrequency;
            this._fpfd.PropertyChanged += this.calculateAutosetReg10;
            #endregion
            #region register 12
            this._resyncClockInt.PropertyChanged += this.calculateRegister12;
            this._resyncClockInt.PropertyChanged += this.calculateResyncClockTimeout;
            this._fpfd.PropertyChanged += this.calculateResyncClockTimeout;
            #endregion
            #endregion

            #endregion

            #region validation method bindings
            this._int.PropertyChanged += this.validateInt;
            this._mod2.PropertyChanged += this.validateMod2;
            this._vco.PropertyChanged += this.validateVco;
            this._phaseResync.PropertyChanged += this.validatePhaseResync;
            this._frac2.PropertyChanged += this.validatePhaseResync;
            this._sdLoadReset.PropertyChanged += this.validatePhaseResync;
            this._feedback.PropertyChanged += this.validatePhaseResync;
            this._outputDividerIndex.PropertyChanged += this.validatePhaseResync;
            this._phaseAdjustment.PropertyChanged += this.validatePhaseAdjust;
            this._autocal.PropertyChanged += this.validatePhaseAdjust;
            this._sdLoadReset.PropertyChanged += this.validatePhaseAdjust;
            this._phaseResync.PropertyChanged += this.validatePhaseAdjust;
            this._muxOutIndex.PropertyChanged += this.validateMuxout;
            this._cpCurrentIndex.PropertyChanged += this.validateCPCurrent;
            this._doubler.PropertyChanged += this.validateDoubler;
            this._referenceInput.PropertyChanged += this.validateDoubler;
            this._autocal.PropertyChanged += this.validateAutocal;
            this._sdLoadReset.PropertyChanged += this.validateSDLoadReset;
            this._negativeBleed.PropertyChanged += this.validateNegativeBleed;
            this._frac1.PropertyChanged += this.validateNegativeBleed;
            this._frac2.PropertyChanged += this.validateNegativeBleed;
            this._fpfd.PropertyChanged += this.validateNegativeBleed;
            this._fpfd.PropertyChanged += this.validateCPBleedCurrent;
            this._negativeBleed.PropertyChanged += this.validateCPBleedCurrent;
            this._cpCurrentIndex.PropertyChanged += this.validateCPBleedCurrent;
            this._cpBleedCurrentInt.PropertyChanged += this.validateCPBleedCurrent;
            this._lolMode.PropertyChanged += this.validateLOLMode;
            this._referenceModeIsSingle.PropertyChanged += this.validateLOLMode;
            this._ldModeInt.PropertyChanged += this.validateLDMode;
            this._intMode.PropertyChanged += this.validateLDMode;
            this._adcEnable.PropertyChanged += this.validateADCEnable;
            #endregion

            #region control specific bindings
            this.numReferenceInput.DataBindings.Add("Value", this._referenceInput, "Value", T, RIGHTNOW);
            this.numDivider.DataBindings.Add("Value", this._divider, "Value", T, RIGHTNOW);
            this.cbDoubler.DataBindings.Add("Checked", this._doubler, "Value", T, RIGHTNOW);
            this.cbDivideby2.DataBindings.Add("Checked", this._divideby2, "Value", T, RIGHTNOW);
            this.numVco.DataBindings.Add("Value", this._vco, "Value", T, RIGHTNOW);
            this.numFchsp.DataBindings.Add("Value", this._fchsp, "Value", T, RIGHTNOW);
            this.cbOutputDivider.DataSource = OUTPUT_DIVIDER_TEXT;
            this.cbOutputDivider.DataBindings.Add("SelectedIndex", this._outputDividerIndex, "Value", T
                , RIGHTNOW);

            this.numN.DataBindings.Add("Value", this._n, "Value", T, RIGHTNOW);
            this.numFpfd.DataBindings.Add("Value", this._fpfd, "Value", T, RIGHTNOW);
            this.numInt.DataBindings.Add("Value", this._int, "Value", T, RIGHTNOW);
            this.numFrac1.DataBindings.Add("Value", this._frac1, "Value", T, RIGHTNOW);
            this.numMod2.DataBindings.Add("Value", this._mod2, "Value", T, RIGHTNOW);
            this.numFrac2.DataBindings.Add("Value", this._frac2, "Value", T, RIGHTNOW);
            this.numRefOut.DataBindings.Add("Value", this._refOut, "Value", T, RIGHTNOW);

            #region registers
            #region register 0 
            this.cbAutocal.DataBindings.Add("Checked", this._autocal, "Value", T, RIGHTNOW);
            this.cbPrescaler.DataSource = PRESCALER_TEXT;
            this.cbPrescaler.DataBindings.Add("SelectedIndex", this._prescaler, "Value", T, RIGHTNOW);
            this._reg0.PropertyChanged += (object sender, PCEArgs e) => {
                txtReg0.Text = this.Register0_HexStr;
            };
            this._int.PropertyChanged += (o, e) => 
            {
                if (this.Int >= INT45_LOWER_LIMIT && this.Int <= INT98_LOWER_LIMIT)
                    this.cbPrescaler.Enabled = false;
                else if (this.Int >= INT98_LOWER_LIMIT && this.Int <= INT45_UPPER_LIMIT)
                    this.cbPrescaler.Enabled = true;
                else if (this.Int >= INT45_UPPER_LIMIT && this.Int <= INT98_LOWER_LIMIT)
                    this.cbPrescaler.Enabled = false;
            };
            #endregion
            #region register 1
            this._reg1.PropertyChanged += (object sender, PCEArgs e) => {
                txtReg1.Text = this.Register1_HexStr;
            };
            #endregion
            #region register 2
            this._reg2.PropertyChanged += (object sender, PCEArgs e) => {
                txtReg2.Text = this.Register2_HexStr;
            };
            #endregion
            #region register 3
            this.cbSDLoadReset.DataBindings.Add("Checked", this._sdLoadReset, "Value", T, RIGHTNOW);
            this.cbPhaseResync.DataBindings.Add("Checked", this._phaseResync, "Value", T, RIGHTNOW);
            this.cbPhaseAdjust.DataBindings.Add("Checked", this._phaseAdjustment, "Value", T, RIGHTNOW);
            this.numPhase.DataBindings.Add("Value", this._phase, "Value", T, RIGHTNOW);
            this._reg3.PropertyChanged += (object sender, PCEArgs e) => {
                txtReg3.Text = this.Register3_HexStr;
            };
            #endregion
            #region register 4
            this.cbDoubleBuffer.DataBindings.Add("Checked", this._doubleBuffer, "Value", T, RIGHTNOW);
            this.cbMuxout.DisplayMember = "Item1";
            this.cbMuxout.ValueMember = "Item2";
            this.cbMuxout.DataSource = MUX_OUT_TEXT;
            this.cbMuxout.DataBindings.Add("SelectedIndex", this._muxOutIndex, "Value", T, RIGHTNOW);
            this.cbCPCurrent.DataSource = CP_CURRENTS_TEXT;
            this.cbCPCurrent.DataBindings.Add("SelectedIndex", this._cpCurrentIndex, "Value", T, RIGHTNOW);
            this.rbSingle.DataBindings.Add("Checked", this._referenceModeIsSingle, "Value", T, RIGHTNOW);
            this.rb18.DataBindings.Add("Checked", this._muxLevelIs18, "Value", T, RIGHTNOW);
            this.rbNegative.DataBindings.Add("Checked", this._pdPolarityIsNegative, "Value", T, RIGHTNOW);
            this.cbPowerDown.DataBindings.Add("Checked", this._powerDown, "Value", T, RIGHTNOW);
            this.cbCPThreeState.DataBindings.Add("Checked", this._cpThreeState, "Value", T, RIGHTNOW);
            this.cbCounterReset.DataBindings.Add("Checked", this._counterReset, "Value", T, RIGHTNOW);
            this._reg4.PropertyChanged += (object sender, PCEArgs e) => {
                txtReg4.Text = this.Register4HexStr;
            };
            #endregion
            #region register 5*
            this._reg5.PropertyChanged += (object sender, PCEArgs e) => {
                txtReg5.Text = this.Register5HexStr;
            };
            #endregion
            #region register 6
            this.cbFeedback.DataSource = FEEDBACK_TEXT;
            this.cbFeedback.DataBindings.Add("SelectedIndex", this._feedback, "Value", T, RIGHTNOW);
            this.numCPBleedCurrentInt.DataBindings.Add("Value", this._cpBleedCurrentInt, "Value", T
                , RIGHTNOW);
            this.numCPBleedCurrentValue.DataBindings.Add("Value", this._cpBleedCurrentValue, "Value", T
                , RIGHTNOW);
            this.cbMuteTillLockDetect.DataBindings.Add("Checked", this._muteTillLockDetect, "Value", T
                , RIGHTNOW);
            this.cbAuxOutEnable.DataBindings.Add("Checked", this._auxOutEnable, "Value", T, RIGHTNOW);
            this.numAuxOutPower.DataBindings.Add("Value", this._auxPowerValue, "Value", T, RIGHTNOW);
            this.cbRfOutEnable.DataBindings.Add("Checked", this._rfOutEnable, "Value", T, RIGHTNOW);
            this.numRfOutPower.DataBindings.Add("Value", this._rfOutPowerValue, "Value", T, RIGHTNOW);
            this.cbNegativeBleed.DataBindings.Add("Checked", this._negativeBleed, "Value", T, RIGHTNOW);
            this.cbGatedBleed.DataBindings.Add(C, this._gatedBleed, V, T, RIGHTNOW);
            this._reg6.PropertyChanged += (o, e) => { txtReg6.Text = this.Register6HexStr; };
            #endregion
            #region register 7
            CONTROL_BIND(this.cbLESync, this._leSync);
            this.cbLDCycleCount.DataSource = LD_CYCLE_COUNTS;
            CONTROL_BIND(this.cbLDCycleCount, this._ldCycleCountInt);
            CONTROL_BIND(this.cbLOLMode, this._lolMode);
            this.cbFracNPrecision.DataSource = FRAC_N_LD_PRECISIONS_TEXT;
            CONTROL_BIND(this.cbFracNPrecision, this._fracNPrecisionInt);
            this.cbLDMode.DataSource = LD_MODE_TEXT;
            CONTROL_BIND(this.cbLDMode, this._ldModeInt);
            this._reg7.PropertyChanged += (o, e) => { txtReg7.Text = this.Register7HexStr; };
            #endregion
            #region register 8*
            this._reg8.PropertyChanged += (o, e) => { txtReg8.Text = this.Register8HexStr; };
            #endregion
            #region register 9
            CONTROL_BIND(this.numVCOBandDivision, this._vcoBandDivisionInt);
            CONTROL_BIND(this.numTimeout, this._timeoutInt);
            CONTROL_BIND(this.numSynthTimeout, this._synthLockTimeoutInt);
            CONTROL_BIND(this.cbAutosetFastestCalibration, this._fastestCalibration);
            CONTROL_BIND(this.numTotalCalculatedTime, this._totalCalculatedTime);
            this._reg9.PropertyChanged += (o, e) => { txtReg9.Text = this.Register9HexStr; };
            #endregion
            #region register 10
            CONTROL_BIND(this.numADCClockDivider, this._adcClockDividerInt);
            CONTROL_BIND(this.cbADCConversion, this._adcConversion);
            CONTROL_BIND(this.cbADCEnable, this._adcEnable);
            CONTROL_BIND(this.numFrequency, this._frequency);
            CONTROL_BIND(this.cbADCClockDividerAutoset, this._adcClockDividerAutoset);
            this._reg10.PropertyChanged += (o, e) => { txtReg10.Text = this.Register10HexStr; };
            #endregion
            #region register 11*
            this._reg11.PropertyChanged += (o, e) => { txtReg11.Text = this.Register11HexStr; };
            #endregion
            #region register 12
            CONTROL_BIND(this.numResyncClock, this._resyncClockInt);
            CONTROL_BIND(this.numResyncClockTimeout, this._resyncClockTimeout);
            this._reg12.PropertyChanged += (o, e) => { txtReg12.Text = this.Register12HexStr; };
            #endregion
            #endregion

            #endregion

            #region SettingsAreSaved bindings
            #region inputs
            this._referenceInput.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._divider.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._divideby2.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._doubler.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._fchsp.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._vco.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._outputDividerIndex.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            #endregion

            #region register 0
            this._autocal.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._prescaler.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            #endregion
            #region register 3
            this._sdLoadReset.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._phaseResync.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._phaseAdjustment.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._phase.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            #endregion
            #region register 4
            this._muxOutIndex.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._doubleBuffer.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._cpCurrentIndex.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._referenceModeIsSingle.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._muxLevelIs18.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._pdPolarityIsNegative.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._powerDown.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._cpThreeState.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._counterReset.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            #endregion
            #region register 6
            this._feedback.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._cpBleedCurrentInt.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._muteTillLockDetect.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._auxOutEnable.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._auxPowerValue.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._rfOutEnable.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._rfOutPowerValue.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._negativeBleed.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._gatedBleed.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            #endregion
            #region register 7
            this._leSync.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._ldCycleCountInt.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._lolMode.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._fracNPrecisionInt.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._ldModeInt.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            #endregion
            #region register 9
            this._fastestCalibration.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._vcoBandDivisionInt.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._timeoutInt.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._synthLockTimeoutInt.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            #endregion
            #region register 10
            this._adcClockDividerAutoset.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._adcClockDividerInt.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._adcConversion.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._adcEnable.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            #endregion
            #region register 12
            this._resyncClockInt.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            this._resyncClockTimeout.PropertyChanged += (s, e) => { SettingsAreSaved = false; };
            #endregion

            #endregion

            #region inital value

            //load settings
            LoadSettings();
            SettingsAreSaved = true;

            #region registers
            #region register 0
            txtReg0.Text = this.Register0_HexStr;
            #endregion
            #region register 1
            txtReg1.Text = this.Register1_HexStr;
            #endregion
            #region register 2
            txtReg2.Text = this.Register2_HexStr;
            #endregion
            #region register 3
            txtReg3.Text = this.Register3_HexStr;
            #endregion
            #region register 4
            this.rbSingle.Checked = this.ReferenceModeIsSingle;
            this.rbDifferential.Checked = !this.ReferenceModeIsSingle;
            this.rb18.Checked = this.MuxLevelIs18;
            this.rb33.Checked = !this.MuxLevelIs18;
            this.rbNegative.Checked = this.PDPolarityIsNegative;
            this.rbPositive.Checked = !this.PDPolarityIsNegative;
            txtReg4.Text = this.Register4HexStr;
            #endregion
            #region register 5*
            this._reg5.Value = 0x00800025;
            this.txtReg5.Text = "00800025";
            #endregion
            #region register 6
            this.txtReg6.Text = this.Register6HexStr;
            #endregion
            #region register 7
            this.txtReg7.Text = this.Register7HexStr;
            #endregion
            #region register 8*
            this._reg8.Value = 0x1A69A6B8;
            this.txtReg8.Text = this.Register8HexStr;
            #endregion
            #region register 9
            this.txtReg9.Text = this.Register9HexStr;
            #endregion
            #region register 10
            this.txtReg10.Text = this.Register10HexStr;
            #endregion
            #region register 11*
            this._reg11.Value = 0x0081200B;
            this.txtReg11.Text = this.Register11HexStr;
            #endregion
            #region register 12
            this.txtReg12.Text = this.Register12HexStr;
            #endregion
            #endregion

            #region error and help messages 
            this.hlblPhaseResync1.Text = "Do not forget to set the \"phase resync\"" +
                    " in Register 12!";
            this.hlblPhaseResync2.Text = "It is necessary for phase critical applications that " +
                "use output divider to set the \"feedback\" in Register 6 to \"divided\".";
            this.hlblPhaseResync3.Text = "For resync applications enable SD Load Reset in Register 3";
            this.hlblPhaseResync4.Text = "Phase resync functions only when Frac2 = 0";
            this.hlblPhaseAdjust1.Text = "Disable autocalibration in Register 0.";
            this.hlblPhaseAdjust2.Text = "Disable SD Load Reset in Register 3.";
            this.hlblPhaseAdjust3.Text = "Phase resync in Register 3 and phase adjustment can not be used " +
                "simultaneously.";
            this.hlblDoublerError1.Text = "The maximum allowable reference frequency when doubler is enabled" +
                "is 100 MHz!";
            this.hlblNegativeBleedError1.Text = "Use negative bleed only when operating in fractional n mode.";
            this.hlblNegativeBleedError2.Text = "FPFD can not be greater than 100 MHz when negative bleed is" +
                " enabled.";
            this.hlblLOLModeError1.Text = "LOL mode does not function reliably when using differential REFin" +
                " mode";
            this.hlblLDModeError1.Text = "Integer-N mode is more appropriate for integer-N app.";
            #endregion

            #endregion

            //start to listen the ports for Arduino
            tmCheckPorts.Start();
        }

        #region properties
        #region inputs
        public decimal ReferenceInput
        {
            get { return this._referenceInput.Value; }
            set
            {
                if (value == this._referenceInput.Value)
                    return;

                this._referenceInput.Value = value;
            }
        }
        public decimal Divider
        {
            get { return this._divider.Value; }
            set
            {
                if (value == this._divider.Value)
                    return;

                this._divider.Value = value;
            }
        }
        public bool Doubler
        {
            get { return this._doubler.Value; }
            set
            {
                if (value == this._doubler.Value)
                    return;

                this._doubler.Value = value;
            }
        }
        public int DoublerInt { get { return this.Doubler ? 1 : 0; } }
        public bool Divideby2
        {
            get { return this._divideby2.Value; }
            set
            {
                if (value == this._divideby2.Value)
                    return;

                this._divideby2.Value = value;
            }
        }
        public int Divideby2Int { get { return this.Divideby2 ? 1 : 0; } }
        public decimal Vco
        {
            get { return this._vco.Value; }
            set
            {
                if (value == this._vco.Value)
                    return;

                this._vco.Value = value;
            }
        }
        public decimal Fchsp
        {
            get { return this._fchsp.Value; }
            set
            {
                if (value == this._fchsp.Value)
                    return;

                this._fchsp.Value = value;
            }
        }

        public decimal N { get { return this._n.Value; } }
        public decimal Fpfd { get { return this._fpfd.Value; } }
        public decimal Int { get { return this._int.Value; } }
        public decimal Frac1 { get { return this._frac1.Value; } }
        public decimal Mod2 { get { return this._mod2.Value; } }
        public decimal Frac2 { get { return this._frac2.Value; } }
        public decimal OutputDivider { get { return OUTPUT_DIVIDER[this._outputDividerIndex.Value]; } }
        public int OutputDividerIndex
        {
            get { return this._outputDividerIndex.Value; }
            set
            {
                if (value == this._outputDividerIndex.Value)
                    return;

                this._outputDividerIndex.Value = value;
            }
        }
        public decimal RefOut { get { return this._refOut.Value; } }
        #endregion

        #region errors
        public bool IsIntValid { get { return this.Int >= INT45_LOWER_LIMIT && this.Int <= INT98_UPPER_LIMIT; } }
        public bool IsMod2Valid { get { return this.Mod2 >= MOD2_LOWER_LIMIT && this.Mod2 <= MOD2_UPPER_LIMIT; } }
        public bool IsVcoValid { get { return this.Vco >= VCO_LOWER_LIMIT && this.Vco <= VCO_UPPER_LIMIT; } }
        #endregion

        public bool IntMode { get { return this._intMode.Value; } }

        #region registers
        #region register 0
        public bool Autocal 
        { 
            get { return this._autocal.Value; }
            set
            {
                if (value == this._autocal.Value)
                    return;

                this._autocal.Value = value;
            }
        }
        public int Autocal_Int { get { return this.Autocal ? 1 : 0; } }
        public int Prescaler 
        { 
            get { return this._prescaler.Value; }
            set
            {
                if (value == this._prescaler.Value)
                    return;

                this._prescaler.Value = value;
            }
        }
        public string Prescaler_Text { get { return PRESCALER_TEXT[this._prescaler.Value]; } }
        public decimal Register0_Dec { get { return this._reg0.Value; } }
        public string Register0_BinStr { get { return NUM_TO_BIN_STR(this._reg0.Value, 8); } }
        public string Register0_HexStr { get { return NUM_TO_HEX_STR(this._reg0.Value, 8); } }
        #endregion
        #region register 1
        public string Register1_HexStr { get { return NUM_TO_HEX_STR(this._reg1.Value, 8); } }
        #endregion
        #region register 2
        public string Register2_HexStr { get { return NUM_TO_HEX_STR(this._reg2.Value, 8); } }
        #endregion
        #region register 3
        public bool SDLoadReset 
        { 
            get { return this._sdLoadReset.Value; }
            set
            {
                if (value == this._sdLoadReset.Value)
                    return;

                this._sdLoadReset.Value = value;
            }
        }
        public int SDLoadResetInt { get { return this.SDLoadReset ? 0 : 1; } }
        public bool PhaseResync
        {
            get { return this._phaseResync.Value; }
            set
            {
                if (value == this._phaseResync.Value)
                    return;

                this._phaseResync.Value = value;
            }
        }
        public int PhaseResyncInt{ get { return this.PhaseResync ? 1 : 0; } }
        public bool PhaseAdjustment
        {
            get { return this._phaseAdjustment.Value; }
            set
            {
                if (value == this._phaseAdjustment.Value)
                    return;

                this._phaseAdjustment.Value = value;
            }
        }
        public int PhaseAdjustmentInt { get { return this.PhaseAdjustment ? 1 : 0; } }
        public decimal Phase
        {
            get { return this._phase.Value; }
            set
            {
                if (value == this._phase.Value)
                    return;

                this._phase.Value = value;
            }
        }
        public decimal PhaseValue { get { return Decimal.Floor(this.Phase / 360 * 16777216); } }
        public string Register3_HexStr { get { return NUM_TO_HEX_STR(this._reg3.Value, 8); } }
        #endregion
        #region register 4
        public int MuxOutIndex
        {
            get { return this._muxOutIndex.Value; }
            set
            {
                if (value == this._muxOutIndex.Value)
                    return;

                this._muxOutIndex.Value = value;
            }
        }
        public int MuxOutValue { get { return MUX_OUT_TEXT[this.MuxOutIndex].Item2; } }
        public bool DoubleBuffer
        {
            get { return this._doubleBuffer.Value; }
            set
            {
                if (value == this._doubleBuffer.Value)
                    return;

                this._doubleBuffer.Value = value;
            }
        }
        public int DoubleBufferInt { get { return this.DoubleBuffer ? 1 : 0; } }
        public int CPCurrentIndex
        {
            get { return this._cpCurrentIndex.Value; }
            set
            {
                if (value == this._cpCurrentIndex.Value)
                    return;

                this._cpCurrentIndex.Value = value;
            }
        }
        public decimal CPCurrentValue { get { return CP_CURRENTS[this.CPCurrentIndex]; } }
        public bool ReferenceModeIsSingle
        {
            get { return this._referenceModeIsSingle.Value; }
            set
            {
                if (value == this._referenceModeIsSingle.Value)
                    return;

                this._referenceModeIsSingle.Value = value;
            }
        }
        public int ReferenceModeInt { get { return this.ReferenceModeIsSingle ? 0 : 1; } }
        public bool MuxLevelIs18
        {
            get { return this._muxLevelIs18.Value; }
            set
            {
                if (value == this._muxLevelIs18.Value)
                    return;

                this._muxLevelIs18.Value = value;
            }
        }
        public int MuxLevelInt { get { return this.MuxLevelIs18 ? 0 : 1; } }
        public bool PDPolarityIsNegative
        {
            get { return this._pdPolarityIsNegative.Value; }
            set
            {
                if (value == this._pdPolarityIsNegative.Value)
                    return;

                this._pdPolarityIsNegative.Value = value;
            }
        }
        public int PDPolarityInt { get { return this.PDPolarityIsNegative ? 0 : 1; } }
        public bool PowerDown
        {
            get { return this._powerDown.Value; }
            set
            {
                if (value == this._powerDown.Value)
                    return;

                this._powerDown.Value = value;
            }
        }
        public int PowerDownInt { get { return this.PowerDown ? 1 : 0; } }
        public bool CPThreeState
        {
            get { return this._cpThreeState.Value; }
            set
            {
                if (value == this._cpThreeState.Value)
                    return;

                this._cpThreeState.Value = value;
            }
        }
        public int CPThreeStateInt { get { return this.CPThreeState ? 1 : 0; } }
        public bool CounterReset
        {
            get { return this._counterReset.Value; }
            set
            {
                if (value == this._counterReset.Value)
                    return;

                this._counterReset.Value = value;
            }
        }
        public int CounterResetInt { get { return this.CounterReset ? 1 : 0; } }
        public decimal Register4 { get { return this._reg4.Value; } }
        public string Register4HexStr { get { return NUM_TO_HEX_STR(this.Register4, 8); } }
        #endregion
        #region register 5*
        public decimal Register5 { get { return this._reg5.Value; } }
        public string Register5HexStr { get { return NUM_TO_HEX_STR(this.Register5, 8); } }
        #endregion
        #region register 6
        public int FeedBack
        {
            get { return this._feedback.Value; }
            set
            {
                if (value == this._feedback.Value)
                    return;

                this._feedback.Value = value;
            }
        }
        public int CPBleedCurrentInt
        {
            get { return this._cpBleedCurrentInt.Value; }
            set
            {
                if (value == this._cpBleedCurrentInt.Value)
                    return;

                this._cpBleedCurrentInt.Value = value;
            }
        }
        public bool MuteTillLockDetect
        {
            get { return this._muteTillLockDetect.Value; }
            set
            {
                if (value == this._muteTillLockDetect.Value)
                    return;

                this._muteTillLockDetect.Value = value;
            }
        }
        public bool AuxOutEnable
        {
            get { return this._auxOutEnable.Value; }
            set
            {
                if (value == this._auxOutEnable.Value)
                    return;

                this._auxOutEnable.Value = value;
            }
        }
        public int AuxPowerValue
        {
            get { return this._auxPowerValue.Value; }
            set
            {
                if (value == this._auxPowerValue.Value)
                    return;

                this._auxPowerValue.Value = value;
            }
        }
        public bool RfOutEnable
        {
            get { return this._rfOutEnable.Value; }
            set
            {
                if (value == this._rfOutEnable.Value)
                    return;

                this._rfOutEnable.Value = value;
            }
        }
        public int RfOutPowerValue
        {
            get { return this._rfOutPowerValue.Value; }
            set
            {
                if (value == this._rfOutPowerValue.Value)
                    return;

                this._rfOutPowerValue.Value = value;
            }
        }
        public bool NegativeBleed
        {
            get { return this._negativeBleed.Value; }
            set
            {
                if (value == this._negativeBleed.Value)
                    return;

                this._negativeBleed.Value = value;
            }
        }
        public bool GatedBleed
        {
            get { return this._gatedBleed.Value; }
            set
            {
                if (value == this._gatedBleed.Value)
                    return;

                this._gatedBleed.Value = value;
            }
        }
        public int GatedBleedInt { get { return this.GatedBleed ? 1 : 0; } }
        public int NegativeBleedInt { get { return this.NegativeBleed ? 1 : 0; } }
        public decimal CPBleedCurrentValue { get { return this._cpBleedCurrentValue.Value; } }
        public int MuteTillLockDetectInt { get { return this.MuteTillLockDetect ? 1 : 0; } }
        public int AuxOutEnableInt { get { return this.AuxOutEnable ? 1 : 0; } }
        public int AuxOutPowerInt { get { return (int)decimal.Floor((this._auxPowerValue.Value + 4) / 3); } }
        public int RfOutEnableInt { get { return this.RfOutEnable ? 1 : 0; } }
        public int RfOutPowerInt { get { return (int)decimal.Floor((this._rfOutPowerValue.Value + 4) / 3); } }
        public decimal Register6 { get { return this._reg6.Value; } }
        public string Register6HexStr { get { return NUM_TO_HEX_STR(this.Register6, 8); } }
        public int OutputDividerInt { get { return this._outputDividerIndex.Value; } }
        #endregion
        #region register 7
        public bool LESync
        {
            get { return this._leSync.Value; }
            set
            {
                if (value == this._leSync.Value)
                    return;

                this._leSync.Value = value;
            }
        }
        public int LESyncInt { get { return this.LESync ? 1 : 0; } }
        public int LDCycleCountInt
        {
            get { return this._ldCycleCountInt.Value; }
            set
            {
                if (value == this._ldCycleCountInt.Value)
                    return;

                this._ldCycleCountInt.Value = value;
            }
        }
        public bool LOLMode
        {
            get { return this._lolMode.Value; }
            set
            {
                if (value == this._lolMode.Value)
                    return;

                this._lolMode.Value = value;
            }
        }
        public int LOLModeInt { get { return this.LOLMode ? 1 : 0; } }
        public int FracNPrecisionInt
        {
            get { return this._fracNPrecisionInt.Value; }
            set
            {
                if (value == this._fracNPrecisionInt.Value)
                    return;

                this._fracNPrecisionInt.Value = value;
            }
        }
        public int LDModeInt
        {
            get { return this._ldModeInt.Value; }
            set
            {
                if (value == this._ldModeInt.Value)
                    return;

                this._ldModeInt.Value = value;
            }
        }
        public decimal Register7 { get { return this._reg7.Value; } }
        public string Register7HexStr { get { return NUM_TO_HEX_STR(this.Register7, 8); } }
        #endregion
        #region register 8*
        public decimal Register8 { get { return this._reg8.Value; } }
        public string Register8HexStr { get { return NUM_TO_HEX_STR(this.Register8, 8); } }
        #endregion
        #region register 9
        public bool FastestCalibration
        {
            get { return this._fastestCalibration.Value; }
            set
            {
                if (value == this._fastestCalibration.Value)
                    return;

                this._fastestCalibration.Value = value;
            }
        }
        public int VCOBandDivisonInt
        {
            get { return this._vcoBandDivisionInt.Value; }
            set
            {
                if (value == this._vcoBandDivisionInt.Value)
                    return;

                this._vcoBandDivisionInt.Value = value;
            }
        }
        public int TimeoutInt
        {
            get { return this._timeoutInt.Value; }
            set
            {
                if (value == this._timeoutInt.Value)
                    return;

                this._timeoutInt.Value = value;
            }
        }
        public int SynthLockTimeoutInt
        {
            get { return this._synthLockTimeoutInt.Value; }
            set
            {
                if (value == this._synthLockTimeoutInt.Value)
                    return;

                this._synthLockTimeoutInt.Value = value;
            }
        }
        public decimal TotalCalculatedTime
        {
            get { return this._totalCalculatedTime.Value; }
            set
            {
                if (value == this._totalCalculatedTime.Value)
                    return;

                this._totalCalculatedTime.Value = value;
            }
        }
        public decimal Register9 { get { return this._reg9.Value; } }
        public string Register9HexStr { get { return NUM_TO_HEX_STR(this.Register9, 8); } }
        #endregion
        #region register 10
        public int ADCClockDividerInt
        {
            get { return this._adcClockDividerInt.Value; }
            set
            {
                if (value == this._adcClockDividerInt.Value)
                    return;

                this._adcClockDividerInt.Value = value;
            }
        }
        public bool ADCConversion
        {
            get { return this._adcConversion.Value; }
            set
            {
                if (value == this._adcConversion.Value)
                    return;

                this._adcConversion.Value = value;
            }
        }
        public int ADCConversionInt { get { return this.ADCConversion ? 1 : 0; } }
        public bool ADCEnable
        {
            get { return this._adcEnable.Value; }
            set
            {
                if (value == this._adcEnable.Value)
                    return;

                this._adcEnable.Value = value;
            }
        }
        public int ADCEnableInt { get { return this.ADCEnable ? 1 : 0; } }
        public bool ADCClockDividerAutoset
        {
            get { return this._adcClockDividerAutoset.Value; }
            set
            {
                if (value == this._adcClockDividerAutoset.Value)
                    return;

                this._adcClockDividerAutoset.Value = value;
            }
        }
        public decimal Register10 { get { return this._reg10.Value; } }
        public string Register10HexStr { get { return NUM_TO_HEX_STR(this.Register10, 8); } }
        public decimal Frequency { get { return this._frequency.Value; } }
        #endregion
        #region register 11*
        public decimal Register11 { get { return this._reg11.Value; } }
        public string Register11HexStr { get { return NUM_TO_HEX_STR(this.Register11, 8); } }
        #endregion
        #region register 12
        private int ResyncClockInt
        {
            get { return this._resyncClockInt.Value; }
            set
            {
                if (value == this._resyncClockInt.Value)
                    return;

                this._resyncClockInt.Value = value;
            }
        }
        private decimal ResyncClockTimeout
        {
            get { return this._resyncClockTimeout.Value; }
            set
            {
                if (value == this._resyncClockTimeout.Value)
                    return;

                this._resyncClockTimeout.Value = value;
            }
        }
        public decimal Register12 { get { return this._reg12.Value; } }
        public string Register12HexStr { get { return NUM_TO_HEX_STR(this.Register12, 8); } }
        #endregion
        #endregion

        #endregion

        #region fields
        private ActiveVar<decimal> _referenceInput;
        private ActiveVar<decimal> _divider;
        private ActiveVar<bool> _doubler;
        private ActiveVar<bool> _divideby2;
        private ActiveVar<decimal> _vco;
        private ActiveVar<decimal> _fchsp;
        private ActiveVar<int> _outputDividerIndex;
        
        private ActiveVar<decimal> _n;
        private ActiveVar<decimal> _fpfd;
        private ActiveVar<decimal> _int;
        private ActiveVar<decimal> _frac1;
        private ActiveVar<decimal> _mod2;
        private ActiveVar<decimal> _frac2;
        private ActiveVar<decimal> _refOut;

        #region errors
        //private ActiveVar<bool> _errFrac2;
        //private ActiveVar<bool> _errMod2;
        //private ActiveVar<bool> _errFrac1;
        //private ActiveVar<bool> _errInt;
        //private ActiveVar<bool> _errVco;
        private ActiveVar<bool> _errInt;

        #endregion

        private ActiveVar<bool> _intMode;

        #region registers
        #region register 0
        private ActiveVar<bool> _autocal;
        private ActiveVar<int> _prescaler;
        private ActiveVar<decimal> _reg0;
        #endregion
        #region register 1
        private ActiveVar<decimal> _reg1;
        #endregion
        #region register 2
        private ActiveVar<decimal> _reg2;
        #endregion
        #region register 3
        private ActiveVar<bool> _sdLoadReset;
        private ActiveVar<bool> _phaseResync;
        private ActiveVar<bool> _phaseAdjustment;
        private ActiveVar<decimal> _phase;
        private ActiveVar<decimal> _reg3;
        #endregion
        #region register 4
        private ActiveVar<int> _muxOutIndex;
        private ActiveVar<bool> _doubleBuffer;
        private ActiveVar<int> _cpCurrentIndex;
        private ActiveVar<bool> _referenceModeIsSingle;
        private ActiveVar<bool> _muxLevelIs18;
        private ActiveVar<bool> _pdPolarityIsNegative;
        private ActiveVar<bool> _powerDown;
        private ActiveVar<bool> _cpThreeState;
        private ActiveVar<bool> _counterReset;
        private ActiveVar<decimal> _reg4;
        #endregion
        #region register 5*
        private ActiveVar<decimal> _reg5;
        #endregion
        #region register 6
        private ActiveVar<bool> _gatedBleed;
        private ActiveVar<bool> _negativeBleed;
        private ActiveVar<int> _feedback;
        private ActiveVar<decimal> _cpBleedCurrentValue;
        private ActiveVar<int> _cpBleedCurrentInt;
        private ActiveVar<bool> _muteTillLockDetect;
        private ActiveVar<bool> _auxOutEnable;
        private ActiveVar<int> _auxPowerValue;
        private ActiveVar<bool> _rfOutEnable;
        private ActiveVar<int> _rfOutPowerValue;
        private ActiveVar<decimal> _reg6;
        #endregion
        #region register 7
        private ActiveVar<bool> _leSync;
        private ActiveVar<int> _ldCycleCountInt;
        private ActiveVar<bool> _lolMode;
        private ActiveVar<int> _fracNPrecisionInt;
        private ActiveVar<int> _ldModeInt;
        private ActiveVar<decimal> _reg7;
        #endregion
        #region register 8*
        private ActiveVar<decimal> _reg8;
        #endregion
        #region register 9
        private ActiveVar<int> _vcoBandDivisionInt;
        private ActiveVar<int> _timeoutInt;
        private ActiveVar<int> _synthLockTimeoutInt;
        private ActiveVar<bool> _fastestCalibration;
        private ActiveVar<decimal> _reg9;
        private ActiveVar<decimal> _totalCalculatedTime;
        #endregion
        #region register 10
        private ActiveVar<int> _adcClockDividerInt;
        private ActiveVar<bool> _adcConversion;
        private ActiveVar<bool> _adcEnable;
        private ActiveVar<decimal> _reg10;
        private ActiveVar<decimal> _frequency;
        private ActiveVar<bool> _adcClockDividerAutoset;
        #endregion
        #region register 11*
        private ActiveVar<decimal> _reg11;
        #endregion
        #region register 12
        private ActiveVar<int> _resyncClockInt;
        private ActiveVar<decimal> _resyncClockTimeout;
        private ActiveVar<decimal> _reg12;
        #endregion
        #endregion

        #region other
        #endregion

        #endregion

        #region calculation methods
        private void calculateN(object sender, EventArgs e)
        {
            decimal val = this.Vco / this.Fpfd;

            if (this._n.Value == val)
                return;

            this._n.Value = val;
        }
        private void calculateFpfd(object sender, EventArgs e)
        {
            decimal val = 1;
            if (this.Doubler)
                val *= 2;
            if (this.Divideby2)
                val /= 2;

            val = this.ReferenceInput * val / this.Divider;

            if (this._fpfd.Value == val)
                return;

            if (val <= 1)
                val = 1;

            this._fpfd.Value = val;
        }
        private void calculateInt(object sender, EventArgs e)
        {
            decimal val = decimal.Floor(this.N);

            if (this._int.Value == val)
                return;

            this._int.Value = val;
        }
        private void calculateFrac1(object sender, EventArgs e)
        {
            decimal val = decimal.Floor((this.N - this.Int) * MOD1);

            if (this._frac1.Value == val)
                return;

            this._frac1.Value = val;
        }
        private void calculateMod2(object sender, EventArgs e)
        {
            decimal val = this.Fpfd * MEGA / GCD(this.Fpfd * MEGA, this.Fchsp * KILO);

            if (this._mod2.Value == val)
                return;

            this._mod2.Value = val;
        }
        private void calculateFrac2(object sender, EventArgs e)
        {
            
            decimal val = ((this.N - this.Int) * MOD1 - this.Frac1) * this.Mod2;

            if (this._frac2.Value == val)
                return;

            this._frac2.Value = val;
        }
        private void calculateRefOut(object sender, EventArgs e)
        {
            decimal val = this.Vco / this.OutputDivider;

            if (this._refOut.Value == val)
                return;

            this._refOut.Value = val;
        }
        private void calculateIntMode(object sender, EventArgs e)
        {
            bool val = this.Frac1 == 0 && this.Frac2 == 0;

            if (this._intMode.Value == val)
                return;

            this._intMode.Value = val;
        }
        private void calculateAutosetReg9(object sender, EventArgs e)
        {
            this.numVCOBandDivision.Enabled = !this.FastestCalibration;
            this.numTimeout.Enabled = !this.FastestCalibration;
            this.numSynthTimeout.Enabled = !this.FastestCalibration;

            if (this.FastestCalibration)
            {
                int val;
                val = (int)decimal.Ceiling(this.Fpfd * 5 / 12);
                if (val != this._vcoBandDivisionInt.Value)
                    this._vcoBandDivisionInt.Value = val;

                val = (int)decimal.Ceiling(1.7M * this.Fpfd);
                if(val != this._timeoutInt.Value)
                    this._timeoutInt.Value = val;
                
                val = (int)decimal.Ceiling(20 * this.Fpfd / this.TimeoutInt);
                if (val != this._synthLockTimeoutInt.Value)
                    this._synthLockTimeoutInt.Value = val;
            }
        }
        private void calculateTotalTime(object sender, EventArgs e)
        {
            decimal val = 11 * 16 * this.VCOBandDivisonInt / this.Fpfd;

            if (this._totalCalculatedTime.Value == val)
                return;

            this._totalCalculatedTime.Value = val;
        }
        private void calculateAutosetReg10(object sender, EventArgs e)
        {
            this.numADCClockDivider.Enabled = !this.ADCClockDividerAutoset;
            if (this.ADCClockDividerAutoset)
            {
                int val = (int)decimal.Ceiling((this.Fpfd * 10 - 2) / 4);
                if (val > 255) val = 255;

                if (val != this._adcClockDividerInt.Value)
                    this._adcClockDividerInt.Value = val;
            }
        }
        private void calculateFrequency(object sender, EventArgs e)
        {
            int temp = this.ADCClockDividerInt;
            if (temp == 0) temp = 1;
            decimal val =this.Fpfd * 1000 / temp;

            if (this._frequency.Value == val)
                return;

            this._frequency.Value = val;
        }
        private void calculateResyncClockTimeout(object sender, EventArgs e)
        {
            decimal val = this.ResyncClockInt / this.Fpfd;

            if (this._resyncClockTimeout.Value == val)
                return;

            this._resyncClockTimeout.Value = val;
        }


        #region register calculations
        private void calculateRegister0(object sender, EventArgs e)
        {
            decimal val = REGISTER_VAL(0, 10, this.Autocal_Int, 1, this.Prescaler, 1, this.Int, 16, 0, 4);

            if (val == this._reg0.Value)
                return;

            this._reg0.Value = val;
        }
        private void calculateRegister1(object sender, EventArgs e)
        {
            decimal val = REGISTER_VAL(0, 4, this.Frac1, 24, 1, 4);

            if (val == this._reg1.Value)
                return;

            this._reg1.Value = val;
        }
        private void calculateRegister2(object sender, EventArgs e)
        {
            decimal val = REGISTER_VAL(this.Frac2, 14, this.Mod2, 14, 2, 4);

            if (val == this._reg2.Value)
                return;

            this._reg2.Value = val;
        }
        private void calculateRegister3(object sender, EventArgs e)
        {
            decimal val = REGISTER_VAL(0, 1, this.SDLoadResetInt, 1, this.PhaseResyncInt, 1
                , this.PhaseAdjustmentInt, 1, this.PhaseValue, 24, 3, 4);

            if (val == this._reg3.Value)
                return;

            this._reg3.Value = val;
        }
        private void calculateRegister4(object sender, EventArgs e)
        {
            decimal val = REGISTER_VAL(0, 2, this.MuxOutValue, 3, this.DoublerInt, 1, this.Divideby2Int, 1
                , this.Divider, 10, this.DoubleBufferInt, 1, this.CPCurrentIndex, 4, this.ReferenceModeInt, 1
                , this.MuxLevelInt, 1, this.PDPolarityInt, 1, this.PowerDownInt, 1, this.CPThreeStateInt, 1
                , this.CounterResetInt, 1, 4, 4);

            if (val == this._reg4.Value)
                return;

            this._reg4.Value = val;
        }
        private void calculateRegister6(object sender, EventArgs e)
        {
            decimal val = REGISTER_VAL(0, 1, this.GatedBleedInt, 1, this.NegativeBleedInt, 1, 0b1010, 4
                , this.FeedBack, 1, this.OutputDividerInt, 3, this.CPBleedCurrentInt, 8, 0, 1
                , this.MuteTillLockDetectInt, 1, 0, 1, this.AuxOutEnableInt, 1, this.AuxOutPowerInt, 2
                , this.RfOutEnableInt, 1, this.RfOutPowerInt, 2, 6, 4);

            if (val == this._reg6.Value)
                return;

            this._reg6.Value = val;
        }
        private void calculateRegister7(object sender, EventArgs e)
        {
            decimal val = REGISTER_VAL(0b000100, 6, this.LESyncInt, 1, 0, 15, this.LDCycleCountInt, 2
                , this.LOLModeInt, 1, this.FracNPrecisionInt, 2, this.LDModeInt, 1, 7, 4);

            if (val == this._reg7.Value)
                return;

            this._reg7.Value = val;
        }
        private void calculateRegister9(object sender, EventArgs e)
        {
            decimal val = REGISTER_VAL(this.VCOBandDivisonInt, 8, this.TimeoutInt, 10, 0b11111, 5
                , this.SynthLockTimeoutInt, 5, 9, 4);

            if (val == this._reg9.Value)
                return;

            this._reg9.Value = val;
        }
        private void calculateRegister10(object sender, EventArgs e)
        {
            decimal val = REGISTER_VAL(0b011000000000000000, 18, this.ADCClockDividerInt, 8
                , this.ADCConversionInt, 1, this.ADCEnableInt, 1, 10, 4);

            if (val == this._reg10.Value)
                return;

            this._reg10.Value = val;
        }
        private void calculateRegister12(object sender, EventArgs e)
        {
            decimal val = REGISTER_VAL(this.ResyncClockInt, 16, 0b000001010000, 12, 12, 4);

            if (val == this._reg12.Value)
                return;

            this._reg12.Value = val;
        }
        #endregion

        #endregion

        #region validation methods
        private void validateInt(object sender, EventArgs e)
        {
            if (this.IsIntValid)
                this.erpInt.Clear();
            else
            {
                this.erpInt.SetIconAlignment(this.lblInt, ErrorIconAlignment.MiddleRight);
                this.erpInt.SetError(this.lblInt, ERROR_INT);
            }
        }
        private void validateMod2(object sender, EventArgs e)
        {
            if (this.IsMod2Valid)
                this.erpMod2.Clear();
            else
            {
                this.erpMod2.SetIconAlignment(this.numMod2, ErrorIconAlignment.MiddleRight);
                this.erpMod2.SetError(this.numMod2, ERROR_MOD2);
            }
        }
        private void validateVco(object sender, EventArgs e)
        {
            if (this.IsVcoValid)
                this.erpVco.Clear();
            else
            {
                this.erpVco.SetIconAlignment(this.lblVCO, ErrorIconAlignment.MiddleLeft);
                this.erpVco.SetError(this.lblVCO, ERROR_VCO);
            }
        }
        private void validatePhaseResync(object sender, EventArgs e)
        {
            if (this.PhaseResync)
            {
                this.hpPhaseResync.SetIconAlignment(this.cbPhaseResync, ErrorIconAlignment.TopRight);
                this.hpPhaseResync.SetError(this.cbPhaseResync, "Do not forget to set the \"phase resync\"" +
                    " in Register 12!");
            }
            else
                this.hpPhaseResync.Clear();

            bool b1 = this.FeedBack != 0 && this.OutputDividerInt != 0 && this.PhaseResync;
            bool b2 = !this.SDLoadReset && this.PhaseResync;
            bool b3 = this.Frac2 != 0 && this.PhaseResync;
            this.gbPhaseResync.Visible = b1 || b2 || b3;
            this.hlblPhaseResync2.Visible = b1;
            this.hlblPhaseResync3.Visible = b2;
            this.hlblPhaseResync4.Visible = b3;
        }
        private void validatePhaseAdjust(object sender, EventArgs e)
        {
            if (this.PhaseAdjustment)
            {
                this.hpPhaseAdjust.SetIconAlignment(this.cbPhaseAdjust, ErrorIconAlignment.TopRight);
                this.hpPhaseAdjust.SetError(this.cbPhaseAdjust, "Do not forget to set the \"phase value\"" +
                    " in Register 3!");
            }
            else
                this.hpPhaseAdjust.Clear();

            bool b1 = this.PhaseAdjustment && this.Autocal;
            bool b2 = this.PhaseAdjustment && this.SDLoadReset;
            bool b3 = this.PhaseAdjustment && this.PhaseResync;
            this.gbPhaseAdjust.Visible = b1 || b2 || b3;
            this.hlblPhaseAdjust1.Visible = b1;
            this.hlblPhaseAdjust2.Visible = b2;
            this.hlblPhaseAdjust3.Visible = b3;
        }
        private void validateMuxout(object sender, EventArgs e)
        {
            if (this.MuxOutValue == 4 || this.MuxOutValue == 3)
            {
                this.hpMuxout.SetIconAlignment(this.cbMuxout, ErrorIconAlignment.MiddleRight);
                this.hpMuxout.SetError(this.cbMuxout, "While writing to Register 1, Muxout must not be set" +
                    " to the N divider output or R divider output.");
            }
            else
                this.hpMuxout.Clear();
        }
        private void validateCPCurrent(object sender, EventArgs e)
        {
            if (this.CPCurrentValue != 0.94M)
            {
                this.hpCPCurrent.SetIconAlignment(this.cbCPCurrent, ErrorIconAlignment.MiddleRight);
                this.hpCPCurrent.SetError(this.cbCPCurrent, "For lowest spurs, 0.9 mA is recommended.");
            }
            else
                this.hpCPCurrent.Clear();
        }
        private void validateDoubler(object sender, EventArgs e)
        {
            bool b1 = this.Doubler && this.ReferenceInput > 100;
            this.gbDoubler.Visible = b1;
            this.hlblDoublerError1.Visible = b1;
        }
        private void validateAutocal(object sender, EventArgs e)
        {
            if (!this.Autocal)
            {
                this.hpAutocal.SetIconAlignment(this.cbAutocal, ErrorIconAlignment.MiddleRight);
                this.hpAutocal.SetError(this.cbAutocal, "Disable autocal only for fixed freq." +
                    " app., phase adjust app. or very small freq. jumps.");
            }
            else
                this.hpAutocal.Clear();
        }
        private void validateSDLoadReset(object sender, EventArgs e)
        {
            if (this.SDLoadReset)
            {
                this.hpSDLoadReset.SetIconAlignment(this.cbSDLoadReset, ErrorIconAlignment.MiddleRight);
                this.hpSDLoadReset.SetError(this.cbSDLoadReset, "This reset may not be reasonable for app." +
                    " in which phase is continually adjusted.");
            }
            else
                this.hpSDLoadReset.Clear();
        }
        private void validateNegativeBleed(object sender, EventArgs e)
        {
            bool b1 = this.NegativeBleed && this.Frac1 == 0 && this.Frac2 == 0;
            bool b2 = this.NegativeBleed && this.Fpfd > 100;
            this.gbNegativeBleed.Visible = b1 || b2;
            this.hlblNegativeBleedError1.Visible = b1;
            this.hlblNegativeBleedError2.Visible = b2;
        }
        private void validateCPBleedCurrent(object sender, EventArgs e)
        {
            if (this.Fpfd <= 80)
            {
                decimal optimal = decimal.Floor(39 * this.Fpfd / 61.44M * this.CPCurrentValue / 0.9M);
                bool b1 = this.NegativeBleed && this.CPBleedCurrentInt != optimal;
                this.gbCPBleedCurrent.Visible = b1;
                this.hlblCPBleedCurrent1.Text = "Optimal bleed value is " + optimal.ToString();
                this.hlblCPBleedCurrent1.Visible = b1;
            }
            else if (this.Fpfd <= 100)
            {
                decimal optimal = decimal.Floor(42 * this.CPCurrentValue / 0.9M);
                bool b1 = this.NegativeBleed && this.CPBleedCurrentInt != optimal;
                this.gbCPBleedCurrent.Visible = b1;
                this.hlblCPBleedCurrent1.Text = "Optimal bleed value is " + optimal.ToString();
                this.hlblCPBleedCurrent1.Visible = b1;
            }
            else
            {
                bool b1 = false;
                this.gbCPBleedCurrent.Visible = b1;
                this.hlblCPBleedCurrent1.Visible = b1;
            }
        }
        private void validateLOLMode(object sender, EventArgs e)
        {
            bool b1 = this.LOLMode && !this.ReferenceModeIsSingle;
            this.gbLOLMode.Visible = b1;
            this.hlblLOLModeError1.Visible = b1;
        }
        private void validateLDMode(object sender, EventArgs e)
        {
            if (this.LDModeInt == 0)
            {
                this.hpLDMode.SetIconAlignment(this.cbLDMode, ErrorIconAlignment.MiddleRight);
                this.hpLDMode.SetError(this.cbLDMode, "Do not forget to set the \"Frac-N LD Precision\"" +
                    " in Register 7!");
            }
            else
                this.hpLDMode.Clear();

            bool b1 = this.LDModeInt == 1 && !this.IntMode;
            this.gbLDMode.Visible = b1;
            this.hlblLDModeError1.Visible = b1;
        }
        private void validateADCEnable(object sender, EventArgs e)
        {
            if (this.ADCEnable)
            {
                this.hpADCEnable.SetIconAlignment(this.cbADCEnable, ErrorIconAlignment.MiddleRight);
                this.hpADCEnable.SetError(this.cbADCEnable, "Do not forget to set the \"ADC Clock Divider\"" +
                    " in Register 10!");
            }
            else
                this.hpADCEnable.Clear();
        }

        #endregion

        private void pbUploadToArduinoStart()
        {
            pbUploadToArduino.Style = ProgressBarStyle.Marquee;
        }
        private void pbUploadToArduinoStop()
        {
            pbUploadToArduino.Style = ProgressBarStyle.Blocks;
        }
        private static StringBuilder outputForUploadToArduino = new StringBuilder();
        private static int lineCountForUploadToArduino = 0;
        private void btnUpload_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(ARDUINO_PROCESS.StartInfo.FileName) || string.IsNullOrWhiteSpace(ARDUINO_PROCESS.StartInfo.FileName))
                ARDUINO_PROCESS.StartInfo.FileName = GetArduinoPathFromEnvironment();

            if (string.IsNullOrEmpty(ARDUINO_PROCESS.StartInfo.FileName) || string.IsNullOrWhiteSpace(ARDUINO_PROCESS.StartInfo.FileName))
            {
                MessageBox.Show("Please provide the path of your arduino.exe or arduino_debug.exe next time."
                    , "No path", MessageBoxButtons.OK, MessageBoxIcon.Error); ;
                return;
            }
            pbUploadToArduinoStart();

            //get port name
            string portName = null;
            if(cbPorts.Items.Count > 0)
            {
                string selectedItem = (string)cbPorts.SelectedItem;
                if(!string.IsNullOrWhiteSpace(selectedItem) && !string.IsNullOrEmpty(selectedItem))
                {
                    selectedItem = Regex.Replace(selectedItem, @"\s+", "");
                    if(Regex.IsMatch(selectedItem, @"^COM\d+$"))
                    {
                        portName = selectedItem;
                    }
                }
            }

            if(string.IsNullOrEmpty(portName) || string.IsNullOrWhiteSpace(portName))
            {
                string response = Interaction.InputBox("Something went wrong while detecting COM port of the Arduino. Please write the COM number of"
                    + " your Arduino.", "COM port of Arduino could not detected!");

                if(string.IsNullOrWhiteSpace(response) || string.IsNullOrEmpty(response))
                {
                    MessageBox.Show("The COM port you provided is not correct!", "Incorrect COM port", MessageBoxButtons.OK
                        , MessageBoxIcon.Error);
                    pbUploadToArduinoStop();
                    return;
                }

                response = Regex.Replace(response, @"\s+", "");
                if (Regex.IsMatch(response, @"^COM\d+$"))
                    portName = response;
                else if(Regex.IsMatch(response, @"^\d+$"))
                    portName = "COM" + response;
                else
                {
                    MessageBox.Show("The COM port you provided is not correct!", "Incorrect COM port", MessageBoxButtons.OK
                        , MessageBoxIcon.Error);
                    pbUploadToArduinoStop();
                    return;
                }
            }

            MessageBox.Show(portName);

            //return;
            string tempInoPath = Path.Combine(INO_DIR, Path.Combine(TEMP_INO_NAME, TEMP_INO_NAME + ".ino"));
            CheckFolders();
            CreateInoCode(tempInoPath);

            ARDUINO_PROCESS.StartInfo.Arguments = "--upload --port " + portName + " \"" + tempInoPath + "\"";
            ARDUINO_PROCESS.Exited += (s, e2) =>
            {
                pbUploadToArduino.GetCurrentParent().Invoke(new Action(() => { pbUploadToArduinoStop(); }));
                Process process = (Process)s;
                process.CancelOutputRead();
                if (process.ExitCode == 0)
                    MessageBox.Show("Success!");
                else
                {
                    MessageBox.Show("Something went wrong!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    MessageBox.Show(outputForOpenWithIde.ToString(), "Output message from Arduino IDE"
                        , MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                outputForUploadToArduino.Clear();
                lineCountForUploadToArduino = 0;
            };
            ARDUINO_PROCESS.OutputDataReceived += (s, e3) =>
            {
                if (!String.IsNullOrEmpty(e3.Data))
                {
                    lineCountForUploadToArduino++;
                    outputForUploadToArduino.Append("\n[" + lineCountForUploadToArduino + "]: " + e3.Data);
                }
            };

            ARDUINO_PROCESS.Start();
            ARDUINO_PROCESS.BeginOutputReadLine();
        }



        private void pbOpenWithIdeStart()
        {
            pbOpenWithIde.Style = ProgressBarStyle.Marquee;
        }
        private void pbOpenWithIdeStop()
        {
            pbOpenWithIde.Style = ProgressBarStyle.Blocks;
        }
        private static StringBuilder outputForOpenWithIde = new StringBuilder();
        private static int lineCountForOpenWithIde = 0;
        private void btnIde_Click(object sender, EventArgs e)
        {
            if(string.IsNullOrEmpty(ARDUINO_PROCESS.StartInfo.FileName) || string.IsNullOrWhiteSpace(ARDUINO_PROCESS.StartInfo.FileName))
                ARDUINO_PROCESS.StartInfo.FileName = GetArduinoPathFromEnvironment();

            if (string.IsNullOrEmpty(ARDUINO_PROCESS.StartInfo.FileName) || string.IsNullOrWhiteSpace(ARDUINO_PROCESS.StartInfo.FileName))
            {
                MessageBox.Show("Please provide the path of your arduino.exe or arduino_debug.exe next time."
                    , "No path", MessageBoxButtons.OK, MessageBoxIcon.Error); ;
                return;
            }

            pbOpenWithIdeStart();

            string tempInoPath = Path.Combine(INO_DIR, Path.Combine(TEMP_INO_NAME, TEMP_INO_NAME + ".ino"));
            CheckFolders();
            CreateInoCode(tempInoPath);

            ARDUINO_PROCESS.StartInfo.Arguments = "\"" + tempInoPath + "\"";
            ARDUINO_PROCESS.Exited += (s, e2) =>
            {
                pbOpenWithIde.GetCurrentParent().Invoke(new Action(() => { pbOpenWithIdeStop(); }));
                Process process = (Process)s;
                process.CancelOutputRead();
                if (process.ExitCode == 0)
                    MessageBox.Show("Success!");
                else
                {
                    MessageBox.Show("Something went wrong!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    MessageBox.Show(outputForOpenWithIde.ToString(), "Output message from Arduino IDE"
                        , MessageBoxButtons.OK, MessageBoxIcon.Error);
                    
                }

                outputForOpenWithIde.Clear();
                lineCountForOpenWithIde = 0;
            };
            ARDUINO_PROCESS.OutputDataReceived += (s, e3) =>
            {
                if (!String.IsNullOrEmpty(e3.Data))
                {
                    lineCountForOpenWithIde++;
                    outputForOpenWithIde.Append("\n[" + lineCountForOpenWithIde + "]: " + e3.Data);
                }
            };

            ARDUINO_PROCESS.Start();
            ARDUINO_PROCESS.BeginOutputReadLine();
        }

        private void CreateInoCode(string path)
        {
            string[] pllLines = Properties.Resources.pll_draft.Split('\n');

            string[,] registers = new string[13,4];
            #region reg0
            registers[0, 0] = this.Register0_HexStr.Substring(0, 2);
            registers[0, 1] = this.Register0_HexStr.Substring(2, 2);
            registers[0, 2] = this.Register0_HexStr.Substring(4, 2);
            registers[0, 3] = this.Register0_HexStr.Substring(6, 2);
            #endregion
            #region reg1
            registers[1, 0] = this.Register1_HexStr.Substring(0, 2);
            registers[1, 1] = this.Register1_HexStr.Substring(2, 2);
            registers[1, 2] = this.Register1_HexStr.Substring(4, 2);
            registers[1, 3] = this.Register1_HexStr.Substring(6, 2);
            #endregion
            #region reg2
            registers[2, 0] = this.Register2_HexStr.Substring(0, 2);
            registers[2, 1] = this.Register2_HexStr.Substring(2, 2);
            registers[2, 2] = this.Register2_HexStr.Substring(4, 2);
            registers[2, 3] = this.Register2_HexStr.Substring(6, 2);
            #endregion
            #region reg3
            registers[3, 0] = this.Register3_HexStr.Substring(0, 2);
            registers[3, 1] = this.Register3_HexStr.Substring(2, 2);
            registers[3, 2] = this.Register3_HexStr.Substring(4, 2);
            registers[3, 3] = this.Register3_HexStr.Substring(6, 2);
            #endregion
            #region reg4
            registers[4, 0] = this.Register4HexStr.Substring(0, 2);
            registers[4, 1] = this.Register4HexStr.Substring(2, 2);
            registers[4, 2] = this.Register4HexStr.Substring(4, 2);
            registers[4, 3] = this.Register4HexStr.Substring(6, 2);
            #endregion
            #region reg5
            registers[5, 0] = this.Register5HexStr.Substring(0, 2);
            registers[5, 1] = this.Register5HexStr.Substring(2, 2);
            registers[5, 2] = this.Register5HexStr.Substring(4, 2);
            registers[5, 3] = this.Register5HexStr.Substring(6, 2);
            #endregion
            #region reg6
            registers[6, 0] = this.Register6HexStr.Substring(0, 2);
            registers[6, 1] = this.Register6HexStr.Substring(2, 2);
            registers[6, 2] = this.Register6HexStr.Substring(4, 2);
            registers[6, 3] = this.Register6HexStr.Substring(6, 2);
            #endregion
            #region reg7
            registers[7, 0] = this.Register7HexStr.Substring(0, 2);
            registers[7, 1] = this.Register7HexStr.Substring(2, 2);
            registers[7, 2] = this.Register7HexStr.Substring(4, 2);
            registers[7, 3] = this.Register7HexStr.Substring(6, 2);
            #endregion
            #region reg8
            registers[8, 0] = this.Register8HexStr.Substring(0, 2);
            registers[8, 1] = this.Register8HexStr.Substring(2, 2);
            registers[8, 2] = this.Register8HexStr.Substring(4, 2);
            registers[8, 3] = this.Register8HexStr.Substring(6, 2);
            #endregion
            #region reg9
            registers[9, 0] = this.Register9HexStr.Substring(0, 2);
            registers[9, 1] = this.Register9HexStr.Substring(2, 2);
            registers[9, 2] = this.Register9HexStr.Substring(4, 2);
            registers[9, 3] = this.Register9HexStr.Substring(6, 2);
            #endregion
            #region reg10
            registers[10, 0] = this.Register10HexStr.Substring(0, 2);
            registers[10, 1] = this.Register10HexStr.Substring(2, 2);
            registers[10, 2] = this.Register10HexStr.Substring(4, 2);
            registers[10, 3] = this.Register10HexStr.Substring(6, 2);
            #endregion
            #region reg11
            registers[11, 0] = this.Register11HexStr.Substring(0, 2);
            registers[11, 1] = this.Register11HexStr.Substring(2, 2);
            registers[11, 2] = this.Register11HexStr.Substring(4, 2);
            registers[11, 3] = this.Register11HexStr.Substring(6, 2);
            #endregion
            #region reg12
            registers[12, 0] = this.Register12HexStr.Substring(0, 2);
            registers[12, 1] = this.Register12HexStr.Substring(2, 2);
            registers[12, 2] = this.Register12HexStr.Substring(4, 2);
            registers[12, 3] = this.Register12HexStr.Substring(6, 2);
            #endregion

            #region line modify
            pllLines[43] = "  WriteADF(0x" + registers[12, 0] + ", 0x" + registers[12, 1] 
                + ", 0x" + registers[12, 2] + ", 0x" + registers[12, 3] + "); //Reg12";

            pllLines[45] = "  WriteADF(0x" + registers[11, 0] + ", 0x" + registers[11, 1]
                + ", 0x" + registers[11, 2] + ", 0x" + registers[11, 3] + "); //Reg11";

            pllLines[47] = "  WriteADF(0x" + registers[10, 0] + ", 0x" + registers[10, 1]
                + ", 0x" + registers[10, 2] + ", 0x" + registers[10, 3] + "); //Reg10";

            pllLines[49] = "  WriteADF(0x" + registers[9, 0] + ", 0x" + registers[9, 1]
                + ", 0x" + registers[9, 2] + ", 0x" + registers[9, 3] + "); //Reg9";

            pllLines[51] = "  WriteADF(0x" + registers[8, 0] + ", 0x" + registers[8, 1]
                + ", 0x" + registers[8, 2] + ", 0x" + registers[8, 3] + "); //Reg8";

            pllLines[53] = "  WriteADF(0x" + registers[7, 0] + ", 0x" + registers[7, 1]
                + ", 0x" + registers[7, 2] + ", 0x" + registers[7, 3] + "); //Reg7";

            pllLines[55] = "  WriteADF(0x" + registers[6, 0] + ", 0x" + registers[6, 1]
                + ", 0x" + registers[6, 2] + ", 0x" + registers[6, 3] + "); //Reg6";

            pllLines[57] = "  WriteADF(0x" + registers[5, 0] + ", 0x" + registers[5, 1]
                + ", 0x" + registers[5, 2] + ", 0x" + registers[5, 3] + "); //Reg5";

            pllLines[59] = "  WriteADF(0x" + registers[4, 0] + ", 0x" + registers[4, 1]
                + ", 0x" + registers[4, 2] + ", 0x" + registers[4, 3] + "); //Reg4";

            pllLines[61] = "  WriteADF(0x" + registers[3, 0] + ", 0x" + registers[3, 1]
                + ", 0x" + registers[3, 2] + ", 0x" + registers[3, 3] + "); //Reg3";

            pllLines[63] = "  WriteADF(0x" + registers[2, 0] + ", 0x" + registers[2, 1]
                + ", 0x" + registers[2, 2] + ", 0x" + registers[2, 3] + "); //Reg2";

            pllLines[65] = "  WriteADF(0x" + registers[1, 0] + ", 0x" + registers[1, 1]
                + ", 0x" + registers[1, 2] + ", 0x" + registers[1, 3] + "); //Reg1";

            pllLines[67] = "  WriteADF(0x" + registers[0, 0] + ", 0x" + registers[0, 1]
                + ", 0x" + registers[0, 2] + ", 0x" + registers[0, 3] + "); //Reg0";
            #endregion

            if (!Directory.Exists(Path.GetDirectoryName(path)))
                Directory.CreateDirectory(Path.GetDirectoryName(path));

            File.WriteAllLines(path, pllLines);
        }

        private void mmSave_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Inputs are saved.", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
            SaveSettings(DEFAULT_SETTINGS_FILE);
            SettingsAreSaved = true;
        }
        private void mmSaveAs_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "xml files|*.xml";
            saveFileDialog.DefaultExt = ".xml";
            saveFileDialog.ShowDialog();
            string filePath = saveFileDialog.FileName;

            if (!string.IsNullOrEmpty(filePath) && !string.IsNullOrWhiteSpace(filePath))
            {
                MessageBox.Show("Inputs are saved as " + filePath + ".", "", MessageBoxButtons.OK
                    , MessageBoxIcon.Information);
                SaveSettings(filePath);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!SettingsAreSaved)
            {
                switch(MessageBox.Show("Do you want to save changes?"
                    , "Save changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question))
                {
                    case DialogResult.Yes:
                        SaveSettings(DEFAULT_SETTINGS_FILE);
                        SettingsAreSaved = true;
                        break;
                    case DialogResult.Cancel:
                        e.Cancel = true;
                        break;
                }
            }
        }

        

        //for debug purposes
        private void button2_Click(object sender, EventArgs e2)
        {
            Console.WriteLine("Serial port names: ");
            string[] names = SerialPort.GetPortNames();
            foreach(string name in names)
            {
                Console.WriteLine(name);
            }
            Console.WriteLine("finish");
        }

        #region about radio buttons
        private void rbSingle_CheckedChanged(object sender, EventArgs e)
        {
            if (this.rbDifferential.Checked != !this.rbSingle.Checked)
                this.rbDifferential.Checked = !this.rbSingle.Checked;
        }

        private void rbDifferential_CheckedChanged(object sender, EventArgs e)
        {
            if (this._referenceModeIsSingle.Value != !this.rbDifferential.Checked)
                this._referenceModeIsSingle.Value= !this.rbDifferential.Checked;
        }

        private void rb18_CheckedChanged(object sender, EventArgs e)
        {
            if (this.rb33.Checked != !this.rb18.Checked)
                this.rb33.Checked = !this.rb18.Checked;
        }

        private void rb33_CheckedChanged(object sender, EventArgs e)
        {
            if (this._muxLevelIs18.Value != !this.rb33.Checked)
                this._muxLevelIs18.Value = !this.rb33.Checked;
        }

        private void rbNegative_CheckedChanged(object sender, EventArgs e)
        {
            if (this.rbPositive.Checked != !this.rbNegative.Checked)
                this.rbPositive.Checked = !this.rbNegative.Checked;
        }

        private void rbPositive_CheckedChanged(object sender, EventArgs e)
        {
            if (this._pdPolarityIsNegative.Value != !this.rbPositive.Checked)
                this._pdPolarityIsNegative.Value = !this.rbPositive.Checked;
        }
        #endregion

        private void tmCheckPorts_Tick(object sender, EventArgs e)
        {
            Console.WriteLine("Detected serial port names: ");
            string[] names = SerialPort.GetPortNames();
            /*foreach (string name in names)
            {
                Console.WriteLine(name);
            }*/

            int lenOfCbPorts = cbPorts.Items.Count;
            int numOfDetectedPort = names.Length;

            if(numOfDetectedPort == lenOfCbPorts)
            {
                bool same = true;
                for(int i = 0; i < lenOfCbPorts; i++)
                {
                    if ((string)(cbPorts.Items[i]) != names[i])
                    {
                        same = false;
                        break;
                    }
                }

                if (same) return;
            }

            List<string> oldPortList = new List<string>(lenOfCbPorts);
            for(int i = 0; i < lenOfCbPorts; i++)
                oldPortList.Add((string)cbPorts.Items[i]);

            cbPorts.Items.Clear();
            foreach (string name in names)
                cbPorts.Items.Add(name);

            for(int i = 0; i < numOfDetectedPort; i++)
            {
                if (!oldPortList.Contains(names[i]))
                {
                    cbPorts.SelectedIndex = i;
                    return;
                }
            }
        }
    }

    public class AppSettings
    {
        public AppSettings()
        {

        }

        //inputs
        public decimal ReferenceInput { get; set; }
        public decimal Divider { get; set; }
        public bool DivideBy2 { get; set; }
        public bool Doubler { get; set; }
        public decimal ChannelSpacing { get; set; }
        public decimal VCOOutput { get; set; }
        public int OutputDividerIndex { get; set;}
        //reg1
        public bool AutoCalibration { get; set; }
        public int Prescaler { get; set; }
        //reg3
        public bool SDLoadReset { get; set; }
        public bool PhaseResync { get; set; }
        public bool PhaseAdjustment { get; set; }
        public decimal Phase { get; set; }
        //reg4
        public int MuxOutputIndex { get; set; }
        public bool DoubleBuffer { get; set; }
        public int CPCurrentIndex { get; set; }
        public bool ReferenceModeIsSingle { get; set; }
        public bool MuxLevelIs18 { get; set; }
        public bool PDPolarityIsNegative { get; set; }
        public bool PowerDown { get; set; }
        public bool CPThreeState { get; set; }
        public bool CounterReset { get; set; }
        //reg6
        public int FeedbackIndex { get; set; }
        public int CPBleedCurrentInt { get; set; }
        public bool MuteTillLockDetect { get; set; }
        public bool AuxOutputEnable { get; set; }
        public int AuxOutputPower { get; set; }
        public bool RfOutputEnable { get; set; }
        public int RfOutputPower { get; set; }
        public bool NegativeBleed { get; set; }
        public bool GatedBleed { get; set; }

        /*public decimal VCOOutput { get; set; }
        public int OutputDividerIndex { get; set; }
        public bool AutoCalibration { get; set; }
        public int Prescaler { get; set; }
        public bool SDLoadReset { get; set; }
        public bool PhaseResync { get; set; }
        public bool PhaseAdjustment { get; set; }
        public decimal Phase { get; set; }
        public int MuxOutputIndex { get; set; }
        public bool DoubleBuffer { get; set; }
        public int CPCurrentIndex { get; set; }
        public bool ReferenceModeIsSingle { get; set; }
        public bool MuxLevelIs18 { get; set; }
        public bool PDPolarityIsNegative { get; set; }
        public bool PowerDown { get; set; }
        public bool CPThreeState { get; set; }
        public bool CounterReset { get; set; }
        public int FeedbackIndex { get; set; }
        public int CPBleedCurrentInt { get; set; }
        public bool MuteTillLockDetect { get; set; }
        public bool AuxOutputEnable { get; set; }*/
    }


}
