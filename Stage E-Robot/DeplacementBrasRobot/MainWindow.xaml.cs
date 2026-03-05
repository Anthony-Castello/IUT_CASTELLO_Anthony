using System;
using System.IO.Ports;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using InverseKinematicsServo_NS;
using ServoFeetech_NS;
using static System.Formats.Asn1.AsnWriter;
using static InverseKinematicsServo_NS.InverseKinematicsServo;

namespace RobotArmIK
{
    public partial class MainWindow : Window
    {

        Feetech servoManager = new Feetech();

        InverseKinematicsServo InverseKinematicsServo;
        MotorsInfo m1 = new MotorsInfo("Epaule", 1740, 2700, 3550);
        MotorsInfo m2 = new MotorsInfo("Coude", 1000, 2325, 3800);
        MotorsInfo m3 = new MotorsInfo("Poignee1", 460, 1550, 2560);

        SerialPort SerialPort1;
        public MainWindow()
        {
            InitializeComponent();
            DrawGrid();

            SerialPort1 = new SerialPort("COM14", 115200, Parity.None, 8, StopBits.One);
            //SerialPort1.DataReceived += SerialPort1_DataReceived;
            SerialPort1.Open();

            servoManager.servos.Add(new FeetechServo("Epaule", 1, FeetechServoModels.SM));
            servoManager.servos.Add(new FeetechServo("Coude", 2, FeetechServoModels.SM));
            servoManager.servos.Add(new FeetechServo("Poignee1", 3, FeetechServoModels.SM));
            servoManager.servos.Add(new FeetechServo("Poignee2", 4, FeetechServoModels.SM));
            servoManager.servos.Add(new FeetechServo("Poignee3", 5, FeetechServoModels.SM));

            servoManager.OnSendMessageEvent += sendTrame;

            // Initialiser les informations des moteurs pour la cinématique inverse
            // Les valeurs minPos, maxPos et midPos sont basées sur les spécifications des servos Feetech SM
            // minPos = 840, maxPos = 3200, midPos = 2070 pour Poignee1
            // minPos = 1070, maxPos = 3200, midPos = 2195 pour Epaule
            // minPos = 1870, maxPos = 4090, midPos = 3050 pour Coude
            // Ces valeurs peuvent être ajustées en fonction de la configuration réelle de votre robot et des limites physiques des joints
            // Le maxAngle est calculé à partir de la plage de mouvement du servo (maxPos - minPos) divisée par 11.375, qui correspond à la conversion de la position en degrés pour les servos Feetech SM
            // Par exemple, pour Epaule : (3200 - 1070) / 11.375 ≈ 192.5°, ce qui correspond à la plage de mouvement totale du servo. Cependant, pour la cinématique inverse, nous allons limiter cette plage à ±(maxAngle/2) pour éviter les positions extrêmes qui pourraient être difficiles à atteindre ou dangereuses pour le robot.
            InverseKinematicsServo = new InverseKinematicsServo(m1, m2, m3);

        }

        private void BtnCalculate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Utiliser InvariantCulture pour accepter le point comme séparateur décimal
                var culture = System.Globalization.CultureInfo.InvariantCulture;

                // Validation et récupération des paramètres
                if (string.IsNullOrWhiteSpace(txtD1.Text) ||
                    string.IsNullOrWhiteSpace(txtD2.Text) ||
                    string.IsNullOrWhiteSpace(txtD3.Text) ||
                    string.IsNullOrWhiteSpace(txtTargetX.Text) ||
                    string.IsNullOrWhiteSpace(txtTargetY.Text) ||
                    string.IsNullOrWhiteSpace(txtAcc.Text) ||
                    string.IsNullOrWhiteSpace(txtVit.Text))
                {
                    MessageBox.Show("Veuillez remplir tous les champs.", "Champs manquants",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Récupérer les paramètres - remplacer la virgule par un point si nécessaire
                double d1 = double.Parse(txtD1.Text.Replace(',', '.'), culture);
                double d2 = double.Parse(txtD2.Text.Replace(',', '.'), culture);
                double d3 = double.Parse(txtD3.Text.Replace(',', '.'), culture);
                double targetX = double.Parse(txtTargetX.Text.Replace(',', '.'), culture);
                double targetY = double.Parse(txtTargetY.Text.Replace(',', '.'), culture);
                int acc = int.Parse(txtAcc.Text.Replace(',', '.'), culture);
                int vit = int.Parse(txtVit.Text.Replace(',', '.'), culture);


                // Calculer la cinématique inverse
                var result = InverseKinematicsServo.SolveInverseKinematics(
                    d1, d2, d3,
                    targetX, targetY,
                    m1, m2, m3
                );

                if (result.Success)
                {
                    // Afficher les résultats
                    txtResultAlpha1.Text = $"α1 = {result.Alpha1:F2}°";
                    txtResultAlpha2.Text = $"α2 = {result.Alpha2:F2}°";
                    txtResultAlpha3.Text = $"α3 = {result.Alpha3:F2}°";
                    txtResultError.Text = $"Erreur = {result.Error:F4} mm";
                    txtResultStatus.Text = "✓ Solution trouvée";
                    txtResultStatus.Foreground = new SolidColorBrush(Colors.Green);

                    // Dessiner le bras
                    DrawRobotArm(d1, d2, d3, result.Alpha1, result.Alpha2, result.Alpha3, targetX, targetY);


                    // Envoyer les angles aux moteurs
                    Dictionary<string, int> resultPos = InverseKinematicsServo.convertAnglesToPos(result.Alpha1, result.Alpha2, result.Alpha3);

                    foreach(var servo in resultPos.Keys)
                    {
                        if(!resultPos.TryGetValue(servo, out int pos)) continue;

                        servoManager.RegWriteServoData(this, new FeetechServoRegWriteArgs
                        {
                            Name = servo,
                            Location = FeetechMemorySM.GoalAcceleration,
                            Payload = new byte[] { (byte)acc, (byte)(pos & 0xFF), (byte)(pos >> 8), 0, 0 , (byte)(vit & 0xFF), (byte)(vit >> 8) }
                        });

                        Thread.Sleep(Feetech.ServoDelay);
                    }

                    servoManager.ActionServoData(this, new FeetechServoActionArgs());

                }
                else
                {
                    txtResultAlpha1.Text = "α1 = -";
                    txtResultAlpha2.Text = "α2 = -";
                    txtResultAlpha3.Text = "α3 = -";
                    txtResultError.Text = "Erreur = -";
                    txtResultStatus.Text = "✗ Aucune solution trouvée. La cible est peut-être hors d'atteinte.";
                    txtResultStatus.Foreground = new SolidColorBrush(Colors.Red);

                    DrawGrid();
                    DrawTarget(targetX, targetY);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void sendTrame(object? sender, ByteArrayArgs e)
        {
            SerialPort1.Write(e.array, 0, e.array.Length);
        }

        private void DrawGrid()
        {
            canvas.Children.Clear();

            double width = canvas.ActualWidth;
            double height = canvas.ActualHeight;

            double centerX = width / 2;
            double centerY = height / 2;
            double scale = 1.5; // Même échelle que pour le dessin du bras

            if (width < 10 || height < 10) return;

            // Grille
            for (int i = 0; i <= 10; i++)
            {
                // Lignes verticales
                Line vLine = new Line
                {
                    X1 = i * width / 10,
                    Y1 = 0,
                    X2 = i * width / 10,
                    Y2 = height,
                    Stroke = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                    StrokeThickness = 1
                };
                canvas.Children.Add(vLine);

                // Lignes horizontales
                Line hLine = new Line
                {
                    X1 = 0,
                    Y1 = i * height / 10,
                    X2 = width,
                    Y2 = i * height / 10,
                    Stroke = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                    StrokeThickness = 1
                };
                canvas.Children.Add(hLine);
            }

            // Axes principaux
            Line xAxis = new Line
            {
                X1 = 0,
                Y1 = height / 2,
                X2 = width,
                Y2 = height / 2,
                Stroke = new SolidColorBrush(Colors.Gray),
                StrokeThickness = 2
            };
            canvas.Children.Add(xAxis);

            Line yAxis = new Line
            {
                X1 = width / 2,
                Y1 = 0,
                X2 = width / 2,
                Y2 = height,
                Stroke = new SolidColorBrush(Colors.Gray),
                StrokeThickness = 2
            };
            canvas.Children.Add(yAxis);

            // === GRADUATIONS SUR L'AXE X ===
            for (int x = -200; x <= 200; x += 50) // Graduation tous les 50mm
            {
                if (x == 0) continue; // Éviter de dessiner sur l'origine

                double screenX = centerX + x * scale;

                // Trait de graduation
                Line tickX = new Line
                {
                    X1 = screenX,
                    Y1 = centerY - 5,
                    X2 = screenX,
                    Y2 = centerY + 5,
                    Stroke = new SolidColorBrush(Colors.Black),
                    StrokeThickness = 2
                };
                canvas.Children.Add(tickX);

                // Label du texte
                TextBlock labelX = new TextBlock
                {
                    Text = x.ToString(),
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Colors.Black),
                    FontWeight = FontWeights.Bold
                };
                Canvas.SetLeft(labelX, screenX - 15);
                Canvas.SetTop(labelX, centerY + 8);
                canvas.Children.Add(labelX);
            }
            for (int y = -200; y <= 200; y += 50) // Graduation tous les 50mm
            {
                if (y == 0) continue; // Éviter de dessiner sur l'origine

                double screenY = centerY - y * scale; // Inverser Y

                // Trait de graduation
                Line tickY = new Line
                {
                    X1 = centerX - 5,
                    Y1 = screenY,
                    X2 = centerX + 5,
                    Y2 = screenY,
                    Stroke = new SolidColorBrush(Colors.Black),
                    StrokeThickness = 2
                };
                canvas.Children.Add(tickY);

                // Label du texte
                TextBlock labelY = new TextBlock
                {
                    Text = y.ToString(),
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Colors.Black),
                    FontWeight = FontWeights.Bold
                };
                Canvas.SetLeft(labelY, centerX + 8);
                Canvas.SetTop(labelY, screenY - 8);
                canvas.Children.Add(labelY);
            }

            // === LABELS DES AXES ===
            // Label "X (mm)"
            TextBlock labelAxisX = new TextBlock
            {
                Text = "X (mm)",
                FontSize = 13,
                Foreground = new SolidColorBrush(Colors.DarkGray),
                FontWeight = FontWeights.Bold
            };
            Canvas.SetLeft(labelAxisX, width - 60);
            Canvas.SetTop(labelAxisX, centerY + 10);
            canvas.Children.Add(labelAxisX);

            // Label "Y (mm)"
            TextBlock labelAxisY = new TextBlock
            {
                Text = "Y (mm)",
                FontSize = 13,
                Foreground = new SolidColorBrush(Colors.DarkGray),
                FontWeight = FontWeights.Bold
            };
            Canvas.SetLeft(labelAxisY, centerX + 10);
            Canvas.SetTop(labelAxisY, 10);
            canvas.Children.Add(labelAxisY);


            // Origine
            Ellipse origin = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = new SolidColorBrush(Colors.Black)
            };
            Canvas.SetLeft(origin, width / 2 - 5);
            Canvas.SetTop(origin, height / 2 - 5);
            canvas.Children.Add(origin);

            TextBlock labelOrigin = new TextBlock
            {
                Text = "0",
                FontSize = 11,
                Foreground = new SolidColorBrush(Colors.Black),
                FontWeight = FontWeights.Bold
            };
            Canvas.SetLeft(labelOrigin, centerX - 20);
            Canvas.SetTop(labelOrigin, centerY + 8);
            canvas.Children.Add(labelOrigin);
        }

        private void DrawTarget(double targetX, double targetY)
        {
            double centerX = canvas.ActualWidth / 2;
            double centerY = canvas.ActualHeight / 2;
            double scale = 1.5;

            double screenX = centerX + targetX * scale;
            double screenY = centerY - targetY * scale; // Inverser Y

            // Croix pour la cible
            Line line1 = new Line
            {
                X1 = screenX - 10,
                Y1 = screenY - 10,
                X2 = screenX + 10,
                Y2 = screenY + 10,
                Stroke = new SolidColorBrush(Colors.Red),
                StrokeThickness = 3
            };
            canvas.Children.Add(line1);

            Line line2 = new Line
            {
                X1 = screenX - 10,
                Y1 = screenY + 10,
                X2 = screenX + 10,
                Y2 = screenY - 10,
                Stroke = new SolidColorBrush(Colors.Red),
                StrokeThickness = 3
            };
            canvas.Children.Add(line2);

            // Cercle autour
            Ellipse circle = new Ellipse
            {
                Width = 25,
                Height = 25,
                Stroke = new SolidColorBrush(Colors.Red),
                StrokeThickness = 2
            };
            Canvas.SetLeft(circle, screenX - 12.5);
            Canvas.SetTop(circle, screenY - 12.5);
            canvas.Children.Add(circle);
        }

        private void DrawRobotArm(
            double d1, double d2, double d3,
            double alpha1Deg, double alpha2Deg, double alpha3Deg,
            double targetX, double targetY)
        {
            DrawGrid();

            double centerX = canvas.ActualWidth / 2;
            double centerY = canvas.ActualHeight / 2;
            double scale = 1.5;

            // Convertir en radians
            double alpha1 = alpha1Deg * Math.PI / 180.0;
            double alpha2 = alpha2Deg * Math.PI / 180.0;
            double alpha3 = alpha3Deg * Math.PI / 180.0;

            // Calculer les positions des joints
            double x1 = d1 * Math.Cos(alpha1);
            double y1 = d1 * Math.Sin(alpha1);

            double x2 = x1 + d2 * Math.Cos(alpha1 + alpha2);
            double y2 = y1 + d2 * Math.Sin(alpha1 + alpha2);

            double x3 = x2 + d3 * Math.Cos(alpha1 + alpha2 + alpha3);
            double y3 = y2 + d3 * Math.Sin(alpha1 + alpha2 + alpha3);

            // Convertir en coordonnées écran
            double sx1 = centerX + x1 * scale;
            double sy1 = centerY - y1 * scale;
            double sx2 = centerX + x2 * scale;
            double sy2 = centerY - y2 * scale;
            double sx3 = centerX + x3 * scale;
            double sy3 = centerY - y3 * scale;

            // Dessiner les segments
            DrawSegment(centerX, centerY, sx1, sy1, Colors.Blue, 6);
            DrawSegment(sx1, sy1, sx2, sy2, Colors.Green, 6);
            DrawSegment(sx2, sy2, sx3, sy3, Colors.Orange, 6);

            // Dessiner les joints
            DrawJoint(centerX, centerY, 12, Colors.DarkBlue);
            DrawJoint(sx1, sy1, 10, Colors.DarkGreen);
            DrawJoint(sx2, sy2, 10, Colors.DarkOrange);
            DrawJoint(sx3, sy3, 8, Colors.Red);

            // Dessiner la cible
            DrawTarget(targetX, targetY);

            // Légende
            DrawLegend(alpha1Deg, alpha2Deg, alpha3Deg);
        }

        private void DrawSegment(double x1, double y1, double x2, double y2, Color color, double thickness)
        {
            Line line = new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = thickness,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeStartLineCap = PenLineCap.Round
            };
            canvas.Children.Add(line);
        }

        private void DrawJoint(double x, double y, double size, Color color)
        {
            Ellipse joint = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = new SolidColorBrush(color),
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 2
            };
            Canvas.SetLeft(joint, x - size / 2);
            Canvas.SetTop(joint, y - size / 2);
            canvas.Children.Add(joint);
        }

        private void DrawLegend(double alpha1, double alpha2, double alpha3)
        {
            StackPanel legend = new StackPanel
            {
                Background = new SolidColorBrush(Color.FromArgb(230, 255, 255, 255)),
                Margin = new Thickness(10)
            };

            legend.Children.Add(CreateLegendItem("Segment 1", Colors.Blue));
            legend.Children.Add(CreateLegendItem("Segment 2", Colors.Green));
            legend.Children.Add(CreateLegendItem("Segment 3", Colors.Orange));

            Canvas.SetLeft(legend, 10);
            Canvas.SetTop(legend, 10);
            canvas.Children.Add(legend);
        }

        private StackPanel CreateLegendItem(string text, Color color)
        {
            StackPanel item = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(5)
            };

            Rectangle rect = new Rectangle
            {
                Width = 30,
                Height = 6,
                Fill = new SolidColorBrush(color),
                Margin = new Thickness(0, 0, 10, 0)
            };

            TextBlock textBlock = new TextBlock
            {
                Text = text,
                FontSize = 12
            };

            item.Children.Add(rect);
            item.Children.Add(textBlock);

            return item;
        }

      private void BtnTest_Click(object sender, RoutedEventArgs e)
      {
            try
            {
                var culture = System.Globalization.CultureInfo.InvariantCulture;

                // Récupérer les paramètres
                double d1 = double.Parse(txtD1.Text.Replace(',', '.'), culture);
                double d2 = double.Parse(txtD2.Text.Replace(',', '.'), culture);
                double d3 = double.Parse(txtD3.Text.Replace(',', '.'), culture);

                // Effacer et redessiner la grille
                DrawGrid();

                double centerX = canvas.ActualWidth / 2;
                double centerY = canvas.ActualHeight / 2;
                double scale = 1.5;

                int reachableCount = 0;
                int totalTests = 0;

                int l = (int) Math.Ceiling(d1 + d2 + d3);

                // Tester tous les points de -100 à 100 (pas de 5mm pour plus de rapidité)
                for (int x = -l; x <= l; x += 3)
                {
                    for (int y = -l; y <= l; y += 3)
                    {
                        totalTests++;
                        
                        // Calculer la cinématique inverse pour ce point
                        var result = InverseKinematicsServo.SolveInverseKinematics(
                            d1, d2, d3,
                            x, y,
                            m1, m2, m3
                        );

                        if (result.Success && result.Error < 1.0) // Tolérance de 1mm
                        {
                            reachableCount++;

                            // Convertir en coordonnées écran
                            double screenX = centerX + x * scale;
                            double screenY = centerY - y * scale;

                            // Dessiner un petit point vert
                            Ellipse point = new Ellipse
                            {
                                Width = 3,
                                Height = 3,
                                Fill = new SolidColorBrush(Color.FromArgb(180, 76, 175, 80)) // Vert semi-transparent
                            };
                            Canvas.SetLeft(point, screenX - 1.5);
                            Canvas.SetTop(point, screenY - 1.5);
                            canvas.Children.Add(point);
                        }
                    }
                }

                // Afficher les statistiques
                txtResultStatus.Text = $"✓ Test terminé : {reachableCount}/{totalTests} points atteignables";
                txtResultStatus.Foreground = new SolidColorBrush(Colors.Blue);
                txtResultAlpha1.Text = $"Points testés : {totalTests}";
                txtResultAlpha2.Text = $"Points atteignables : {reachableCount}";
                txtResultAlpha3.Text = $"Taux : {(reachableCount * 100.0 / totalTests):F1}%";
                txtResultError.Text = "Zone verte = atteignable";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}