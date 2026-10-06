
using System.Globalization;
using System.IO;
using AtelierManager.Data;
using AtelierManager.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;

namespace AtelierManager.Services
{
    public static class ExcelDataService
    {
        private static readonly CultureInfo RuCulture = CultureInfo.GetCultureInfo("ru-RU");

        public static async Task<int> ExportClientsAsync(string filePath)
        {
            using var db = new AtelierContext();
            var clients = await db.Clients
                .AsNoTracking()
                .OrderBy(c => c.FullName)
                .ToListAsync();

            WriteWorkbook(filePath, CreateClientsSheet(clients));
            return clients.Count;
        }

        public static async Task<int> ImportClientsAsync(string filePath)
        {
            var workbook = ReadWorkbook(filePath);
            var sheet = GetWorksheet(workbook, "Клиенты");
            using var db = new AtelierContext();

            var importedCount = 0;
            foreach (var row in GetDataRows(sheet))
            {
                var fullName = GetText(sheet, row, 1);
                var phone = GetText(sheet, row, 2);
                var email = GetText(sheet, row, 3);

                if (string.IsNullOrWhiteSpace(fullName))
                    continue;

                var clientQuery = db.Clients.Where(c => c.FullName == fullName);
                Client? client;
                if (string.IsNullOrWhiteSpace(phone))
                {
                    client = await clientQuery.FirstOrDefaultAsync();
                }
                else
                {
                    client = await clientQuery.FirstOrDefaultAsync(c => c.Phone == phone);
                    client ??= await clientQuery.FirstOrDefaultAsync();
                }

                if (client == null)
                {
                    db.Clients.Add(new Client
                    {
                        FullName = fullName,
                        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone,
                        Email = string.IsNullOrWhiteSpace(email) ? null : email,
                        CreatedAt = DateTime.Now
                    });

                    importedCount++;
                }
                else
                {
                    client.Phone = string.IsNullOrWhiteSpace(phone) ? client.Phone : phone;
                    client.Email = string.IsNullOrWhiteSpace(email) ? client.Email : email;
                }
            }

            await db.SaveChangesAsync();
            return importedCount;
        }

        public static async Task<int> ExportMaterialsAsync(string filePath)
        {
            using var db = new AtelierContext();
            var materials = await db.Materials
                .AsNoTracking()
                .OrderBy(m => m.Name)
                .ToListAsync();

            WriteWorkbook(filePath, CreateMaterialsSheet(materials));
            return materials.Count;
        }

        public static async Task<int> ImportMaterialsAsync(string filePath)
        {
            var workbook = ReadWorkbook(filePath);
            var sheet = GetWorksheet(workbook, "Материалы");
            using var db = new AtelierContext();

            var importedCount = 0;
            foreach (var row in GetDataRows(sheet))
            {
                var name = GetText(sheet, row, 1);
                var quantity = GetDouble(sheet, row, 2);
                var unit = GetText(sheet, row, 3);
                var price = GetDouble(sheet, row, 4);

                if (string.IsNullOrWhiteSpace(name) || quantity < 0 || price < 0)
                    continue;

                if (string.IsNullOrWhiteSpace(unit))
                    unit = "шт";

                var material = await db.Materials.FirstOrDefaultAsync(m =>
                    m.Name == name &&
                    m.Unit == unit);

                if (material == null)
                {
                    db.Materials.Add(new Material
                    {
                        Name = name,
                        Quantity = quantity,
                        Unit = unit,
                        Price = price
                    });

                    importedCount++;
                }
                else
                {
                    material.Quantity = quantity;
                    material.Price = price;
                }
            }

            await db.SaveChangesAsync();
            return importedCount;
        }

        public static async Task<int> ExportServicesAsync(string filePath)
        {
            using var db = new AtelierContext();
            var services = await db.Services
                .AsNoTracking()
                .OrderBy(s => s.ServiceType)
                .ThenBy(s => s.Name)
                .ToListAsync();

            WriteWorkbook(filePath, CreateServicesSheet(services));
            return services.Count;
        }

        public static async Task<int> ImportServicesAsync(string filePath)
        {
            var workbook = ReadWorkbook(filePath);
            var sheet = GetWorksheet(workbook, "Услуги");
            using var db = new AtelierContext();

            var importedCount = 0;
            foreach (var row in GetDataRows(sheet))
            {
                var name = GetText(sheet, row, 1);
                var serviceType = GetText(sheet, row, 2);
                var basePrice = GetDouble(sheet, row, 3);
                var description = GetText(sheet, row, 4);
                var activeText = GetText(sheet, row, 5);

                if (string.IsNullOrWhiteSpace(name) || basePrice < 0)
                    continue;

                if (string.IsNullOrWhiteSpace(serviceType))
                    serviceType = "Ремонт";

                var isActive = string.IsNullOrWhiteSpace(activeText) || IsYes(activeText);
                var service = await db.Services.FirstOrDefaultAsync(s =>
                    s.Name == name &&
                    s.ServiceType == serviceType);

                if (service == null)
                {
                    db.Services.Add(new Service
                    {
                        Name = name,
                        ServiceType = serviceType,
                        BasePrice = basePrice,
                        Description = string.IsNullOrWhiteSpace(description) ? null : description,
                        IsActive = isActive
                    });

                    importedCount++;
                }
                else
                {
                    service.BasePrice = basePrice;
                    service.Description = string.IsNullOrWhiteSpace(description) ? null : description;
                    service.IsActive = isActive;
                }
            }

            await db.SaveChangesAsync();
            return importedCount;
        }

        public static async Task<int> ExportPersonsAsync(string filePath)
        {
            using var db = new AtelierContext();
            var persons = await db.Persons
                .AsNoTracking()
                .Include(p => p.Client)
                .OrderBy(p => p.Client!.FullName)
                .ThenBy(p => p.DisplayName)
                .ToListAsync();

            WriteWorkbook(filePath, CreatePersonsSheet(persons));
            return persons.Count;
        }

        public static async Task<int> ImportPersonsAsync(string filePath)
        {
            var workbook = ReadWorkbook(filePath);
            var sheet = GetWorksheet(workbook, "Персоны");
            using var db = new AtelierContext();

            var importedCount = 0;
            foreach (var row in GetDataRows(sheet))
            {
                var clientName = GetText(sheet, row, 1);
                var clientPhone = GetText(sheet, row, 2);
                var personName = GetText(sheet, row, 3);
                var gender = GetText(sheet, row, 4);

                if (string.IsNullOrWhiteSpace(clientName) || string.IsNullOrWhiteSpace(personName))
                    continue;

                var client = await FindClientAsync(db, clientName, clientPhone, createIfMissing: true);
                var person = await db.Persons.FirstOrDefaultAsync(p =>
                    p.ClientId == client.Id &&
                    p.DisplayName == personName);

                if (person == null)
                {
                    db.Persons.Add(new Person
                    {
                        ClientId = client.Id,
                        DisplayName = personName,
                        Gender = string.IsNullOrWhiteSpace(gender) ? null : gender
                    });
                    importedCount++;
                }
                else
                {
                    person.Gender = string.IsNullOrWhiteSpace(gender) ? person.Gender : gender;
                }
            }

            await db.SaveChangesAsync();
            return importedCount;
        }

        public static async Task<int> ExportMeasurementSetsAsync(string filePath)
        {
            using var db = new AtelierContext();
            var measurements = await db.MeasurementSets
                .AsNoTracking()
                .Include(m => m.Person)
                .ThenInclude(p => p.Client)
                .OrderBy(m => m.Person!.Client!.FullName)
                .ThenBy(m => m.Person!.DisplayName)
                .ThenByDescending(m => m.TakenAt)
                .ToListAsync();

            WriteWorkbook(filePath, CreateMeasurementsSheet(measurements));
            return measurements.Count;
        }

        public static async Task<int> ImportMeasurementSetsAsync(string filePath)
        {
            var workbook = ReadWorkbook(filePath);
            var sheet = GetWorksheet(workbook, "Мерки");
            using var db = new AtelierContext();

            var importedCount = 0;
            foreach (var row in GetDataRows(sheet))
            {
                var clientName = GetText(sheet, row, 1);
                var clientPhone = GetText(sheet, row, 2);
                var personName = GetText(sheet, row, 3);

                if (string.IsNullOrWhiteSpace(clientName) || string.IsNullOrWhiteSpace(personName))
                    continue;

                var takenAt = GetDate(sheet, row, 4) ?? DateTime.Now;
                var client = await FindClientAsync(db, clientName, clientPhone, createIfMissing: true);
                var person = await FindPersonAsync(db, client.Id, personName, createIfMissing: true);

                var measurementDateStart = takenAt.Date;
                var measurementDateEnd = measurementDateStart.AddDays(1);
                var measurement = await db.MeasurementSets.FirstOrDefaultAsync(m =>
                    m.PersonId == person.Id &&
                    m.TakenAt >= measurementDateStart &&
                    m.TakenAt < measurementDateEnd);

                if (measurement == null)
                {
                    measurement = new MeasurementSet
                    {
                        PersonId = person.Id,
                        TakenAt = takenAt.Date
                    };
                    db.MeasurementSets.Add(measurement);
                    importedCount++;
                }

                measurement.Height = GetNullableDouble(sheet, row, 5);
                measurement.Chest = GetNullableDouble(sheet, row, 6);
                measurement.Waist = GetNullableDouble(sheet, row, 7);
                measurement.Hips = GetNullableDouble(sheet, row, 8);
                measurement.Shoulder = GetNullableDouble(sheet, row, 9);
                measurement.Sleeve = GetNullableDouble(sheet, row, 10);
                measurement.Neck = GetNullableDouble(sheet, row, 11);
                measurement.ShoulderWidth = GetNullableDouble(sheet, row, 12);
                measurement.BackLength = GetNullableDouble(sheet, row, 13);
                measurement.Wrist = GetNullableDouble(sheet, row, 14);
                measurement.Bicep = GetNullableDouble(sheet, row, 15);
                measurement.Inseam = GetNullableDouble(sheet, row, 16);
                measurement.Outseam = GetNullableDouble(sheet, row, 17);
                measurement.Thigh = GetNullableDouble(sheet, row, 18);
                measurement.Knee = GetNullableDouble(sheet, row, 19);
                measurement.Ankle = GetNullableDouble(sheet, row, 20);
                measurement.Rise = GetNullableDouble(sheet, row, 21);
                measurement.ProductLength = GetNullableDouble(sheet, row, 22);

                var comment = GetText(sheet, row, 23);
                measurement.Comment = string.IsNullOrWhiteSpace(comment) ? null : comment;
            }

            await db.SaveChangesAsync();
            return importedCount;
        }

        public static async Task<int> ExportOrdersAsync(string filePath)
        {
            using var db = new AtelierContext();
            var orders = await LoadOrdersForExport(db);
            var orderServices = await LoadOrderServicesForExport(db);
            var orderExtras = await LoadOrderServiceExtrasForExport(db);
            var orderMaterials = await LoadOrderMaterialsForExport(db);

            WriteWorkbook(filePath,
                CreateOrdersSheet(orders),
                CreateOrderServicesSheet(orderServices),
                CreateOrderServiceExtrasSheet(orderExtras),
                CreateOrderMaterialsSheet(orderMaterials));

            return orders.Count + orderServices.Count + orderExtras.Count + orderMaterials.Count;
        }

        public static async Task<int> ImportOrdersAsync(string filePath)
        {
            var workbook = ReadWorkbook(filePath);
            using var db = new AtelierContext();
            var importedCount = 0;

            var ordersSheet = FindWorksheet(workbook, "Заказы");
            if (ordersSheet != null)
                importedCount += await ImportOrdersSheetAsync(db, ordersSheet);

            var servicesSheet = FindWorksheet(workbook, "Услуги заказа");
            if (servicesSheet != null)
                importedCount += await ImportOrderServicesSheetAsync(db, servicesSheet);

            var extrasSheet = FindWorksheet(workbook, "Доп элементы заказа") ?? FindWorksheet(workbook, "Доп. элементы заказа");
            if (extrasSheet != null)
                importedCount += await ImportOrderServiceExtrasSheetAsync(db, extrasSheet);

            var materialsSheet = FindWorksheet(workbook, "Материалы заказа");
            if (materialsSheet != null)
                importedCount += await ImportOrderMaterialsSheetAsync(db, materialsSheet);

            await db.SaveChangesAsync();
            return importedCount;
        }

        public static async Task<int> ExportAllTablesAsync(string filePath)
        {
            using var db = new AtelierContext();

            var clients = await db.Clients.AsNoTracking().OrderBy(c => c.FullName).ToListAsync();
            var persons = await db.Persons.AsNoTracking().Include(p => p.Client)
                .OrderBy(p => p.Client!.FullName).ThenBy(p => p.DisplayName).ToListAsync();
            var measurements = await db.MeasurementSets.AsNoTracking().Include(m => m.Person).ThenInclude(p => p.Client)
                .OrderBy(m => m.Person!.Client!.FullName).ThenBy(m => m.Person!.DisplayName).ThenByDescending(m => m.TakenAt).ToListAsync();
            var materials = await db.Materials.AsNoTracking().OrderBy(m => m.Name).ToListAsync();
            var services = await db.Services.AsNoTracking().OrderBy(s => s.ServiceType).ThenBy(s => s.Name).ToListAsync();
            var extraElements = await db.ExtraElements.AsNoTracking().Include(e => e.Service)
                .OrderBy(e => e.Service!.ServiceType).ThenBy(e => e.Service!.Name).ThenBy(e => e.Name).ToListAsync();
            var statuses = await db.Statuses.AsNoTracking().OrderBy(s => s.SortOrder).ThenBy(s => s.Name).ToListAsync();
            var orders = await LoadOrdersForExport(db);
            var orderServices = await LoadOrderServicesForExport(db);
            var orderExtras = await LoadOrderServiceExtrasForExport(db);
            var orderMaterials = await LoadOrderMaterialsForExport(db);

            WriteWorkbook(filePath,
                CreateClientsSheet(clients),
                CreatePersonsSheet(persons),
                CreateMeasurementsSheet(measurements),
                CreateMaterialsSheet(materials),
                CreateServicesSheet(services),
                CreateExtraElementsSheet(extraElements),
                CreateStatusesSheet(statuses),
                CreateOrdersSheet(orders),
                CreateOrderServicesSheet(orderServices),
                CreateOrderServiceExtrasSheet(orderExtras),
                CreateOrderMaterialsSheet(orderMaterials));

            return clients.Count + persons.Count + measurements.Count + materials.Count + services.Count +
                   extraElements.Count + statuses.Count + orders.Count + orderServices.Count + orderExtras.Count +
                   orderMaterials.Count;
        }

        private static async Task<List<Order>> LoadOrdersForExport(AtelierContext db)
        {
            return await db.Orders
                .AsNoTracking()
                .Include(o => o.Client)
                .Include(o => o.Status)
                .OrderByDescending(o => o.IntakeDate)
                .ThenBy(o => o.Client!.FullName)
                .ToListAsync();
        }

        private static async Task<List<OrderService>> LoadOrderServicesForExport(AtelierContext db)
        {
            return await db.OrderServices
                .AsNoTracking()
                .Include(os => os.Order)
                .ThenInclude(o => o.Client)
                .Include(os => os.Service)
                .Include(os => os.Person)
                .Include(os => os.MeasurementSet)
                .OrderByDescending(os => os.Order!.IntakeDate)
                .ThenBy(os => os.Order!.Client!.FullName)
                .ThenBy(os => os.Service!.Name)
                .ToListAsync();
        }

        private static async Task<List<OrderServiceExtra>> LoadOrderServiceExtrasForExport(AtelierContext db)
        {
            return await db.OrderServiceExtras
                .AsNoTracking()
                .Include(e => e.OrderService)
                .ThenInclude(os => os.Order)
                .ThenInclude(o => o.Client)
                .Include(e => e.OrderService)
                .ThenInclude(os => os.Service)
                .Include(e => e.OrderService)
                .ThenInclude(os => os.Person)
                .Include(e => e.ExtraElement)
                .OrderByDescending(e => e.OrderService!.Order!.IntakeDate)
                .ThenBy(e => e.OrderService!.Order!.Client!.FullName)
                .ThenBy(e => e.OrderService!.Service!.Name)
                .ThenBy(e => e.ExtraElement!.Name)
                .ToListAsync();
        }

        private static async Task<List<OrderMaterial>> LoadOrderMaterialsForExport(AtelierContext db)
        {
            return await db.OrderMaterials
                .AsNoTracking()
                .Include(om => om.Order)
                .ThenInclude(o => o.Client)
                .Include(om => om.Material)
                .OrderByDescending(om => om.Order!.IntakeDate)
                .ThenBy(om => om.Order!.Client!.FullName)
                .ThenBy(om => om.Material!.Name)
                .ToListAsync();
        }

        private static ExcelSheetExport CreateClientsSheet(IReadOnlyCollection<Client> clients)
        {
            var sheet = new ExcelSheetExport("Клиенты", new[] { "ФИО", "Телефон", "Email", "Дата добавления" }, "Всего клиентов:");
            foreach (var client in clients)
            {
                sheet.Rows.Add(new object?[]
                {
                    client.FullName,
                    client.Phone,
                    client.Email,
                    FormatDate(client.CreatedAt)
                });
            }
            return sheet;
        }

        private static ExcelSheetExport CreateMaterialsSheet(IReadOnlyCollection<Material> materials)
        {
            var sheet = new ExcelSheetExport("Материалы", new[] { "Название", "Количество", "Единица измерения", "Цена" }, "Всего материалов:");
            foreach (var material in materials)
            {
                sheet.Rows.Add(new object?[] { material.Name, material.Quantity, material.Unit, material.Price });
            }
            return sheet;
        }

        private static ExcelSheetExport CreateServicesSheet(IReadOnlyCollection<Service> services)
        {
            var sheet = new ExcelSheetExport("Услуги", new[] { "Название", "Тип", "Базовая цена", "Описание", "Активна" }, "Всего услуг:");
            foreach (var service in services)
            {
                sheet.Rows.Add(new object?[]
                {
                    service.Name,
                    service.ServiceType,
                    service.BasePrice,
                    service.Description,
                    service.IsActive ? "Да" : "Нет"
                });
            }
            return sheet;
        }

        private static ExcelSheetExport CreatePersonsSheet(IReadOnlyCollection<Person> persons)
        {
            var sheet = new ExcelSheetExport("Персоны", new[] { "Клиент", "Телефон клиента", "Персона", "Пол" }, "Всего персон:");
            foreach (var person in persons)
            {
                sheet.Rows.Add(new object?[]
                {
                    person.Client?.FullName,
                    person.Client?.Phone,
                    person.DisplayName,
                    person.Gender
                });
            }
            return sheet;
        }

        private static ExcelSheetExport CreateMeasurementsSheet(IReadOnlyCollection<MeasurementSet> measurements)
        {
            var sheet = new ExcelSheetExport("Мерки", new[]
            {
                "Клиент", "Телефон клиента", "Персона", "Дата снятия", "Рост", "Обхват груди", "Обхват талии",
                "Обхват бедер", "Плечо", "Рукав", "Шея", "Ширина плеч", "Длина спины", "Запястье", "Бицепс",
                "Шаговый шов", "Внешняя длина", "Бедро", "Колено", "Низ", "Высота сидения", "Длина изделия", "Комментарий"
            }, "Всего комплектов мерок:");

            foreach (var measurement in measurements)
            {
                sheet.Rows.Add(new object?[]
                {
                    measurement.Person?.Client?.FullName,
                    measurement.Person?.Client?.Phone,
                    measurement.Person?.DisplayName,
                    FormatDate(measurement.TakenAt),
                    measurement.Height,
                    measurement.Chest,
                    measurement.Waist,
                    measurement.Hips,
                    measurement.Shoulder,
                    measurement.Sleeve,
                    measurement.Neck,
                    measurement.ShoulderWidth,
                    measurement.BackLength,
                    measurement.Wrist,
                    measurement.Bicep,
                    measurement.Inseam,
                    measurement.Outseam,
                    measurement.Thigh,
                    measurement.Knee,
                    measurement.Ankle,
                    measurement.Rise,
                    measurement.ProductLength,
                    measurement.Comment
                });
            }
            return sheet;
        }

        private static ExcelSheetExport CreateStatusesSheet(IReadOnlyCollection<Status> statuses)
        {
            var sheet = new ExcelSheetExport("Статусы", new[] { "Название", "Порядок", "Финальный" }, "Всего статусов:");
            foreach (var status in statuses)
            {
                sheet.Rows.Add(new object?[] { status.Name, status.SortOrder, status.IsFinal ? "Да" : "Нет" });
            }
            return sheet;
        }

        private static ExcelSheetExport CreateExtraElementsSheet(IReadOnlyCollection<ExtraElement> extraElements)
        {
            var sheet = new ExcelSheetExport("Доп элементы", new[] { "Услуга", "Тип услуги", "Название", "Цена по умолчанию", "Активен" }, "Всего доп. элементов:");
            foreach (var extra in extraElements)
            {
                sheet.Rows.Add(new object?[]
                {
                    extra.Service?.Name,
                    extra.Service?.ServiceType,
                    extra.Name,
                    extra.DefaultPrice,
                    extra.IsActive ? "Да" : "Нет"
                });
            }
            return sheet;
        }

        private static ExcelSheetExport CreateOrdersSheet(IReadOnlyCollection<Order> orders)
        {
            var sheet = new ExcelSheetExport("Заказы", new[]
            {
                "Клиент", "Телефон клиента", "Статус", "Дата приема", "Срок выполнения", "Источник материала",
                "Комментарий по материалу", "Примечание", "Итоговая стоимость"
            }, "Всего заказов:");

            foreach (var order in orders)
            {
                sheet.Rows.Add(new object?[]
                {
                    order.Client?.FullName,
                    order.Client?.Phone,
                    order.Status?.Name,
                    FormatDate(order.IntakeDate),
                    FormatDate(order.DueDate),
                    order.MaterialSource,
                    order.MaterialComment,
                    order.Notes,
                    order.TotalCost
                });
            }
            return sheet;
        }

        private static ExcelSheetExport CreateOrderServicesSheet(IReadOnlyCollection<OrderService> orderServices)
        {
            var sheet = new ExcelSheetExport("Услуги заказа", new[]
            {
                "Клиент", "Телефон клиента", "Дата приема", "Услуга", "Тип услуги", "Персона", "Дата мерок",
                "Базовая цена", "Сумма строки"
            }, "Всего услуг в заказах:");

            foreach (var line in orderServices)
            {
                sheet.Rows.Add(new object?[]
                {
                    line.Order?.Client?.FullName,
                    line.Order?.Client?.Phone,
                    FormatDate(line.Order?.IntakeDate),
                    line.Service?.Name,
                    line.Service?.ServiceType,
                    line.Person?.DisplayName,
                    FormatDate(line.MeasurementSet?.TakenAt),
                    line.BasePrice,
                    line.LineTotal
                });
            }
            return sheet;
        }

        private static ExcelSheetExport CreateOrderServiceExtrasSheet(IReadOnlyCollection<OrderServiceExtra> extras)
        {
            var sheet = new ExcelSheetExport("Доп элементы заказа", new[]
            {
                "Клиент", "Телефон клиента", "Дата приема", "Услуга", "Тип услуги", "Персона",
                "Дополнительный элемент", "Количество", "Цена", "Сумма"
            }, "Всего доп. элементов в заказах:");

            foreach (var extra in extras)
            {
                var line = extra.OrderService;
                sheet.Rows.Add(new object?[]
                {
                    line?.Order?.Client?.FullName,
                    line?.Order?.Client?.Phone,
                    FormatDate(line?.Order?.IntakeDate),
                    line?.Service?.Name,
                    line?.Service?.ServiceType,
                    line?.Person?.DisplayName,
                    extra.ExtraElement?.Name,
                    extra.Qty,
                    extra.Price,
                    extra.Total
                });
            }
            return sheet;
        }

        private static ExcelSheetExport CreateOrderMaterialsSheet(IReadOnlyCollection<OrderMaterial> materials)
        {
            var sheet = new ExcelSheetExport("Материалы заказа", new[]
            {
                "Клиент", "Телефон клиента", "Дата приема", "Материал", "Единица измерения", "Количество",
                "Цена за единицу", "Сумма"
            }, "Всего материалов в заказах:");

            foreach (var material in materials)
            {
                sheet.Rows.Add(new object?[]
                {
                    material.Order?.Client?.FullName,
                    material.Order?.Client?.Phone,
                    FormatDate(material.Order?.IntakeDate),
                    material.Material?.Name,
                    material.Material?.Unit,
                    material.QtyUsed,
                    material.UnitPrice,
                    material.Total
                });
            }
            return sheet;
        }

        private static async Task<int> ImportOrdersSheetAsync(AtelierContext db, ExcelSheet sheet)
        {
            var importedCount = 0;
            foreach (var row in GetDataRows(sheet))
            {
                var clientName = GetText(sheet, row, 1);
                var clientPhone = GetText(sheet, row, 2);
                var statusName = GetText(sheet, row, 3);
                var intakeDate = GetDate(sheet, row, 4);

                if (string.IsNullOrWhiteSpace(clientName) || intakeDate == null)
                    continue;

                var client = await FindClientAsync(db, clientName, clientPhone, createIfMissing: true);
                var status = await FindStatusAsync(db, statusName);
                var order = await FindOrderAsync(db, client.Id, intakeDate.Value);

                if (order == null)
                {
                    order = new Order
                    {
                        ClientId = client.Id,
                        IntakeDate = intakeDate.Value.Date
                    };
                    db.Orders.Add(order);
                    importedCount++;
                }

                order.StatusId = status.Id;
                order.DueDate = GetDate(sheet, row, 5)?.Date;
                order.MaterialSource = GetText(sheet, row, 6);
                if (string.IsNullOrWhiteSpace(order.MaterialSource))
                    order.MaterialSource = "Клиент";
                order.MaterialComment = ToNull(GetText(sheet, row, 7));
                order.Notes = ToNull(GetText(sheet, row, 8));
                order.TotalCost = GetDouble(sheet, row, 9);
            }

            await db.SaveChangesAsync();
            return importedCount;
        }

        private static async Task<int> ImportOrderServicesSheetAsync(AtelierContext db, ExcelSheet sheet)
        {
            var importedCount = 0;
            foreach (var row in GetDataRows(sheet))
            {
                var clientName = GetText(sheet, row, 1);
                var clientPhone = GetText(sheet, row, 2);
                var intakeDate = GetDate(sheet, row, 3);
                var serviceName = GetText(sheet, row, 4);
                var serviceType = GetText(sheet, row, 5);
                var personName = GetText(sheet, row, 6);

                if (string.IsNullOrWhiteSpace(clientName) || intakeDate == null ||
                    string.IsNullOrWhiteSpace(serviceName) || string.IsNullOrWhiteSpace(personName))
                    continue;

                var client = await FindClientAsync(db, clientName, clientPhone, createIfMissing: true);
                var order = await FindOrCreateOrderAsync(db, client.Id, intakeDate.Value);
                var service = await FindServiceAsync(db, serviceName, serviceType, GetDouble(sheet, row, 8));
                var person = await FindPersonAsync(db, client.Id, personName, createIfMissing: true);
                var measurementDate = GetDate(sheet, row, 7);
                var measurement = measurementDate == null
                    ? null
                    : await FindMeasurementByDateAsync(db, person.Id, measurementDate.Value);

                var line = await FindOrderServiceAsync(db, order.Id, service.Id, person.Id);
                if (line == null)
                {
                    line = new OrderService
                    {
                        OrderId = order.Id,
                        ServiceId = service.Id,
                        PersonId = person.Id
                    };
                    db.OrderServices.Add(line);
                    importedCount++;
                }

                line.MeasurementSetId = measurement?.Id;
                line.BasePrice = GetDouble(sheet, row, 8);
                line.LineTotal = GetDouble(sheet, row, 9);
            }

            await db.SaveChangesAsync();
            return importedCount;
        }

        private static async Task<int> ImportOrderServiceExtrasSheetAsync(AtelierContext db, ExcelSheet sheet)
        {
            var importedCount = 0;
            foreach (var row in GetDataRows(sheet))
            {
                var clientName = GetText(sheet, row, 1);
                var clientPhone = GetText(sheet, row, 2);
                var intakeDate = GetDate(sheet, row, 3);
                var serviceName = GetText(sheet, row, 4);
                var serviceType = GetText(sheet, row, 5);
                var personName = GetText(sheet, row, 6);
                var extraName = GetText(sheet, row, 7);

                if (string.IsNullOrWhiteSpace(clientName) || intakeDate == null ||
                    string.IsNullOrWhiteSpace(serviceName) || string.IsNullOrWhiteSpace(personName) ||
                    string.IsNullOrWhiteSpace(extraName))
                    continue;

                var client = await FindClientAsync(db, clientName, clientPhone, createIfMissing: true);
                var order = await FindOrCreateOrderAsync(db, client.Id, intakeDate.Value);
                var service = await FindServiceAsync(db, serviceName, serviceType, GetDouble(sheet, row, 9));
                var person = await FindPersonAsync(db, client.Id, personName, createIfMissing: true);
                var line = await FindOrderServiceAsync(db, order.Id, service.Id, person.Id);
                if (line == null)
                {
                    line = new OrderService
                    {
                        OrderId = order.Id,
                        ServiceId = service.Id,
                        PersonId = person.Id,
                        BasePrice = service.BasePrice,
                        LineTotal = 0
                    };
                    db.OrderServices.Add(line);
                    await db.SaveChangesAsync();
                }

                var extraElement = await FindExtraElementAsync(db, service.Id, extraName, GetDouble(sheet, row, 9));
                var extra = await db.OrderServiceExtras.FirstOrDefaultAsync(e =>
                    e.OrderServiceId == line.Id &&
                    e.ExtraElementId == extraElement.Id);

                if (extra == null)
                {
                    extra = new OrderServiceExtra
                    {
                        OrderServiceId = line.Id,
                        ExtraElementId = extraElement.Id
                    };
                    db.OrderServiceExtras.Add(extra);
                    importedCount++;
                }

                extra.Qty = Math.Max(1, GetInt(sheet, row, 8, 1));
                extra.Price = GetDouble(sheet, row, 9);
                extra.Total = GetDouble(sheet, row, 10);
            }

            await db.SaveChangesAsync();
            return importedCount;
        }

        private static async Task<int> ImportOrderMaterialsSheetAsync(AtelierContext db, ExcelSheet sheet)
        {
            var importedCount = 0;
            foreach (var row in GetDataRows(sheet))
            {
                var clientName = GetText(sheet, row, 1);
                var clientPhone = GetText(sheet, row, 2);
                var intakeDate = GetDate(sheet, row, 3);
                var materialName = GetText(sheet, row, 4);
                var unit = GetText(sheet, row, 5);

                if (string.IsNullOrWhiteSpace(clientName) || intakeDate == null || string.IsNullOrWhiteSpace(materialName))
                    continue;

                if (string.IsNullOrWhiteSpace(unit))
                    unit = "шт";

                var client = await FindClientAsync(db, clientName, clientPhone, createIfMissing: true);
                var order = await FindOrCreateOrderAsync(db, client.Id, intakeDate.Value);
                var material = await FindMaterialAsync(db, materialName, unit, GetDouble(sheet, row, 7));
                var orderMaterial = await db.OrderMaterials.FirstOrDefaultAsync(om =>
                    om.OrderId == order.Id &&
                    om.MaterialId == material.Id);

                if (orderMaterial == null)
                {
                    orderMaterial = new OrderMaterial
                    {
                        OrderId = order.Id,
                        MaterialId = material.Id
                    };
                    db.OrderMaterials.Add(orderMaterial);
                    importedCount++;
                }

                orderMaterial.QtyUsed = GetDouble(sheet, row, 6);
                orderMaterial.UnitPrice = GetDouble(sheet, row, 7);
                orderMaterial.Total = GetDouble(sheet, row, 8);
            }

            await db.SaveChangesAsync();
            return importedCount;
        }

        private static async Task<Client> FindClientAsync(AtelierContext db, string fullName, string? phone, bool createIfMissing)
        {
            fullName = fullName.Trim();
            phone = phone?.Trim();

            var query = db.Clients.Where(c => c.FullName == fullName);
            Client? client = null;

            if (!string.IsNullOrWhiteSpace(phone))
                client = await query.FirstOrDefaultAsync(c => c.Phone == phone);

            client ??= await query.FirstOrDefaultAsync();

            if (client != null)
            {
                if (!string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(client.Phone))
                    client.Phone = phone;
                return client;
            }

            if (!createIfMissing)
                throw new InvalidOperationException($"Клиент '{fullName}' не найден.");

            client = new Client
            {
                FullName = fullName,
                Phone = string.IsNullOrWhiteSpace(phone) ? null : phone,
                CreatedAt = DateTime.Now
            };
            db.Clients.Add(client);
            await db.SaveChangesAsync();

            return client;
        }

        private static async Task<Person> FindPersonAsync(AtelierContext db, int clientId, string personName, bool createIfMissing)
        {
            personName = personName.Trim();
            var person = await db.Persons.FirstOrDefaultAsync(p =>
                p.ClientId == clientId &&
                p.DisplayName == personName);

            if (person != null)
                return person;

            if (!createIfMissing)
                throw new InvalidOperationException($"Персона '{personName}' не найдена.");

            person = new Person
            {
                ClientId = clientId,
                DisplayName = personName
            };
            db.Persons.Add(person);
            await db.SaveChangesAsync();

            return person;
        }

        private static async Task<Status> FindStatusAsync(AtelierContext db, string statusName)
        {
            statusName = string.IsNullOrWhiteSpace(statusName) ? "Новый" : statusName.Trim();
            var status = await db.Statuses.FirstOrDefaultAsync(s => s.Name == statusName);
            if (status != null)
                return status;

            var sortOrder = await db.Statuses.Select(s => (int?)s.SortOrder).MaxAsync() ?? 0;
            status = new Status
            {
                Name = statusName,
                SortOrder = sortOrder + 1,
                IsFinal = false
            };
            db.Statuses.Add(status);
            await db.SaveChangesAsync();

            return status;
        }

        private static async Task<Service> FindServiceAsync(AtelierContext db, string serviceName, string serviceType, double basePrice)
        {
            serviceName = serviceName.Trim();
            serviceType = string.IsNullOrWhiteSpace(serviceType) ? "Ремонт" : serviceType.Trim();

            var service = await db.Services.FirstOrDefaultAsync(s =>
                s.Name == serviceName &&
                s.ServiceType == serviceType);

            if (service != null)
            {
                if (basePrice > 0)
                    service.BasePrice = basePrice;
                return service;
            }

            service = new Service
            {
                Name = serviceName,
                ServiceType = serviceType,
                BasePrice = Math.Max(0, basePrice),
                IsActive = true
            };
            db.Services.Add(service);
            await db.SaveChangesAsync();

            return service;
        }

        private static async Task<ExtraElement> FindExtraElementAsync(AtelierContext db, int serviceId, string extraName, double price)
        {
            extraName = extraName.Trim();
            var extra = await db.ExtraElements.FirstOrDefaultAsync(e =>
                e.ServiceId == serviceId &&
                e.Name == extraName);

            if (extra != null)
            {
                extra.DefaultPrice = price;
                return extra;
            }

            extra = new ExtraElement
            {
                ServiceId = serviceId,
                Name = extraName,
                DefaultPrice = price,
                IsActive = true
            };
            db.ExtraElements.Add(extra);
            await db.SaveChangesAsync();

            return extra;
        }

        private static async Task<Material> FindMaterialAsync(AtelierContext db, string materialName, string unit, double price)
        {
            materialName = materialName.Trim();
            unit = string.IsNullOrWhiteSpace(unit) ? "шт" : unit.Trim();

            var material = await db.Materials.FirstOrDefaultAsync(m =>
                m.Name == materialName &&
                m.Unit == unit);

            if (material != null)
            {
                if (price > 0)
                    material.Price = price;
                return material;
            }

            material = new Material
            {
                Name = materialName,
                Unit = unit,
                Quantity = 0,
                Price = Math.Max(0, price)
            };
            db.Materials.Add(material);
            await db.SaveChangesAsync();

            return material;
        }

        private static async Task<Order?> FindOrderAsync(AtelierContext db, int clientId, DateTime intakeDate)
        {
            var dateStart = intakeDate.Date;
            var dateEnd = dateStart.AddDays(1);
            return await db.Orders.FirstOrDefaultAsync(o =>
                o.ClientId == clientId &&
                o.IntakeDate >= dateStart &&
                o.IntakeDate < dateEnd);
        }

        private static async Task<Order> FindOrCreateOrderAsync(AtelierContext db, int clientId, DateTime intakeDate)
        {
            var order = await FindOrderAsync(db, clientId, intakeDate);
            if (order != null)
                return order;

            var status = await FindStatusAsync(db, "Новый");
            order = new Order
            {
                ClientId = clientId,
                StatusId = status.Id,
                IntakeDate = intakeDate.Date,
                MaterialSource = "Клиент"
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync();

            return order;
        }

        private static async Task<MeasurementSet?> FindMeasurementByDateAsync(AtelierContext db, int personId, DateTime takenAt)
        {
            var dateStart = takenAt.Date;
            var dateEnd = dateStart.AddDays(1);
            return await db.MeasurementSets.FirstOrDefaultAsync(m =>
                m.PersonId == personId &&
                m.TakenAt >= dateStart &&
                m.TakenAt < dateEnd);
        }

        private static async Task<OrderService?> FindOrderServiceAsync(AtelierContext db, int orderId, int serviceId, int personId)
        {
            return await db.OrderServices.FirstOrDefaultAsync(os =>
                os.OrderId == orderId &&
                os.ServiceId == serviceId &&
                os.PersonId == personId);
        }

        private static ExcelSheet GetWorksheet(IReadOnlyList<ExcelSheet> workbook, string preferredSheetName)
        {
            return FindWorksheet(workbook, preferredSheetName)
                   ?? workbook.FirstOrDefault()
                   ?? throw new InvalidOperationException("В Excel-файле нет вкладок.");
        }

        private static ExcelSheet? FindWorksheet(IReadOnlyList<ExcelSheet> workbook, string sheetName)
        {
            return workbook.FirstOrDefault(w =>
                string.Equals(w.Name, sheetName, StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<int> GetDataRows(ExcelSheet sheet)
        {
            for (var row = 2; row <= sheet.LastRow; row++)
            {
                if (IsSummaryRow(sheet, row) || RowIsEmpty(sheet, row))
                    continue;

                yield return row;
            }
        }

        private static bool RowIsEmpty(ExcelSheet sheet, int row)
        {
            if (!sheet.Rows.TryGetValue(row, out var cells))
                return true;

            return cells.Values.All(string.IsNullOrWhiteSpace);
        }

        private static string GetText(ExcelSheet sheet, int row, int column)
        {
            if (!sheet.Rows.TryGetValue(row, out var cells))
                return string.Empty;

            return cells.TryGetValue(column, out var value) ? value.Trim() : string.Empty;
        }

        private static double GetDouble(ExcelSheet sheet, int row, int column)
        {
            var nullable = GetNullableDouble(sheet, row, column);
            return nullable ?? 0;
        }

        private static double? GetNullableDouble(ExcelSheet sheet, int row, int column)
        {
            var text = GetText(sheet, row, column);
            if (string.IsNullOrWhiteSpace(text))
                return null;

            text = text.Replace("см", string.Empty, StringComparison.OrdinalIgnoreCase)
                       .Replace("руб.", string.Empty, StringComparison.OrdinalIgnoreCase)
                       .Replace("₽", string.Empty)
                       .Trim();

            if (double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out var value))
                return value;
            if (double.TryParse(text, NumberStyles.Any, RuCulture, out value))
                return value;
            if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                return value;

            return null;
        }

        private static int GetInt(ExcelSheet sheet, int row, int column, int defaultValue = 0)
        {
            var value = GetNullableDouble(sheet, row, column);
            return value == null ? defaultValue : Convert.ToInt32(Math.Round(value.Value));
        }

        private static DateTime? GetDate(ExcelSheet sheet, int row, int column)
        {
            var text = GetText(sheet, row, column);
            if (string.IsNullOrWhiteSpace(text))
                return null;

            if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var number) && number > 0)
            {
                try
                {
                    return DateTime.FromOADate(number);
                }
                catch
                {
                    
                }
            }

            var formats = new[]
            {
                "dd.MM.yyyy", "dd.MM.yyyy HH:mm", "dd.MM.yyyy HH:mm:ss",
                "yyyy-MM-dd", "yyyy-MM-dd HH:mm", "yyyy-MM-dd HH:mm:ss",
                "d.M.yyyy", "d.M.yyyy H:mm", "d.M.yyyy H:mm:ss"
            };

            if (DateTime.TryParseExact(text, formats, RuCulture, DateTimeStyles.None, out var exactDate))
                return exactDate;
            if (DateTime.TryParse(text, RuCulture, DateTimeStyles.None, out var ruDate))
                return ruDate;
            if (DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out var currentDate))
                return currentDate;
            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var invariantDate))
                return invariantDate;

            return null;
        }

        private static bool IsYes(string text)
        {
            return text.Equals("Да", StringComparison.OrdinalIgnoreCase) ||
                   text.Equals("True", StringComparison.OrdinalIgnoreCase) ||
                   text.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                   text.Equals("Активна", StringComparison.OrdinalIgnoreCase) ||
                   text.Equals("Активен", StringComparison.OrdinalIgnoreCase);
        }

        private static string? ToNull(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }

        private static bool IsSummaryRow(ExcelSheet sheet, int row)
        {
            var firstCellText = GetText(sheet, row, 1);
            return firstCellText.StartsWith("Всего", StringComparison.OrdinalIgnoreCase);
        }

        private static string FormatDate(DateTime? value)
        {
            return value == null ? string.Empty : value.Value.ToString("dd.MM.yyyy", RuCulture);
        }

        private static void WriteWorkbook(string filePath, params ExcelSheetExport[] sheets)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);

            using var document = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook);
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();
            var workbookSheets = workbookPart.Workbook.AppendChild(new Sheets());

            uint sheetId = 1;
            foreach (var sheet in sheets)
            {
                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                var sheetData = new SheetData();
                worksheetPart.Worksheet = new Worksheet(sheetData);

                AppendRow(sheetData, 1, sheet.Headers.Cast<object?>().ToArray());

                var rowIndex = 2;
                foreach (var row in sheet.Rows)
                {
                    AppendRow(sheetData, (uint)rowIndex, row);
                    rowIndex++;
                }

                if (!string.IsNullOrWhiteSpace(sheet.SummaryLabel))
                {
                    rowIndex++;
                    AppendRow(sheetData, (uint)rowIndex, sheet.SummaryLabel, sheet.Rows.Count);
                }

                worksheetPart.Worksheet.Save();

                workbookSheets.Append(new Sheet
                {
                    Id = workbookPart.GetIdOfPart(worksheetPart),
                    SheetId = sheetId++,
                    Name = SanitizeSheetName(sheet.Name)
                });
            }

            workbookPart.Workbook.Save();
        }

        private static void AppendRow(SheetData sheetData, uint rowIndex, params object?[] values)
        {
            var row = new Row { RowIndex = rowIndex };
            for (var i = 0; i < values.Length; i++)
                row.Append(CreateCell(values[i], i + 1, rowIndex));
            sheetData.Append(row);
        }

        private static Cell CreateCell(object? value, int columnIndex, uint rowIndex)
        {
            var cell = new Cell { CellReference = GetColumnName(columnIndex) + rowIndex };
            if (value == null)
            {
                cell.DataType = CellValues.InlineString;
                cell.InlineString = new InlineString(new Text(string.Empty));
                return cell;
            }

            if (value is int or long or float or double or decimal)
            {
                cell.DataType = CellValues.Number;
                cell.CellValue = new CellValue(Convert.ToString(value, CultureInfo.InvariantCulture) ?? "0");
                return cell;
            }

            cell.DataType = CellValues.InlineString;
            cell.InlineString = new InlineString(new Text(Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty));
            return cell;
        }

        private static List<ExcelSheet> ReadWorkbook(string filePath)
        {
            using var document = SpreadsheetDocument.Open(filePath, false);
            var workbookPart = document.WorkbookPart ?? throw new InvalidOperationException("Не удалось открыть книгу Excel.");
            var sharedStrings = workbookPart.SharedStringTablePart?.SharedStringTable;
            var result = new List<ExcelSheet>();

            var sheets = workbookPart.Workbook.Sheets?.Elements<Sheet>() ?? Enumerable.Empty<Sheet>();
            foreach (var sheetInfo in sheets)
            {
                var relationshipId = sheetInfo.Id?.Value;
                if (string.IsNullOrWhiteSpace(relationshipId))
                    continue;

                var worksheetPart = (WorksheetPart)workbookPart.GetPartById(relationshipId);
                var sheet = new ExcelSheet(sheetInfo.Name?.Value ?? string.Empty);
                var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
                if (sheetData == null)
                {
                    result.Add(sheet);
                    continue;
                }

                foreach (var row in sheetData.Elements<Row>())
                {
                    var rowIndex = (int)(row.RowIndex?.Value ?? 0);
                    if (rowIndex <= 0)
                        rowIndex = sheet.Rows.Count + 1;

                    var cells = new Dictionary<int, string>();
                    var currentColumn = 1;
                    foreach (var cell in row.Elements<Cell>())
                    {
                        var columnIndex = GetColumnIndex(cell.CellReference?.Value);
                        if (columnIndex <= 0)
                            columnIndex = currentColumn;

                        cells[columnIndex] = ReadCellValue(cell, sharedStrings);
                        currentColumn = columnIndex + 1;
                    }

                    sheet.Rows[rowIndex] = cells;
                }

                result.Add(sheet);
            }

            return result;
        }

        private static string ReadCellValue(Cell cell, SharedStringTable? sharedStrings)
        {
            var rawValue = cell.CellValue?.Text ?? string.Empty;

            if (cell.DataType == null)
                return rawValue;

            if (cell.DataType == CellValues.SharedString)
            {
                if (int.TryParse(rawValue, out var sharedStringIndex))
                    return sharedStrings?.Elements<SharedStringItem>().ElementAtOrDefault(sharedStringIndex)?.InnerText ?? string.Empty;
                return string.Empty;
            }

            if (cell.DataType == CellValues.InlineString)
                return cell.InlineString?.InnerText ?? string.Empty;

            if (cell.DataType == CellValues.Boolean)
                return rawValue == "1" ? "Да" : "Нет";

            return rawValue;
        }

        private static string GetColumnName(int columnIndex)
        {
            var dividend = columnIndex;
            var columnName = string.Empty;

            while (dividend > 0)
            {
                var modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar('A' + modulo) + columnName;
                dividend = (dividend - modulo) / 26;
            }

            return columnName;
        }

        private static int GetColumnIndex(string? cellReference)
        {
            if (string.IsNullOrWhiteSpace(cellReference))
                return 0;

            var columnName = new string(cellReference.TakeWhile(char.IsLetter).ToArray()).ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(columnName))
                return 0;

            var sum = 0;
            foreach (var c in columnName)
            {
                sum *= 26;
                sum += c - 'A' + 1;
            }

            return sum;
        }

        private static string SanitizeSheetName(string name)
        {
            var invalidChars = new[] { ':', '\\', '/', '?', '*', '[', ']' };
            var sanitized = string.Concat(name.Select(c => invalidChars.Contains(c) ? '_' : c));
            if (string.IsNullOrWhiteSpace(sanitized))
                sanitized = "Лист";
            return sanitized.Length <= 31 ? sanitized : sanitized[..31];
        }

        private sealed class ExcelSheetExport
        {
            public ExcelSheetExport(string name, string[] headers, string summaryLabel)
            {
                Name = name;
                Headers = headers;
                SummaryLabel = summaryLabel;
            }

            public string Name { get; }
            public string[] Headers { get; }
            public string SummaryLabel { get; }
            public List<object?[]> Rows { get; } = new();
        }

        private sealed class ExcelSheet
        {
            public ExcelSheet(string name)
            {
                Name = name;
            }

            public string Name { get; }
            public Dictionary<int, Dictionary<int, string>> Rows { get; } = new();
            public int LastRow => Rows.Keys.DefaultIfEmpty(0).Max();
        }
    }
}
