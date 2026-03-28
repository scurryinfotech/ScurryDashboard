using Microsoft.Extensions.Configuration;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace YourAppNamespace.Services
{
    public class TwilioService
    {
        private readonly string _accountSid;
        private readonly string _authToken;
        private readonly string _fromPhoneNumber;

        // Constructor to get values from appsettings.json
        public TwilioService(IConfiguration configuration)
        {
            _accountSid = configuration["Twilio:AccountSid"];
            _authToken = configuration["Twilio:AuthToken"];
            _fromPhoneNumber = configuration["Twilio:FromPhoneNumber"];
        }

        // Function to send SMS
        public bool SendSms(string toPhoneNumber, string messageText)
        {
            try
            {
                TwilioClient.Init(_accountSid, _authToken);

                var message = MessageResource.Create(
                    to: new PhoneNumber(toPhoneNumber),
                    from: new PhoneNumber(_fromPhoneNumber),
                    body: messageText
                );

                return message.ErrorCode == null; // true = sent successfully
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Twilio Error: {ex.Message}");
                return false;
            }
        }
    }
}
