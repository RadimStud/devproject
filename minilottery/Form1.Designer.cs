namespace WinFormsApp1;

partial class Form1
{
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            lifetime.Cancel();
            DisposeWarrior();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent() => BuildInterface();
}
