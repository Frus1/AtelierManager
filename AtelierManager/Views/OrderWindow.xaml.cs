using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using AtelierManager;
using AtelierManager.ViewModels;

namespace AtelierManager.Views
{
    public partial class OrderWindow : Window
    {
        public OrderWindow(OrderWindowViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not OrderWindowViewModel vm) return;

            vm.RefreshMaterialsAndTotals();

            if (!vm.Validate(out var error))
            {
                UiMessages.Validation(error);
                return;
            }

            DialogResult = true;
        }

        private void AddLine_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is OrderWindowViewModel vm) vm.AddLine();
        }

        private void DeleteLine_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is OrderWindowViewModel vm) vm.DeleteSelectedLine();
        }

        private void AddExtra_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is OrderWindowViewModel vm) vm.AddExtra();
        }

        private void DeleteExtra_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is OrderWindowViewModel vm) vm.DeleteSelectedExtra();
        }

        private void AddMaterial_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is OrderWindowViewModel vm) vm.AddMaterialLine();
        }

        private void DeleteMaterial_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is OrderWindowViewModel vm) vm.DeleteSelectedMaterialLine();
        }

        private void OrderMaterialsGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            RefreshMaterialTotalsAfterGridUpdate();
        }

        private void OrderMaterialsGrid_CurrentCellChanged(object sender, System.EventArgs e)
        {
            RefreshMaterialTotalsAfterGridUpdate();
        }

        private void RefreshMaterialTotalsAfterGridUpdate()
        {
            Dispatcher.BeginInvoke(new System.Action(() =>
            {
                if (DataContext is OrderWindowViewModel vm)
                    vm.RefreshMaterialsAndTotals();
            }), DispatcherPriority.Background);
        }
    }
}
