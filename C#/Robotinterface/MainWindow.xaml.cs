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
        int bit = 0;
        Robot robot = new Robot();
        public MainWindow()
        {
            
            serialPort1 = new ExtendedSerialPort("COM8", 115200, Parity.None, 8, StopBits.One);
            serialPort1.DataReceived += SerialPort1_DataReceived;
            serialPort1.Open();
            InitializeComponent();
            timerAffichage = new DispatcherTimer();
            timerAffichage.Interval = new TimeSpan(0, 0, 0, 0, 100);
            timerAffichage.Tick += TimerAffichage_Tick;
            timerAffichage.Start();
        }
        byte CalculateChecksum(int msgFunction, int msgPayloadLength, byte[] msgPayload)
        {
            byte checksum = 0x00;
            checksum ^= 0xFE;
            checksum ^= (byte)(msgFunction >> 8);
            checksum ^= (byte)(msgFunction >> 0);
            checksum ^= (byte)(msgPayloadLength >> 8);
            checksum ^= (byte)(msgPayloadLength >> 0);
            foreach (byte b in msgPayload) 
                checksum ^= b;

            return checksum;
        }
        void UartEncodeAndSendMessage(int msgFunction, int msgPayloadLength, byte[] msgPayload)
        {
            byte[] trame = new byte[6+msgPayloadLength];
            int pos = 0;
            trame[pos++] = 0xFE;
            trame[pos++] = (byte)(msgFunction >> 8);
            trame[pos++] = (byte)(msgFunction >> 0);
            trame[pos++] = (byte)(msgPayloadLength >> 8);
            trame[pos++] = (byte)(msgPayloadLength >> 0);
            foreach (byte b in msgPayload)
                trame[pos++] += b;
            trame[pos++] = CalculateChecksum(msgFunction, msgPayloadLength, msgPayload);
            serialPort1.Write(trame, 0, trame.Length);
        }
        public enum StateReception
        {
            Waiting,
            FunctionMSB,
            FunctionLSB,
            PayloadLengthMSB,
            PayloadLengthLSB,
            Payload,
            CheckSum
        }
        StateReception rcvState = StateReception.Waiting;
        int msgDecodedFunction = 0;
        int msgDecodedPayloadLength = 0;
        byte[] msgDecodedPayload;
        int msgDecodedPayloadIndex = 0;
        int realtimebyte = 0;
        private void DecodeMessage(byte c)
        {
            switch (rcvState)
            {
                case StateReception.Waiting:
                    if (c == 0xFE)
                    {
                        rcvState = StateReception.FunctionMSB;
                    }
                    break;
                case StateReception.FunctionMSB:
                    msgDecodedFunction = (int)(c<<8);
                    rcvState = StateReception.FunctionLSB;
                    break;
                case StateReception.FunctionLSB:
                    msgDecodedFunction += (int)c;
                    rcvState = StateReception.PayloadLengthMSB;
                    break;
                case StateReception.PayloadLengthMSB:
                    msgDecodedPayloadLength = (int)(c<<8);
                    rcvState = StateReception.PayloadLengthLSB;
                    break;
                case StateReception.PayloadLengthLSB:
                    msgDecodedPayloadLength += (int)c;
                    rcvState = StateReception.Payload;
                    break;
                case StateReception.Payload:
                    msgDecodedPayload[msgDecodedPayloadIndex] = c;
                    msgDecodedPayloadIndex++;
                    if (msgDecodedPayloadIndex == msgDecodedPayloadLength)
                    {
                        msgDecodedPayloadIndex = 0;
                        rcvState = StateReception.CheckSum;
                    }
                    break;
                case StateReception.CheckSum:

                    byte calculatedChecksum = CalculateChecksum(msgDecodedFunction, msgDecodedPayloadLength, msgDecodedPayload);
                    if (calculatedChecksum == c)
                    {
                        //Success, on a un message valide
                    }
                    break;
                default:
                    rcvState = StateReception.Waiting;
                    break;
            }
        }

        private void TimerAffichage_Tick(object? sender, EventArgs e)
        {
            while (robot.byteListReceived.Count() > 0)
            {
                DecodeMessage(robot.byteListReceived.Dequeue());
                //robot.receivedText = robot.byteListReceived.Dequeue().ToString("X2");
                //TextBoxReception.Text += ("0x" + robot.receivedText + " ");

            }
        }

        bool toggle = true;
        bool toggle2 = true;
        bool toggle3 = true;
        public void SerialPort1_DataReceived(object sender, DataReceivedArgs e){
            foreach (byte item in e.Data)
            {
                robot.byteListReceived.Enqueue(item);
            }
                flag = 1;
        }

        private void SendMessage()
        {
            //TextBoxReception.Text += ("Reçu : " + text + "\n");

            UartEncodeAndSendMessage(128, TextBoxEmission.Text.Length, Encoding.ASCII.GetBytes(TextBoxEmission.Text));
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

        private void boutonClear_Click(object sender, RoutedEventArgs e)
        {
            TextBoxReception.Text = "";
            if (toggle2)
            {
                boutonClear.Background = Brushes.Beige;
                toggle2 = !toggle2;
            }
            else
            {
                boutonClear.Background = Brushes.RoyalBlue;
                toggle2 = !toggle2;
            }
        }

        private void boutonTest_Click(object sender, RoutedEventArgs e)
        {
            //int i;
            //byte[] byteList = new byte[48];
            //for (i = 0; i< 48; i++)
            //{
            //    byteList[i] = (byte)(i);
            //}
            //serialPort1.Write(byteList, 0, 48);
            //if (toggle3)
            //{
            //    boutonTest.Background = Brushes.Beige;
            //    toggle3 = !toggle3;
            //}
            //else
            //{
            //    boutonTest.Background = Brushes.RoyalBlue;
            //    toggle3 = !toggle3;
            //}
            byte[] array = Encoding.ASCII.GetBytes("Bonjour");
            UartEncodeAndSendMessage(128, 7, array);
        }
    }
}