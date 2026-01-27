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

        public enum StateReception
        {
            Waiting1,
            Waiting2,
            ID,
            Length,
            Error,
            Payload,
            CheckSum
        }

        public enum Commands
        {
            send = 0x01,
            couple = 0x02,
            voltage = 0x03
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

            Trames.Packet p = new Trames.Packet(id, instruction, payload, (byte)Trames.Commands.send);
            p.sendPacket(port);

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
        static public void readCouple(byte id, SerialPort port)
        {
            byte instruction = 0x02; // READ DATA
            byte[] payload = new byte[] { 0x3C, 0x02 }; // Adresse de départ et longueur de lecture

            Packet p = new Packet(id, instruction, payload, (byte)Commands.couple);

            // Envoi du packet
            p.sendPacket(port);
        }

        static StateReception rcvState = StateReception.Waiting1;
        static byte msgDecodedId = 0;
        static byte msgDecodedPayloadLength = 0;
        static byte msgDecodedError = 0;
        static byte[] msgDecodedPayload;
        static int msgDecodedPayloadIndex = 0;
        public static void DecodeMessage(byte c)
        {
            switch (rcvState)
            {
                case StateReception.Waiting1:
                    if (c == 0xFF)
                    {
                        rcvState = StateReception.Waiting2;
                    }
                    break;
                case StateReception.Waiting2:
                    if (c == 0xFF)
                    {
                        rcvState = StateReception.ID;
                    }
                    break;
                case StateReception.ID:
                    msgDecodedId = c;
                    rcvState = StateReception.Length;
                    break;
                case StateReception.Length:
                    msgDecodedPayloadLength = c;
                    msgDecodedPayload = new byte[msgDecodedPayloadLength - 2];
                    msgDecodedPayloadIndex = 0;
                    rcvState = StateReception.Error;
                    break;
                case StateReception.Error:
                    msgDecodedError = c;
                    if (msgDecodedPayloadLength > 2)
                        rcvState = StateReception.Payload;
                    else
                        rcvState = StateReception.CheckSum;
                    break;
                case StateReception.Payload:


                    msgDecodedPayload[msgDecodedPayloadIndex] = c;
                    msgDecodedPayloadIndex++;
                    if (msgDecodedPayloadIndex >= msgDecodedPayloadLength - 2)
                        rcvState = StateReception.CheckSum;
                    break;
                case StateReception.CheckSum:

                    byte calculatedChecksum = CalculateChecksum(msgDecodedId, msgDecodedError, msgDecodedPayloadLength, msgDecodedPayload);
                    if (calculatedChecksum == c && requests.Count() > 0)
                    {
                        ProcessDecodedMessage(msgDecodedId, msgDecodedPayloadLength, msgDecodedPayload);
                    }
                    rcvState = StateReception.Waiting1;
                    break;
                default:
                    rcvState = StateReception.Waiting1;
                    break;
            }
        }


        private static void ProcessDecodedMessage(byte id, byte length, byte[] msgPayload)
        {
            switch (requests.Dequeue()) // Prochaine requête reçue
            {
                case (byte)Commands.send:
                    mainWin.TextBoxReception.Text += "COMMANDE RECU";
                    break;
                case (byte)Commands.couple:
                    TextBlock? tb = mainWin.FindName("C_ID" + id.ToString()) as TextBlock;
                    if (tb == null) break;
                    if (msgPayload.Length < 2)
                        break;

                    int rawValue = (msgPayload[0] << 8) | msgPayload[1];
                    if(id == 5 || id == 6)
                    {
                        rawValue = (msgPayload[1] << 8) | msgPayload[0];
                    }
                    if (rawValue >= 1024) rawValue -= 1024;

                    tb.Text = id.ToString() + ": " + rawValue.ToString();
                    break;
                default:
                    break;
            }
        }

        public static byte CalculateChecksum(byte id, byte inst_err, int length, byte[] payload)
        {
            int sum = id + inst_err + length;
            for (int i = 0; i < length - 2; i++)
            {
                sum += payload[i];
            }

            return (byte)(~(sum & 0xFF));
        }

        // --- Envoi des Packets ---

        public struct Packet
        {
            public byte Id;             // ID du moteur (0-6)
            public byte Length;         // Taille des données (Paramètres + 2) 
            public byte Command;        // Instruction (envoi) ou Error (réception)
            public byte[] Payload;      // Données utiles (Position, Charge, etc.)
            public byte Checksum;       // Somme de vérification
            public byte request;        // Requête associée

            // Constructeur pour faciliter la création d'un paquet
            public Packet(byte id, byte command, byte[] payload, byte request)
            {
                Id = id;
                Command = command;
                Payload = payload ?? Array.Empty<byte>(); // Si payload est null, on crée un tableau vide au lieu de laisser null
                // La longueur est égale au nombre de paramètres + 2 (Command/Error + Checksum)
                Length = (byte)(payload.Length + 2);
                Checksum = 0; // À calculer ensuite avec CalculateChecksum
                this.request = request;
            }

            public byte[] ToByteArray()
            {
                List<byte> byteArray = new List<byte>
                {
                    0xFF, 0xFF, // En-tête
                    Id,
                    Length,
                    Command
                };
                byteArray.AddRange(Payload);
                Checksum = Trames.CalculateChecksum(Id, Command, Length, Payload);
                byteArray.Add(Checksum);
                return byteArray.ToArray();
            }

            public void sendPacket(SerialPort port)
            {
                byte[] packetBytes = ToByteArray();
                requests.Enqueue(request);
                port.Write(packetBytes, 0, packetBytes.Length);
            }

            public byte getRequest()
            {
                return request;
            }
        }





    }
}
