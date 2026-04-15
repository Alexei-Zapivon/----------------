using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lab3_MVVM_Variant9.Models;
using Lab3_MVVM_Variant9.Services;

namespace Lab3_MVVM_Variant9.ViewModels;

// ViewModel списка документов
public partial class DocumentsViewModel : ObservableValidator
{
    private readonly DocumentService _service;
    private CancellationTokenSource? _searchCts;

    public ObservableCollection<Document> Documents { get; } = new();

    // ─── Форма добавления ───────────────────────────────────

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Название обязательно")]
    [MinLength(3, ErrorMessage = "Минимум 3 символа")]
    private string _newTitle = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Сумма обязательна")]
    private string _newAmountText = string.Empty;

    [ObservableProperty]
    private string _newDescription = string.Empty;

    [ObservableProperty]
    private int _newTypeIndex = 0; // 0=Квитанция, 1=Накладная, 2=Счёт

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _validationError = string.Empty;

    public DocumentsViewModel(DocumentService service)
    {
        _service = service;
        LoadDocuments();
    }

    private void LoadDocuments()
    {
        Documents.Clear();
        foreach (var d in _service.GetAll())
            Documents.Add(d);
    }

    // Поиск с задержкой (debouncing) — ждём 400мс после последнего нажатия
    partial void OnSearchTextChanged(string value)
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        Task.Delay(400, token).ContinueWith(_ =>
        {
            if (token.IsCancellationRequested) return;
            App.Current.Dispatcher.Invoke(() =>
            {
                Documents.Clear();
                foreach (var d in _service.Search(value))
                    Documents.Add(d);
            });
        }, token);
    }

    // Валидация суммы в реальном времени
    partial void OnNewAmountTextChanged(string value)
    {
        if (!decimal.TryParse(value, out var amount) || amount <= 0)
            ValidationError = "Введите корректную сумму (больше 0)";
        else
            ValidationError = string.Empty;
    }

    [RelayCommand]
    private void AddDocument()
    {
        ValidateAllProperties();
        if (HasErrors || !string.IsNullOrEmpty(ValidationError)) return;

        if (!decimal.TryParse(NewAmountText, out var amount)) return;

        Document doc = NewTypeIndex switch
        {
            0 => new Receipt { PayerName = "Новый плательщик", ReceiverName = "Получатель", PaymentMethod = "Карта", IsPaid = false },
            1 => new Invoice { SupplierName = "Поставщик", BuyerName = "Покупатель", DeliveryDate = DateTime.Now.AddDays(3), ItemsCount = 1, ShippingAddress = "Адрес" },
            2 => new Bill    { CustomerName = "Клиент", DueDate = DateTime.Now.AddDays(30), TaxAmount = Math.Round(amount * 0.20m, 2), IsOverdue = false, BankAccount = "40702810000000000000" },
            _ => throw new InvalidOperationException()
        };

        doc.Title = NewTitle;
        doc.Amount = amount;
        doc.Description = NewDescription;
        doc.CreatedAt = DateTime.Now;

        _service.Add(doc);
        Documents.Add(doc);

        // Сбрасываем форму
        NewTitle = string.Empty;
        NewAmountText = string.Empty;
        NewDescription = string.Empty;
    }

    [RelayCommand]
    private void DeleteDocument(Document doc)
    {
        if (_service.Delete(doc.Id))
            Documents.Remove(doc);
    }
}
