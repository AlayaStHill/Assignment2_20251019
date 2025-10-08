using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Interfaces;
using Presentation.ViewModels;

namespace Presentation.Services;

public class ViewNavigationService : IViewNavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly MainViewModel _mainViewModel;

    public ViewNavigationService(IServiceProvider serviceProvider, MainViewModel mainViewModel)
    {
        _serviceProvider = serviceProvider;
        _mainViewModel = mainViewModel;
    }

    public void NavigateTo<TViewModel>(Action<TViewModel>? configure = null) where TViewModel : ObservableObject
    {
        // Hämta ViewModel från DI-containern
        TViewModel viewModel = _serviceProvider.GetRequiredService<TViewModel>();
        // eventuell konfiguration
        configure?.Invoke(viewModel);

        // Byt aktiv vy i MainViewModel
        _mainViewModel.CurrentViewModel = viewModel;
    }
}

/*
(Action<TViewModel>? configure = null), används för att slippa injicera ett beroende av IserviceProvider i klassen. Parametern configure är en metodparameter till NavigateTo() 
av typen Action<TViewModel>?. configure representerar alltså en metod som ska utföras på den nya ViewModel-instansen innan den visas?? = innan dess korresponderande vy visas i UI??
När man anropar NavigateTo och använder configure, görs det i form av ett lambdauttryck.

 Med configure:
 _viewNavigationService.NavigateTo<ProductEditViewModel>(viewmodel => viewmodel.SetProduct(product));

 Utan:
_viewNavigationService.NavigateTo<ProductListViewModel>();


*/