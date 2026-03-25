using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServoFeetech_NS;

using System.Diagnostics;
using System.Runtime.CompilerServices;

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

        //public volatile bool epaulePosAtteinte = true;
        //public volatile bool coudePosAtteinte = true;
        //public volatile bool poignet1PosAtteinte = true;
        //public volatile bool poignet2PosAtteinte = true;
        //public volatile bool poignet3PosAtteinte = true;


        private int epaulePos;
        private int coudePos;
        private int poignet1Pos;
        private int poignet2Pos;
        private int poignet3Pos;

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

            //if (info.PresentPosition != null && info.GoalPosition != null)
            //{
            //    switch(servo.Name)
            //    {
            //        // Seuil de tolérance pour considérer que la position est atteinte à 100 unités près
            //        case "Epaule":
            //            epaulePosAtteinte = Math.Abs((int)info.PresentPosition - (int)info.GoalPosition) <= tol || Math.Abs((int)info.PresentPosition - (int)info.GoalPosition) >= 4095 - tol;
            //            break;
            //        case "Coude":
            //            coudePosAtteinte = Math.Abs((int)info.PresentPosition - (int)info.GoalPosition) <= tol || Math.Abs((int)info.PresentPosition - (int)info.GoalPosition) >= 4095 - tol;
            //            break;
            //        case "Poignet1":
            //            poignet1PosAtteinte = Math.Abs((int)info.PresentPosition - (int)info.GoalPosition) <= tol || Math.Abs((int)info.PresentPosition - (int)info.GoalPosition) >= 4095 - tol;
            //            break;
            //        case "Poignet2":
            //            poignet2PosAtteinte = Math.Abs((int)info.PresentPosition - (int)info.GoalPosition) <= tol || Math.Abs((int)info.PresentPosition - (int)info.GoalPosition) >= 4095 - tol;
            //            break;
            //        case "Poignet3":
            //            poignet3PosAtteinte = Math.Abs((int)info.PresentPosition - (int)info.GoalPosition) <= tol || Math.Abs((int)info.PresentPosition - (int)info.GoalPosition) >= 4095 - tol;
            //            break;
            //    }
            //}

            if (info.PresentPosition != null)
            {
                switch (servo.Name)
                {
                    // Seuil de tolérance pour considérer que la position est atteinte à 100 unités près
                    case "Epaule":
                        epaulePos = (int)info.PresentPosition;
                        break;
                    case "Coude":
                        coudePos = (int)info.PresentPosition; 
                        break;
                    case "Poignet1":
                        poignet1Pos = (int)info.PresentPosition; 
                        break;
                    case "Poignet2":
                        poignet2Pos = (int)info.PresentPosition; 
                        break;
                    case "Poignet3":
                        poignet3Pos = (int)info.PresentPosition; 
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
                                    new Position(1807, 881, 2103, 538, 0),
                                    new Position(1676, 873, 2103, 538, 0)};

        private Position[] WaitToLeft = new Position[] {
                                    new Position(3679, 479, 2633, 1513, 0),
                                    new Position(3740, 1652, 1019, 2623, 0),
                                    new Position(3740, 1652, 936, 2623, 0) };

        private Position[] WaitToPick = new Position[] {
                                    new Position(3679, 479, 2633, 1513, 0),
                                    new Position(3218, 1048, 2337, 1513, 0) };

        private Position[] RightToWait => WaitToRight.Reverse().ToArray();
        private Position[] LeftToWait => WaitToLeft.Reverse().ToArray();

        private Position[] PickToWait => WaitToPick.Reverse().ToArray();


        private bool _enMouvement = false;
        public void goToPosition(BrasPosition position, int speed)
        {
            if (_enMouvement) return;
            _enMouvement = true;

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
                //epaulePosAtteinte = false;
                //coudePosAtteinte = false;
                //poignet1PosAtteinte = false;
                //poignet2PosAtteinte = false;
                //poignet3PosAtteinte = false;

                servoManager.goToPositionSM("Epaule", pos[i].Epaule, 0, speed);
                servoManager.goToPositionSM("Coude", pos[i].Coude, 0, speed);
                servoManager.goToPositionSM("Poignet1", pos[i].Poignet1, 0, speed);
                servoManager.goToPositionSM("Poignet2", pos[i].Poignet2, 0, speed);
                servoManager.goToPositionSM("Poignet3", pos[i].Poignet3, 0, speed);

                bool end = false;

                var sw = Stopwatch.StartNew();
                while (!end)
                {
                    readPositions();
                    Thread.Sleep(10);
                    end = servoPosAtteinte(pos[i]);

                    // ERREUR, Le bras n'arrive pas a atteindre la position
                    if (sw.ElapsedMilliseconds > 5000)
                    {
                        break;
                        //goToPosition(BrasPosition.Waiting, acc);
                        //return;
                    }

                    if(brasPosition == BrasPosition.Waiting && newPosition == BrasPosition.StockageRight)
                    {
                        if(i == 1)
                        {
                            readPositions();
                            Thread.Sleep(10);
                            pos[i].Poignet1 = getPoignet1Parallele() + 1024; // +90°
                            servoManager.goToPositionSM("Poignet1", pos[i].Poignet1, 0, speed);
                        }
                    }

                }
                
            }

            _enMouvement = false;
            brasPosition = newPosition;
            if (brasPosition != position)   
                goToPosition(position, speed);
        }



        // Positions de référence (à calibrer selon votre bras, position "zéro degré")
        private const int EPAULE_ZERO = 2695;
        private const int COUDE_ZERO = 1285;
        private const int POIGNET1_ZERO = 1553;
        private double posToAngle(int pos, int zero)
        {
            return (pos - zero) * (360.0 / 4096.0);
        }

        private int angleToPos(double angle, int zero)
        {
            return (int)(zero + angle * (4096.0 / 360.0));
        }
        public int getPoignet1Parallele()
        {
            double angleEpaule = posToAngle(epaulePos, EPAULE_ZERO);
            double angleCoude = posToAngle(coudePos, COUDE_ZERO);

            double anglePoignet1 = angleEpaule + angleCoude;

            return angleToPos(anglePoignet1, POIGNET1_ZERO);
        }


        private int circularDiff(int a, int b)
        {
            int diff = Math.Abs(a - b);
            return Math.Min(diff, 4096 - diff);
        }

        int tol = 10;
        private bool servoPosAtteinte(Position pos)
        {
            return circularDiff(epaulePos, pos.Epaule) <= tol &&
                   circularDiff(coudePos, pos.Coude) <= tol &&
                   circularDiff(poignet1Pos, pos.Poignet1) <= tol &&
                   circularDiff(poignet2Pos, pos.Poignet2) <= tol &&
                   circularDiff(poignet3Pos, pos.Poignet3) <= tol;
        }

        private void readPositions()
        {
            servoManager.SyncReadServoData(this, new FeetechServoSyncReadArgs
            {
                Location = FeetechMemorySTS.PresentPosition,
                NumberOfBytes = 2,
                Names = new string[] { "Epaule", "Coude", "Poignet1", "Poignet2", "Poignet3" }
            });
        }

    }
}
