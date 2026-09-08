namespace Forneria.Demo.Pages.Pages.Showcase;

public partial class DataGridEmptyStateExample
{
    private int _emptyDemoState;
    private void ShowData() => _emptyDemoState = 0;
    private void ShowEmpty() => _emptyDemoState = 1;
    private void ShowLoading() => _emptyDemoState = 2;
    private void OnEmptyCtaClick() => Toast.Success("Demo", "Botão CTA do EmptyTemplate funciona!");
}
