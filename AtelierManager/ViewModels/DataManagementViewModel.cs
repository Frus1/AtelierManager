using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using AtelierManager.Services;
using Microsoft.Win32;

namespace AtelierManager.ViewModels
{
    public class BackupFileItem
    {
        public required string FileName { get; init; }
        public required string FullPath { get; init; }
        public required string DisplayName { get; init; }
        public required string Description { get; init; }
        public DateTime CreatedAt { get; init; }
        public long SizeBytes { get; init; }
        public string SizeText => SizeBytes < 1024 * 1024
            ? $"{SizeBytes / 1024.0:0.##} КБ"
            : $"{SizeBytes / 1024.0 / 1024.0:0.##} МБ";
    }

    public class DataManagementViewModel : BaseViewModel
    {
        public ObservableCollection<BackupFileItem> Backups { get; } = new();

        private BackupFileItem? _selectedBackup;
        public BackupFileItem? SelectedBackup
        {
            get => _selectedBackup;
            set
            {
                _selectedBackup = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedBackup));
            }
        }

        public bool HasSelectedBackup => SelectedBackup != null;

        private string _backupDirectory = DatabaseBackupService.BackupDirectory;
        public string BackupDirectory
        {
            get => _backupDirectory;
            set
            {
                _backupDirectory = value;
                OnPropertyChanged();
            }
        }

        private string _databasePath = DatabaseBackupService.DatabasePath;
        public string DatabasePath
        {
            get => _databasePath;
            set
            {
                _databasePath = value;
                OnPropertyChanged();
            }
        }

        private string _lastOperationMessage = "Готово к работе.";
        public string LastOperationMessage
        {
            get => _lastOperationMessage;
            set
            {
                _lastOperationMessage = value;
                OnPropertyChanged();
            }
        }

        public DataManagementViewModel()
        {
            RefreshBackups();
        }

        public void RefreshBackups()
        {
            Backups.Clear();

            foreach (var file in DatabaseBackupService.GetBackups())
            {
                Backups.Add(CreateBackupFileItem(file));
            }

            BackupDirectory = DatabaseBackupService.BackupDirectory;
            DatabasePath = DatabaseBackupService.DatabasePath;
            SelectedBackup = null;
        }

        public void CreateBackup()
        {
            try
            {
                var backupPath = DatabaseBackupService.CreateBackup();
                RefreshBackups();
                LastOperationMessage = $"Создана резервная копия: {Path.GetFileNameWithoutExtension(backupPath)}";
                UiMessages.Info($"Создана резервная копия:\n\n{backupPath}");
            }
            catch (Exception ex)
            {
                LastOperationMessage = "Не удалось создать резервную копию.";
                UiMessages.Error($"Не удалось создать резервную копию.\n\n{ex.Message}");
            }
        }

        public void RestoreSelectedBackup()
        {
            if (SelectedBackup == null)
            {
                UiMessages.Validation("Выберите резервную копию из списка.");
                return;
            }

            PrepareRestore(SelectedBackup.FullPath, SelectedBackup.DisplayName);
        }

        public void RestoreBackupFromFile()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Выберите резервную копию базы данных",
                Filter = "База данных SQLite (*.db)|*.db|Все файлы (*.*)|*.*"
            };

            if (dialog.ShowDialog() != true)
                return;

            PrepareRestore(dialog.FileName, Path.GetFileNameWithoutExtension(dialog.FileName));
        }

        public void DeleteSelectedBackup()
        {
            if (SelectedBackup == null)
            {
                UiMessages.Validation("Выберите резервную копию, которую нужно удалить.");
                return;
            }

            var deletedBackupName = SelectedBackup.DisplayName;

            if (!UiMessages.Confirm($"Удалить резервную копию?\n\n{deletedBackupName}\n\nЭто действие нельзя отменить."))
                return;

            try
            {
                DatabaseBackupService.DeleteBackup(SelectedBackup.FullPath);
                RefreshBackups();
                LastOperationMessage = $"Удалена резервная копия: {deletedBackupName}";
                UiMessages.Info("Резервная копия удалена.");
            }
            catch (Exception ex)
            {
                LastOperationMessage = "Не удалось удалить резервную копию.";
                UiMessages.Error($"Не удалось удалить резервную копию.\n\n{ex.Message}");
            }
        }

        public async void ExportClients()
        {
            await ExportAsync("Экспорт клиентов", "clients_export.xlsx", ExcelDataService.ExportClientsAsync, "клиентов");
        }

        public async void ImportClients()
        {
            await ImportAsync("Импорт клиентов", ExcelDataService.ImportClientsAsync, "клиентов");
        }

        public async void ExportMaterials()
        {
            await ExportAsync("Экспорт материалов", "materials_export.xlsx", ExcelDataService.ExportMaterialsAsync, "материалов");
        }

        public async void ImportMaterials()
        {
            await ImportAsync("Импорт материалов", ExcelDataService.ImportMaterialsAsync, "материалов");
        }

        public async void ExportServices()
        {
            await ExportAsync("Экспорт услуг", "services_export.xlsx", ExcelDataService.ExportServicesAsync, "услуг");
        }

        public async void ImportServices()
        {
            await ImportAsync("Импорт услуг", ExcelDataService.ImportServicesAsync, "услуг");
        }

        public async void ExportPersons()
        {
            await ExportAsync("Экспорт персон", "persons_export.xlsx", ExcelDataService.ExportPersonsAsync, "персон");
        }

        public async void ImportPersons()
        {
            await ImportAsync("Импорт персон", ExcelDataService.ImportPersonsAsync, "персон");
        }

        public async void ExportMeasurementSets()
        {
            await ExportAsync("Экспорт мерок", "measurements_export.xlsx", ExcelDataService.ExportMeasurementSetsAsync, "записей мерок");
        }

        public async void ImportMeasurementSets()
        {
            await ImportAsync("Импорт мерок", ExcelDataService.ImportMeasurementSetsAsync, "записей мерок");
        }

        public async void ExportOrders()
        {
            await ExportAsync("Экспорт заказов", "orders_export.xlsx", ExcelDataService.ExportOrdersAsync, "записей заказов");
        }

        public async void ImportOrders()
        {
            await ImportAsync("Импорт заказов", ExcelDataService.ImportOrdersAsync, "записей заказов");
        }

        public async void ExportAllTables()
        {
            await ExportAsync("Экспорт всех таблиц", "atelier_all_tables_export.xlsx", ExcelDataService.ExportAllTablesAsync, "записей");
        }

        private void PrepareRestore(string backupPath, string backupName)
        {
            const string confirmationText =
                "Приложение переключится на выбранную резервную копию после перезапуска.\n\n" +
                "Перед переключением текущая база будет автоматически сохранена как «Копия перед переключением». " +
                "Эту копию можно выбрать позже, если понадобится вернуться назад.\n\n" +
                "Продолжить?";

            if (!UiMessages.Confirm(confirmationText))
                return;

            try
            {
                DatabaseBackupService.PrepareRestore(backupPath);
                LastOperationMessage = $"Подготовлено переключение на копию: {backupName}";
                UiMessages.Info(
                    "Переключение подготовлено.\n\n" +
                    "Перезапустите приложение, чтобы открыть выбранную резервную копию.\n\n" +
                    "Чтобы вернуться обратно: после перезапуска выберите в списке «Копия перед переключением» и нажмите «Переключиться на выбранную»."
                );
            }
            catch (Exception ex)
            {
                LastOperationMessage = "Не удалось подготовить переключение на резервную копию.";
                UiMessages.Error($"Не удалось подготовить переключение на резервную копию.\n\n{ex.Message}");
            }
        }

        private static BackupFileItem CreateBackupFileItem(FileInfo file)
        {
            var (displayName, description) = DescribeBackup(file);

            return new BackupFileItem
            {
                FileName = file.Name,
                FullPath = file.FullName,
                DisplayName = displayName,
                Description = description,
                CreatedAt = file.LastWriteTime,
                SizeBytes = file.Length
            };
        }

        private static (string DisplayName, string Description) DescribeBackup(FileInfo file)
        {
            var fileName = file.Name;
            var createdAtText = file.LastWriteTime.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("ru-RU"));

            if (fileName.StartsWith("Копия перед переключением", StringComparison.OrdinalIgnoreCase) ||
                fileName.StartsWith("Автокопия перед восстановлением", StringComparison.OrdinalIgnoreCase) ||
                fileName.StartsWith("atelier_before_restore_", StringComparison.OrdinalIgnoreCase))
            {
                return (
                    $"Копия для возврата назад от {createdAtText}",
                    "Создана автоматически перед переключением на другую копию. Выберите её, если хотите вернуть базу, которая была до переключения."
                );
            }

            if (fileName.StartsWith("atelier_backup_", StringComparison.OrdinalIgnoreCase))
            {
                return (
                    $"Резервная копия от {createdAtText}",
                    "Создана в старой версии программы. Название файла старое, но восстановление и удаление работают как обычно."
                );
            }

            if (fileName.StartsWith("Резервная копия", StringComparison.OrdinalIgnoreCase))
            {
                return (
                    $"Резервная копия от {createdAtText}",
                    "Создана вручную. Её можно открыть через переключение или удалить, если она больше не нужна."
                );
            }

            return (
                $"Файл резервной копии от {createdAtText}",
                "Файл базы данных из папки резервных копий. Проверьте, что это нужная копия, перед переключением."
            );
        }

        private static SaveFileDialog CreateSaveDialog(string title, string defaultFileName)
        {
            return new SaveFileDialog
            {
                Title = title,
                FileName = defaultFileName,
                Filter = "Excel workbook (*.xlsx)|*.xlsx|Все файлы (*.*)|*.*"
            };
        }

        private static OpenFileDialog CreateOpenExcelDialog(string title)
        {
            return new OpenFileDialog
            {
                Title = title,
                Filter = "Excel workbook (*.xlsx)|*.xlsx|Все файлы (*.*)|*.*"
            };
        }

        private async Task ExportAsync(string title, string defaultFileName, Func<string, Task<int>> exportFunc, string objectName)
        {
            var dialog = CreateSaveDialog(title, defaultFileName);
            if (dialog.ShowDialog() != true)
                return;

            try
            {
                var count = await exportFunc(dialog.FileName);
                LastOperationMessage = $"Экспортировано {count} {objectName}.";
                UiMessages.Info($"Экспорт завершён.\n\nЭкспортировано: {count} {objectName}.\nФайл: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                LastOperationMessage = "Ошибка экспорта данных.";
                UiMessages.Error($"Не удалось выполнить экспорт.\n\n{ex.Message}");
            }
        }

        private async Task ImportAsync(string title, Func<string, Task<int>> importFunc, string objectName)
        {
            var dialog = CreateOpenExcelDialog(title);
            if (dialog.ShowDialog() != true)
                return;

            if (!UiMessages.Confirm("Данные из выбранного Excel-файла будут добавлены в базу. Уже существующие записи будут пропущены или обновлены. Продолжить?"))
                return;

            try
            {
                var count = await importFunc(dialog.FileName);
                LastOperationMessage = $"Импортировано новых записей: {count} {objectName}.";
                UiMessages.Info($"Импорт завершён.\n\nДобавлено новых записей: {count} {objectName}.");
            }
            catch (Exception ex)
            {
                LastOperationMessage = "Ошибка импорта данных.";
                UiMessages.Error($"Не удалось выполнить импорт.\n\n{ex.Message}");
            }
        }
    }
}
