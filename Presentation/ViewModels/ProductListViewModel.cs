//using CommunityToolkit.Mvvm.ComponentModel;
//using Presentation.Interfaces;
//using System.Collections.ObjectModel;

//namespace Presentation.ViewModels;

//public partial class ProductListViewModel : ObservableObject
//{
//    private readonly INavigationService _navigationService;
//    private readonly IProductService _productService;
//    [ObservableProperty]
//    // ObservableCollection = en lista med extra funktionalitet --> implementerar INotifyCollectionChanged (signalerar när innehållet i samlingen förändras) och INotifyPropertyChanged (signalerar när propertyn byts ut mot en ny instans - propertyn pekar på en ny lista)
//    private ObservableCollection<Product> _productList = [];

//    // Fälten får automatiskt rätt värde direkt när objektet skapas. Iom konstruktorn ej hårdkodat och utbytbart
//    public ProductListViewModel(INavigationService navigationService, IProductService productService)
//    {
//        _navigationService = navigationService;
//        _productService = productService;

//        // ctor kan inte vara async
//        Task.Run
//    }



//}
///* 
//Skapa LoadAsync() i din ViewModel.

//Anropa den efter att ViewModeln har skapats, skriv extra kod för laddning av viewmodeln t.ex. i MainViewModel när du navigerar dit, eller i View när den laddas.
//*/


