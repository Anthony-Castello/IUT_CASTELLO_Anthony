using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Robotinterface // À adapter au nom de votre projet
{
    public partial class RobotVisualizer : UserControl
    {
        private const double GridMin = -3.0;
        private const double GridMax = 3.0;
        private const double GridRange = GridMax - GridMin;

        // Stockage de l'état actuel et de l'historique
        private double currentX = 0;
        private double currentY = 0;
        private double currentAngleRad = 0;

        // Liste stockant les coordonnées mathématiques [x, y]
        private List<Point> pathHistory = new List<Point>();

        public RobotVisualizer()
        {
            InitializeComponent();

            // On initialise l'historique avec le point de départ au centre
            pathHistory.Add(new Point(0, 0));
        }

        /// <summary>
        /// Met à jour la position du robot et ajoute le point à la trace.
        /// </summary>
        public void UpdateRobot(double x, double y, double angleRadian)
        {
            // Détection de la commande de Reset
            if (x == 0 && y == 0 && angleRadian == 0)
            {
                // Mise à jour des coordonnées en mémoire
                currentX = 0;
                currentY = 0;
                currentAngleRad = 0;

                // On efface l'historique et on place le nouveau point de départ
                ClearTrail();

                // On replace visuellement le robot au centre
                DrawRobot();

                // On sort de la fonction immédiatement pour empêcher le tracé
                return;
            }

            // Comportement normal si ce n'est pas un reset
            currentX = x;
            currentY = y;
            currentAngleRad = angleRadian;

            // Ajouter la nouvelle position à l'historique
            pathHistory.Add(new Point(x, y));

            DrawRobot();
            UpdateTrailDrawing();
        }

        /// <summary>
        /// Efface l'historique de la trace.
        /// </summary>
        public void ClearTrail()
        {
            pathHistory.Clear();
            // On conserve toujours la position actuelle pour ne pas casser la ligne suivante
            pathHistory.Add(new Point(currentX, currentY));
            RobotTrail.Points.Clear();
        }

        /// <summary>
        /// S'occupe uniquement de placer visuellement la flèche
        /// </summary>
        private void DrawRobot()
        {
            if (GridCanvas.ActualWidth == 0 || GridCanvas.ActualHeight == 0) return;

            // Rotation
            RobotRotation.Angle = -(currentAngleRad * 180.0 / Math.PI);

            // Mise à l'échelle
            double pixelsPerUnitX = GridCanvas.ActualWidth / GridRange;
            double pixelsPerUnitY = GridCanvas.ActualHeight / GridRange;

            // Position sur l'écran
            double screenX = (GridCanvas.ActualWidth / 2.0) + (currentX * pixelsPerUnitX);
            double screenY = (GridCanvas.ActualHeight / 2.0) - (currentY * pixelsPerUnitY);

            Canvas.SetLeft(RobotArrow, screenX - (RobotArrow.Width / 2.0));
            Canvas.SetTop(RobotArrow, screenY - (RobotArrow.Height / 2.0));
        }

        /// <summary>
        /// S'occupe de redessiner la ligne de trace
        /// </summary>
        private void UpdateTrailDrawing()
        {
            if (GridCanvas.ActualWidth == 0 || GridCanvas.ActualHeight == 0) return;

            double pixelsPerUnitX = GridCanvas.ActualWidth / GridRange;
            double pixelsPerUnitY = GridCanvas.ActualHeight / GridRange;

            RobotTrail.Points.Clear();
            foreach (Point p in pathHistory)
            {
                double sx = (GridCanvas.ActualWidth / 2.0) + (p.X * pixelsPerUnitX);
                double sy = (GridCanvas.ActualHeight / 2.0) - (p.Y * pixelsPerUnitY);
                RobotTrail.Points.Add(new Point(sx, sy));
            }
        }

        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawGrid();

            // Le redimensionnement met juste à jour le visuel sans ajouter de points à l'historique
            DrawRobot();
            UpdateTrailDrawing();
        }

        private void DrawGrid()
        {
            GridCanvas.Children.Clear();
            double width = GridCanvas.ActualWidth;
            double height = GridCanvas.ActualHeight;

            if (width == 0 || height == 0) return;

            double pixelsPerUnitX = width / GridRange;
            double pixelsPerUnitY = height / GridRange;

            SolidColorBrush gridBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
            SolidColorBrush axisBrush = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255));
            SolidColorBrush textBrush = new SolidColorBrush(Color.FromArgb(150, 255, 255, 255));

            for (int i = (int)GridMin; i <= (int)GridMax; i++)
            {
                // Lignes verticales
                double screenX = (width / 2.0) + (i * pixelsPerUnitX);
                Line vLine = new Line { X1 = screenX, Y1 = 0, X2 = screenX, Y2 = height, Stroke = i == 0 ? axisBrush : gridBrush, StrokeThickness = i == 0 ? 2 : 1 };
                GridCanvas.Children.Add(vLine);

                // Lignes horizontales
                double screenY = (height / 2.0) - (i * pixelsPerUnitY);
                Line hLine = new Line { X1 = 0, Y1 = screenY, X2 = width, Y2 = screenY, Stroke = i == 0 ? axisBrush : gridBrush, StrokeThickness = i == 0 ? 2 : 1 };
                GridCanvas.Children.Add(hLine);

                // Ajout des valeurs numériques (on évite de les superposer sur le 0 central)
                if (i != 0)
                {
                    // Légende Axe X
                    TextBlock textX = new TextBlock { Text = i.ToString(), Foreground = textBrush, FontSize = 10 };
                    Canvas.SetLeft(textX, screenX + 3);
                    Canvas.SetTop(textX, (height / 2.0) + 3);
                    GridCanvas.Children.Add(textX);

                    // Légende Axe Y
                    TextBlock textY = new TextBlock { Text = i.ToString(), Foreground = textBrush, FontSize = 10 };
                    Canvas.SetLeft(textY, (width / 2.0) + 5);
                    Canvas.SetTop(textY, screenY - 15);
                    GridCanvas.Children.Add(textY);
                }
            }

            // Le repère "0" central
            TextBlock textZero = new TextBlock { Text = "0", Foreground = textBrush, FontSize = 10 };
            Canvas.SetLeft(textZero, (width / 2.0) + 3);
            Canvas.SetTop(textZero, (height / 2.0) + 3);
            GridCanvas.Children.Add(textZero);

            // Identifiants des axes (X à droite, Y en haut)
            TextBlock labelX = new TextBlock { Text = "X", Foreground = Brushes.White, FontWeight = FontWeights.Bold };
            Canvas.SetLeft(labelX, width - 15);
            Canvas.SetTop(labelX, (height / 2.0) - 20);
            GridCanvas.Children.Add(labelX);

            TextBlock labelY = new TextBlock { Text = "Y", Foreground = Brushes.White, FontWeight = FontWeights.Bold };
            Canvas.SetLeft(labelY, (width / 2.0) + 10);
            Canvas.SetTop(labelY, 5);
            GridCanvas.Children.Add(labelY);
        }
    }
}