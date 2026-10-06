using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using AtelierManager.ViewModels;

namespace AtelierManager.Views
{
    public partial class ReportsView : UserControl
    {
        public ReportsView()
        {
            InitializeComponent();
            DataContext = new ReportsViewModel();
        }

        private void ReportsView_Loaded(object sender, RoutedEventArgs e)
        {
            QueueChartRefresh();
        }

        private void ReportsView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            QueueChartRefresh();
        }

        private void BuildReport_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ReportsViewModel vm)
            {
                vm.BuildReport();
                QueueChartRefresh();
            }
        }

        private void ExportReport_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ReportsViewModel vm)
                vm.ExportToExcel();
        }

        private void QueueChartRefresh()
        {
            Dispatcher.BeginInvoke(new Action(RefreshChartsLayout), DispatcherPriority.Loaded);
        }

        private void RefreshChartsLayout()
        {
            FrameworkElement[] charts =
            {
                StatusChart,
                OrderTypeChart,
                EmployeeChart,
                MonthChart
            };

            foreach (var chart in charts)
            {
                chart.InvalidateMeasure();
                chart.InvalidateArrange();
                chart.UpdateLayout();
            }
        }
    }
}
