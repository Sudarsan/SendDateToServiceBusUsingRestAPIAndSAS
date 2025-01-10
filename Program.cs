using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;

class Program
{
    static async Task Main()
    {
        // Replace with your Service Bus details
        string serviceNamespace = "Add the Service Bus Name Space Here";
        string topicName = "The Topic/Queue Name goes Here";
        string messageBody = "This is a message.";

        // Create the HTTP client
        using HttpClient client = new();
        // Set the request URI
        string requestUri = $"https://{serviceNamespace}.servicebus.windows.net/{topicName}/messages?timeout=60";
        string sasToken = GetSasToken(requestUri, "Add Shared Access Policy Key Name", "Add Shared Access Policy Key Value", TimeSpan.FromDays(1));
        // Create the message content with corrected content type
        StringContent content = new(messageBody, Encoding.UTF8, "application/xml");

        // Add required headers
        client.DefaultRequestHeaders.Add("Authorization", sasToken);
        string messageId = Guid.NewGuid().ToString();

        //Setting the broker properties
        Dictionary<string, string> brokerProperties = new()
        {
            { "CorrelationId", messageId },
            { "SessionId", "123" }
        };
        // Adding the BrokerProperties header with SessionId
        client.DefaultRequestHeaders.Add("BrokerProperties", JsonSerializer.Serialize(brokerProperties));

        //Setting the Custom Properties
        client.DefaultRequestHeaders.Add("message_type", "My test");
        client.DefaultRequestHeaders.Add("message_id", messageId);
        client.DefaultRequestHeaders.ExpectContinue = true;

        // Service Bus Endpoint Call
        HttpResponseMessage response = await client.PostAsync(requestUri, content);

        // Check the response
        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Message sent successfully with status code: {response.StatusCode}");
        }
        else
        {
            Console.WriteLine($"Failed to send message. Status code: {response.StatusCode}, Reason: {response.ReasonPhrase}");
        }
    }

    public static string GetSasToken(string resourceUri, string keyName, string key, TimeSpan ttl)
    {
        var expiry = GetExpiry(ttl);
        string stringToSign = HttpUtility.UrlEncode(resourceUri) + "\n" + expiry;
        HMACSHA256 hmac = new(Encoding.UTF8.GetBytes(key));
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
        var sasToken = string.Format(CultureInfo.InvariantCulture, "SharedAccessSignature sr={0}&sig={1}&se={2}&skn={3}",
        HttpUtility.UrlEncode(resourceUri), HttpUtility.UrlEncode(signature), expiry, keyName);
        return sasToken;
    }

    private static string GetExpiry(TimeSpan ttl)
    {
        TimeSpan expirySinceEpoch = DateTime.UtcNow - new DateTime(1970, 1, 1) + ttl;
        return Convert.ToString((int)expirySinceEpoch.TotalSeconds);
    }
}