namespace DevStart.Application.DealDocuments.Generation
{
    /// <summary>
    /// Renders a <see cref="TermSheetModel"/> into the markdown term sheet by filling the
    /// instrument's template from storage. Owns all formatting of the values it is handed.
    /// </summary>
    public interface ITermSheetMarkdownRenderer
    {
        Task<string> RenderAsync(TermSheetModel model, CancellationToken cancellationToken);
    }
}
