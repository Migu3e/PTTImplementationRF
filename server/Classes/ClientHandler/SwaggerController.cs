using Microsoft.AspNetCore.Mvc;
using server.ClientHandler.ClientDatabase;
using server.ClientHandler.ChannelDatabase;
using server.ClientHandler.VolumeDatabase;
using server.ClientHandler.FrequencyDatabase;
using System.Threading.Tasks;

namespace server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SwaggerController : ControllerBase
    {
        private readonly AccountService _accountService;
        private readonly ChannelService _channelService;
        private readonly VolumeService _volumeService;
        private readonly FrequencyService _frequencyService;

        public SwaggerController(
            AccountService accountService,
            ChannelService channelService,
            VolumeService volumeService,
            FrequencyService frequencyService)
        {
            _accountService = accountService;
            _channelService = channelService;
            _volumeService = volumeService;
            _frequencyService = frequencyService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] ClientModel clientModel)
        {
            if (clientModel == null || string.IsNullOrEmpty(clientModel.ClientID) || 
                string.IsNullOrEmpty(clientModel.Password) || !System.Enum.IsDefined(typeof(ClientType), clientModel.Type))
            {
                return BadRequest(new { message = "Error in the registration data" });
            }

            var existingAccount = await _accountService.GetAccount(clientModel.ClientID);
            if (existingAccount != null)
            {
                return Conflict(new { message = "Personal Number already in the system" });
            }

            await _accountService.CreateAccount(clientModel);

            var frequencyRange = await _frequencyService.GetFrequencyRange(clientModel.Type);
            if (frequencyRange == null)
            {
                return BadRequest(new { message = "Invalid client type" });
            }

            await _channelService.AddChannelInfo(clientModel.ClientID, 1, frequencyRange.MinFrequency);
            await _volumeService.AddVolume(clientModel.ClientID, 50);

            return StatusCode(201, new { message = "Registration successful" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] ClientModel clientModel)
        {
            if (clientModel == null || string.IsNullOrEmpty(clientModel.ClientID) || string.IsNullOrEmpty(clientModel.Password))
            {
                return BadRequest(new { message = "Invalid login data" });
            }

            var isValid = await _accountService.ValidateCredentials(clientModel.ClientID, clientModel.Password);
            if (isValid)
            {
                var account = await _accountService.GetAccount(clientModel.ClientID);
                var channelInfo = await _channelService.GetChannelInfo(clientModel.ClientID);
                var volume = await _volumeService.GetLastVolume(clientModel.ClientID);
                var frequencyRange = await _frequencyService.GetFrequencyRange(account.Type);

                var responseData = new
                {
                    message = "Login successful",
                    clientId = account.ClientID,
                    type = account.Type,
                    channel = channelInfo?.Channel ?? 1,
                    frequency = channelInfo?.Frequency ?? 30.0000,
                    volume = volume,
                    minFrequency = frequencyRange?.MinFrequency,
                    maxFrequency = frequencyRange?.MaxFrequency
                };

                return Ok(responseData);
            }
            else
            {
                return Unauthorized(new { message = "Password or username are incorrect" });
            }
        }
    }
}