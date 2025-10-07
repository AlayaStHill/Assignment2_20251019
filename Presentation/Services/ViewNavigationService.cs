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

    public void NavigateTo<TViewModel>() where TViewModel : ObservableObject
    {
        // Hämta ViewModel från DI-containern
        TViewModel viewModel = _serviceProvider.GetRequiredService<TViewModel>();

        // Byt aktiv vy i MainViewModel
        _mainViewModel.CurrentViewModel = viewModel;
    }
}
//_viewNavigationService.NavigateTo<ProductListViewModel>(); Där ProductListViewModel är<TViewModel>
