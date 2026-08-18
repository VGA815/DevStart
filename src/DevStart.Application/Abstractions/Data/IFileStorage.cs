namespace DevStart.Application.Abstractions.Data
{
    public interface IFileStorage
    {
        Task UploadAsync(
            string objectKey,
            Stream data,
            string bucket,
            string contentType,
            CancellationToken cancellationToken);

        Task<Stream> DownloadAsync(
            string objectName,
            string bucket,
            CancellationToken cancellationToken);

        Task DeleteAsync(
            string objectKey,
            string bucket,
            CancellationToken cancellationToken);

        /// <param name="downloadFileName">
        /// When set, the presigned URL asks storage to serve the object as an attachment under this
        /// name. Without it the browser names the saved file after the object key, which is the same
        /// for every deal — several downloaded term sheets would all be called "term-sheet.pdf".
        /// ASCII only: the name travels in a Content-Disposition header through a signed query string.
        /// </param>
        Task<string> GetPresignedUrl(
            string objectKey,
            string bucket,
            int expirySeconds,
            CancellationToken cancellationToken,
            string? downloadFileName = null);
    }
}
