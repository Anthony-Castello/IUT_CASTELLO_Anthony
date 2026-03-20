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
        RobotStockage StockageLeft, StockageRight;
        SerialPort SerialPort1;
        RobotToolBox robotToolBox;
        DispatcherTimer timerAffichage;

        public MainWindow()
        {
            
            InitializeComponent();

            robotToolBox = new RobotToolBox(servoManager);

            SerialPort1 = new SerialPort("COM14", 115200, Parity.None, 8, StopBits.One);
            //SerialPort1.DataReceived += SerialPort1_DataReceived;
            SerialPort1.Open();

            timerAffichage = new DispatcherTimer();
            timerAffichage.Interval = new TimeSpan(0, 0, 0, 0, 100);
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
            servoManager.OnServoDataEvent += servoDataEvent;

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
                NumberOfBytes = 50,
                Names = new string[] { "Epaule", "Coude", "Poignet1", "Poignet2", "Poignet3", "Ascenseur" }
            });

        }

        private void servoDataEvent(object? sender, FeetechServoDataArgs e)
        {
            robotToolBox.sendServoInfo(e.info);
        }

        private void sendTrame(object? sender, ByteArrayArgs e)
        {
            SerialPort1.Write(e.array, 0, e.array.Length);
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

        private void GoToStockageLeft_Click(object sender, RoutedEventArgs e)
        {
            StockageLeft.goToStockage();
        }

        private void GoToStockageRight_Click(object sender, RoutedEventArgs e)
        {
            StockageRight.goToStockage();
        }

        private void Pick_Click(object sender, RoutedEventArgs e)
        {
            StockageLeft.Pick();
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