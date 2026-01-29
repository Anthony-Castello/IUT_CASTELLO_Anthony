using System.Reflection;
using System.Runtime.InteropServices;

namespace ServoFeetech_NS
{
    public class Feetech 
    {
        public static int ServoDelay = 10; // Delai entre deux requête (peut être modifié)

        public static List<FeetechServo> servos = new List<FeetechServo>();
        public List<FeetechServo> getServos()
        {
            return servos;
        }

        public FeetechServo? getServoByName(string name)
        {
            foreach (var servo in servos)
            {
                if (servo.Name == name)
                    return servo;
            }
            return null;
        }

        public FeetechServo? getServoById(byte id)
        {
            foreach (var servo in servos)
            {
                if (servo.Id == id)
                    return servo;
            }
            return null;
        }

        Dictionary<byte, byte> InstructionRequestedDictionary = new Dictionary<byte, byte>();

        ///  Input events 
        public void ReadServoData(object sender, FeetechServoReadArgs e)
        {
            
            byte id = getServoByName(e.Name)!.Id;
            byte address = Convert.ToByte(e.Location);

            Packet p = new Packet(id, (byte)FeetechCommands.READ, new byte[] { address, e.NumberOfBytes });
            if (!InstructionRequestedDictionary.ContainsKey(id))
                InstructionRequestedDictionary.Add(id, address);
            else
                InstructionRequestedDictionary[id] = address;

            var message = p.ToByteArray();
            OnSendMessage(message);
        }

        public void SyncReadServoData(object sender, FeetechServoSyncReadArgs e)
        {
            byte[] ids = new byte[e.Names.Length];
            foreach (string name in e.Names)
            {
                ids[Array.IndexOf(e.Names, name)] = getServoByName(name)!.Id;
            }
            byte address = Convert.ToByte(e.Location);

            List<byte> payloadList = new List<byte>() { address, e.NumberOfBytes };
            payloadList.AddRange(ids);
            byte[] result = payloadList.ToArray();

            Packet p = new Packet(0xFE, (byte)FeetechCommands.SYNCREAD, result);
            foreach(byte id in ids)
                if (!InstructionRequestedDictionary.ContainsKey(id))
                    InstructionRequestedDictionary.Add(id, address);
                else
                    InstructionRequestedDictionary[id] = address;

            var message = p.ToByteArray();
            OnSendMessage(message);
        }

        public void WriteServoData(object sender, FeetechServoWriteArgs e)
        {

            byte id = getServoByName(e.Name)!.Id;
            byte address = Convert.ToByte(e.Location);

            List<byte> payloadList = e.Payload.ToList();
            payloadList.Insert(0, address); 
            byte[] result = payloadList.ToArray();
            Packet p = new Packet(id, (byte)FeetechCommands.WRITE, result);
            if (!InstructionRequestedDictionary.ContainsKey(id))
                InstructionRequestedDictionary.Add(id, address);
            else
                InstructionRequestedDictionary[id] = address;

            var message = p.ToByteArray();
            OnSendMessage(message);
        }
        public void SyncWriteServoData(object sender, FeetechServoSyncWriteArgs e)
        {

            byte[] ids = new byte[e.Names.Length];
            byte address = Convert.ToByte(e.Location);
            foreach (string name in e.Names)
            {
                ids[Array.IndexOf(e.Names, name)] = getServoByName(name)!.Id;
            }

            byte[] payload = new byte[2 + ids.Length * (e.Payload.Length + 1)];
            int pos = 0;
            payload[pos++] = address;
            payload[pos++] = (byte)e.Payload.Length;
            foreach ( byte id in ids)
            {
                payload[pos++] = id;
                foreach (byte datas in e.Payload)
                {
                    payload[pos++] = datas;
                }
            }

            Packet p = new Packet(0xFE, (byte)FeetechCommands.SYNCWRITE, payload);

            if (!InstructionRequestedDictionary.ContainsKey(0xFE))
                InstructionRequestedDictionary.Add(0xFE, address);
            else
                InstructionRequestedDictionary[0xFE] = address;

            var message = p.ToByteArray();
            OnSendMessage(message);
        }

        public void RegWriteServoData(object sender, FeetechServoRegWriteArgs e)
        {

            byte id = getServoByName(e.Name)!.Id;
            byte address = Convert.ToByte(e.Location);
            byte[] payload = new byte[e.Payload.Length + 1];
            int pos = 0;
            payload[pos++] = address;
            foreach (byte datas in e.Payload)
            {
               payload[pos++] = datas;
            }

            Packet p = new Packet(id, (byte)FeetechCommands.REGWRITE, payload);

            if (!InstructionRequestedDictionary.ContainsKey(id))
                InstructionRequestedDictionary.Add(id, address);
            else
                InstructionRequestedDictionary[id] = address;

            var message = p.ToByteArray();
            OnSendMessage(message);
        }


        public void ActionServoData(object sender, FeetechServoActionArgs e)
        {
           
            Packet p = new Packet(0xFE, (byte)FeetechCommands.ACTION, new byte[] {});
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
            for (int i = 0; i < payload.Length; i++)
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

            // Récupère des données de base de la trame
            info.Id = id;
            info.Adress = requestedRegister;
            info.Length = length;
            info.payload = msgPayload;

            int pos = 0;
            int lastPos = 0;

            FeetechServo? s = getServoById(id);
            if (s == null) return;

            Type? mem = null; // Initialisation
            switch (s.Model)
            {
                case FeetechServoModels.SCS:
                    mem = typeof(FeetechMemorySCS);
                    break;
                case FeetechServoModels.STS:
                    mem = typeof(FeetechMemorySTS);
                    break;
                case FeetechServoModels.SM:
                    mem = typeof(FeetechMemorySM);
                    break;
            }
            if (mem == null) return;

            while (pos < msgPayload.Length)
            {
                
                // Actualise les données a récupérer
                requestedRegister += (byte)(pos - lastPos);
                lastPos = pos;

                // Récupération du type de la donnée
                string? nameInfo = Enum.GetName(mem, requestedRegister);
                if (string.IsNullOrEmpty(nameInfo)) continue;
                Type? dataType = info.GetTypeByName(nameInfo);
                if (dataType == null) continue;


                var field = info.GetType().GetField(nameInfo);
                // Problème de performance possible car manipulation de string
                switch (dataType.Name)
                {
                    case "Byte":
                        field.SetValue(info, msgPayload[pos++]);
                        break;
                    case "Int16":
                        Int16 int16Value;
                        // Gestion de l'Endianness selon le modèle
                        if (s.Model == FeetechServoModels.STS || s.Model == FeetechServoModels.SM)
                            int16Value = (Int16)((msgPayload[pos + 1] << 8) | msgPayload[pos]);
                        else
                            int16Value = (Int16)((msgPayload[pos] << 8) | msgPayload[pos + 1]);
                        field.SetValue(info, int16Value);
                        pos += 2;
                        break;
                    case "Boolean":
                        field.SetValue(info, msgPayload[pos++] != 0);
                        break;
                    default:
                        // Type non géré
                        pos++;
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
        public string Name;
        public Enum Location;
        public byte NumberOfBytes;
    }

    public class FeetechServoSyncReadArgs : EventArgs
    {
        public string[] Names;
        public Enum Location;
        public byte NumberOfBytes;
    }

    public class FeetechServoWriteArgs : EventArgs
    {
        public string Name;
        public Enum Location;
        public byte[] Payload;
    }

    public class FeetechServoSyncWriteArgs : EventArgs
    {
        public string[] Names;
        public Enum Location;
        public byte[] Payload;
    }

    public class FeetechServoRegWriteArgs : EventArgs
    {
        public string Name;
        public Enum Location;
        public byte[] Payload;
    }
    public class FeetechServoActionArgs : EventArgs
    {

    }
    
    public class FeetechServoInfo
    {

        public Type? GetTypeByName(string name)
        {
            // On cherche la définition du champ dans la structure de la classe
            FieldInfo? field = typeof(FeetechServoInfo).GetField(name);

            if (field == null) return null;

            // IMPORTANT : Si c'est un Nullable (ex: Int16?), on veut souvent le type réel (Int16)
            // Sinon le switch renverra "Nullable`1" au lieu de "Int16"
            return Nullable.GetUnderlyingType(field.FieldType) ?? field.FieldType;
        }

        public byte? Id = null;
        public byte? Adress = null;
        public byte? Length = null;
        public byte[]? payload = null;



        public byte? BaudRate = null;
        public byte? ReturnDelayTime = null;
        public byte? StatusReturnLevel = null;
        public byte? SettingByte;
        public bool? ReplyState = null;
        public byte? MinimalAngle = null;
        public Int16? MinPositionLimit = null;
        public Int16? MaxPositionLimit = null;
        public byte? MaxTemperatureLimit = null;
        public byte? MaxInputVoltage = null;
        public byte? MinInputVoltage = null;
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
        public bool? MovingStatus = null;
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

    public enum FeetechMemorySCS : byte
    {
        Id = 0x05,
        Baudrate = 0x06,
        StatusReturnLevel = 0x08,
        MinPositionLimit = 0x09, //2 octets
        MaxPositionLimit = 0x0B, //2 octets
        MaxTemperatureLimit = 0x0D,
        MaxInputVoltage = 0x0E,
        MinInputVoltage = 0x0F,
        MaxTorqueLimit = 0x10, // 2 octets
        ProtectionSwitch = 0x13,
        LEDAlarmCondition = 0x14,
        PositionPGain = 0x15,
        PositionDGain = 0x16,
        PositionIGain = 0x17,
        Punch = 0x18,
        CWDeadBand = 0x1A,
        CCWDeadBand = 0x1B,
        ProtectTorque = 0x25,
        OverloadProtectionTime_SCS = 0x26,
        OverloadTorque = 0x27,
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
    }

    public enum FeetechMemorySTS : byte
    {
        Id = 0x05,
        Baudrate = 0x06,
        StatusReturnLevel = 0x08,
        MinPositionLimit = 0x09, //2 octets
        MaxPositionLimit = 0x0B, //2 octets
        MaxTemperatureLimit = 0x0D,
        MaxInputVoltage = 0x0E,
        MinInputVoltage = 0x0F,
        MaxTorqueLimit = 0x10, // 2 octets
        SettingByte = 0x12, 
        ProtectionSwitch = 0x13,
        LEDAlarmCondition = 0x14,
        PositionPGain = 0x15,
        PositionDGain = 0x16,
        PositionIGain = 0x17,
        Punch = 0x18,
        MAX_I = 0x19,
        CWDeadBand = 0x1A,
        CCWDeadBand = 0x1B,
        OverloadCurrent = 0x1C, // 2 octets
        AngularResolution = 0x1E,
        PositionOffsetValue = 0x1F, // 2 octets
        WorkMode = 0x21,
        ProtectTorque = 0x22,
        OverLoadProtectionTime = 0x23,
        OverloadTorque = 0x24,
        VelocityPGain = 0x25,
        OvercurrentProtectionTime = 0x26,
        VelocityIGain = 0x27,
        TorqueEnable = 0x28,
        GoalAcceleration = 0x29,
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

    public enum FeetechMemorySM : byte
    {
        Id = 0x05,
        Baudrate = 0x06,
        ReturnDelayTime = 0x07,
        StatusReturnLevel = 0x08,
        MinPositionLimit = 0x09, //2 octets
        MaxPositionLimit = 0x0B, //2 octets
        MaxTemperatureLimit = 0x0D,
        MaxInputVoltage = 0x0E,
        MinInputVoltage = 0x0F,
        MaxTorqueLimit = 0x10, // 2 octets
        SettingByte = 0x12,
        ProtectionSwitch = 0x13,
        LEDAlarmCondition = 0x14,
        PositionPGain = 0x15,
        PositionDGain = 0x16,
        PositionIGain = 0x17,
        Punch = 0x18,
        MAX_I = 0x19,
        CWDeadBand = 0x1A,
        CCWDeadBand = 0x1B,
        OverloadCurrent = 0x1C, // 2 octets
        AngularResolution = 0x1E,
        PositionOffsetValue = 0x1F, // 2 octets
        WorkMode = 0x21,
        ProtectTorque = 0x22,
        OverLoadProtectionTime = 0x23,
        OverloadTorque = 0x24,
        VelocityPGain = 0x25,
        OvercurrentProtectionTime = 0x26,
        VelocityIGain = 0x27,
        TorqueEnable = 0x28,
        GoalAcceleration = 0x29,
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

    public class FeetechServo
    {
        public string Name; 
        public byte Id;        
        public FeetechServoModels Model;

        // Constructeur pour faciliter la création d'un paquet
        public FeetechServo(string name, byte id, FeetechServoModels model)
        {
            Name = name;
            Id = id;
            Model = model;
        }
    }

    public enum FeetechServoModels
    {
        SCS,
        STS,
        SM
    }

}
