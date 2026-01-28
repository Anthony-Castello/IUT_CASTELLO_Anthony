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
            Trames.sendCommande(0xFE, 0, 0, 1500, SerialPort1);
            Trames.sendCommande(5, 0, 0, 1500, SerialPort1);
            Trames.sendCommande(6, 0, 0, 1500, SerialPort1);

            OnRequestServoDataEvent += servoManager.RequestServoData;
            servoManager.OnSendMessageEvent += ServoManager_OnSendMessageEvent;
            OnSendDataToServoEvent += servoManager.DecodeData;
            servoManager.OnServoDataEvent += ServoManager_OnServoDataEvent;
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
                //byte receivedByte = (byte)SerialPort1.ReadByte();
                //Trames.byteListReceived.Enqueue(receivedByte);
            }
        }

        private void TimerAffichage_Tick(object? sender, EventArgs e)
        {
            while (Trames.byteListReceived.Count() > 0)
            {
                byte b = Trames.byteListReceived.Dequeue();
                //Trames.DecodeMessage(b);
            }

            //Trames.readDatas(SerialPort1);

            OnRequestServoData(0x01, FeetechMemory.Baudrate, 10);

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            byte id;
            try { id = byte.Parse(Id.Text); }
            catch { id = 0; }
            UInt16 pos;
            UInt16 vit;
            UInt16 time;
            if (id == 0)
                id = 0xFE;
            try { pos = UInt16.Parse(Pos.Text); }
            catch { pos = 0; }
            try { vit = UInt16.Parse(Vit.Text); }
            catch { vit = 0; }
            try { time = UInt16.Parse(Time.Text); }
            catch { time = 0;}




            Trames.sendCommande(id, pos, time, vit, SerialPort1);
            if (id == 0xFE) // Commande Moteur sur 4096
            {
                Trames.sendCommande(5, pos, time, vit, SerialPort1);
                Trames.sendCommande(6, pos, time, vit, SerialPort1);

            }
        }

        private void TClick(object sender, RoutedEventArgs e)
        {

            CheckBox? cb = sender as CheckBox;
            if (cb == null) return;

            string idString = cb.Name.Replace("T", "");
            if (!byte.TryParse(idString, out byte id)) return;

            byte torque = (cb.IsChecked ?? false) ? (byte)1 : (byte)0;

            byte instruction = 0x03; // WRITE DATA
            byte[] payload = new byte[] { 0x28, torque }; // Adresse de départ et longueur de lecture

            //Packet p = new Packet(id, instruction, payload, (byte)Commands.Send);

            //// Envoi du packet
            //p.sendPacket(SerialPort1);

        }

        ///  Output events 
        public event EventHandler<FeetechServoRequestArgs> OnRequestServoDataEvent;
        public new virtual void OnRequestServoData(byte id, FeetechMemory loc, byte nbBytes)
        {
            var handler = OnRequestServoDataEvent;
            if (handler != null)
            {
                handler(this, new FeetechServoRequestArgs { Id = id, Location = loc, NumberOfBytes = nbBytes});
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