namespace InverseKinematicsServo_NS
{

    public class InverseKinematicsServo
    {

        MotorsInfo m1;
        MotorsInfo m2;
        MotorsInfo m3;

        public InverseKinematicsServo(MotorsInfo m1, MotorsInfo m2, MotorsInfo m3)
        {
            this.m1 = m1;
            this.m2 = m2;
            this.m3 = m3;
        }

        public struct MotorsInfo
        {

            public string name;
            public int minPos;
            public int maxPos;
            public int midPos;
            public double maxAngle;

            public MotorsInfo(string name, int minPos, int maxPos, int midPos)
            {
                this.name = name;
                this.minPos = minPos;
                this.maxPos = maxPos;
                this.midPos = midPos;
                this.maxAngle = (maxPos - minPos) / 11.375;
            }

        }

        public struct IKResult
        {
            public bool Success;
            public double Alpha1;
            public double Alpha2;
            public double Alpha3;
            public double Error;
        }

        public IKResult SolveInverseKinematics(
            double d1, double d2, double d3,
            double targetX, double targetY,
            double alpha1MaxDeg, double alpha2MaxDeg, double alpha3MaxDeg)
        {
            // Les limites sont symétriques : ±(max/2)
            // Par exemple, si alpha1Max = 128°, la plage est -64° à +64°
            double alpha1Limit = (alpha1MaxDeg / 2.0) * Math.PI / 180.0;
            double alpha2Limit = (alpha2MaxDeg / 2.0) * Math.PI / 180.0;
            double alpha3Limit = (alpha3MaxDeg / 2.0) * Math.PI / 180.0;

            IKResult bestResult = new IKResult { Success = false, Error = double.MaxValue };

            // Balayer toutes les orientations absolues possibles du segment 3
            int steps = 150; // Nombre de pas pour le balayage

            for (int i = 0; i <= steps; i++)
            {
                // theta3_abs est l'orientation ABSOLUE du segment 3 par rapport à l'axe X
                // On balaie de -π à +π pour couvrir toutes les orientations possibles
                double theta3_abs = -Math.PI + (2 * Math.PI * i / steps);

                // Pour chaque configuration de coude (coude haut/coude bas)
                for (int elbowConfig = 0; elbowConfig < 2; elbowConfig++)
                {
                    // Calculer la position du poignet (joint entre segment 2 et 3)
                    // Le poignet est à distance d3 de la cible dans la direction -theta3_abs
                    double wristX = targetX - d3 * Math.Cos(theta3_abs);
                    double wristY = targetY - d3 * Math.Sin(theta3_abs);

                    // Résoudre le problème 2R pour atteindre le poignet avec d1 et d2
                    double distSq = wristX * wristX + wristY * wristY;
                    double dist = Math.Sqrt(distSq);

                    // Vérifier si le poignet est atteignable par les 2 premiers segments
                    if (dist > d1 + d2 || dist < Math.Abs(d1 - d2))
                        continue;

                    // Calculer alpha2 avec la loi des cosinus
                    double cosAlpha2 = (distSq - d1 * d1 - d2 * d2) / (2 * d1 * d2);

                    if (Math.Abs(cosAlpha2) > 1.0)
                        continue;

                    // Deux solutions pour alpha2 : coude en haut ou en bas
                    double alpha2 = elbowConfig == 0 ?
                        Math.Acos(cosAlpha2) : -Math.Acos(cosAlpha2);

                    // Calculer alpha1
                    double k1 = d1 + d2 * Math.Cos(alpha2);
                    double k2 = d2 * Math.Sin(alpha2);
                    double alpha1 = Math.Atan2(wristY, wristX) - Math.Atan2(k2, k1);

                    // Calculer alpha3 (angle relatif du moteur 3)
                    // theta3_abs = alpha1 + alpha2 + alpha3
                    // donc alpha3 = theta3_abs - (alpha1 + alpha2)
                    double alpha3 = theta3_abs - (alpha1 + alpha2);

                    // Normaliser alpha3 dans [-π, π]
                    while (alpha3 > Math.PI) alpha3 -= 2 * Math.PI;
                    while (alpha3 < -Math.PI) alpha3 += 2 * Math.PI;

                    // Vérifier les contraintes angulaires (plages symétriques)
                    if (Math.Abs(alpha1) > alpha1Limit ||
                        Math.Abs(alpha2) > alpha2Limit ||
                        Math.Abs(alpha3) > alpha3Limit)
                        continue;

                    // Calculer la position réelle atteinte (vérification)
                    double actualX = d1 * Math.Cos(alpha1) +
                                   d2 * Math.Cos(alpha1 + alpha2) +
                                   d3 * Math.Cos(alpha1 + alpha2 + alpha3);
                    double actualY = d1 * Math.Sin(alpha1) +
                                   d2 * Math.Sin(alpha1 + alpha2) +
                                   d3 * Math.Sin(alpha1 + alpha2 + alpha3);

                    double error = Math.Sqrt(
                        (actualX - targetX) * (actualX - targetX) +
                        (actualY - targetY) * (actualY - targetY)
                    );

                    // Garder la meilleure solution
                    if (error < bestResult.Error)
                    {
                        bestResult.Success = true;
                        bestResult.Alpha1 = alpha1 * 180.0 / Math.PI;
                        bestResult.Alpha2 = alpha2 * 180.0 / Math.PI;
                        bestResult.Alpha3 = alpha3 * 180.0 / Math.PI;
                        bestResult.Error = error;
                    }
                }
            }

            return bestResult;
        }

        public Dictionary<string, int> convertAnglesToPos(double a1, double a2, double a3)
        {
            // 11,375 = 1deg
            // IL FAUDRA FAIRE UN CALCUL DE DEPASSEMENT
            int pos1 = (int)Math.Round(m1.midPos + (a1 * 11.375));
            int pos2 = (int)Math.Round(m2.midPos + (a2 * 11.375));
            int pos3 = (int)Math.Round(m3.midPos + (a3 * 11.375));
            Dictionary<string, int> result = new Dictionary<string, int>();
            result.Add(m1.name, pos1);
            result.Add(m2.name, pos2);
            result.Add(m3.name, pos3);

            return result;

        }

    }

}
