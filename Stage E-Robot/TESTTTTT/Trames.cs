using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace TESTTTTT
{
    internal class Trames
    {
        static private MainWindow mainWin = (MainWindow)Application.Current.MainWindow;

        static public Queue<byte> byteListReceived = new Queue<byte>();
        static public Queue<byte> requests = new Queue<byte>();


        public enum Commands
        {
            Send = 0x01,
            Datas = 0x02,
        }


        static public void sendCommande(byte id, UInt16 pos, UInt16 time, UInt16 vit, SerialPort port)
        {
            if(id == 5 || id == 6)
            {
                pos *= 4;
                vit *= 4;
            }

            byte instruction = 0x03; // WRITE DATA
            byte address = 0x2A; // Goal Position

            byte[] payload = new byte[7];
            payload[0] = address;

            if (id == 5 || id == 6) // Mode STS (Little-Endian) 
            {
                payload[1] = (byte)(pos & 0xFF);
                payload[2] = (byte)(pos >> 8);
                payload[3] = (byte)(time & 0xFF);
                payload[4] = (byte)(time >> 8);
                payload[5] = (byte)(vit & 0xFF);
                payload[6] = (byte)(vit >> 8);
            }
            else // Mode SCS (Big-Endian) 
            {
                payload[1] = (byte)(pos >> 8);
                payload[2] = (byte)(pos & 0xFF);
                payload[3] = (byte)(time >> 8);
                payload[4] = (byte)(time & 0xFF);
                payload[5] = (byte)(vit >> 8);
                payload[6] = (byte)(vit & 0xFF);
            }

            //Trames.Packet p = new Trames.Packet(id, instruction, payload, (byte)Trames.Commands.Send);
            //p.sendPacket(port);

            if(id == 0xFE)
            {
                for (int i = 1; i <= mainWin.moteursNum; i++)
                {
                    CheckBox? cb = mainWin.FindName("T" + i.ToString()) as CheckBox;
                    if (cb == null) continue;
                    cb.IsChecked = true;
                }
            }
            else
            {
                CheckBox? cb = mainWin.FindName("T" + id.ToString()) as CheckBox;
                if (cb == null) return;
                cb.IsChecked = true;
            }

        }
        static public void readDatas(SerialPort port)
        {
            byte id = 0xFE;
            byte instruction = 0x82; // SYNC READ (on lit tous les moteurs
            byte[] payload = new byte[] { 0x38, 0x06, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 }; // Adresse de départ et longueur de lecture

            //Packet p = new Packet(id, instruction, payload, (byte)Commands.Datas);

            //// Envoi du packet
            //p.sendPacket(port);
        }

        static public void readPosition(byte id, SerialPort port)
        {
            //byte instruction = 0x02; // READ DATA
            //byte[] payload = new byte[] { 0x38, 0x02 }; // Adresse de départ et longueur de lecture

            //Packet p = new Packet(id, instruction, payload, (byte)Commands.Position);

            //// Envoi du packet
            //p.sendPacket(port);
        }







        // --- Envoi des Packets ---







    }
}
