namespace Antlr.Runtime
{
    /// <summary>
    /// Preserves the non-generic parser return-scope type exposed by the legacy
    /// NCalc package while the embedded ANTLR runtime uses the newer generic base.
    /// </summary>
    public class ParserRuleReturnScope : ParserRuleReturnScope<IToken>
    {
    }
}
