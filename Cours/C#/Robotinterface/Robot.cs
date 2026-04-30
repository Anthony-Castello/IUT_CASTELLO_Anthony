using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;

namespace Robotinterface
{

    public class Robot
    {
        public string receivedText = "";
        public Queue<byte> byteListReceived = new Queue<byte>();
        

        public float distanceTelemetreExDroit;
        public float distanceTelemetreDroit;
        public float distanceTelemetreCentre;
        public float distanceTelemetreGauche;
        public float distanceTelemetreExGauche;
        public int autoControlActivated;
        public float Kp_X;
        public float Ki_X;
        public float Kd_X;
        public float erreurproportionelleMax_X;
        public float erreurintegralMax_X;
        public float erreurderiveeMax_X;
        public float erreur_X;
        public float corrP_X;
        public float corrI_X;
        public float corrD_X;
        public float Kp_Theta;
        public float Ki_Theta;
        public float Kd_Theta;
        public float erreurproportionelleMax_Theta;
        public float erreurintegralMax_Theta;
        public float erreurderiveeMax_Theta;
        public float erreur_Theta;
        public float corrP_Theta;
        public float corrI_Theta;
        public float corrD_Theta;

        public byte PidX = 0;
        public byte PidTheta = 1;
        public Robot()
        {
        }
}
}

