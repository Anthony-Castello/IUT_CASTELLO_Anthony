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
        public int autoControlActivated = 1;
        public Robot()
        {
        }
}
}

