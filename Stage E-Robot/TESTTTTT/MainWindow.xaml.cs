using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using ExtendedSerialPort_NS;
using ServoFeetech_NS;
using static TESTTTTT.Trames;


namespace TESTTTTT
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public byte moteursNum = 6; // Nombre de moteurs branchés (id de 1 à MAX)

        public SerialPort SerialPort1;
        DispatcherTimer timerAffichage;

        Feetech servoManager = new Feetech();

        public MainWindow()
        {
            InitializeComponent();


            SerialPort1 = new SerialPort("COM14", 115200, Parity.None, 8, StopBits.One);
            SerialPort1.DataReceived += SerialPort1_DataReceived;
            SerialPort1.Open();

            timerAffichage = new DispatcherTimer();
            timerAffichage.Interval = new TimeSpan(0, 0, 0, 0, 50); // NE PAS TROP BAISSER LE TIMER (Buffer requests se dequeue trop lentement, désynchronisament des requêtes)
            timerAffichage.Tick += TimerAffichage_Tick;
            timerAffichage.Start();

            // Reset les moteurs au démarrage

            OnSyncWriteServoDataEvent += servoManager.SyncWriteServoData;
            OnReadServoDataEvent += servoManager.ReadServoData;
            OnWriteServoDataEvent += servoManager.WriteServoData;
            servoManager.OnSendMessageEvent += ServoManager_OnSendMessageEvent;
            OnSendDataToServoEvent += servoManager.DecodeData;
            servoManager.OnServoDataEvent += ServoManager_OnServoDataEvent;

            OnSyncWriteServoData(new byte[] { 0x02, 0x04 }, FeetechMemory.GoalPosition,
                new byte[] { (byte)(0 >> 8), (byte)(0 & 0xFF), 0x00, 0x00, (byte)(1000 >> 8), (byte)(1000 & 0xFF) });
        }

        private void ServoManager_OnServoDataEvent(object? sender, FeetechServoDataArgs e)
        {

        }

        private void ServoManager_OnSendMessageEvent(object? sender, ByteArrayArgs e)
        {
            SerialPort1.Write(e.array, 0, e.array.Length);
        }

        private void SerialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            while (SerialPort1.BytesToRead > 0)
            {
                byte[] buffer = new byte[SerialPort1.BytesToRead];
                SerialPort1.Read(buffer, 0, buffer.Length);
                OnSendDataToServo(buffer);
            }
        }

        private void TimerAffichage_Tick(object? sender, EventArgs e)
        {
            UInt16 pos = 785;
            UInt16 vit = 762;
            //OnWriteServoData(0xFE, FeetechMemory.GoalVelocity, new byte[] { (byte)(vit >> 8), (byte)(vit & 0xFF) });
            //OnWriteServoData(0xFE, FeetechMemory.GoalPosition, new byte[] { (byte)(pos >> 8), (byte)(pos & 0xFF) });

            OnSyncWriteServoData(new byte[] { 0x02,0x04 }, FeetechMemory.GoalPosition, 
                new byte[] { (byte)(pos >> 8), (byte)(pos & 0xFF), 0x00, 0x00, (byte)(vit >> 8), (byte)(vit & 0xFF) });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
           
        }

        private void TClick(object sender, RoutedEventArgs e)
        {

        }

        ///  Output events 
        public event EventHandler<FeetechServoReadArgs> OnReadServoDataEvent;
        public new virtual void OnReadServoData(byte id, FeetechMemory loc, byte nbBytes)
        {
            var handler = OnReadServoDataEvent;
            if (handler != null)
            {
                handler(this, new FeetechServoReadArgs { Id = id, Location = loc, NumberOfBytes = nbBytes});
            }
        }

        public event EventHandler<FeetechServoWriteArgs> OnWriteServoDataEvent;
        public new virtual void OnWriteServoData(byte id, FeetechMemory loc, byte[] payload)
        {
            var handler = OnWriteServoDataEvent;
            if (handler != null)
            {
                handler(this, new FeetechServoWriteArgs { Id = id, Location = loc, Payload = payload });
            }
        }

        public event EventHandler<FeetechServoSyncWriteArgs> OnSyncWriteServoDataEvent;
        public new virtual void OnSyncWriteServoData(byte[] id, FeetechMemory loc, byte[] payload)
        {
            var handler = OnSyncWriteServoDataEvent;
            if (handler != null)
            {
                handler(this, new FeetechServoSyncWriteArgs { Id = id, Location = loc, Payload = payload });
            }
        }

        public event EventHandler<ByteArrayArgs> OnSendDataToServoEvent;
        public new virtual void OnSendDataToServo(byte[] msg)
        {
            var handler = OnSendDataToServoEvent;
            if (handler != null)
            {
                handler(this, new ByteArrayArgs { array = msg});
            }
        }
    }
}