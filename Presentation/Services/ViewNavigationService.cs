using CommunityToolkit.Mvvm.ComponentModel;
using Presentation.Interfaces;

namespace Presentation.Services;

public class ViewNavigationService : IViewNavigationService
{
    public void NavigateTo<TViewModel>() where TViewModel : ObservableObject
    {
        throw new NotImplementedException();
    }
}