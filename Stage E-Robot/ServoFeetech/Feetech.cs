namespace ServoFeetech_NS
{
    public class Feetech 
    {
        Dictionary<byte, byte> InstructionRequestedDictionary = new Dictionary<byte, byte>();
        //Dictionary<byte, FeetechServoInfo> servoInfo = new Dictionary<byte, FeetechServoInfo>();

        ///  Input events 
        public void ReadServoData(object sender, FeetechServoReadArgs e)
        {
            Packet p = new Packet(e.Id, (byte)FeetechCommands.READ, new byte[] { (byte)(e.Location), e.NumberOfBytes });
            if (!InstructionRequestedDictionary.ContainsKey(e.Id))
                InstructionRequestedDictionary.Add(e.Id, (byte)e.Location);
            else
                InstructionRequestedDictionary[e.Id] = (byte)e.Location;

            var message = p.ToByteArray();
            OnSendMessage(message);
        }

        //public void SyncReadServoData(object sender, FeetechServoSyncReadArgs e)
        //{
        //    Packet p = new Packet(e.Id, (byte)FeetechCommands.READ, new byte[] { (byte)(e.Location), e.NumberOfBytes });
        //    if (!InstructionRequestedDictionary.ContainsKey(e.Id))
        //        InstructionRequestedDictionary.Add(e.Id, (byte)e.Location);
        //    else
        //        InstructionRequestedDictionary[e.Id] = (byte)e.Location;

        //    var message = p.ToByteArray();
        //    OnSendMessage(message);
        //}

        public void WriteServoData(object sender, FeetechServoWriteArgs e)
        {
            List<byte> payloadList = e.Payload.ToList();
            payloadList.Insert(0, (byte)e.Location); 
            byte[] result = payloadList.ToArray();
            Packet p = new Packet(e.Id, (byte)FeetechCommands.WRITE, result);
            if (!InstructionRequestedDictionary.ContainsKey(e.Id))
                InstructionRequestedDictionary.Add(e.Id, (byte)e.Location);
            else
                InstructionRequestedDictionary[e.Id] = (byte)e.Location;

            var message = p.ToByteArray();
            OnSendMessage(message);
        }
        public void SyncWriteServoData(object sender, FeetechServoSyncWriteArgs e)
        {

            byte[] payload = new byte[2 + e.Id.Length * (e.Payload.Length + 1)];
            int pos = 0;
            payload[pos++] = (byte)e.Location;
            payload[pos++] = (byte)e.Payload.Length;
            foreach ( byte id in e.Id)
            {
                payload[pos++] = id;
                foreach (byte datas in e.Payload)
                {
                    payload[pos++] = datas;
                }
            }

            Packet p = new Packet(0xFE, (byte)FeetechCommands.SYNCWRITE, payload);

            if (!InstructionRequestedDictionary.ContainsKey(0xFE))
                InstructionRequestedDictionary.Add(0xFE, (byte)e.Location);
            else
                InstructionRequestedDictionary[0xFE] = (byte)e.Location;

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
            int lastPos = 0;

            while (pos < msgPayload.Length)
            {
                
                // Actualise les données a récupérer
                requestedRegister += (byte)(pos - lastPos);
                lastPos = pos;

                switch ((FeetechMemory)requestedRegister)
                {
                    case FeetechMemory.Baudrate:
                        info.BaudRate = msgPayload[pos++];
                        break;
                    case FeetechMemory.AngleLimitMin:
                        var AngleLimitMin = (Int16)(msgPayload[pos] << 8 | msgPayload[pos + 1] << 0);
                        pos += 2;
                        info.AngleLimitMin = AngleLimitMin;
                        break;
                    case FeetechMemory.AngleLimitMax:
                        var AngleLimitMax = (Int16)(msgPayload[pos] << 8 | msgPayload[pos + 1] << 0);
                        pos += 2;
                        info.AngleLimitMax = AngleLimitMax;
                        break;
                    case FeetechMemory.PresentPosition:
                        var PresentPosition = (Int16)(msgPayload[pos] << 8 | msgPayload[pos + 1] << 0);
                        pos += 2;
                        info.PresentPosition = PresentPosition;
                        break;
                    case FeetechMemory.PresentVelocity:
                        var PresentVelocity = (Int16)(msgPayload[pos] << 8 | msgPayload[pos + 1] << 0);
                        pos += 2;
                        info.PresentVelocity = PresentVelocity;
                        break;
                    case FeetechMemory.PresentPWM:
                        var PresentPWM = (Int16)(msgPayload[pos] << 8 | msgPayload[pos + 1] << 0);
                        pos += 2;
                        info.PresentPWM = PresentPWM;
                        break;

                    case FeetechMemory.TorqueEnable:
                        info.TorqueEnable = (msgPayload[pos++] != 0); // True or false
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
            Length = (byte)(payload.Length + 2);// La longueur est égale au nombre de paramètres + 2 (Command/Error + Checksum)
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

    }

    public class FeetechServoDataArgs : EventArgs
    {
        public FeetechServoInfo info;
    }
    public class ByteArrayArgs : EventArgs
    {
        public byte[] array;
    }

    public class FeetechServoReadArgs : EventArgs
    {
        public byte Id;
        public FeetechMemory Location;
        public byte NumberOfBytes;
    }

    public class FeetechServoSyncReadArgs : EventArgs
    {
        public byte[] Id;
        public FeetechMemory Location;
        public byte NumberOfBytes;
    }

    public class FeetechServoWriteArgs : EventArgs
    {
        public byte Id;
        public FeetechMemory Location;
        public byte[] Payload;
    }
    public class FeetechServoSyncWriteArgs : EventArgs
    {
        public byte[] Id;
        public FeetechMemory Location;
        public byte[] Payload;
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
        public byte? MaxTemperatureLimit = null;
        public byte? MaxInputVoltage = null;
        public byte? MixInputVoltage = null;
        public Int16? MaxCouple = null;
        public byte? phase = null;
        public byte? ProtectionSwitch = null;
        public byte? LEDAlarmCondition = null;
        public byte? PositionPGain = null;
        public byte? PositionDGain = null;
        public byte? PositionIGain = null;
        public byte? Punch = null;
        public byte? MAX_I = null;
        public byte? CWDeadBand = null;
        public byte? CCWDeadBand = null;
        public Int16? OverloadCurrent = null;
        public byte? AngularResolution = null;
        public Int16? PositionOffsetValue= null;
        public byte? WorkMode = null;
        public byte? ProtectTorque = null;
        public byte? OverloadProtectionTime = null;
        public byte? OverloadTorque = null;
        public byte? VelocityPGain = null;
        public byte? OvercurrentProtectionTime = null;
        public byte? VelocityIGain = null;
        public bool? TorqueEnable = null;
        public byte? GoalAcceleration = null;
        public Int16? GoalPosition = null;
        public Int16? GoalPWM = null;
        public Int16? GoalVelocity = null;
        public Int16? TorqueLimit = null;
        public byte? Lock = null;
        public Int16? PresentPosition = null;
        public Int16? PresentVelocity = null;
        public Int16? PresentPWM = null;
        public byte? PresentInputVoltage= null;
        public byte? PresentTemperature = null;
        public byte? SyncWriteFlag = null;
        public byte? HardwareErrorStatus = null;
        public byte? MovingStatus = null;
        public byte? PresentCurrent = null;
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
        Delay = 0x07,
        ReplyState = 0x08,
        AngleLimitMin = 0x09, //2 octets
        AngleLimitMax = 0x0B, //2 octets
        MaxTemperatureLimit = 0x0D,
        MaxInputVoltage = 0x0E,
        MixInputVoltage = 0x0F,
        MaxTorqueLimit = 0x10, // 2 octets
        phase = 0x12, 
        ProtectionSwitch = 0x13,
        LEDAlarmCondition = 0x14,
        PositionPGain = 0x15,
        PositionDGain = 0x16,
        PositionIGain = 0x17,
        Punch = 0x18,
        MAX_I= 0x19,
        CWDeadBand = 0x1A,
        CCWDeadBand = 0x1B,
        OverloadCurrent = 0x1C, // 2 octets
        AngularResolution = 0x1E,
        PositionShift = 0x1F, // 2 octets
        WorkMode = 0x21,
        ProtectTorque = 0x22,
        OverloadProtectionTime = 0x23,
        OverloadTorque = 0x24,
        VelocityPGain = 0x25,
        OverLoadProtectionTime = 0x26,
        VelocityIGain = 0x27,
        TorqueEnable = 0x28,
        Acceleration = 0x29,
        GoalPosition = 0x2A,  // 2 octets
        GoalPWM = 0x2C,  // 2 octets (val du torque)
        GoalVelocity = 0x2E,  // 2 octets
        TorqueLimit = 0x30,  // 2 octets
        Lock = 0x37,
        PresentPosition = 0x38,  // 2 octets
        PresentVelocity = 0x3A,  // 2 octets
        PresentPWM = 0x3C,  // 2 octets (Torque)
        PresentInputVoltage = 0x3E,
        PresentTemperature = 0x3F,
        SyncWriteFlag = 0x40,
        HardwareErrorStatus = 0x41,
        MovingStatus = 0x42,
        PresentCurrent = 0x45, // 2 octets
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
