using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Controls;

namespace Presentation.ViewModels;
// Har all funktionalitet som styr MainWindow, ex. knappar som byter vy (hantera navigeringen mellan vyerna) och laddar innehåll. 
// Install-Package CommunityToolkit.Mvvm
public partial class MainViewModel : ObservableObject 
{
    // Skrivs ovanför en property för att kunna binda den till UI och uppdatera det. Genererar automatiskt propertyn get, set --> SetProperty bakom kulisserna..
    [ObservableProperty]
    // Property som bestämmer vilken vy (ViewModel) som visas i MainWindow. Ändring anropar setproperty-kedjan.
    private ObservableObject _currentViewModel = null!; 
}






/*
ObservableObject-klassen från CommunityToolkit implementerar INotifyPropertyChanged, som innehåller eventet PropertyChanged 
och metoderna OnPropertyChanged + SetProperty. Tillsammans ser de till att UI uppdateras när en property ändras.

I en klass som ärver från ObservableObject kan man använda attributet [ObservableProperty] (metadata) ovanpå ett fält. 
Det genererar automatiskt en property vars set-del anropar SetProperty.

När SetProperty körs:
- uppdateras fältet
- OnPropertyChanged anropas (hjälpmetod för att trigga PropertyChanged)
- PropertyChanged signalerar via {Binding} till View:n
--> UI uppdateras automatiskt.
*/

/*
MainWindow = huvudfönstret. MainViewModel = styr innehållet i huvudfönstret. MainWindow ska visa olika vyer beroende av vad som händer i MainViewModel. 
I MainWindow.XAML knyts de två ihop via d:DataContext="{d:DesignInstance Type=viewmodels:MainViewModel}" = MainViewModel blir DataContext för MainWindow i designläget. 
DataContext = new MainViewModel(); kopplar vyn till den riktiga ViewModel-instansen i körläge. Skrivs i MainWidow vart???

 <ContentControl Content="{Binding CurrentView}" />.
*/