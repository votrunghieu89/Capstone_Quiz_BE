using Capstone.DTOs.Auth;
using Capstone.Repositories;
using Capstone.Settings;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;

namespace Capstone.Services
{
    public class GoogleService : IGoogleService
    {
        private readonly GoogleSetting _googleSetting;
        private readonly ILogger<GoogleService> _logger;
        
        public GoogleService(IOptions<GoogleSetting> options, ILogger<GoogleService> logger)
        {
            _googleSetting = options.Value;
            _logger = logger;
        }
        
        public async Task<GoogleResponse> checkIdToken(string idToken)
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken,
                    new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new[] { _googleSetting.ClientId }
                    });
            if(payload == null)
            {
                _logger.LogError("Invalid ID token.");
                return null;
            }
            GoogleResponse googleResponse = new GoogleResponse
            {
                Email = payload.Email,
                Name = payload.Name,
                AvartarURL = payload.Picture,
            };
            return googleResponse;
        }
    }
}
