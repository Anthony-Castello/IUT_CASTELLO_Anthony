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
using WpfOscilloscopeControl;
using static SciChart.Drawing.Utility.PointUtil;
using SciChart.Data.Model;
using WpfAsservissementDisplay_NS;
using System.Linq.Expressions;





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
            
            serialPort1 = new ExtendedSerialPort("COM8", 115200, Parity.None, 8, StopBits.One);
            serialPort1.DataReceived += SerialPort1_DataReceived;
            serialPort1.Open();
            InitializeComponent();
            timerAffichage = new DispatcherTimer();
            timerAffichage.Interval = new TimeSpan(0, 0, 0, 0, 100);
            timerAffichage.Tick += TimerAffichage_Tick;
            timerAffichage.Start();
            //var _globalKeyboardHook = new GlobalKeyboardHook();
            //_globalKeyboardHook.KeyPressed += _globalKeyboardHook_KeyPressed;
            UartEncodeAndSendMessage(0x0052, 2, new byte[] { (byte)robot.autoControlActivated });
            oscilloSpeed.AddOrUpdateLine(1, 200, "Ligne1");
            oscilloSpeed.ChangeLineColor(1, Color.FromRgb(0,0,255));
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

            //UartEncodeAndSendMessage(0x0080, TextBoxEmission.Text.Length, Encoding.ASCII.GetBytes(TextBoxEmission.Text));
            //TextBoxEmission.Text = "";
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
        //private void TextBoxEmission_KeyUp(object sender, KeyEventArgs e)
        //{
        //    if (e.Key == Key.Enter)
        //    {
        //        SendMessage();
        //    }
        //}

        //private void TextBoxEmission_TextChanged(object sender, TextChangedEventArgs e)
        //{

        //}

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
            Odometrie= 0x0060,
            PID = 0x0061,
        }

        int TelemExGauche = 0;
        int TelemGauche = 0;
        int TelemCentre = 0;
        int TelemDroit = 0;
        int TelemExDroit = 0;

        float v_moteur_G = 0;
        float v_moteur_D = 0;
   
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
                    //AsservissementRobot2RouesDisplayControl.UpdateIndependantOdometrySpeed((double)v_moteur_G, (double)v_moteur_D);
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

                    byte[] array = new byte[4];
                    Array.Copy(msgPayload, 0, array, 0, 4);
                    array = array.Reverse().ToArray();
                    var instant = BitConverter.ToInt32(array, 0);
                    instant = instant / 100;

                    float positionX = BitConverter.ToSingle(msgPayload, 4);
                    float positionY = BitConverter.ToSingle(msgPayload, 8);
                    oscilloSpeed.AddPointToLine(1, positionX, positionY);
                    float ang = BitConverter.ToSingle(msgPayload, 12);
                    float vit_lin = BitConverter.ToSingle(msgPayload, 16);
                    float vit_ang = BitConverter.ToSingle(msgPayload, 20);
                    posX.Text = ("Position X : " + positionX.ToString("N3"));
                    posY.Text = ("Position Y : " + positionY.ToString("N3"));
                    temps.Text = (instant.ToString() + " s");
                    angle.Text = ("Angle : " + ang.ToString("N3") + " rad");
                    v_lin.Text = ("Vitesse linéaire : " + vit_lin.ToString("N3") + " m/s");
                    v_ang.Text = ("Vitesse angulaire : " + vit_ang.ToString("N3") + " rad/s");
                    //affichage aservdisplay
                    asservSpeedDisplay.UpdatePolarOdometrySpeed(vit_lin, vit_ang);
                    break;
                case (int)functionID.PID:
                    robot.Kp_X = BitConverter.ToSingle(msgPayload, 0); ;
                    robot.Ki_X = BitConverter.ToSingle(msgPayload, 4);
                    robot.Kd_X = BitConverter.ToSingle(msgPayload, 8);
                    robot.erreurproportionelleMax_X = BitConverter.ToSingle(msgPayload, 12);
                    robot.erreurintegralMax_X = BitConverter.ToSingle(msgPayload, 16);
                    robot.erreurderiveeMax_X = BitConverter.ToSingle(msgPayload, 20);
                    robot.Kp_Theta = BitConverter.ToSingle(msgPayload, 24);
                    robot.Ki_Theta = BitConverter.ToSingle(msgPayload, 28)  ;
                    robot.Kd_Theta = BitConverter.ToSingle(msgPayload, 32);
                    robot.erreurproportionelleMax_Theta = BitConverter.ToSingle(msgPayload, 36);
                    robot.erreurintegralMax_Theta = BitConverter.ToSingle(msgPayload, 40);
                    robot.erreurderiveeMax_Theta = BitConverter.ToSingle(msgPayload, 44);
                    asservSpeedDisplay.UpdatePolarSpeedCorrectionGains(robot.Kp_X, robot.Kp_Theta, robot.Ki_X, robot.Ki_Theta, robot.Kd_X, robot.Kd_Theta);
                    asservSpeedDisplay.UpdatePolarSpeedCorrectionLimits(robot.erreurproportionelleMax_X, robot.erreurproportionelleMax_Theta, robot.erreurintegralMax_X, robot.erreurintegralMax_Theta, robot.erreurderiveeMax_X, robot.erreurderiveeMax_Theta);
                    break;
            }
        }

        private void boutonTest_Click(object sender, RoutedEventArgs e)
        {
          
            if (robot.autoControlActivated == 0)
                robot.autoControlActivated = 1;
            else
            {
                robot.autoControlActivated = 0;
                UartEncodeAndSendMessage(0x0051, 1, new byte[] { (byte)12 });
            }
            UartEncodeAndSendMessage(0x0052, 2, new byte[] { (byte)robot.autoControlActivated });
        }

        private void oscilloSpeed_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void SET_PIDX_Click(object sender, RoutedEventArgs e)
        {
            List<byte> payload = new List<byte>();
            payload.Add(0);
            if(TextBoxKp.Text != "")
                payload.AddRange(BitConverter.GetBytes(float.Parse(TextBoxKp.Text)));
            else 
                payload.AddRange(BitConverter.GetBytes(float.Parse("0")));
            if (TextBoxKi.Text != "")
                payload.AddRange(BitConverter.GetBytes(float.Parse(TextBoxKi.Text)));
            else
                payload.AddRange(BitConverter.GetBytes(float.Parse("0")));
            if (TextBoxKd.Text != "")
                payload.AddRange(BitConverter.GetBytes(float.Parse(TextBoxKd.Text)));
            else
                payload.AddRange(BitConverter.GetBytes(float.Parse("0")));
            if (P_Max.Text != "")
                payload.AddRange(BitConverter.GetBytes(float.Parse(P_Max.Text)));
            else
                payload.AddRange(BitConverter.GetBytes(float.Parse("0")));
            if (I_Max.Text != "")
                payload.AddRange(BitConverter.GetBytes(float.Parse(I_Max.Text)));
            else
                payload.AddRange(BitConverter.GetBytes(float.Parse("0")));
            if (D_Max.Text != "")
                payload.AddRange(BitConverter.GetBytes(float.Parse(D_Max.Text)));
            else
                payload.AddRange(BitConverter.GetBytes(float.Parse("0")));
            UartEncodeAndSendMessage(0x0060, payload.Count(), payload.ToArray()); //type de pid (0 = X, 1 = theta), 4 octets de Kp
        }

        private void SET_PIDTheta_Click(object sender, RoutedEventArgs e)
        {
            List<byte> payload = new List<byte>();
            payload.Add(1);
            if (TextBoxKp.Text != "")
                payload.AddRange(BitConverter.GetBytes(float.Parse(TextBoxKp.Text)));
            else
                payload.AddRange(BitConverter.GetBytes(float.Parse("0")));
            if (TextBoxKi.Text != "")
                payload.AddRange(BitConverter.GetBytes(float.Parse(TextBoxKi.Text)));
            else
                payload.AddRange(BitConverter.GetBytes(float.Parse("0")));
            if (TextBoxKd.Text != "")
                payload.AddRange(BitConverter.GetBytes(float.Parse(TextBoxKd.Text)));
            else
                payload.AddRange(BitConverter.GetBytes(float.Parse("0")));
            if (P_Max.Text != "")
                payload.AddRange(BitConverter.GetBytes(float.Parse(P_Max.Text)));
            else
                payload.AddRange(BitConverter.GetBytes(float.Parse("0")));
            if (I_Max.Text != "")
                payload.AddRange(BitConverter.GetBytes(float.Parse(I_Max.Text)));
            else
                payload.AddRange(BitConverter.GetBytes(float.Parse("0")));
            if (D_Max.Text != "")
                payload.AddRange(BitConverter.GetBytes(float.Parse(D_Max.Text)));
            else
                payload.AddRange(BitConverter.GetBytes(float.Parse("0")));
            UartEncodeAndSendMessage(0x0060, payload.Count(), payload.ToArray()); //type de pid (0 = X, 1 = theta), 4 octets de Kp
        }
    }
}