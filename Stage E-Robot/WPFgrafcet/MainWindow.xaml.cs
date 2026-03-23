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
using System.Windows.Threading;
using System.Xml.Linq;
using DeplacerBras_NS;
using GrafcetRobot_NS;
using ServoFeetech_NS;

namespace WPFgrafcet
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool torque = true;
        Feetech servoManager = new Feetech();
        DeplacerBras bras;
        RobotStockage StockageLeft, StockageRight;
        SerialPort SerialPort1;
        DispatcherTimer timerAffichage;

        public MainWindow()
        {
            
            InitializeComponent();

            this.bras = new DeplacerBras(servoManager);

            SerialPort1 = new SerialPort("COM14", 115200, Parity.None, 8, StopBits.One);
            SerialPort1.DataReceived += decodeTrame;
            SerialPort1.Open();

            timerAffichage = new DispatcherTimer();
            timerAffichage.Interval = new TimeSpan(0, 0, 0, 0, 300);
            timerAffichage.Tick += TimerAffichage_Tick;
            timerAffichage.Start();


            servoManager.servos.Add(new FeetechServo("Epaule", 1, FeetechServoModels.SM));
            servoManager.servos.Add(new FeetechServo("Coude", 2, FeetechServoModels.SM));
            servoManager.servos.Add(new FeetechServo("Poignet1", 3, FeetechServoModels.SM));
            servoManager.servos.Add(new FeetechServo("Poignet2", 4, FeetechServoModels.SM));
            servoManager.servos.Add(new FeetechServo("Poignet3", 5, FeetechServoModels.SM));
            servoManager.servos.Add(new FeetechServo("Ascenseur", 6, FeetechServoModels.SM));

            servoManager.servos.Add(new FeetechServo("GauchePlateforme1", 20, FeetechServoModels.STS));
            servoManager.servos.Add(new FeetechServo("GauchePousser1", 21, FeetechServoModels.STS));
            servoManager.servos.Add(new FeetechServo("GauchePlateforme2", 22, FeetechServoModels.STS));
            servoManager.servos.Add(new FeetechServo("GauchePlateforme3", 23, FeetechServoModels.STS));
            servoManager.servos.Add(new FeetechServo("GauchePousser2", 24, FeetechServoModels.STS));

            servoManager.servos.Add(new FeetechServo("DroitePlateforme1", 10, FeetechServoModels.STS));
            servoManager.servos.Add(new FeetechServo("DroitePousser1", 11, FeetechServoModels.STS));
            servoManager.servos.Add(new FeetechServo("DroitePlateforme2", 12, FeetechServoModels.STS));
            servoManager.servos.Add(new FeetechServo("DroitePlateforme3", 13, FeetechServoModels.STS));
            servoManager.servos.Add(new FeetechServo("DroitePousser2", 14, FeetechServoModels.STS));

            servoManager.servos.Add(new FeetechServo("All", 0xFE, FeetechServoModels.STS));

            servoManager.OnSendMessageEvent += sendTrame;
            servoManager.OnServoDataEvent += ServoManager_OnServoDataEvent;

            StockageLeft = new RobotStockage(servoManager, StockageType.Left, new Dictionary<string, string>()
            {
                {"Plateforme1", "GauchePlateforme1"},
                {"Plateforme2", "GauchePlateforme2"},
                {"Plateforme3", "GauchePlateforme3"},
                {"Plateforme4", "GauchePlateforme4"},
                {"Pousser1", "GauchePousser1"},
                {"Pousser2", "GauchePousser2"},
            });

            StockageRight = new RobotStockage(servoManager, StockageType.Right, new Dictionary<string, string>()
            {
                {"Plateforme1", "DroitePlateforme1"},
                {"Plateforme2", "DroitePlateforme2"},
                {"Plateforme3", "DroitePlateforme3"},
                {"Plateforme4", "DroitePlateforme4"},
                {"Pousser1", "DroitePousser1"},
                {"Pousser2", "DroitePousser2"},
            });

            StockageLeft.initServos();
            StockageRight.initServos();

        }

        private void TimerAffichage_Tick(object? sender, EventArgs e)
        {
            servoManager.SyncReadServoData(sender, new FeetechServoSyncReadArgs
            {
                Location = FeetechMemorySTS.Baudrate,
                NumberOfBytes = 80,
                Names = new string[] { "Epaule", "Coude", "Poignet1", "Poignet2", "Poignet3", "Ascenseur" }
            });

            EpaulePosAtteinte.IsChecked = bras.epaulePosAtteinte;
            CoudePosAtteinte.IsChecked = bras.coudePosAtteinte;
            Poignet1PosAtteinte.IsChecked = bras.poignet1PosAtteinte;
            Poignet2PosAtteinte.IsChecked = bras.poignet2PosAtteinte;
            Poignet3PosAtteinte.IsChecked = bras.poignet3PosAtteinte;

        }

        private void sendTrame(object? sender, ByteArrayArgs e)
        {
            SerialPort1.Write(e.array, 0, e.array.Length);
        }

        private void decodeTrame(object? sender, SerialDataReceivedEventArgs e)
        {
            while (SerialPort1.BytesToRead > 0)
            {
                byte[] buffer = new byte[SerialPort1.BytesToRead];
                SerialPort1.Read(buffer, 0, buffer.Length);
                servoManager.DecodeData(sender, new ByteArrayArgs {  array = buffer });
            }

        }


        private void ServoManager_OnServoDataEvent(object? sender, FeetechServoDataArgs e)
        {
            bras.servoInfoReceived(e.info);
        }

        private void ServoManager_OnServoErrorEvent(object? sender, FeetechServoErrorArgs e)
        {
        }


        private void StockerLeft_Click(object sender, RoutedEventArgs e)
        {
            StockageLeft.Stock();
        }

        private void PousserLeft_Click(object sender, RoutedEventArgs e)
        {
            StockageLeft.Push();
        }

        private void StockerRight_Click(object sender, RoutedEventArgs e)
        {
            StockageRight.Stock();
        }

        private void PousserRight_Click(object sender, RoutedEventArgs e)
        {
            StockageRight.Push();
        }

        private async void GoToStockageLeft_Click(object sender, RoutedEventArgs e)
        {
            await bras.goToPosition(BrasPosition.StockageLeft);
        }

        private async void GoToStockageRight_Click(object sender, RoutedEventArgs e)
        {
            await bras.goToPosition(BrasPosition.StockageRight);
        }

        private async void Pick_Click(object sender, RoutedEventArgs e)
        {
            await bras.goToPosition(BrasPosition.Picking);
        }
        private void Toggle_torque_click(object sender, RoutedEventArgs e)
        {
            if (torque)
            {
                
                servoManager.WriteServoData(this, new FeetechServoWriteArgs
                {
                    Name = "All",
                    Location = FeetechMemorySTS.TorqueEnable,
                    Payload = new byte[] {0}
                });
                Cons.Text += "Couple résistant désactivé \n";
            }
            else
            {
                servoManager.WriteServoData(this, new FeetechServoWriteArgs
                {
                    Name = "All",
                    Location = FeetechMemorySTS.TorqueEnable,
                    Payload = new byte[] {1}
                });
                Cons.Text += "Couple résistant activé \n";
            }
            torque = !torque;
        }
    }
}