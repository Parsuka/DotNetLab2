namespace ATMLibrary
{
    /// <summary>Тип операції, яку виконує банкомат.</summary>
    public enum OperationType
    {
        CardInserted,
        Authentication,
        BalanceCheck,
        Withdrawal,
        Deposit,
        Transfer,
        SessionEnd
    }
}
