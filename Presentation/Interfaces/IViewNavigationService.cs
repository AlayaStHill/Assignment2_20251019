using CommunityToolkit.Mvvm.ComponentModel;

namespace Presentation.Interfaces; 
public interface IViewNavigationService
{
    void NavigateTo<TViewModel>() where TViewModel : ObservableObject;
}


/* 
DI-containern via ctor så att man kan använda _serviceProviderGetRequiredService för att plocka ut dependencies
Med IServiceProvider i konstruktorn: hela DI-containern blir klassens beroende även om klassen bara använder lite. Man måste då bygga en DI-container i testet.
Om man ger klassen bara det den behöver, t.ex IProductService, INavigationService kan man mocka just dessa beroenden i testning.
*/