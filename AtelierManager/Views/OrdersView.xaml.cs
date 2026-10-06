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
using AtelierManager.ViewModels;

namespace AtelierManager.Views
{
    /// <summary>
    /// Логика взаимодействия для OrdersView.xaml
    /// </summary>
    public partial class OrdersView : UserControl
    {
        public OrdersView()
        {
            InitializeComponent();
        }

        private void AddOrder_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is OrdersViewModel vm)
                vm.AddOrder();
        }

        private void EditOrder_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is OrdersViewModel vm)
                vm.EditSelectedOrder();
        }

        private void DeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is OrdersViewModel vm)
                vm.DeleteSelectedOrder();
        }

        private void RefreshOrder_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is OrdersViewModel vm)
                vm.RefreshOrders();
        }
    }
}
