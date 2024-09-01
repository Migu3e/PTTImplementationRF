using MongoDB.Driver;
using server.ClientHandler.ClientDatabase;

namespace server.ClientHandler.ClientDatabase
{
    public class AccountService
    {
        private readonly IMongoCollection<ClientModel> _accounts;

        public AccountService(IMongoDatabase database)
        {
            _accounts = database.GetCollection<ClientModel>("Accounts");
        }

        public async Task<ClientModel> GetAccount(string clientId)
        {
            return await _accounts.Find(a => a.ClientID == clientId).FirstOrDefaultAsync();
        }

        public async Task CreateAccount(ClientModel account)
        {
            await _accounts.InsertOneAsync(account);
        }

        public async Task<bool> ValidateCredentials(string clientId, string password)
        {
            var account = await GetAccount(clientId);
            return account != null && account.Password == password;
        }
    }
}