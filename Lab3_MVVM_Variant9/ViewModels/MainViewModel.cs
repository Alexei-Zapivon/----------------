using CommunityToolkit.Mvvm.ComponentModel;
using Lab3_MVVM_Variant9.Services;

namespace Lab3_MVVM_Variant9.ViewModels;

// Главная ViewModel — управляет вкладками
public partial class MainViewModel : ObservableObject
{
    private readonly DocumentService _service = new();

    public DocumentsViewModel DocumentsVM { get; }
    public StatisticsViewModel StatisticsVM { get; }

    [ObservableProperty]
    private int _selectedTabIndex = 0;

    public MainViewModel()
    {
        DocumentsVM = new DocumentsViewModel(_service);
        StatisticsVM = new StatisticsViewModel(_service);

        // При переключении на вкладку статистики — обновляем графики
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SelectedTabIndex) && SelectedTabIndex == 1)
                StatisticsVM.Refresh();
        };
    }
}
