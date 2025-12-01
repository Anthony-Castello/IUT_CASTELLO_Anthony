using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ExtendedSerialPort_NS;
using System.IO.Ports;
using System.Windows.Threading;





namespace Robotinterface
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ExtendedSerialPort serialPort1;
        DispatcherTimer timerAffichage;
        int flag = 0;
        public MainWindow()
        {
            
            serialPort1 = new ExtendedSerialPort("COM4", 115200, Parity.None, 8, StopBits.One);
            serialPort1.DataReceived += SerialPort1_DataReceived;
            serialPort1.Open();
            InitializeComponent();
            timerAffichage = new DispatcherTimer();
            timerAffichage.Interval = new TimeSpan(0, 0, 0, 0, 100);
            timerAffichage.Tick += TimerAffichage_Tick;
            timerAffichage.Start();
        }

        private void TimerAffichage_Tick(object? sender, EventArgs e)
        {
            if (flag == 1)
            {
                TextBoxReception.Text += ("Reçu : " + receivedText);
                flag = 0;

            }
            else
            {
                receivedText = "";
            }
        }

        bool toggle = true;
        String receivedText;
        public void SerialPort1_DataReceived(object sender, DataReceivedArgs e){

            receivedText += Encoding.UTF8.GetString(e.Data, 0, e.Data.Length);
            flag = 1;
        }

        private void SendMessage()
        {
            //TextBoxReception.Text += ("Reçu : " + text + "\n");
            
            serialPort1.WriteLine(TextBoxEmission.Text);
            TextBoxEmission.Text = "";
        }
        private void boutonEnvoyer_Click(object sender, RoutedEventArgs e)
        {
            
            SendMessage();
            if (toggle)
            {
                boutonEnvoyer.Background = Brushes.Beige;
                toggle = !toggle;
            }
            else
            {
                boutonEnvoyer.Background = Brushes.RoyalBlue;
                toggle = !toggle;
            }
            
        }
        private void TextBoxEmission_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendMessage();
            }
        }

        private void TextBoxEmission_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        
    }
}