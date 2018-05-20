using System.Windows;

namespace CryptoRtd.MVVM
{
    public interface IMessageBoxService
    {
        MessageBoxResult ShowMessage(string text, string caption, MessageBoxButton messageButtons, MessageBoxImage messageIcon);
    }
}
