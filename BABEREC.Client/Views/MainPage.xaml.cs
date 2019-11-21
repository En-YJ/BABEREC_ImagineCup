using System;

using BABEREC.Client.ViewModels;

using Windows.UI.Xaml.Controls;

namespace BABEREC.Client.Views
{
    public sealed partial class MainPage : Page
    {
        public MainViewModel ViewModel { get; } = new MainViewModel();

        public MainPage()
        {
            InitializeComponent();
        }
    }
}
