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

// http://doc.feetech.cn/#/prodinfodownload?srcType=FT-SCSCL-emanual-cbcc8ab2e3384282a01d4bf3 (doc technique de tout les appels possibles)

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

            OnActionServoDataEvent += servoManager.ActionServoData;
            OnRegWriteServoDataEvent += servoManager.RegWriteServoData;
            OnSyncWriteServoDataEvent += servoManager.SyncWriteServoData;
            OnSyncReadServoDataEvent += servoManager.SyncReadServoData;
            OnReadServoDataEvent += servoManager.ReadServoData;
            OnWriteServoDataEvent += servoManager.WriteServoData;
            servoManager.OnSendMessageEvent += ServoManager_OnSendMessageEvent;
            OnSendDataToServoEvent += servoManager.DecodeData;
            servoManager.OnServoDataEvent += ServoManager_OnServoDataEvent;

            servoManager.getServos().Add(new FeetechServo("Epaule1", 1, FeetechServoModels.SCS));
            servoManager.getServos().Add(new FeetechServo("Epaule2", 2, FeetechServoModels.SCS));
            servoManager.getServos().Add(new FeetechServo("Coude", 3, FeetechServoModels.SCS));
            servoManager.getServos().Add(new FeetechServo("Poignee1", 4, FeetechServoModels.SCS));
            servoManager.getServos().Add(new FeetechServo("Poignee2", 5, FeetechServoModels.STS));
            servoManager.getServos().Add(new FeetechServo("Main", 6, FeetechServoModels.SM));

            Int16 vit = 1500;
            OnWriteServoData("Epaule1", FeetechMemorySCS.GoalPosition, new byte[] { 0, 0, 0, 0, (byte)(vit >> 8), (byte)(vit & 0xFF) });
            System.Threading.Thread.Sleep(20);
            OnWriteServoData("Epaule2", FeetechMemorySCS.GoalPosition, new byte[] { 0, 0, 0, 0, (byte)(vit >> 8), (byte)(vit & 0xFF) });
            System.Threading.Thread.Sleep(20);
            OnWriteServoData("Coude", FeetechMemorySCS.GoalPosition, new byte[] { 0, 0, 0, 0, (byte)(vit >> 8), (byte)(vit & 0xFF) });
            System.Threading.Thread.Sleep(20);
            OnWriteServoData("Poignee1", FeetechMemorySCS.GoalPosition, new byte[] { 0, 0, 0, 0, (byte)(vit >> 8), (byte)(vit & 0xFF) });
            System.Threading.Thread.Sleep(20);
            OnWriteServoData("Poignee2", FeetechMemorySTS.GoalPosition, new byte[] { 0, 0, 0, 0, (byte)(vit*4 & 0xFF), (byte)(vit*4 >> 8) });
            System.Threading.Thread.Sleep(20);
            OnWriteServoData("Main", FeetechMemorySM.GoalPosition, new byte[] { 0, 0, 0, 0, (byte)(vit*4 & 0xFF), (byte)(vit*4 >> 8) });

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
            UInt16 vit = 1000;
            //OnWriteServoData(0xFE, FeetechMemory.GoalVelocity, new byte[] { (byte)(vit >> 8), (byte)(vit & 0xFF) });
            //OnWriteServoData(0xFE, FeetechMemory.GoalPosition, new byte[] { (byte)(pos >> 8), (byte)(pos & 0xFF) });

            //OnSyncWriteServoData(new byte[] { 1,4 }, FeetechMemory.GoalPosition, 
            //    new byte[] { (byte)(pos >> 8), (byte)(pos & 0xFF), 0x00, 0x00, (byte)(vit >> 8), (byte)(vit & 0xFF) }); // les deux 0x00 correspondent au temps, ici pas utilisé


            OnReadServoData("Main", FeetechMemorySM.PresentPosition, 8);

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            if (!Byte.TryParse(Pos.Text, out byte pos)) pos = 0;
            if (!Byte.TryParse(Time.Text, out byte time)) time = 0;
            if (!Byte.TryParse(Vit.Text, out byte vit)) vit = 0;

            FeetechServo s = servoManager.getServoByName(Name.Text);

            switch (s.Model)
            {
                case FeetechServoModels.SCS:
                    OnWriteServoData(Name.Text, FeetechMemorySCS.GoalPosition, new byte[] { (byte)(pos >> 8), (byte)(pos & 0xFF), (byte)(time >> 8), (byte)(time & 0xFF) });
                    break;
                case FeetechServoModels.STS:
                    OnWriteServoData(Name.Text, FeetechMemorySTS.GoalPosition, new byte[] { (byte)(pos * 4 & 0xFF), (byte)(pos * 4 >> 8), 0, 0, (byte)((pos * 4 / (time / 1000)) & 0xFF), (byte)((pos * 4 / (time / 1000)) >> 8) });
                    break;
                case FeetechServoModels.SM:
                    OnWriteServoData(Name.Text, FeetechMemorySM.GoalPosition, new byte[] { (byte)(pos * 4 & 0xFF), (byte)(pos * 4 >> 8), 0, 0, (byte)((pos * 4 / (time / 1000)) & 0xFF), (byte)((pos * 4 / (time / 1000)) >> 8) });
                    break;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            UInt16 pos = 1000;
            UInt16 time = 1000;


            OnWriteServoData("Epaule1", FeetechMemorySCS.GoalPosition,
                new byte[] { (byte)(pos >> 8), (byte)(pos & 0xFF), (byte)(time >> 8), (byte)(time & 0xFF) });
            Thread.Sleep(time);
            OnWriteServoData("Epaule2", FeetechMemorySCS.GoalPosition,
                new byte[] { (byte)(pos >> 8), (byte)(pos & 0xFF), (byte)(time >> 8), (byte)(time & 0xFF) });
            Thread.Sleep(time);
            OnWriteServoData("Coude", FeetechMemorySCS.GoalPosition,
                new byte[] { (byte)(pos >> 8), (byte)(pos & 0xFF), (byte)(time >> 8), (byte)(time & 0xFF) });
            Thread.Sleep(time);
            OnWriteServoData("Poignee1", FeetechMemorySCS.GoalPosition,
                new byte[] { (byte)(pos >> 8), (byte)(pos & 0xFF), (byte)(time >> 8), (byte)(time & 0xFF) });
            Thread.Sleep(time);
            OnWriteServoData("Poignee2", FeetechMemorySTS.GoalPosition,
                new byte[] { (byte)(pos * 4 & 0xFF), (byte)(pos * 4 >> 8), 0, 0, (byte)((pos * 4 / (time / 1000)) & 0xFF), (byte)((pos * 4 / (time / 1000)) >> 8) });
            Thread.Sleep(time);
            OnWriteServoData("Main", FeetechMemorySM.GoalPosition,
                new byte[] { (byte)(pos * 4 & 0xFF), (byte)(pos * 4 >> 8), 0, 0, (byte)((pos * 4 / (time / 1000)) & 0xFF), (byte)((pos * 4 / (time / 1000)) >> 8) });
            Thread.Sleep(time * 3);
            OnWriteServoData("Main", FeetechMemorySM.GoalPosition,
                new byte[] { 0, 0, 0, 0, (byte)((pos * 4 / (time / 1000)) & 0xFF), (byte)((pos * 4 / (time / 1000)) >> 8) });
            Thread.Sleep(time);
            OnWriteServoData("Poignee2", FeetechMemorySTS.GoalPosition,
                new byte[] { 0, 0, 0, 0, (byte)((pos * 4 / (time / 1000)) & 0xFF), (byte)((pos * 4 / (time / 1000)) >> 8) });
            Thread.Sleep(time);
            OnWriteServoData("Poignee1", FeetechMemorySCS.GoalPosition,
                new byte[] { 0, 0, (byte)(time >> 8), (byte)(time & 0xFF) });
            Thread.Sleep(time);
            OnWriteServoData("Coude", FeetechMemorySCS.GoalPosition,
                new byte[] { 0, 0, (byte)(time >> 8), (byte)(time & 0xFF) });
            Thread.Sleep(time);
            OnWriteServoData("Epaule2", FeetechMemorySCS.GoalPosition,
                new byte[] { 0, 0, (byte)(time >> 8), (byte)(time & 0xFF) });
            Thread.Sleep(time);
            OnWriteServoData("Epaule1", FeetechMemorySCS.GoalPosition,
                new byte[] { 0, 0, (byte)(time >> 8), (byte)(time & 0xFF) });
        }
        private void TClick(object sender, RoutedEventArgs e)
        {
            
        }
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if(!Byte.TryParse(Pos.Text, out byte pos)) pos = 0;
            if(!Byte.TryParse(Time.Text, out byte time)) time = 0;
            if (!Byte.TryParse(Vit.Text, out byte vit)) vit = 0;

            FeetechServo s = servoManager.getServoByName(Name.Text);

            switch (s.Model)
            {
                case FeetechServoModels.SCS:
                    OnRegWriteServoData(Name.Text, FeetechMemorySCS.GoalPosition, new byte[] { (byte)(pos >> 8), (byte)(pos & 0xFF), (byte)(time >> 8), (byte)(time & 0xFF) });
                    break;
                case FeetechServoModels.STS:
                    OnRegWriteServoData(Name.Text, FeetechMemorySTS.GoalPosition, new byte[] { (byte)(pos * 4 & 0xFF), (byte)(pos * 4 >> 8), 0, 0, (byte)((pos * 4 / (time / 1000)) & 0xFF), (byte)((pos * 4 / (time / 1000)) >> 8) });
                    break;
                case FeetechServoModels.SM:
                    OnRegWriteServoData(Name.Text, FeetechMemorySM.GoalPosition, new byte[] { (byte)(pos * 4 & 0xFF), (byte)(pos * 4 >> 8), 0, 0, (byte)((pos * 4 / (time / 1000)) & 0xFF), (byte)((pos * 4 / (time / 1000)) >> 8) });
                    break;
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            OnActionServoData();
        }


        ///  Output events 
        public event EventHandler<FeetechServoReadArgs> OnReadServoDataEvent;
        public new virtual void OnReadServoData<FeetechMemory>(string name, FeetechMemory loc, byte nbBytes) where FeetechMemory : Enum
        {
            var handler = OnReadServoDataEvent;
            if (handler != null)
            {
                handler(this, new FeetechServoReadArgs { Name = name, Location = loc, NumberOfBytes = nbBytes});
            }
        }

        public event EventHandler<FeetechServoWriteArgs> OnWriteServoDataEvent;
        public new virtual void OnWriteServoData<FeetechMemory>(string name, FeetechMemory loc, byte[] payload) where FeetechMemory : Enum
        {
            var handler = OnWriteServoDataEvent;
            if (handler != null)
            {
                handler(this, new FeetechServoWriteArgs { Name = name, Location = loc, Payload = payload });
            }
        }

        public event EventHandler<FeetechServoSyncWriteArgs> OnSyncWriteServoDataEvent;
        public new virtual void OnSyncWriteServoData<FeetechMemory>(string[] names, FeetechMemory loc, byte[] payload) where FeetechMemory : Enum
        {
            var handler = OnSyncWriteServoDataEvent;
            if (handler != null)
            {
                handler(this, new FeetechServoSyncWriteArgs { Names = names, Location = loc, Payload = payload });
            }
        }

        public event EventHandler<FeetechServoRegWriteArgs> OnRegWriteServoDataEvent;
        public new virtual void OnRegWriteServoData<FeetechMemory>(string name, FeetechMemory loc, byte[] payload) where FeetechMemory : Enum
        {
            var handler = OnRegWriteServoDataEvent;
            if (handler != null)
            {
                handler(this, new FeetechServoRegWriteArgs { Name = name, Location = loc, Payload = payload });
            }
        }

        public event EventHandler<FeetechServoSyncReadArgs> OnSyncReadServoDataEvent;
        public new virtual void OnSyncReadServoData<FeetechMemory>(string[] names, FeetechMemory loc, byte nbBytes) where FeetechMemory : Enum
        {
            var handler = OnSyncReadServoDataEvent;
            if (handler != null)
            {
                handler(this, new FeetechServoSyncReadArgs { Names = names, Location = loc, NumberOfBytes = nbBytes });
            }
        }

        public event EventHandler<FeetechServoActionArgs> OnActionServoDataEvent;
        public new virtual void OnActionServoData()
        {
            var handler = OnActionServoDataEvent;
            if (handler != null)
            {
                handler(this, new FeetechServoActionArgs {  });
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