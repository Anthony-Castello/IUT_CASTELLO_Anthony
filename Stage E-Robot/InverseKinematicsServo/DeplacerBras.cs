using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServoFeetech_NS;
using GrafcetRobot_NS;

namespace DeplacerBras_NS
{
    public enum BrasPosition
    {
        StockageRight,
        StockageLeft,
        Waiting,
        Picking
    }

    public class DeplacerBras
    {
        Feetech servoManager;

        public volatile bool epaulePosAtteinte = true;
        public volatile bool coudePosAtteinte = true;
        public volatile bool poignet1PosAtteinte = true;
        public volatile bool poignet2PosAtteinte = true;
        public volatile bool poignet3PosAtteinte = true;

        BrasPosition brasPosition = BrasPosition.Waiting;

        public DeplacerBras(Feetech servoManager)
        {
            this.servoManager = servoManager;
        }

        public void servoInfoReceived(FeetechServoInfo info)
        {
            if (info == null)
                return;

            FeetechServo? servo = servoManager.getServoById((byte)info.Id);
            if (servo == null)
                return;

            if (info.PresentPosition != null && info.GoalPosition != null)
            {
                switch(servo.Name)
                {
                    // Seuil de tolérance pour considérer que la position est atteinte à 100 unités près
                    case "Epaule":
                        epaulePosAtteinte = Math.Abs((int)info.PresentPosition - (int)info.GoalPosition) <= 100 || Math.Abs((int)info.PresentPosition - (int)info.GoalPosition) >= 4095 - 100;
                        break;
                    case "Coude":
                        coudePosAtteinte = Math.Abs((int)info.PresentPosition - (int)info.GoalPosition) <= 100 || Math.Abs((int)info.PresentPosition - (int)info.GoalPosition) >= 4095 - 100;
                        break;
                    case "Poignet1":
                        poignet1PosAtteinte = Math.Abs((int)info.PresentPosition - (int)info.GoalPosition) <= 100 || Math.Abs((int)info.PresentPosition - (int)info.GoalPosition) >= 4095 - 100;
                        break;
                    case "Poignet2":
                        poignet2PosAtteinte = Math.Abs((int)info.PresentPosition - (int)info.GoalPosition) <= 100 || Math.Abs((int)info.PresentPosition - (int)info.GoalPosition) >= 4095 - 100;
                        break;
                    case "Poignet3":
                        poignet3PosAtteinte = Math.Abs((int)info.PresentPosition - (int)info.GoalPosition) <= 100 || Math.Abs((int)info.PresentPosition - (int)info.GoalPosition) >= 4095 - 100;
                        break;
                }
            }

        }


        private struct Position
        {
            public int Epaule;
            public int Coude;
            public int Poignet1;
            public int Poignet2;
            public int Poignet3;
            public Position(int epaule, int coude, int poignet1, int poignet2, int poignet3)
            {
                Epaule = epaule;
                Coude = coude;
                Poignet1 = poignet1;
                Poignet2 = poignet2;
                Poignet3 = poignet3;
            }
        }

        // Séquence de positions pour chaque transition entre les états du bras

        private Position[] WaitToRight = new Position[] {
                                    new Position(3679, 479, 2633, 1513, 0),
                                    new Position(2873, 479, 1874, 1513, 0),
                                    new Position(2199, 993, 1874, 538, 0),
                                    new Position(1676, 873, 2103, 538, 0)};

        /*
        private readonly Position[] RightToWait = new Position[] {
                                    new Position(1676, 873, 2103, 538, 0),
                                    new Position(2873, 479, 1874, 1513, 0),
                                    new Position(2199, 993, 1874, 538, 0),
                                    new Position()};
        */

        private Position[] WaitToLeft = new Position[] {
                                    new Position(3679, 479, 2633, 1513, 0),
                                    new Position(3740, 1652, 1019, 2623, 0),
                                    new Position(3740, 1652, 936, 2623, 0) };

        private Position[] WaitToPick = new Position[] { };

        private Position[] RightToWait => WaitToRight.Reverse().ToArray();
        private Position[] LeftToWait => WaitToLeft.Reverse().ToArray();

        private Position[] PickToWait => WaitToPick.Reverse().ToArray();

        public async Task goToPosition(BrasPosition position)
        {
            Position[] pos = new Position[0];
            BrasPosition newPosition = brasPosition;

            switch (position)
            {
                // STOCKAGE DROIT
                case BrasPosition.StockageRight:
                    switch (brasPosition)
                    {
                        case BrasPosition.Waiting:
                            pos = WaitToRight;
                            newPosition = BrasPosition.StockageRight;
                            break;
                        case BrasPosition.StockageLeft:
                            pos = LeftToWait;
                            newPosition = BrasPosition.Waiting;
                            break;
                        case BrasPosition.StockageRight:
                            pos = new Position[0];
                            newPosition = BrasPosition.StockageRight;
                            break;
                        case BrasPosition.Picking:
                            pos = PickToWait;
                            newPosition = BrasPosition.Waiting;
                            break;
                    }
                    break;

                // STOCKAGE GAUCHE
                case BrasPosition.StockageLeft:
                    switch (brasPosition)
                    {
                        case BrasPosition.Waiting:
                            pos = WaitToLeft;
                            newPosition = BrasPosition.StockageLeft;
                            break;
                        case BrasPosition.StockageLeft:
                            pos = new Position[0];
                            newPosition = BrasPosition.StockageLeft;
                            break;
                        case BrasPosition.StockageRight:
                            pos = RightToWait;
                            newPosition = BrasPosition.Waiting;
                            break;
                        case BrasPosition.Picking:
                            pos = PickToWait;
                            newPosition = BrasPosition.Waiting;
                            break;
                    }
                    break;

                // EN ATTENTE
                case BrasPosition.Waiting:
                    switch (brasPosition)
                    {
                        case BrasPosition.Waiting:
                            pos = new Position[0];
                            newPosition = BrasPosition.Waiting;
                            break;
                        case BrasPosition.StockageLeft:
                            pos = LeftToWait;
                            newPosition = BrasPosition.Waiting;
                            break;
                        case BrasPosition.StockageRight:
                            pos = RightToWait;
                            newPosition = BrasPosition.Waiting;
                            break;
                        case BrasPosition.Picking:
                            pos = PickToWait;
                            newPosition = BrasPosition.Waiting;
                            break;
                    }
                    break;

                // EN AVANT
                case BrasPosition.Picking:
                    switch (brasPosition)
                    {
                        case BrasPosition.Waiting:
                            pos = WaitToPick;
                            newPosition = BrasPosition.Picking;
                            break;
                        case BrasPosition.StockageLeft:
                            pos = LeftToWait;
                            newPosition = BrasPosition.Waiting;
                            break;
                        case BrasPosition.StockageRight:
                            pos = RightToWait;
                            newPosition = BrasPosition.Waiting;
                            break;
                        case BrasPosition.Picking:
                            pos = new Position[0];
                            newPosition = BrasPosition.Waiting;
                            break;
                    }
                    break;
            }

            for (int i = 0; i < pos.Length; i++)
            {
                // ✅ Réinitialiser les flags AVANT chaque déplacement
                epaulePosAtteinte = false;
                coudePosAtteinte = false;
                poignet1PosAtteinte = false;
                poignet2PosAtteinte = false;
                poignet3PosAtteinte = false;

                servoManager.goToPositionSM("Epaule", pos[i].Epaule, 200);
                servoManager.goToPositionSM("Coude", pos[i].Coude, 200);
                servoManager.goToPositionSM("Poignet1", pos[i].Poignet1, 200);
                servoManager.goToPositionSM("Poignet2", pos[i].Poignet2, 200);
                servoManager.goToPositionSM("Poignet3", pos[i].Poignet3, 200);

                // ✅ await Task.Delay au lieu de Thread.Sleep (non bloquant)
                while (!epaulePosAtteinte || !coudePosAtteinte || !poignet1PosAtteinte || !poignet2PosAtteinte || !poignet3PosAtteinte)
                {
                    await Task.Delay(30);
                }
            }

            brasPosition = newPosition;
            if (brasPosition != position)
                await goToPosition(position);
        }



        //public async Task goToStockage(StockageType stockage)
        //{


        //    Position[] pos;

        //    if(stockage == StockageType.Right)
        //    {
        //        pos = new Position[] {
        //            new Position(3679, 479, 2633, 1513, 0),
        //            new Position(1676, 1264, 2061, 1513, 0),
        //            new Position(1676, 1264, 2061, 538, 0),
        //            new Position(1676, 873, 2103, 538, 0),
        //        };
        //    }
        //    else
        //    {
        //        pos = new Position[] {
        //            new Position(0, 0, 0, 0, 0),
        //            new Position(0, 0, 0, 0, 0),
        //            new Position(0, 0, 0, 0, 0),
        //        };
        //    }

        //    for (int i = 0; i < pos.Length; i++)
        //    {
        //        // ✅ Réinitialiser les flags AVANT chaque déplacement
        //        epaulePosAtteinte = false;
        //        coudePosAtteinte = false;
        //        poignet1PosAtteinte = false;
        //        poignet2PosAtteinte = false;
        //        poignet3PosAtteinte = false;

        //        servoManager.goToPositionSM("Poignet1", pos[i].Poignet1, 20);
        //        servoManager.goToPositionSM("Coude", pos[i].Coude, 20);
        //        servoManager.goToPositionSM("Epaule", pos[i].Epaule, 20);
        //        servoManager.goToPositionSM("Poignet2", pos[i].Poignet2, 20);
        //        servoManager.goToPositionSM("Poignet3", pos[i].Poignet3, 20);

        //        // ✅ await Task.Delay au lieu de Thread.Sleep (non bloquant)
        //        while (!epaulePosAtteinte || !coudePosAtteinte || !poignet1PosAtteinte || !poignet2PosAtteinte || !poignet3PosAtteinte)
        //        {
        //            await Task.Delay(50);
        //        }
        //    }


        //}

    }
}
