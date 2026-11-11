namespace ATMLibrary
{
    /// <summary>
    /// Власний тип делегата для подій банкомата.
    /// Будь-який метод із сигнатурою void (object, AtmEventArgs)
    /// може бути підписаний на події класу <see cref="AutomatedTellerMachine"/>.
    /// </summary>
    /// <param name="sender">Банкомат, що згенерував подію.</param>
    /// <param name="e">Дані про операцію.</param>
    public delegate void AtmEventHandler(object sender, AtmEventArgs e);
}
