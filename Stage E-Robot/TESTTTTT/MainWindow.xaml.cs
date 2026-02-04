using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq.Expressions;
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

        Queue<List<object>> bufferRead = new Queue<List<Object>>();

        public MainWindow()
        {
            InitializeComponent();


            SerialPort1 = new SerialPort("COM14", 115200, Parity.None, 8, StopBits.One);
            SerialPort1.DataReceived += SerialPort1_DataReceived;
            SerialPort1.Open();

            timerAffichage = new DispatcherTimer();
            timerAffichage.Interval = new TimeSpan(0, 0, 0, 0, 70); // NE PAS TROP BAISSER LE TIMER (Buffer requests se dequeue trop lentement, désynchronisament des requêtes)
            timerAffichage.Tick += TimerAffichage_Tick;
            timerAffichage.Start();

            // Reset les moteurs au démarrage

            OnActionServoDataEvent += servoManager.ActionServoData;
            OnRegWriteServoDataEvent += servoManager.RegWriteServoData;
            OnSyncWriteServoDataEvent += servoManager.SyncWriteServoData;
            OnSyncReadServoDataEvent += servoManager.SyncReadServoData;
            OnReadServoDataEvent += servoManager.ReadServoData;
            OnWriteServoDataEvent += servoManager.WriteServoData;
            OnSendDataToServoEvent += servoManager.DecodeData;
            servoManager.OnSendMessageEvent += ServoManager_OnSendMessageEvent;
            servoManager.OnServoDataEvent += ServoManager_OnServoDataEvent;
            servoManager.OnServoErrorEvent += ServoManager_OnServoErrorEvent;

            servoManager.servos.Add(new FeetechServo("Epaule", 1, FeetechServoModels.SM));
            servoManager.servos.Add(new FeetechServo("Coude", 2, FeetechServoModels.SM));
            servoManager.servos.Add(new FeetechServo("Poignee1", 3, FeetechServoModels.SM));
            servoManager.servos.Add(new FeetechServo("Poignee2", 4, FeetechServoModels.SM));
            servoManager.servos.Add(new FeetechServo("Poignee3", 5, FeetechServoModels.SM));
            

            // Tous les moteurs
            servoManager.servos.Add(new FeetechServo("All", 0xFE, FeetechServoModels.SM));

            

        }

        private void ServoManager_OnServoDataEvent(object? sender, FeetechServoDataArgs e)
        {
            byte id = e.info.Id ?? 0;
            if (servoManager.getServoById(id) == null) return;
            if (e.info.TorqueEnable != null) 
            {
                List<object> objs = new List<object>();
                objs.Add("Torque");
                objs.Add(servoManager.getServoById(id).Name);
                objs.Add(e.info.TorqueEnable);
                bufferRead.Enqueue(objs);
            }
            if (e.info.PresentPosition != null)
            {
                List<object> objs = new List<object>();
                objs.Add("Position");
                objs.Add(servoManager.getServoById(id).Name);
                objs.Add(e.info.PresentPosition);
                bufferRead.Enqueue(objs);
            }
            if (e.info.PresentPWM != null)
            {
                List<object> objs = new List<object>();
                objs.Add("Couple");
                objs.Add(servoManager.getServoById(id).Name);
                objs.Add(e.info.PresentPWM);
                bufferRead.Enqueue(objs);
            }
        }

        private void ServoManager_OnServoErrorEvent(object? sender, FeetechServoErrorArgs e)
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

            //OnSyncWriteServoData(new byte[] { 1,4 }, FeetechMemory.GoalPosition, 
            //    new byte[] { (byte)(pos >> 8), (byte)(pos & 0xFF), 0x00, 0x00, (byte)(vit >> 8), (byte)(vit & 0xFF) }); // les deux 0x00 correspondent au temps, ici pas utilisé
            OnSyncReadServoData(new string[] { "Epaule", "Coude", "Poignee1", "Poignee2", "Poignee3" }, FeetechMemorySM.TorqueEnable, 30);

            while (bufferRead.Count > 0)
            {
                List<object> objs = bufferRead.Dequeue();
                string type = (string)objs[0];
                string Name = (string)objs[1];
                if(type == "Torque")
                {
                    Thread.Sleep(Feetech.ServoDelay);
                    bool torque = (bool)objs[2];
                    CheckBox? cb = this.FindName(Name) as CheckBox;
                    if (cb == null) return;
                    cb.IsChecked = torque;
                }
                if (type == "Position")
                {
                    Int16 pos = (Int16)objs[2];
                    TextBlock? tb = this.FindName(("P"+Name)) as TextBlock;
                    if (tb == null) return;
                    tb.Text = (Name+" : " + pos.ToString());
                }
                if (type == "Couple")
                {
                    Int16 couple = (Int16)objs[2];
                    if(couple > 1023) couple -= 1023; 
                    TextBlock? tb = this.FindName(("C" + Name)) as TextBlock;
                    if (tb == null) return;
                    tb.Text = (Name + " : " + couple.ToString());
                }


            }

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            if (!UInt16.TryParse(Pos.Text, out UInt16 pos)) pos = 0;
            if (!UInt16.TryParse(Time.Text, out UInt16 time)) time = 0;
            if (!UInt16.TryParse(Vit.Text, out UInt16 vit)) vit = 0;

            FeetechServo s = servoManager.getServoByName(Name.Text);

            switch (s.Model)
            {
                case FeetechServoModels.SCS:
                    OnWriteServoData(Name.Text, FeetechMemorySCS.GoalPosition, new byte[] { (byte)(pos >> 8), (byte)(pos & 0xFF), (byte)(time >> 8), (byte)(time & 0xFF) });
                    break;
                case FeetechServoModels.STS:
                    try { OnWriteServoData(Name.Text, FeetechMemorySTS.GoalPosition, new byte[] { (byte)(pos & 0xFF), (byte)(pos >> 8), 0, 0, (byte)((pos / (time / 1000)) & 0xFF), (byte)((pos / (time / 1000)) >> 8) });}
                    catch { OnWriteServoData(Name.Text, FeetechMemorySTS.GoalPosition, new byte[] { (byte)(pos & 0xFF), (byte)(pos >> 8), 0, 0, (byte)(vit*4 & 0xFF), (byte)(vit*4 >> 8) });}

                    break;
                case FeetechServoModels.SM:
                    try { OnWriteServoData(Name.Text, FeetechMemorySTS.GoalPosition, new byte[] { (byte)(pos & 0xFF), (byte)(pos >> 8), 0, 0, (byte)((pos / (time / 1000)) & 0xFF), (byte)((pos / (time / 1000)) >> 8) }); }
                    catch { OnWriteServoData(Name.Text, FeetechMemorySTS.GoalPosition, new byte[] { (byte)(pos & 0xFF), (byte)(pos >> 8), 0, 0, (byte)(vit*4 & 0xFF), (byte)(vit*4 >> 8) }); }
                    break;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            UInt16 posE = 3223;
            UInt16 posC = 1038;
            UInt16 posP1 = 3697;
            UInt16 posP2 = 1023;
            UInt16 posP3 = 332;
            UInt16 time = 1000;


            OnRegWriteServoData("Epaule", FeetechMemorySM.GoalPosition,
                new byte[] { (byte)(posE & 0xFF), (byte)(posE >> 8), 0, 0, (byte)((posE / (time / 1000)) & 0xFF), (byte)((posE / (time / 1000)) >> 8) });
            Thread.Sleep(Feetech.ServoDelay);
            OnRegWriteServoData("Coude", FeetechMemorySM.GoalPosition,
                new byte[] { (byte)(posC & 0xFF), (byte)(posC >> 8), 0, 0, (byte)((posC / (time / 1000)) & 0xFF), (byte)((posC / (time / 1000)) >> 8) });
            Thread.Sleep(Feetech.ServoDelay);
            OnRegWriteServoData("Poignee1", FeetechMemorySM.GoalPosition,
                new byte[] { (byte)(posP1 & 0xFF), (byte)(posP1 >> 8), 0, 0, (byte)((posP1 / (time / 1000)) & 0xFF), (byte)((posP1 / (time / 1000)) >> 8) });
            Thread.Sleep(Feetech.ServoDelay);
            OnRegWriteServoData("Poignee2", FeetechMemorySM.GoalPosition,
                new byte[] { (byte)(posP2 & 0xFF), (byte)(posP2 >> 8), 0, 0, (byte)((posP2 / (time / 1000)) & 0xFF), (byte)((posP2 / (time / 1000)) >> 8) });
            Thread.Sleep(Feetech.ServoDelay);
            OnRegWriteServoData("Poignee3", FeetechMemorySM.GoalPosition,
                new byte[] { (byte)(posP3 & 0xFF), (byte)(posP3 >> 8), 0, 0, (byte)((posP3 / (time / 1000)) & 0xFF), (byte)((posP3 / (time / 1000)) >> 8) });
            Thread.Sleep(Feetech.ServoDelay);
            OnActionServoData();
        }
        private void TClick(object sender, RoutedEventArgs e)
        {
            CheckBox? cb = sender as CheckBox;
            if (cb == null) return;

            byte torque = (cb.IsChecked ?? false) ? (byte)1 : (byte)0;

            OnWriteServoData(cb.Name, FeetechMemorySM.TorqueEnable, new byte[] { torque });
        }
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if(!UInt16.TryParse(Pos.Text, out UInt16 pos)) pos = 0;
            if(!UInt16.TryParse(Time.Text, out UInt16 time)) time = 0;
            if (!UInt16.TryParse(Vit.Text, out UInt16 vit)) vit = 0;

            FeetechServo s = servoManager.getServoByName(Name.Text);

            switch (s.Model)
            {
                case FeetechServoModels.SCS:
                    OnRegWriteServoData(Name.Text, FeetechMemorySCS.GoalPosition, new byte[] { (byte)(pos >> 8), (byte)(pos & 0xFF), (byte)(time >> 8), (byte)(time & 0xFF) });
                    break;
                case FeetechServoModels.STS:
                    try { OnRegWriteServoData(Name.Text, FeetechMemorySTS.GoalPosition, new byte[] { (byte)(pos * 4 & 0xFF), (byte)(pos * 4 >> 8), 0, 0, (byte)((pos * 4 / (time / 1000)) & 0xFF), (byte)((pos * 4 / (time / 1000)) >> 8) }); }
                    catch { OnRegWriteServoData(Name.Text, FeetechMemorySTS.GoalPosition, new byte[] { (byte)(pos * 4 & 0xFF), (byte)(pos * 4 >> 8), 0, 0, (byte)(vit*4 & 0xFF), (byte)(vit*4 >> 8) }); }
                    break;
                case FeetechServoModels.SM:
                    try { OnRegWriteServoData(Name.Text, FeetechMemorySTS.GoalPosition, new byte[] { (byte)(pos * 4 & 0xFF), (byte)(pos * 4 >> 8), 0, 0, (byte)((pos * 4 / (time / 1000)) & 0xFF), (byte)((pos * 4 / (time / 1000)) >> 8) }); }
                    catch { OnRegWriteServoData(Name.Text, FeetechMemorySTS.GoalPosition, new byte[] { (byte)(pos * 4 & 0xFF), (byte)(pos * 4 >> 8), 0, 0, (byte)(vit*4 & 0xFF), (byte)(vit*4 >> 8) }); }
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