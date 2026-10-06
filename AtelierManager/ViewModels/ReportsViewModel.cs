using System;
using System.Collections.ObjectModel;
using System.Linq;
using AtelierManager.Data;
using AtelierManager.Models;
using Microsoft.EntityFrameworkCore;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WPF;
using SkiaSharp;
using LiveChartsCore.Measure;
using ClosedXML.Excel;
using Microsoft.Win32;
using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Drawing.Spreadsheet;
using A = DocumentFormat.OpenXml.Drawing;
using C = DocumentFormat.OpenXml.Drawing.Charts;
using Xdr = DocumentFormat.OpenXml.Drawing.Spreadsheet;
using S = DocumentFormat.OpenXml.Spreadsheet;

namespace AtelierManager.ViewModels
{
    public class ReportsViewModel : BaseViewModel
    {
        private readonly AtelierContext _db = new();

        private ISeries[] _statusSeries = Array.Empty<ISeries>();
        public ISeries[] StatusSeries
        {
            get => _statusSeries;
            set { _statusSeries = value; OnPropertyChanged(); }
        }

        private ISeries[] _orderTypeSeries = Array.Empty<ISeries>();
        public ISeries[] OrderTypeSeries
        {
            get => _orderTypeSeries;
            set { _orderTypeSeries = value; OnPropertyChanged(); }
        }

        private ISeries[] _employeeSeries = Array.Empty<ISeries>();
        public ISeries[] EmployeeSeries
        {
            get => _employeeSeries;
            set { _employeeSeries = value; OnPropertyChanged(); }
        }

        private ISeries[] _monthSeries = Array.Empty<ISeries>();
        public ISeries[] MonthSeries
        {
            get => _monthSeries;
            set { _monthSeries = value; OnPropertyChanged(); }
        }

        private Axis[] _monthXAxes = Array.Empty<Axis>();
        public Axis[] MonthXAxes
        {
            get => _monthXAxes;
            set { _monthXAxes = value; OnPropertyChanged(); }
        }

        private Axis[] _monthYAxes = Array.Empty<Axis>();
        public Axis[] MonthYAxes
        {
            get => _monthYAxes;
            set { _monthYAxes = value; OnPropertyChanged(); }
        }
        public ObservableCollection<Models.Order> ReportOrders { get; } = new();
        public ObservableCollection<ServiceReportRow> TopServices { get; } = new();
        public ObservableCollection<MaterialReportRow> MaterialUsage { get; } = new();
        public ObservableCollection<ReportStatusFilterItem> StatusFilters { get; } = new();

        private ReportStatusFilterItem? _selectedStatusFilter;
        public ReportStatusFilterItem? SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                _selectedStatusFilter = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<ReportClientFilterItem> ClientFilters { get; } = new();

        private ReportClientFilterItem? _selectedClientFilter;
        public ReportClientFilterItem? SelectedClientFilter
        {
            get => _selectedClientFilter;
            set
            {
                _selectedClientFilter = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<ReportEmployeeFilterItem> EmployeeFilters { get; } = new();

        private ReportEmployeeFilterItem? _selectedEmployeeFilter;
        public ReportEmployeeFilterItem? SelectedEmployeeFilter
        {
            get => _selectedEmployeeFilter;
            set
            {
                _selectedEmployeeFilter = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<OrderTypeReportRow> OrderTypeStats { get; } = new();
        public ObservableCollection<StatusReportRow> StatusStats { get; } = new();
        public ObservableCollection<EmployeeReportRow> EmployeeStats { get; } = new();
        public ObservableCollection<MonthReportRow> MonthStats { get; } = new();

        private DateTime _dateFrom = DateTime.Today.AddMonths(-1);
        public DateTime DateFrom
        {
            get => _dateFrom;
            set { _dateFrom = value; OnPropertyChanged(); }
        }

        private DateTime _dateTo = DateTime.Today;
        public DateTime DateTo
        {
            get => _dateTo;
            set { _dateTo = value; OnPropertyChanged(); }
        }

        private int _ordersCount;
        public int OrdersCount
        {
            get => _ordersCount;
            set { _ordersCount = value; OnPropertyChanged(); }
        }

        private double _totalAmount;
        public double TotalAmount
        {
            get => _totalAmount;
            set { _totalAmount = value; OnPropertyChanged(); }
        }

        private int _completedCount;
        public int CompletedCount
        {
            get => _completedCount;
            set { _completedCount = value; OnPropertyChanged(); }
        }

        public ReportsViewModel()
        {
            LoadStatuses();
            LoadClients();
            LoadEmployees();
            BuildReport();
        }

        public async void BuildReport()
        {
            ReportOrders.Clear();
            TopServices.Clear();
            MaterialUsage.Clear();
            OrderTypeStats.Clear();
            StatusStats.Clear();
            EmployeeStats.Clear();
            MonthStats.Clear();

            var from = DateFrom.Date;
            var to = DateTo.Date.AddDays(1).AddTicks(-1);

            var query = _db.Orders
               .AsNoTracking()
               .Include(x => x.Client)
               .Include(x => x.Status)
               .Include(x => x.Employee)
               .Where(x => x.IntakeDate >= from && x.IntakeDate <= to);

            if (SelectedStatusFilter?.Id != null)
            {
                int statusId = SelectedStatusFilter.Id.Value;
                query = query.Where(x => x.StatusId == statusId);
            }

            if (SelectedClientFilter?.Id != null)
            {
                int clientId = SelectedClientFilter.Id.Value;
                query = query.Where(x => x.ClientId == clientId);
            }

            if (SelectedEmployeeFilter?.Id != null)
            {
                int employeeId = SelectedEmployeeFilter.Id.Value;
                query = employeeId == 0
                    ? query.Where(x => x.EmployeeId == null)
                    : query.Where(x => x.EmployeeId == employeeId);
            }

            var orders = await query
                .OrderByDescending(x => x.IntakeDate)
                .ToListAsync();

            var orderIds = orders.Select(x => x.Id).ToList();

            var orderServiceRowsForTotals = await _db.OrderServices
                .AsNoTracking()
                .Where(x => orderIds.Contains(x.OrderId))
                .Select(x => new { x.Id, x.OrderId, x.BasePrice })
                .ToListAsync();

            var orderServiceIdsForTotals = orderServiceRowsForTotals.Select(x => x.Id).ToList();

            var orderExtraRowsForTotals = await _db.OrderServiceExtras
                .AsNoTracking()
                .Where(x => orderServiceIdsForTotals.Contains(x.OrderServiceId))
                .Select(x => new { x.OrderServiceId, x.Price, x.Qty })
                .ToListAsync();

            var extrasByOrderService = orderExtraRowsForTotals
                .GroupBy(x => x.OrderServiceId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.Price * (x.Qty <= 0 ? 1 : x.Qty)));

            var serviceDict = orderServiceRowsForTotals
                .GroupBy(x => x.OrderId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.BasePrice + (extrasByOrderService.TryGetValue(x.Id, out var extras) ? extras : 0)));

            var materialRowsForTotals = await _db.OrderMaterials
                .AsNoTracking()
                .Where(x => orderIds.Contains(x.OrderId))
                .Select(x => new { x.OrderId, x.QtyUsed, x.UnitPrice })
                .ToListAsync();

            var materialDict = materialRowsForTotals
                .GroupBy(x => x.OrderId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.QtyUsed * x.UnitPrice));

            foreach (var order in orders)
            {
                var services = serviceDict.TryGetValue(order.Id, out var st) ? st : 0;
                var materials = materialDict.TryGetValue(order.Id, out var mt) ? mt : 0;
                var actualTotal = services + materials;

                order.TotalCost = IsCanceledOrder(order) ? 0 : actualTotal;
                ReportOrders.Add(order);
            }

            OrdersCount = orders.Count;
            TotalAmount = orders.Sum(x => x.TotalCost);

            var incomeOrderIds = orders
                .Where(x => !IsCanceledOrder(x))
                .Select(x => x.Id)
                .ToList();

            CompletedCount = orders.Count(x =>
                x.Status != null &&
                (x.Status.Name == "Готов" || x.Status.Name == "Выдан"));

            var orderLines = await _db.OrderServices
                .AsNoTracking()
                .Where(x => orderIds.Contains(x.OrderId))
                .Include(x => x.Service)
                .ToListAsync();

            var orderTypeRows = orders
                .Select(order =>
                {
                    var types = orderLines
                        .Where(l => l.OrderId == order.Id)
                        .Select(l => l.Service?.ServiceType?.Trim() ?? "")
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .Distinct()
                        .ToList();

                    string orderType;

                    if (types.Count == 0)
                        orderType = "Не определён";
                    else if (types.Count == 1)
                        orderType = types[0];
                    else
                        orderType = "Смешанный";

                    return new
                    {
                        OrderType = orderType,
                        Amount = order.TotalCost
                    };
                })
                .GroupBy(x => x.OrderType)
                .Select(g => new OrderTypeReportRow
                {
                    OrderType = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(x => x.Amount)
                })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.OrderType)
                .ToList();

            foreach (var row in orderTypeRows)
                OrderTypeStats.Add(row);

            var statusRows = orders
                .GroupBy(x => x.Status?.Name ?? "—")
                .Select(g => new StatusReportRow
                {
                    StatusName = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(x => x.TotalCost)
                })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.StatusName)
                .ToList();

            foreach (var row in statusRows)
                StatusStats.Add(row);

            var employeeRows = orders
                .GroupBy(x => x.Employee?.FullName ?? "Не указан")
                .Select(g => new EmployeeReportRow
                {
                    EmployeeName = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(x => x.TotalCost)
                })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.EmployeeName)
                .ToList();

            foreach (var row in employeeRows)
                EmployeeStats.Add(row);

            var monthRows = orders
                .GroupBy(x => new { x.IntakeDate.Year, x.IntakeDate.Month })
                .Select(g => new MonthReportRow
                {
                    MonthName = $"{g.Key.Month:00}.{g.Key.Year}",
                    Count = g.Count(),
                    TotalAmount = g.Sum(x => x.TotalCost)
                })
                .OrderBy(x => x.MonthName)
                .ToList();

            foreach (var row in monthRows)
                MonthStats.Add(row);

            MonthSeries = new ISeries[]
            {
                new ColumnSeries<int>
                {
                    Name = "Заказов",
                    Values = MonthStats.Select(x => x.Count).ToArray(),
                    DataLabelsSize = 10,
                    DataLabelsFormatter = point => $"{point.Model}",
                    MaxBarWidth = 42
                }
            };

            MonthXAxes = new Axis[]
            {
                new Axis
                {
                    Labels = MonthStats.Select(x => ShortChartLabel(x.MonthName, 12)).ToArray(),
                    LabelsRotation = MonthStats.Count > 4 ? 35 : 0,
                    TextSize = 11
                }
            };

            MonthYAxes = new Axis[]
            {
                 new Axis
                 {
                     Name = "Количество",
                     MinLimit = 0,
                     TextSize = 11
                 }
            };


            var serviceRows = await _db.OrderServices
                .AsNoTracking()
                .Where(x => incomeOrderIds.Contains(x.OrderId))
                .Include(x => x.Service)
                .ToListAsync();

            var serviceReportRows = serviceRows
                .GroupBy(x => new
                {
                    x.ServiceId,
                    ServiceName = x.Service != null ? x.Service.Name : "—"
                })
                .Select(g => new ServiceReportRow
                {
                    ServiceName = g.Key.ServiceName,
                    Count = g.Count(),
                    TotalAmount = g.Sum(x => x.BasePrice + (extrasByOrderService.TryGetValue(x.Id, out var extras) ? extras : 0))
                })
                .OrderByDescending(x => x.Count)
                .ThenByDescending(x => x.TotalAmount)
                .ToList();

            foreach (var row in serviceReportRows)
                TopServices.Add(row);

            var materialRows = await _db.OrderMaterials
                .AsNoTracking()
                .Where(x => orderIds.Contains(x.OrderId))
                .Include(x => x.Material)
                .GroupBy(x => new
                {
                    x.MaterialId,
                    MaterialName = x.Material != null ? x.Material.Name : "—",
                    Unit = x.Material != null ? x.Material.Unit : ""
                })
                .Select(g => new MaterialReportRow
                {
                    MaterialName = g.Key.MaterialName,
                    Unit = g.Key.Unit,
                    QuantityUsed = g.Sum(x => x.QtyUsed),
                    TotalAmount = g.Sum(x => x.QtyUsed * x.UnitPrice)
                })
                .OrderByDescending(x => x.QuantityUsed)
                .ToListAsync();

            foreach (var row in materialRows)
                MaterialUsage.Add(row);

            StatusSeries = StatusStats
                .Select(x => new PieSeries<double>
                {
                    Values = new[] { (double)x.Count },
                    Name = ShortChartLabel(x.StatusName),
                    DataLabelsSize = 10,
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                    DataLabelsFormatter = point => $"{point.Model:0}"
                })
                .Cast<ISeries>()
                .ToArray();

            OrderTypeSeries = OrderTypeStats
                .Select(x => new PieSeries<double>
                {
                    Values = new[] { (double)x.Count },
                    Name = ShortChartLabel(x.OrderType),
                    DataLabelsSize = 10,
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                    DataLabelsFormatter = point => $"{point.Model:0}"
                })
                .Cast<ISeries>()
                .ToArray();

            EmployeeSeries = EmployeeStats
                .Select(x => new PieSeries<double>
                {
                    Values = new[] { (double)x.Count },
                    Name = ShortChartLabel(x.EmployeeName),
                    DataLabelsSize = 10,
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                    DataLabelsFormatter = point => $"{point.Model:0}"
                })
                .Cast<ISeries>()
                .ToArray();
        }

        private static bool IsCanceledOrder(Models.Order order)
        {
            var statusName = order.Status?.Name?.Trim();
            return string.Equals(statusName, "Отменён", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(statusName, "Отменен", StringComparison.OrdinalIgnoreCase);
        }

        private static string ShortChartLabel(string? value, int maxLength = 24)
        {
            var text = string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
            return text.Length <= maxLength ? text : text.Substring(0, maxLength - 1) + "…";
        }

        private async void LoadStatuses()
        {
            StatusFilters.Clear();

            StatusFilters.Add(new ReportStatusFilterItem
            {
                Id = null,
                Name = "Все статусы"
            });

            var statuses = await _db.Statuses
                .AsNoTracking()
                .OrderBy(x => x.SortOrder)
                .ToListAsync();

            foreach (var status in statuses)
            {
                StatusFilters.Add(new ReportStatusFilterItem
                {
                    Id = status.Id,
                    Name = status.Name
                });
            }

            SelectedStatusFilter = StatusFilters.FirstOrDefault();
        }

        private async void LoadClients()
        {
            ClientFilters.Clear();

            ClientFilters.Add(new ReportClientFilterItem
            {
                Id = null,
                FullName = "Все клиенты"
            });

            var clients = await _db.Clients
                .AsNoTracking()
                .OrderBy(x => x.FullName)
                .ToListAsync();

            foreach (var client in clients)
            {
                ClientFilters.Add(new ReportClientFilterItem
                {
                    Id = client.Id,
                    FullName = client.FullName
                });
            }

            SelectedClientFilter = ClientFilters.FirstOrDefault();
        }

        private async void LoadEmployees()
        {
            EmployeeFilters.Clear();

            EmployeeFilters.Add(new ReportEmployeeFilterItem
            {
                Id = null,
                FullName = "Все работники"
            });

            EmployeeFilters.Add(new ReportEmployeeFilterItem
            {
                Id = 0,
                FullName = "Не указан"
            });

            var employees = await _db.Employees
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.FullName)
                .ToListAsync();

            foreach (var employee in employees)
            {
                EmployeeFilters.Add(new ReportEmployeeFilterItem
                {
                    Id = employee.Id,
                    FullName = employee.FullName
                });
            }

            SelectedEmployeeFilter = EmployeeFilters.FirstOrDefault();
        }

        public void ExportToExcel()
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Title = "Сохранить отчёт",
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    FileName = $"Отчет_ателье_{DateTime.Now:yyyy-MM-dd_HH-mm}.xlsx"
                };

                if (dialog.ShowDialog() != true)
                    return;

                using var workbook = new XLWorkbook();

                var wsSummary = workbook.Worksheets.Add("Сводка");

                wsSummary.Cell(1, 1).Value = "Показатель";
                wsSummary.Cell(1, 2).Value = "Значение";

                wsSummary.Cell(2, 1).Value = "Период с";
                wsSummary.Cell(2, 2).Value = DateFrom;

                wsSummary.Cell(3, 1).Value = "Период по";
                wsSummary.Cell(3, 2).Value = DateTo;

                wsSummary.Cell(4, 1).Value = "Статус";
                wsSummary.Cell(4, 2).Value = SelectedStatusFilter?.Name ?? "Все статусы";

                wsSummary.Cell(5, 1).Value = "Клиент";
                wsSummary.Cell(5, 2).Value = SelectedClientFilter?.FullName ?? "Все клиенты";

                wsSummary.Cell(6, 1).Value = "Работник";
                wsSummary.Cell(6, 2).Value = SelectedEmployeeFilter?.FullName ?? "Все работники";

                wsSummary.Cell(7, 1).Value = "Количество заказов";
                wsSummary.Cell(7, 2).Value = OrdersCount;

                wsSummary.Cell(8, 1).Value = "Выручка без отменённых заказов";
                wsSummary.Cell(8, 2).Value = TotalAmount;

                wsSummary.Cell(9, 1).Value = "Готово / Выдано";
                wsSummary.Cell(9, 2).Value = CompletedCount;

                var wsOrders = workbook.Worksheets.Add("Заказы");

                wsOrders.Cell(1, 1).Value = "Клиент";
                wsOrders.Cell(1, 2).Value = "Примечание";
                wsOrders.Cell(1, 3).Value = "Принят";
                wsOrders.Cell(1, 4).Value = "Срок";
                wsOrders.Cell(1, 5).Value = "Статус";
                wsOrders.Cell(1, 6).Value = "Работник";
                wsOrders.Cell(1, 7).Value = "Выручка";

                int row = 2;
                foreach (var order in ReportOrders)
                {
                    wsOrders.Cell(row, 1).Value = order.Client?.FullName ?? "";
                    wsOrders.Cell(row, 2).Value = order.Notes ?? "";
                    wsOrders.Cell(row, 3).Value = order.IntakeDate;
                    wsOrders.Cell(row, 4).Value = order.DueDate;
                    wsOrders.Cell(row, 5).Value = order.Status?.Name ?? "";
                    wsOrders.Cell(row, 6).Value = order.Employee?.FullName ?? "Не указан";
                    wsOrders.Cell(row, 7).Value = order.TotalCost;
                    row++;
                }

                var wsServices = workbook.Worksheets.Add("Популярные услуги");

                wsServices.Cell(1, 1).Value = "Услуга";
                wsServices.Cell(1, 2).Value = "Количество";
                wsServices.Cell(1, 3).Value = "Выручка";

                row = 2;
                foreach (var item in TopServices)
                {
                    wsServices.Cell(row, 1).Value = item.ServiceName;
                    wsServices.Cell(row, 2).Value = item.Count;
                    wsServices.Cell(row, 3).Value = item.TotalAmount;
                    row++;
                }

                var wsMaterials = workbook.Worksheets.Add("Расход материалов");

                wsMaterials.Cell(1, 1).Value = "Материал";
                wsMaterials.Cell(1, 2).Value = "Количество";
                wsMaterials.Cell(1, 3).Value = "Ед.";
                wsMaterials.Cell(1, 4).Value = "Стоимость";

                row = 2;
                foreach (var item in MaterialUsage)
                {
                    wsMaterials.Cell(row, 1).Value = item.MaterialName;
                    wsMaterials.Cell(row, 2).Value = item.QuantityUsed;
                    wsMaterials.Cell(row, 3).Value = item.Unit;
                    wsMaterials.Cell(row, 4).Value = item.TotalAmount;
                    row++;
                }

                var wsOrderTypes = workbook.Worksheets.Add("Типы заказов");

                wsOrderTypes.Cell(1, 1).Value = "Тип заказа";
                wsOrderTypes.Cell(1, 2).Value = "Количество";
                wsOrderTypes.Cell(1, 3).Value = "Выручка";

                row = 2;
                foreach (var item in OrderTypeStats)
                {
                    wsOrderTypes.Cell(row, 1).Value = item.OrderType;
                    wsOrderTypes.Cell(row, 2).Value = item.Count;
                    wsOrderTypes.Cell(row, 3).Value = item.TotalAmount;
                    row++;
                }

                var wsStatuses = workbook.Worksheets.Add("Статусы");

                wsStatuses.Cell(1, 1).Value = "Статус";
                wsStatuses.Cell(1, 2).Value = "Количество";
                wsStatuses.Cell(1, 3).Value = "Выручка";

                row = 2;
                foreach (var item in StatusStats)
                {
                    wsStatuses.Cell(row, 1).Value = item.StatusName;
                    wsStatuses.Cell(row, 2).Value = item.Count;
                    wsStatuses.Cell(row, 3).Value = item.TotalAmount;
                    row++;
                }

                var wsEmployees = workbook.Worksheets.Add("Работники");

                wsEmployees.Cell(1, 1).Value = "Работник";
                wsEmployees.Cell(1, 2).Value = "Количество";
                wsEmployees.Cell(1, 3).Value = "Выручка";

                row = 2;
                foreach (var item in EmployeeStats)
                {
                    wsEmployees.Cell(row, 1).Value = item.EmployeeName;
                    wsEmployees.Cell(row, 2).Value = item.Count;
                    wsEmployees.Cell(row, 3).Value = item.TotalAmount;
                    row++;
                }

                var wsMonths = workbook.Worksheets.Add("По месяцам");

                wsMonths.Cell(1, 1).Value = "Месяц";
                wsMonths.Cell(1, 2).Value = "Количество";
                wsMonths.Cell(1, 3).Value = "Выручка";

                row = 2;
                foreach (var item in MonthStats)
                {
                    wsMonths.Cell(row, 1).Value = item.MonthName;
                    wsMonths.Cell(row, 2).Value = item.Count;
                    wsMonths.Cell(row, 3).Value = item.TotalAmount;
                    row++;
                }

                foreach (var ws in workbook.Worksheets)
                {
                    var used = ws.RangeUsed();
                    if (used != null)
                    {
                        used.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        used.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                        ws.Row(1).Style.Font.Bold = true;
                        ws.Row(1).Style.Fill.BackgroundColor = XLColor.LightGray;

                        wsSummary.Columns().Width = 22;
                        wsOrders.Columns().Width = 18;
                        wsServices.Columns().Width = 18;
                        wsMaterials.Columns().Width = 18;
                        wsOrderTypes.Columns().Width = 18;
                        wsStatuses.Columns().Width = 18;
                        wsEmployees.Columns().Width = 18;
                        wsMonths.Columns().Width = 18;

                        wsOrders.Column(2).Width = 35;
                        wsOrders.Column(1).Width = 28;
                        wsServices.Column(1).Width = 30;
                        wsMaterials.Column(1).Width = 30;
                        wsEmployees.Column(1).Width = 30;
                    }
                }

                workbook.SaveAs(dialog.FileName);

                if (StatusStats.Count > 0)
                {
                    AddPieChartOnNewWorksheet(
                        dialog.FileName,
                        chartSheetName: "Диагр_Статусы",
                        sourceSheetName: "Статусы",
                        chartTitle: "Распределение по статусам",
                        categoryColumn: "A",
                        valueColumn: "B",
                        startDataRow: 2,
                        endDataRow: StatusStats.Count + 1);
                }

                if (OrderTypeStats.Count > 0)
                {
                    AddPieChartOnNewWorksheet(
                        dialog.FileName,
                        chartSheetName: "Диагр_Типы",
                        sourceSheetName: "Типы заказов",
                        chartTitle: "Распределение по типам заказов",
                        categoryColumn: "A",
                        valueColumn: "B",
                        startDataRow: 2,
                        endDataRow: OrderTypeStats.Count + 1);
                }

                if (EmployeeStats.Count > 0)
                {
                    AddPieChartOnNewWorksheet(
                        dialog.FileName,
                        chartSheetName: "Диагр_Работники",
                        sourceSheetName: "Работники",
                        chartTitle: "Распределение по работникам",
                        categoryColumn: "A",
                        valueColumn: "B",
                        startDataRow: 2,
                        endDataRow: EmployeeStats.Count + 1);
                }

                if (MonthStats.Count > 0)
                {
                    AddBarChartOnNewWorksheet(
                        dialog.FileName,
                        chartSheetName: "Диагр_Месяцы",
                        sourceSheetName: "По месяцам",
                        chartTitle: "Количество заказов по месяцам",
                        categoryColumn: "A",
                        valueColumn: "B",
                        startDataRow: 2,
                        endDataRow: MonthStats.Count + 1);
                }

                UiMessages.Info("Отчёт успешно экспортирован в Excel.");
            }
            catch (Exception ex)
            {
                UiMessages.Error($"Ошибка при экспорте отчёта: {ex.Message}");
            }
        }

        private void AddPieChartOnNewWorksheet(
            string filePath,
            string chartSheetName,
            string sourceSheetName,
            string chartTitle,
            string categoryColumn,
            string valueColumn,
            int startDataRow,
            int endDataRow)
        {
            using var document = SpreadsheetDocument.Open(filePath, true);
            var workbookPart = document.WorkbookPart!;
            var workbook = workbookPart.Workbook;

            var existing = workbook.Descendants<S.Sheet>().FirstOrDefault(s => s.Name == chartSheetName);
            if (existing != null)
                return;

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new S.Worksheet(new S.SheetData());

            var drawingsPart = worksheetPart.AddNewPart<DrawingsPart>();
            drawingsPart.WorksheetDrawing = new Xdr.WorksheetDrawing();

            var drawingRelId = worksheetPart.GetIdOfPart(drawingsPart);
            worksheetPart.Worksheet.Append(new S.Drawing { Id = drawingRelId });

            var chartPart = drawingsPart.AddNewPart<ChartPart>();
            GeneratePieChartContentForSourceSheet(
                chartPart,
                sourceSheetName,
                chartTitle,
                categoryColumn,
                valueColumn,
                startDataRow,
                endDataRow);

            uint drawingObjectId = 1;

            var twoCellAnchor = new Xdr.TwoCellAnchor(
                new Xdr.FromMarker(
                    new Xdr.ColumnId("1"),
                    new Xdr.ColumnOffset("0"),
                    new Xdr.RowId("1"),
                    new Xdr.RowOffset("0")
                ),
                new Xdr.ToMarker(
                    new Xdr.ColumnId("10"),
                    new Xdr.ColumnOffset("0"),
                    new Xdr.RowId("20"),
                    new Xdr.RowOffset("0")
                ),
                new Xdr.GraphicFrame(
                    new Xdr.NonVisualGraphicFrameProperties(
                        new Xdr.NonVisualDrawingProperties
                        {
                            Id = drawingObjectId,
                            Name = chartTitle
                        },
                        new Xdr.NonVisualGraphicFrameDrawingProperties()
                    ),
                    new Xdr.Transform(
                        new A.Offset { X = 0L, Y = 0L },
                        new A.Extents { Cx = 0L, Cy = 0L }
                    ),
                    new A.Graphic(
                        new A.GraphicData(
                            new C.ChartReference { Id = drawingsPart.GetIdOfPart(chartPart) }
                        )
                        { Uri = "http://schemas.openxmlformats.org/drawingml/2006/chart" }
                    )
                ),
                new Xdr.ClientData()
            );

            drawingsPart.WorksheetDrawing.Append(twoCellAnchor);
            drawingsPart.WorksheetDrawing.Save();
            worksheetPart.Worksheet.Save();

            var sheets = workbook.GetFirstChild<S.Sheets>();
            if (sheets == null)
            {
                sheets = new S.Sheets();
                workbook.Append(sheets);
            }

            uint newSheetId = 1;
            var existingIds = sheets.Elements<S.Sheet>().Select(s => s.SheetId?.Value ?? 0U);
            if (existingIds.Any())
                newSheetId = existingIds.Max() + 1;

            var sheet = new S.Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = newSheetId,
                Name = chartSheetName
            };

            sheets.Append(sheet);
            workbook.Save();
        }

        private void GeneratePieChartContentForSourceSheet(
            ChartPart chartPart,
            string sourceSheetName,
            string chartTitle,
            string categoryColumn,
            string valueColumn,
            int startDataRow,
            int endDataRow)
        {
            var escapedSheetName = sourceSheetName.Replace("'", "''");
            var categoryRef = $"'{escapedSheetName}'!${categoryColumn}${startDataRow}:${categoryColumn}${endDataRow}";
            var valuesRef = $"'{escapedSheetName}'!${valueColumn}${startDataRow}:${valueColumn}${endDataRow}";

            var chartSpace = new C.ChartSpace();
            chartSpace.Append(new C.EditingLanguage { Val = "ru-RU" });

            var chart = new C.Chart();

            chart.Append(
                new C.Title(
                    new C.ChartText(
                        new C.RichText(
                            new A.BodyProperties(),
                            new A.ListStyle(),
                            new A.Paragraph(
                                new A.Run(
                                    new A.RunProperties { Language = "ru-RU" },
                                    new A.Text(chartTitle)
                                )
                            )
                        )
                    )
                )
            );

            chart.Append(new C.AutoTitleDeleted { Val = false });

            var plotArea = new C.PlotArea();
            plotArea.Append(new C.Layout());

            var pieChart = new C.PieChart(
                new C.VaryColors { Val = true }
            );

            var series = new C.PieChartSeries(
                new C.Index { Val = 0U },
                new C.Order { Val = 0U }
            );

            series.Append(
                new C.SeriesText(
                    new C.NumericValue { Text = chartTitle }
                )
            );

            series.Append(
                new C.CategoryAxisData(
                    new C.StringReference(
                        new C.Formula { Text = categoryRef }
                    )
                )
            );

            series.Append(
                new C.Values(
                    new C.NumberReference(
                        new C.Formula { Text = valuesRef }
                    )
                )
            );

            pieChart.Append(series);

            pieChart.Append(
                new C.DataLabels(
                    new C.ShowLegendKey { Val = false },
                    new C.ShowValue { Val = true },
                    new C.ShowCategoryName { Val = true },
                    new C.ShowSeriesName { Val = false },
                    new C.ShowPercent { Val = false },
                    new C.ShowBubbleSize { Val = false }
                )
            );

            plotArea.Append(pieChart);
            chart.Append(plotArea);

            chart.Append(
                new C.Legend(
                    new C.LegendPosition { Val = C.LegendPositionValues.Right },
                    new C.Layout()
                )
            );

            chart.Append(new C.PlotVisibleOnly { Val = true });
            chart.Append(new C.DisplayBlanksAs { Val = C.DisplayBlanksAsValues.Gap });
            chart.Append(new C.ShowDataLabelsOverMaximum { Val = false });

            chartSpace.Append(chart);
            chartPart.ChartSpace = chartSpace;
            chartPart.ChartSpace.Save();
        }

        private (int Col, int Row) ParseCellReference(string cellRef)
        {
            int i = 0;
            while (i < cellRef.Length && char.IsLetter(cellRef[i]))
                i++;

            var colLetters = cellRef.Substring(0, i).ToUpperInvariant();
            var rowNumber = int.Parse(cellRef.Substring(i));

            int col = 0;
            foreach (char c in colLetters)
            {
                col *= 26;
                col += (c - 'A' + 1);
            }

            return (col - 1, rowNumber - 1);
        }

        private void AddBarChartOnNewWorksheet(
            string filePath,
            string chartSheetName,
            string sourceSheetName,
            string chartTitle,
            string categoryColumn,
            string valueColumn,
            int startDataRow,
            int endDataRow)
        {
            using var document = SpreadsheetDocument.Open(filePath, true);
            var workbookPart = document.WorkbookPart!;
            var workbook = workbookPart.Workbook;

            var existing = workbook.Descendants<S.Sheet>().FirstOrDefault(s => s.Name == chartSheetName);
            if (existing != null)
                return;

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new S.Worksheet(new S.SheetData());

            var drawingsPart = worksheetPart.AddNewPart<DrawingsPart>();
            drawingsPart.WorksheetDrawing = new Xdr.WorksheetDrawing();

            var drawingRelId = worksheetPart.GetIdOfPart(drawingsPart);
            worksheetPart.Worksheet.Append(new S.Drawing { Id = drawingRelId });

            var chartPart = drawingsPart.AddNewPart<ChartPart>();
            GenerateBarChartContentForSourceSheet(
                chartPart,
                sourceSheetName,
                chartTitle,
                categoryColumn,
                valueColumn,
                startDataRow,
                endDataRow);

            uint drawingObjectId = 1;

            var twoCellAnchor = new Xdr.TwoCellAnchor(
                new Xdr.FromMarker(
                    new Xdr.ColumnId("1"),
                    new Xdr.ColumnOffset("0"),
                    new Xdr.RowId("1"),
                    new Xdr.RowOffset("0")
                ),
                new Xdr.ToMarker(
                    new Xdr.ColumnId("12"),
                    new Xdr.ColumnOffset("0"),
                    new Xdr.RowId("22"),
                    new Xdr.RowOffset("0")
                ),
                new Xdr.GraphicFrame(
                    new Xdr.NonVisualGraphicFrameProperties(
                        new Xdr.NonVisualDrawingProperties
                        {
                            Id = drawingObjectId,
                            Name = chartTitle
                        },
                        new Xdr.NonVisualGraphicFrameDrawingProperties()
                    ),
                    new Xdr.Transform(
                        new A.Offset { X = 0L, Y = 0L },
                        new A.Extents { Cx = 0L, Cy = 0L }
                    ),
                    new A.Graphic(
                        new A.GraphicData(
                            new C.ChartReference { Id = drawingsPart.GetIdOfPart(chartPart) }
                        )
                        { Uri = "http://schemas.openxmlformats.org/drawingml/2006/chart" }
                    )
                ),
                new Xdr.ClientData()
            );

            drawingsPart.WorksheetDrawing.Append(twoCellAnchor);
            drawingsPart.WorksheetDrawing.Save();
            worksheetPart.Worksheet.Save();

            var sheets = workbook.GetFirstChild<S.Sheets>();
            if (sheets == null)
            {
                sheets = new S.Sheets();
                workbook.Append(sheets);
            }

            uint newSheetId = 1;
            var existingIds = sheets.Elements<S.Sheet>().Select(s => s.SheetId?.Value ?? 0U);
            if (existingIds.Any())
                newSheetId = existingIds.Max() + 1;

            var sheet = new S.Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = newSheetId,
                Name = chartSheetName
            };

            sheets.Append(sheet);
            workbook.Save();
        }

        private void GenerateBarChartContentForSourceSheet(
            ChartPart chartPart,
            string sourceSheetName,
            string chartTitle,
            string categoryColumn,
            string valueColumn,
            int startDataRow,
            int endDataRow)
        {
            var escapedSheetName = sourceSheetName.Replace("'", "''");
            var categoryRef = $"'{escapedSheetName}'!${categoryColumn}${startDataRow}:${categoryColumn}${endDataRow}";
            var valuesRef = $"'{escapedSheetName}'!${valueColumn}${startDataRow}:${valueColumn}${endDataRow}";

            var chartSpace = new C.ChartSpace();
            chartSpace.Append(new C.EditingLanguage { Val = "ru-RU" });

            var chart = new C.Chart();

            chart.Append(
                new C.Title(
                    new C.ChartText(
                        new C.RichText(
                            new A.BodyProperties(),
                            new A.ListStyle(),
                            new A.Paragraph(
                                new A.Run(
                                    new A.RunProperties { Language = "ru-RU" },
                                    new A.Text(chartTitle)
                                )
                            )
                        )
                    )
                )
            );

            chart.Append(new C.AutoTitleDeleted { Val = false });

            var plotArea = new C.PlotArea();
            plotArea.Append(new C.Layout());

            var barChart = new C.BarChart(
                new C.BarDirection { Val = C.BarDirectionValues.Column },
                new C.BarGrouping { Val = C.BarGroupingValues.Clustered },
                new C.VaryColors { Val = false }
            );

            var series = new C.BarChartSeries(
                new C.Index { Val = 0U },
                new C.Order { Val = 0U }
            );

            series.Append(
                new C.SeriesText(
                    new C.NumericValue { Text = chartTitle }
                )
            );

            series.Append(
                new C.CategoryAxisData(
                    new C.StringReference(
                        new C.Formula { Text = categoryRef }
                    )
                )
            );

            series.Append(
                new C.Values(
                    new C.NumberReference(
                        new C.Formula { Text = valuesRef }
                    )
                )
            );

            barChart.Append(series);

            barChart.Append(new C.AxisId { Val = 48650112U });
            barChart.Append(new C.AxisId { Val = 48672768U });

            plotArea.Append(barChart);

            var catAx = new C.CategoryAxis(
                new C.AxisId { Val = 48650112U },
                new C.Scaling(new C.Orientation { Val = C.OrientationValues.MinMax }),
                new C.Delete { Val = false },
                new C.AxisPosition { Val = C.AxisPositionValues.Bottom },
                new C.TickLabelPosition { Val = C.TickLabelPositionValues.NextTo },
                new C.CrossingAxis { Val = 48672768U },
                new C.Crosses { Val = C.CrossesValues.AutoZero },
                new C.AutoLabeled { Val = true },
                new C.LabelAlignment { Val = C.LabelAlignmentValues.Center },
                new C.LabelOffset { Val = (UInt16Value)100U }
            );

            var valAx = new C.ValueAxis(
                new C.AxisId { Val = 48672768U },
                new C.Scaling(new C.Orientation { Val = C.OrientationValues.MinMax }),
                new C.Delete { Val = false },
                new C.AxisPosition { Val = C.AxisPositionValues.Left },
                new C.MajorGridlines(),
                new C.NumberingFormat { FormatCode = "General", SourceLinked = true },
                new C.TickLabelPosition { Val = C.TickLabelPositionValues.NextTo },
                new C.CrossingAxis { Val = 48650112U },
                new C.Crosses { Val = C.CrossesValues.AutoZero },
                new C.CrossBetween { Val = C.CrossBetweenValues.Between }
            );

            plotArea.Append(catAx);
            plotArea.Append(valAx);

            chart.Append(plotArea);

            chart.Append(
                new C.Legend(
                    new C.LegendPosition { Val = C.LegendPositionValues.Right },
                    new C.Layout()
                )
            );

            chart.Append(new C.PlotVisibleOnly { Val = true });
            chart.Append(new C.DisplayBlanksAs { Val = C.DisplayBlanksAsValues.Gap });
            chart.Append(new C.ShowDataLabelsOverMaximum { Val = false });

            chartSpace.Append(chart);
            chartPart.ChartSpace = chartSpace;
            chartPart.ChartSpace.Save();
        }
    }

    public class ServiceReportRow
    {
        public string ServiceName { get; set; } = "";
        public int Count { get; set; }
        public double TotalAmount { get; set; }
    }

    public class MaterialReportRow
    {
        public string MaterialName { get; set; } = "";
        public double QuantityUsed { get; set; }
        public string Unit { get; set; } = "";
        public double TotalAmount { get; set; }
    }

    public class ReportStatusFilterItem
    {
        public int? Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class ReportClientFilterItem
    {
        public int? Id { get; set; }
        public string FullName { get; set; } = "";
    }

    public class ReportEmployeeFilterItem
    {
        public int? Id { get; set; }
        public string FullName { get; set; } = "";
    }

    public class EmployeeReportRow
    {
        public string EmployeeName { get; set; } = "";
        public int Count { get; set; }
        public double TotalAmount { get; set; }
    }

    public class OrderTypeReportRow
    {
        public string OrderType { get; set; } = "";
        public int Count { get; set; }
        public double TotalAmount { get; set; }
    }

    public class StatusReportRow
    {
        public string StatusName { get; set; } = "";
        public int Count { get; set; }
        public double TotalAmount { get; set; }
    }

    public class MonthReportRow
    {
        public string MonthName { get; set; } = "";
        public int Count { get; set; }
        public double TotalAmount { get; set; }
    }
}
