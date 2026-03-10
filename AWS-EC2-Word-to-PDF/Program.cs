using Amazon.S3;
using Amazon;
using Amazon.S3.Model;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocIORenderer;
using Syncfusion.Pdf;
using Syncfusion.DocIO;


namespace WordtoPDF
{
    class Program
    {
        private static readonly RegionEndpoint bucketRegion = RegionEndpoint.USEast1;
        private static IAmazonS3 s3Client;
        static async Task Main()
        {
            
            var credentials = new Amazon.Runtime.BasicAWSCredentials("AKIASCRGFNWJGTKKK3GJ", "SSsROv24+PwSKeGxMig8dyXqMoJK8XiK6ptvE5kh");
            var config = new AmazonS3Config
            {
                RegionEndpoint = bucketRegion
            };
            s3Client = new AmazonS3Client(credentials, config);
            Console.WriteLine("Kindly enter the S3 bucket name: ");
            string bucketName = Console.ReadLine();
            Console.WriteLine("Kindly enter the input folder name that has the input Word Document: ");
            string inputFolderName = Console.ReadLine();
            Console.WriteLine("Kindly enter the output folder name in which the output Pdf should be stored: ");
            string outputFolderName = Console.ReadLine();
            //Gets the list of imput files from the input folder.
            List<string> inputFileNames = await ListFilesAsync(inputFolderName, bucketName);

            for (int i = 0; i < inputFileNames.Count; i++)
            {
                //Converts PPTX to Image.
                await ConvertWordtoPDF(inputFileNames[i], inputFolderName, bucketName, outputFolderName);
            }
        }
        private static async Task<List<string>> ListFilesAsync(string inputFolderName, string bucketName)
        {
            List<string> files = new List<string>();
            try
            {
                var request = new ListObjectsV2Request
                {
                    BucketName = bucketName,
                    Prefix = $"{inputFolderName}/",
                    Delimiter = "/"
                };
                ListObjectsV2Response response;
                do
                {
                    response = await s3Client.ListObjectsV2Async(request);
                    foreach (S3Object entry in response.S3Objects)
                    {
                        // Skip the "folder" itself
                        if (entry.Key.EndsWith("/"))
                            continue;

                        // Extract the filename by removing the folder path prefix
                        string fileName = entry.Key.Substring(inputFolderName.Length);
                        files.Add(fileName);
                    }
                    request.ContinuationToken = response.NextContinuationToken;
                } while (response.IsTruncated);
                return files;
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine("Error encountered on server. Message:'{0}' when listing objects", e.Message);
                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine("Unknown encountered on server. Message:'{0}' when listing objects", e.Message);
                return null;
            }
        }
        static async Task ConvertWordtoPDF(string inputFileName, string inputFolderName, string bucketName, string outputFolderName)
        {
            try
            {
                //Download the file from S3 into the MemoryStream
                var response = await s3Client.GetObjectAsync(new Amazon.S3.Model.GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = inputFolderName + inputFileName
                });
                using (Stream responseStream = response.ResponseStream)
                {
                    MemoryStream fileStream = new MemoryStream();
                    await responseStream.CopyToAsync(fileStream);
                    fileStream.Position = 0;
                    //Open the existing Word document.
                    using (WordDocument document = new WordDocument(fileStream, Syncfusion.DocIO.FormatType.Automatic))
                    {
                        //Initialize DocIORenderer.
                        DocIORenderer renderer = new DocIORenderer();
                        PdfDocument pdfDocument = renderer.ConvertToPDF(document);
                        MemoryStream outputStream = new MemoryStream();
                        pdfDocument.Save(outputStream);
                        // reset before upload
                        outputStream.Position = 0;
                        //Uploads the pdf to the S3 bucket.
                        await UploadPdfAsync(outputStream, $"{Path.GetFileNameWithoutExtension(inputFileName)}" + ".pdf", bucketName, outputFolderName);

                    }
                }
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine($"Error encountered on server. Message:'{e.Message}'");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unknown error encountered. Message:'{e.Message}'");
            }
        }
        public static async Task UploadPdfAsync(Stream pdfStream, string outputFileName, string bucketName, string outputFolderName)
        {
            try
            {
                var key = $"{outputFolderName}/{outputFileName}"; // e.g., "pdf/output.pdf"

                var request = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = key,
                    InputStream = pdfStream,
                    ContentType = "application/pdf"
                };
                var response = await s3Client.PutObjectAsync(request);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    Console.WriteLine("PDF uploaded successfully.");
                }
                else
                {
                    Console.WriteLine($"Failed to upload PDF. HTTP Status Code: {response.HttpStatusCode}");
                }
            }
            catch (AmazonS3Exception ex)
            {
                Console.WriteLine($"Error encountered on server. Message:'{ex.Message}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unknown error encountered. Message:'{ex.Message}'");
            }
        }
    }

}
