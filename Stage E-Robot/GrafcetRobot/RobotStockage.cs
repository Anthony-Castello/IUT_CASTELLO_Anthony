using System.Diagnostics;
using System.Linq.Expressions;
using ServoFeetech_NS;

namespace GrafcetRobot_NS
{
    public enum RobotState
    {
        Waiting,
        Stocking,
        Pushing,
    }

    public enum RobotTrigger
    {
        Stock,
        Push,
        Wait,
    }

    public enum StockageType
    {
        Left,
        Right,
    }

    public class RobotStockage
    {
        private enum RobotPosition
        {
            In,
            Out,
        }

        static int max = 4096;
        static int min = 0;
        static int max_acc = 0;
        static int acc_pousser = 3000;
        int stocked = 0;

        public Dictionary<string, int> InPositions = new Dictionary<string, int>
        {
            {"Plateforme1", min},
            {"Plateforme2", max},
            {"Plateforme3", max},
            {"Plateforme4", max},
            {"Pousser2", max},
        };

        public Dictionary<string, int> OutPositions = new Dictionary<string, int>
        {
            {"Plateforme1", max},
            {"Plateforme2", min},
            {"Plateforme3", min},
            {"Plateforme4", min},
            {"Pousser2", min},
        };


        public Dictionary<string, string> motors = new Dictionary<string, string>();
        private Feetech servoManager;
        private StockageType stockageType;
        public RobotState CurrentState = RobotState.Waiting;

        // Transition : (EtatActuel, Trigger) → EtatSuivant
        private readonly Dictionary<(RobotState, RobotTrigger), RobotState> transitions = new Dictionary<(RobotState, RobotTrigger), RobotState>
        {
            {(RobotState.Waiting, RobotTrigger.Stock), RobotState.Stocking},
            {(RobotState.Waiting, RobotTrigger.Push), RobotState.Pushing},

            {(RobotState.Stocking, RobotTrigger.Wait), RobotState.Waiting},
            {(RobotState.Pushing, RobotTrigger.Wait), RobotState.Waiting},

        };

        private readonly Dictionary<RobotState, Action> enterActions;
        private readonly Dictionary<RobotState, Action> exitActions;

        public RobotStockage(Feetech servoManager, StockageType stockageType, Dictionary<string, string> motors)
        {
            this.servoManager = servoManager;
            this.stockageType = stockageType;
            this.motors = motors;

            // Actions d'entrée : EtatSuivant → Action à exécuter
            enterActions = new Dictionary<RobotState, Action>
            {
                {RobotState.Stocking, onEnterStocking},
                {RobotState.Pushing, onEnterPushing},
            };

            // Actions de sortie : EtatPrécédent → Action à exécuter
            exitActions = new Dictionary<RobotState, Action>
            {
            };

            if (stockageType == StockageType.Left)
            {
                InPositions["Pousser1"] = max;
                OutPositions["Pousser1"] = min;
            }
            else
            {
                InPositions["Pousser1"] = min;
                OutPositions["Pousser1"] = max;
            }

        }

        // Moteur de la machine à états
        private void Fire(RobotTrigger trigger)
        {
            if (!transitions.TryGetValue((CurrentState, trigger), out var nextState))
            {
                Debug.WriteLine($"[WARNING] Transition invalide : {CurrentState} + {trigger}");
                return;
            }

            Debug.WriteLine($"[SM] {CurrentState} ──[{trigger}]──► {nextState}");
            CurrentState = nextState;

            // Éxécuter les actions d'entrée et de sortie
            if (exitActions.TryGetValue(CurrentState, out var exitAction))
                exitAction();
            if (enterActions.TryGetValue(nextState, out var enterAction))
                enterAction();
        }

        // Actions
        private void onEnterStocking()
        {
            if (stocked > 3)
            {
                Wait();
                return;
            }

            MoveServo("Plateforme1", RobotPosition.In, max_acc);
            MoveServo("Pousser1", RobotPosition.In, max_acc);
            Thread.Sleep(300);
            MoveServo("Pousser1", RobotPosition.Out, max_acc);

            switch (stocked)
            {
                case 0:
                    MoveServo("Plateforme2", RobotPosition.Out, max_acc);
                    MoveServo("Plateforme1", RobotPosition.Out, max_acc);
                    Thread.Sleep(500);
                    MoveServo("Plateforme2", RobotPosition.In, max_acc);
                    MoveServo("Plateforme1", RobotPosition.In, max_acc);
                    Thread.Sleep(500);
                    MoveServo("Plateforme3", RobotPosition.Out, max_acc);
                    Thread.Sleep(500);
                    MoveServo("Plateforme3", RobotPosition.In, max_acc);
                    break;
                case 1:
                    MoveServo("Plateforme2", RobotPosition.Out, max_acc);
                    MoveServo("Plateforme1", RobotPosition.Out, max_acc);
                    Thread.Sleep(500);
                    MoveServo("Plateforme2", RobotPosition.In, max_acc);
                    MoveServo("Plateforme1", RobotPosition.In, max_acc);
                    break;
                case 2:
                    MoveServo("Plateforme1", RobotPosition.Out, max_acc);
                    Thread.Sleep(500);
                    MoveServo("Plateforme1", RobotPosition.In, max_acc);
                    break;

            }

            stocked++;
            Wait();
        }
        private void onEnterPushing()
        {
            if (stocked <= 0)
            {
                Wait();
                return;
            }

            MoveServo("Pousser2", RobotPosition.In, acc_pousser);
            Thread.Sleep(500);
            MoveServo("Pousser2", RobotPosition.Out, max_acc);
            Thread.Sleep(200);

            for (int i = 3; i > 4 - stocked; i--)
            {
                MoveServo(("Plateforme" + i), RobotPosition.Out, max_acc);
                Thread.Sleep(500);
                MoveServo(("Plateforme" + i), RobotPosition.In, max_acc);
            }

            if (stocked > 0)
                stocked--;
            Wait();
        }


        // Appel des trigger
        public void Stock() => Fire(RobotTrigger.Stock);
        public void Push() => Fire(RobotTrigger.Push);
        public void Wait() => Fire(RobotTrigger.Wait);



        // Moteurs
        public void initServos()
        {
            MoveServo("Plateforme1", RobotPosition.In, max_acc);
            MoveServo("Plateforme2", RobotPosition.In, max_acc);
            MoveServo("Plateforme3", RobotPosition.In, max_acc);
            MoveServo("Pousser1", RobotPosition.Out, max_acc);
            MoveServo("Pousser1", RobotPosition.Out, max_acc);
        }

        public void goToStockage()
        {
            byte acc = 10;
            int wait = 200;

            if(stockageType == StockageType.Left)
            {
                int EpauleValue = 1740;
                int CoudeValue = 1000;
                int PoignetValue = 2330;
                int Poignet2Value = 540;

                servoManager.WriteServoData(this, new FeetechServoWriteArgs
                {
                    Name = "Poignet2",
                    Location = FeetechMemorySM.GoalAcceleration,
                    Payload = new byte[] { acc, (byte)(Poignet2Value & 0xFF), (byte)(Poignet2Value >> 8), }
                });
                Thread.Sleep(wait);
                servoManager.WriteServoData(this, new FeetechServoWriteArgs
                {
                    Name = "Epaule",
                    Location = FeetechMemorySM.GoalAcceleration,
                    Payload = new byte[] { acc, (byte)(EpauleValue & 0xFF), (byte)(EpauleValue >> 8), }
                });
                Thread.Sleep(wait);
                servoManager.WriteServoData(this, new FeetechServoWriteArgs
                {
                    Name = "Coude",
                    Location = FeetechMemorySM.GoalAcceleration,
                    Payload = new byte[] { acc, (byte)(CoudeValue & 0xFF), (byte)(CoudeValue >> 8), }
                });
                Thread.Sleep(wait);
                servoManager.WriteServoData(this, new FeetechServoWriteArgs
                {
                    Name = "Poignet1",
                    Location = FeetechMemorySM.GoalAcceleration,
                    Payload = new byte[] { acc, (byte)(PoignetValue & 0xFF), (byte)(PoignetValue >> 8), }
                });
            }
            else
            {
                int EpauleValue = 3550;
                int CoudeValue = 1900;
                int PoignetValue = 1000;
                int Poignet2Value = 2600;

                servoManager.WriteServoData(this, new FeetechServoWriteArgs
                {
                    Name = "Poignet2",
                    Location = FeetechMemorySM.GoalAcceleration,
                    Payload = new byte[] { acc, (byte)(Poignet2Value & 0xFF), (byte)(Poignet2Value >> 8), }
                });
                Thread.Sleep(wait);
                servoManager.WriteServoData(this, new FeetechServoWriteArgs
                {
                    Name = "Epaule",
                    Location = FeetechMemorySM.GoalAcceleration,
                    Payload = new byte[] { acc, (byte)(EpauleValue & 0xFF), (byte)(EpauleValue >> 8), }
                });
                Thread.Sleep(wait);
                servoManager.WriteServoData(this, new FeetechServoWriteArgs
                {
                    Name = "Coude",
                    Location = FeetechMemorySM.GoalAcceleration,
                    Payload = new byte[] { acc, (byte)(CoudeValue & 0xFF), (byte)(CoudeValue >> 8), }
                });
                Thread.Sleep(wait);
                servoManager.WriteServoData(this, new FeetechServoWriteArgs
                {
                    Name = "Poignet1",
                    Location = FeetechMemorySM.GoalAcceleration,
                    Payload = new byte[] { acc, (byte)(PoignetValue & 0xFF), (byte)(PoignetValue >> 8), }
                });
            }



        }

        private void MoveServo(string name, RobotPosition position, int acc)
        {
            if (!motors.TryGetValue(name, out string? realName))
                return;

            if (position == RobotPosition.In)
            {
                if (!InPositions.TryGetValue(name, out int value))
                    return;
                value = InPositions[name];
                servoManager.WriteServoData(this, new FeetechServoWriteArgs
                {
                    Name = realName,
                    Location = FeetechMemorySTS.GoalAcceleration,
                    Payload = new byte[] { (byte)acc, (byte)(value & 0xFF), (byte)(value >> 8), }
                });
            }
            if (position == RobotPosition.Out)
            {
                if (!OutPositions.TryGetValue(name, out int value))
                    return;
                value = OutPositions[name];
                servoManager.WriteServoData(this, new FeetechServoWriteArgs
                {
                    Name = realName,
                    Location = FeetechMemorySTS.GoalAcceleration,
                    Payload = new byte[] { (byte)acc, (byte)(value & 0xFF), (byte)(value >> 8), }
                });
            }


        }

    }
}
