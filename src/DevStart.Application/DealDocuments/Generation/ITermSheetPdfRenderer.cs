namespace DevStart.Application.DealDocuments.Generation
{
    /// <summary>
    /// Renders a <see cref="TermSheetModel"/> into a PDF. Deterministic: the same model always
    /// produces the same bytes, so the stored hash identifies the document's content rather than the
    /// moment it happened to be produced.
    /// </summary>
    public interface ITermSheetPdfRenderer
    {
        byte[] Render(TermSheetModel model);
    }
}
