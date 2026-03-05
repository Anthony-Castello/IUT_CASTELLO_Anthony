using System.Diagnostics;
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
    public class GrafcetRobot
    {
        int max = 4096;
        int min = 0;
        int max_acc = 0;
        int acc_pousser = 1000;
        int stocked = 0;

        private Feetech servoManager;
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

        public GrafcetRobot(Feetech servoManager)
        {
            this.servoManager = servoManager;

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
            if (stocked > 5)
            {
                Wait();
                return;
            }

            MoveServo("Plateforme1", max,max_acc);
            Thread.Sleep(500);
            MoveServo("Pousser1", min, max_acc);
            Thread.Sleep(500);
            MoveServo("Pousser1", max, max_acc);
            for (int i = 1; i <= 4 - stocked ; i++)
            {
                Thread.Sleep(500);
                MoveServo(("Plateforme" + i), min, max_acc);
                Thread.Sleep(500);
                MoveServo(("Plateforme" + i), max, max_acc);
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

            MoveServo("Pousser2", max, acc_pousser);
            Thread.Sleep(500);
            MoveServo("Pousser2", min, acc_pousser);

            for (int i = 4; i > 5 - stocked; i--)
            {
                Thread.Sleep(500);
                MoveServo(("Plateforme" + i), min, max_acc);
                Thread.Sleep(500);
                MoveServo(("Plateforme" + i), max, max_acc);
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
        private void MoveServo(string name, int position, int acc)
        {
            servoManager.WriteServoData(this, new FeetechServoWriteArgs
            {
                Name = name,
                Location = FeetechMemorySTS.GoalAcceleration,
                Payload = new byte[] {(byte)acc, (byte)(position & 0xFF), (byte)(position >> 8), }
            });
        }

    }
}
