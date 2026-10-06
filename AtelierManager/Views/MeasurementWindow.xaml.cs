using System;
using System.Linq;
using System.Windows;
using AtelierManager;
using AtelierManager.Models;

namespace AtelierManager.Views
{
    public partial class MeasurementWindow : Window
    {
        public MeasurementWindow(MeasurementSet measurementSet)
        {
            InitializeComponent();
            DataContext = measurementSet;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MeasurementSet set) return;

            if (set.TakenAt == default)
                set.TakenAt = DateTime.Today;

            double?[] values =
            {
                set.Height, set.Chest, set.Waist, set.Hips,
                set.Shoulder, set.Sleeve,
                set.Neck, set.ShoulderWidth, set.BackLength,
                set.Wrist, set.Bicep,
                set.Inseam, set.Outseam, set.Thigh, set.Knee, set.Ankle, set.Rise,
                set.ProductLength
            };

            if (values.Any(v => v < 0))
            {
                UiMessages.Validation("Мерки не могут быть отрицательными.");
                return;
            }

            if (values.All(v => v == null))
            {
                UiMessages.Validation("Укажите хотя бы одну мерку.");
                return;
            }

            set.Comment = string.IsNullOrWhiteSpace(set.Comment) ? null : set.Comment.Trim();

            DialogResult = true;
        }
    }
}