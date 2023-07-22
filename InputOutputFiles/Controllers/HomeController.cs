
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Net.Http.Headers;
using InputOutputFiles.Models;

namespace InputOutputFiles.Controllers
{
    public class HomeController : Controller
    {
        
        private const string API_KEY = "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";
        private static readonly HttpClient client = new HttpClient();
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }


        public IActionResult Index()
        {
            var model = new InputOutputModel();
            if (HttpContext.Session.TryGetValue("GeneratedResponse", out var generatedResponse))
            {
                model.Response = System.Text.Encoding.UTF8.GetString(generatedResponse);
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create (InputOutputModel model, IFormFile file)
        {
            try
            {
                // Read the content of the uploaded file
                string fileContent;
                using (var reader = new StreamReader(file.OpenReadStream()))
                {
                    fileContent = await reader.ReadToEndAsync();
                }

                // Prepare the data for OpenAI API request
                var options = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = "File Analysis"
                        },
                        new
                        {
                            role = "user",
                            content = $" {model.Prompt} from file content\nFile Content:\n{fileContent} note: give only response which only has result not process and no introduction only result"
                        }
                    },
                    max_tokens = 3500,
                    temperature = 0.2
                };

                var json = JsonConvert.SerializeObject(options);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", API_KEY);

                // Send request to OpenAI API for text analysis
                var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();

                dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);
                string result = jsonResponse.choices[0].message.content;

                // Assign the output result to the model
                model.Response = result;

                // Save the response to Session for displaying it in the Index view
                HttpContext.Session.Set("GeneratedResponse", Encoding.UTF8.GetBytes(model.Response));

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Handle any exceptions
                Debug.WriteLine(ex.Message);
                return Content("An error occurred during file analysis.");
            }
        }

        public IActionResult DownloadResponse()
        {
            // Check if the response is available in Session
            if (HttpContext.Session.TryGetValue("GeneratedResponse", out var generatedResponse))
            {
                var responseFileName = $"{Guid.NewGuid().ToString()}.txt";

                // Set the Content-Disposition header to specify the file name for downloading
                var contentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileNameStar = responseFileName,
                    FileName = responseFileName
                };
                Response.Headers.Add(HeaderNames.ContentDisposition, contentDisposition.ToString());

                // Create a stream from the response bytes and return it as a FileStreamResult
                var responseStream = new MemoryStream(generatedResponse);
                return new FileStreamResult(responseStream, "text/plain");
            }

            return Content("Response not found.");
        }
    }
}
