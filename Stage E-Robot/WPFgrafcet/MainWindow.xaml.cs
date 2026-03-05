using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Documents.Serialization;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using GrafcetRobot_NS;
using ServoFeetech_NS;

namespace WPFgrafcet
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        Feetech servoManager = new Feetech();
        GrafcetRobot grafcetRobot;
        SerialPort SerialPort1;

        public MainWindow()
        {
            InitializeComponent();

            SerialPort1 = new SerialPort("COM14", 115200, Parity.None, 8, StopBits.One);
            //SerialPort1.DataReceived += SerialPort1_DataReceived;
            SerialPort1.Open();

            grafcetRobot = new GrafcetRobot(servoManager);

            servoManager.servos.Add(new FeetechServo("Plateforme1", 7, FeetechServoModels.STS));
            servoManager.servos.Add(new FeetechServo("Pousser1", 8, FeetechServoModels.STS));
            servoManager.servos.Add(new FeetechServo("Plateforme2", 9, FeetechServoModels.STS));
            servoManager.servos.Add(new FeetechServo("Plateforme3", 10, FeetechServoModels.STS));
            servoManager.servos.Add(new FeetechServo("Plateforme4", 11, FeetechServoModels.STS));
            servoManager.servos.Add(new FeetechServo("Pousser2", 12, FeetechServoModels.STS));

            servoManager.servos.Add(new FeetechServo("All", 0xFE, FeetechServoModels.STS));

            servoManager.OnSendMessageEvent += sendTrame;

            servoManager.WriteServoData(this, new FeetechServoWriteArgs
            {
                Name = "All",
                Location = FeetechMemorySTS.GoalPosition,
                Payload = new byte[] { (byte)(4095 & 0xFF), (byte)(4095 >> 8), }
            });
            Thread.Sleep(Feetech.ServoDelay);
            servoManager.WriteServoData(this, new FeetechServoWriteArgs
            {
                Name = "Pousser2",
                Location = FeetechMemorySTS.GoalPosition,
                Payload = new byte[] { (byte)(0 & 0xFF), (byte)(0 >> 8), }
            });


        }
        private void sendTrame(object? sender, ByteArrayArgs e)
        {
            SerialPort1.Write(e.array, 0, e.array.Length);
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            Cons.Text = "";

        }

        private void Stocker_Click(object sender, RoutedEventArgs e)
        {
            grafcetRobot.Stock();
        }

        private void Pousser_Click(object sender, RoutedEventArgs e)
        {
            grafcetRobot.Push();
        }
    }
}