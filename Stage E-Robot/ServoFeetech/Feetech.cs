using System.Diagnostics;

namespace ServoFeetech_NS
{
    public class Feetech
    {
        Dictionary<byte, byte> InstructionRequestedDictionary = new Dictionary<byte, byte>();
        //Dictionary<byte, FeetechServoInfo> servoInfo = new Dictionary<byte, FeetechServoInfo>();

        ///  Input events 
        public void RequestServoData(object sender, FeetechServoRequestArgs e)
        {
            Packet p = new Packet(e.Id, (byte)FeetechCommands.READ, new byte[] { (byte)(e.Location), e.NumberOfBytes });
            if (!InstructionRequestedDictionary.ContainsKey(e.Id))
                InstructionRequestedDictionary.Add(e.Id, (byte)e.Location);
            else
                InstructionRequestedDictionary[e.Id] = (byte)e.Location;

            var message = p.ToByteArray();
            OnSendMessage(message);
        }
        public void DecodeData(object sender, ByteArrayArgs e)
        {
            foreach (var b in e.array)
            {
                DecodeMessage(b);
            }
        }

        ///  Output events 
        public event EventHandler<FeetechServoDataArgs> OnServoDataEvent;
        public new virtual void OnServoData(FeetechServoInfo fi)
        {
            var handler = OnServoDataEvent;
            if (handler != null)
            {
                handler(this, new FeetechServoDataArgs { info = fi});
            }
        }
        
        public event EventHandler<ByteArrayArgs> OnSendMessageEvent;
        public new virtual void OnSendMessage(byte[] data)
        {
            var handler = OnSendMessageEvent;
            if (handler != null)
            {
                handler(this, new ByteArrayArgs {  array = data });
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

        StateReception rcvState = StateReception.Waiting1;
        byte msgDecodedId = 0;
        byte msgDecodedPayloadLength = 0;
        byte msgDecodedError = 0;
        byte[] msgDecodedPayload;
        int msgDecodedPayloadIndex = 0;
        public void DecodeMessage(byte c)
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

                    if (calculatedChecksum == c)
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

        private void ProcessDecodedMessage(byte id, byte length, byte[] msgPayload)
        {
            var requestedRegister = InstructionRequestedDictionary[id];
            FeetechServoInfo info = new FeetechServoInfo();
            int pos = 0;

            while (pos < msgPayload.Length)
            {
                switch ((FeetechMemory)requestedRegister)
                {
                    case FeetechMemory.Baudrate:
                        info.BaudRate = msgPayload[pos++];
                        break;
                    case FeetechMemory.AngleLimitMin:
                        var v = (Int16)(msgPayload[pos] << 8 + msgPayload[pos + 1] << 0);
                        pos += 2;
                        info.AngleLimitMin = v;
                        break;
                }
            }

            OnServoData(info);
        }

    }

    public struct Packet
    {
        public byte Id;             // ID du moteur (0-6)
        public byte Length;         // Taille des données (Paramètres + 2) 
        public byte Instruction;    // Instruction (envoi) ou Error (réception)
        public byte[] Payload;      // Données utiles (Position, Charge, etc.)
        public byte Checksum;       // Somme de vérification

        // Constructeur pour faciliter la création d'un paquet
        public Packet(byte id, byte command, byte[] payload)
        {
            Id = id;
            Instruction = command;
            Payload = payload ?? Array.Empty<byte>(); // Si payload est null, on crée un tableau vide au lieu de laisser null
                                                      // La longueur est égale au nombre de paramètres + 2 (Command/Error + Checksum)
            Length = (byte)(payload.Length + 2);
            Checksum = 0; // À calculer ensuite avec CalculateChecksum
        }

        public byte[] ToByteArray()
        {
            List<byte> byteArray = new List<byte>
                {
                    0xFF, 0xFF, // En-tête
                    Id,
                    Length,
                    Instruction
                };
            byteArray.AddRange(Payload);
            Checksum = Feetech.CalculateChecksum(Id, Instruction, Length, Payload);
            byteArray.Add(Checksum);
            return byteArray.ToArray();
        }

        //public void sendPacket(SerialPort port)
        //{
        //    byte[] packetBytes = ToByteArray();
        //    requests.Enqueue(request);
        //    port.Write(packetBytes, 0, packetBytes.Length);
        //}

        
    }

    public class FeetechServoDataArgs : EventArgs
    {
        public FeetechServoInfo info;
    }
    public class ByteArrayArgs : EventArgs
    {
        public byte[] array;
    }

    public class FeetechServoRequestArgs : EventArgs
    {
        public byte Id;
        public FeetechMemory Location;
        public byte NumberOfBytes;
    }

    public class FeetechServoInfo
    {
        public byte? Id = null;
        public byte? BaudRate = null;
        public byte? Delay = null;
        public byte? ReplyState = null;
        public byte? MinimalAngle = null;
        public Int16? AngleLimitMin = null;
        public Int16? AngleLimitMax = null;
    }
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

    public enum FeetechMemory : byte
    {
        Id = 0x05,
        Baudrate = 0x06,
        ResponseDelay = 0x07,
        ResponseState = 0x08,
        AngleLimitMin = 0x09, //2 octets
        AngleLimitMax = 0x10, //2 octets
    }
    public enum FeetechCommands : byte
    {
        PING = 0x01,
        READ = 0x02,
        WRITE = 0x03,
        REGWRITE = 0x04,
        ACTION = 0x05,
        SYNCREAD = 0x82,
        SYNCWRITE = 0x83,
        REINIT = 0x0A,
        CALIBRATION_POSITION = 0x0B,
        RESTORE_PARAMS = 0x06,
        SAVE_PARAMS = 0x09,
        REBOOT = 0x08,
    }
}
