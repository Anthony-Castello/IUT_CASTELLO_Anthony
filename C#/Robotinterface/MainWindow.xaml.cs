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
using KeyboardHook_NS;
using System.Security.Cryptography.X509Certificates;





namespace Robotinterface
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ExtendedSerialPort serialPort1;
        DispatcherTimer timerAffichage;
        byte bytelistdecoded;
        Robot robot = new Robot();

        public MainWindow()
        {
            
            serialPort1 = new ExtendedSerialPort("COM9", 115200, Parity.None, 8, StopBits.One);
            serialPort1.DataReceived += SerialPort1_DataReceived;
            serialPort1.Open();
            InitializeComponent();
            timerAffichage = new DispatcherTimer();
            timerAffichage.Interval = new TimeSpan(0, 0, 0, 0, 100);
            timerAffichage.Tick += TimerAffichage_Tick;
            timerAffichage.Start();
            var _globalKeyboardHook = new GlobalKeyboardHook();
            _globalKeyboardHook.KeyPressed += _globalKeyboardHook_KeyPressed;
            UartEncodeAndSendMessage(0x0052, 2, new byte[] { (byte)robot.autoControlActivated });
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
                trame[pos++] = b;
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
                    msgDecodedPayload = new byte[msgDecodedPayloadLength];
                    msgDecodedPayloadIndex = 0;
                    rcvState = StateReception.Payload;
                    break;
                case StateReception.Payload:
                    msgDecodedPayload[msgDecodedPayloadIndex] = c;
                    msgDecodedPayloadIndex++;
                    if (msgDecodedPayloadIndex == msgDecodedPayloadLength)
                    {
                        rcvState = StateReception.CheckSum;
                    }
                    break;
                case StateReception.CheckSum:

                    byte calculatedChecksum = CalculateChecksum(msgDecodedFunction, msgDecodedPayloadLength, msgDecodedPayload);
                    if (calculatedChecksum == c)
                    {
                        //TextBoxReception.Text += "message recu\r\n";
                        //Success, on a un message valide
                        //on appelle la fonction ProcessDecodedMessage
                        ProcessDecodedMessage(msgDecodedFunction, msgDecodedPayloadLength, msgDecodedPayload);
                    }
                        rcvState = StateReception.Waiting;
                    break;
                default:
                    rcvState = StateReception.Waiting;
                    break;
            }
        }

        private void _globalKeyboardHook_KeyPressed(object? sender, KeyArgs e)
        {
            if (robot.autoControlActivated == 0)
            {
                switch (e.keyCode)
                {
                    case KeyCode.LEFT:
                        UartEncodeAndSendMessage(0x0051, 1, new byte[] { (byte)8 });
                break;
                    case KeyCode.RIGHT:
                    UartEncodeAndSendMessage(0x0051, 1, new byte[] { (byte)10 });
                    break;
                case KeyCode.UP:
                    UartEncodeAndSendMessage(0x0051, 1, new byte[] { (byte)2 });
                    break;
                case KeyCode.DOWN:
                    UartEncodeAndSendMessage(0x0051, 1, new byte[] { (byte)12 });
                    break;
                case KeyCode.PAGEDOWN:
                    UartEncodeAndSendMessage(0x0051, 1, new byte[] { (byte)14 });
                    break;
                }
            }
        }

        private void TimerAffichage_Tick(object? sender, EventArgs e)
        {
            while (robot.byteListReceived.Count() > 0)
            {
                bytelistdecoded = robot.byteListReceived.Dequeue();
                DecodeMessage(bytelistdecoded);
                //robot.receivedText = bytelistdecoded.ToString("X2");
                //TextBoxReception.Text += ("0x" + robot.receivedText + " ");

            }
        }

        bool toggle = true;
        bool toggle2 = true;
        public void SerialPort1_DataReceived(object sender, DataReceivedArgs e){
            foreach (byte item in e.Data)
            {
                robot.byteListReceived.Enqueue(item);
            }
        }

        private void SendMessage()
        {
            //TextBoxReception.Text += (Encoding.ASCII.GetBytes(TextBoxEmission.Text) + "\n");

            UartEncodeAndSendMessage(0x0080, TextBoxEmission.Text.Length, Encoding.ASCII.GetBytes(TextBoxEmission.Text));
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
        public enum functionID
        {
            Text_transmission = 0x0080,
            LED = 0x0020,
            Dist_IR = 0x0030,
            c_vitesse = 0x0040,
            IsState = 0x0050,
            Odometrie= 0x0060
        }

        int TelemExGauche = 0;
        int TelemGauche = 0;
        int TelemCentre = 0;
        int TelemDroit = 0;
        int TelemExDroit = 0;

        float v_moteur_G = 0;
        float v_moteur_D = 0;
        int etape = 0;
    

        string message;
        private void ProcessDecodedMessage(int msgFunction, int msgPayloadLength, byte[] msgPayload)
        {
            switch (msgFunction)
            {
                case (int)functionID.LED:
                    if ((int)msgPayload[0] == 0)
                    {
                        if((int)msgPayload[1] == 1)
                            Led1.IsChecked = true;
                        else 
                            Led1.IsChecked = false;
                    }
                    if ((int)msgPayload[0] == 1)
                    {
                        if ((int)msgPayload[1] == 1)
                            Led2.IsChecked = true;
                        else
                            Led2.IsChecked = false;
                    }
                    if ((int)msgPayload[0] == 2)
                    {
                        if ((int)msgPayload[1] == 1)
                            Led3.IsChecked = true;
                        else
                            Led3.IsChecked = false;
                    }
                    if ((int)msgPayload[0] == 3)
                    {
                        if ((int)msgPayload[1] == 1)
                            Led4.IsChecked = true;
                        else
                            Led4.IsChecked = false;
                    }
                    if ((int)msgPayload[0] == 4)
                    {
                        if ((int)msgPayload[1] == 1)
                            Led5.IsChecked = true;
                        else
                            Led5.IsChecked = false;
                    }
                    break;
                case (int)functionID.Dist_IR:
                    TelemExGauche = (int)msgPayload[0];
                    TelemGauche = (int)msgPayload[1];
                    TelemCentre = (int)msgPayload[2];
                    TelemDroit = (int)msgPayload[3];
                    TelemExDroit = (int)msgPayload[4];
                    ExG.Text = ("IR ExGauche : " + TelemExGauche);
                    G.Text = ("IR Gauche : " + TelemGauche);
                    C.Text = ("IR Centre : " + TelemCentre);
                    D.Text = ("IR Droit : " + TelemDroit);
                    ExD.Text = ("IR ExDroit : " + TelemExDroit);
                    break;
                case (int)functionID.c_vitesse:
                    v_moteur_G = BitConverter.ToSingle(msgPayload, 0);
                    v_moteur_D = BitConverter.ToSingle(msgPayload, 4);
                    M_G.Text = ("Vitesse Gauche : "+ v_moteur_G.ToString("N1") + "%");
                    M_D.Text = ("Vitesse Droit : " + v_moteur_D.ToString("N1") + "%");
                    break;
                case (int)functionID.Text_transmission:
                    TextBoxReception.Text += ("Texte reçu : " + Encoding.ASCII.GetString(msgPayload) + "\n");
                    break;
                case (int)functionID.IsState:
                    if (msgPayload[0] == 1)
                        TextBoxReception.Text += ("Robot avance " + msgPayload[4].ToString("N1") + "ms \n");
                    else if (msgPayload[1] == 1)
                        TextBoxReception.Text += ("Robot se retourne. Temps : " + msgPayload[4].ToString("N1") + "ms \n");
                    else if (msgPayload[2] == 1)
                        TextBoxReception.Text += ("Robot tourne à gauche. Temps : " + msgPayload[4].ToString("N1") + "ms \n");
                    else if (msgPayload[3] == 1)
                        TextBoxReception.Text += ("Robot tourne à droite. Temps : " + msgPayload[4].ToString("N1") + "ms \n");
                    else
                        TextBoxReception.Text += ("Robot arrété. Temps : " + msgPayload[4].ToString("N1") + "s \n");
                    break;
                case (int)functionID.Odometrie:
                    TextBoxReception.Text +=("Position x :" + msgPayload[4].ToString("N1") + "  Position y :" + msgPayload[8].ToString("N1") + "Temps : " + msgPayload[0].ToString("N1") + " ms\n");
                    break;
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
            //byte[] array = Encoding.ASCII.GetBytes("Bonjour");
            //UartEncodeAndSendMessage(0x0080, 7, array);
            //byte[] LED = new byte[2];
            //LED[1] = (byte)1;
            //UartEncodeAndSendMessage(0x0020, 2, LED);
            //byte[] IR = new byte[5];
            //IR[0] = (byte)20;
            //IR[1] = (byte)10;
            //IR[2] = (byte)99;
            //IR[3] = (byte)32;
            //IR[4] = (byte)47;
            //UartEncodeAndSendMessage(0x0030, 5, IR);
            //byte[] vitesse = new byte[2];
            //vitesse[0] = (byte)50;
            //vitesse[1] = (byte)42;
            //UartEncodeAndSendMessage(0x0040, 2, vitesse);
            if (robot.autoControlActivated == 0)
                robot.autoControlActivated = 1;
            else
            {
                robot.autoControlActivated = 0;
                UartEncodeAndSendMessage(0x0051, 1, new byte[] { (byte)12 });
            }
            UartEncodeAndSendMessage(0x0052, 2, new byte[] { (byte)robot.autoControlActivated });
        }
        
    }
}