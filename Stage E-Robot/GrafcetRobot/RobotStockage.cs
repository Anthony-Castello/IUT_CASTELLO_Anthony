using System.Diagnostics;
using System.Linq.Expressions;
using ServoFeetech_NS;

namespace GrafcetRobot_NS
{
    public enum RobotState
    {
        Waiting,
        Picking,
        GoingToStockage,
        Stocking,
        Pushing,
    }

    public enum RobotTrigger
    {
        Stock,
        Pick,
        GoToStockage,
        Push,
        Wait,
    }

    public enum StockageType
    {
        Left,
        Right,
    }


    public class RobotToolBox
    {
        Feetech servoManager;
        public RobotToolBox(Feetech servoManager)
        {
            this.servoManager = servoManager;
        }


        public void sendServoInfo(FeetechServoInfo info)
        {
            FeetechServo servo = servoManager.getServoById((byte)info.Id);
            if (servo == null)
                return;



            if(servo.Name == "Epaule" || servo.Name == "Coude" || servo.Name == "Poignet1")
            {
                if (info.PresentPosition == null || )
                    return;

            }

        }

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

        static int ascenseur_haut = 0;
        static int ascenseur_bas = 9900;

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
            {(RobotState.Waiting, RobotTrigger.Pick), RobotState.Picking},
            {(RobotState.Waiting, RobotTrigger.GoToStockage), RobotState.GoingToStockage},


            {(RobotState.Stocking, RobotTrigger.Wait), RobotState.Waiting},
            {(RobotState.Pushing, RobotTrigger.Wait), RobotState.Waiting},
            {(RobotState.Picking, RobotTrigger.Wait), RobotState.Waiting},
            {(RobotState.GoingToStockage, RobotTrigger.Wait), RobotState.Waiting},

        };

        private readonly Dictionary<RobotState, Action> enterActions;
        private readonly Dictionary<RobotState, Action> exitActions;

        public RobotStockage(Feetech servoManager, StockageType stockageType, Dictionary<string, string> motors, RobotToolBox rtb)
        {
            this.servoManager = servoManager;
            this.stockageType = stockageType;
            this.motors = motors;

            // Actions d'entrée : EtatSuivant → Action à exécuter
            enterActions = new Dictionary<RobotState, Action>
            {
                {RobotState.Stocking, onEnterStocking},
                {RobotState.Pushing, onEnterPushing},
                {RobotState.Picking, onEnterPicking},
                {RobotState.GoingToStockage, onEnterGoingToStockage},
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

        private void onEnterPicking()
        {
            byte acc = 20;
            int wait = 200;

            int EpauleValue = 3550;
            int CoudeValue = 1859;
            int PoignetValue = 2560;
            int Poignet2Value = 1565;

            MoveServo("Poignet2", Poignet2Value, acc);
            Thread.Sleep(wait);
            MoveServo("Epaule", EpauleValue, acc);
            Thread.Sleep(wait);
            MoveServo("Coude", CoudeValue, acc);
            Thread.Sleep(wait);
            MoveServo("Poignet1", PoignetValue, acc);
            Thread.Sleep(wait);
            MoveServo("Ascenseur", ascenseur_bas, 0);

            Wait();
        }
            
        private void onEnterGoingToStockage()
        {
            byte acc = 20;
            int wait = 1000;

            int EpauleValue;
            int CoudeValue;
            int middlePoignetValue = 1500;
            int PoignetValue;
            int Poignet2Value;

            if (stockageType == StockageType.Left)
            {
                EpauleValue = 1740;
                CoudeValue = 1000;
                PoignetValue = 2330;
                Poignet2Value = 540;

            }
            else
            {
                EpauleValue = 3637;
                CoudeValue = 2698;
                PoignetValue = 879;
                Poignet2Value = 2565;
            }

            MoveServo("Ascenseur", ascenseur_haut, 0);
            Thread.Sleep(3000);
            MoveServo("Poignet2", Poignet2Value, acc);
            Thread.Sleep(wait);
            MoveServo("Poignet1", middlePoignetValue, acc);
            Thread.Sleep(wait);
            MoveServo("Epaule", EpauleValue, acc);
            Thread.Sleep(wait);
            MoveServo("Coude", CoudeValue, acc);
            Thread.Sleep(wait);
            MoveServo("Poignet1", PoignetValue, acc);

            Wait();
        }

        // Appel des trigger
        public void Stock() => Fire(RobotTrigger.Stock);
        public void Push() => Fire(RobotTrigger.Push);
        public void Wait() => Fire(RobotTrigger.Wait);
        public void Pick() => Fire(RobotTrigger.Pick);
        public void goToStockage() => Fire(RobotTrigger.GoToStockage);

        // Moteurs

        public void initServos()
        {
            MoveServo("Plateforme1", RobotPosition.In, max_acc);
            MoveServo("Plateforme2", RobotPosition.In, max_acc);
            MoveServo("Plateforme3", RobotPosition.In, max_acc);
            MoveServo("Pousser1", RobotPosition.Out, max_acc);
            MoveServo("Pousser1", RobotPosition.Out, max_acc);
        }

        private void MoveServo(string name, int position, int acc)
        {
            servoManager.WriteServoData(this, new FeetechServoWriteArgs
            {
                Name = name,
                Location = FeetechMemorySTS.GoalAcceleration,
                Payload = new byte[] { (byte)acc, (byte)(position & 0xFF), (byte)(position >> 8), }
            });
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
