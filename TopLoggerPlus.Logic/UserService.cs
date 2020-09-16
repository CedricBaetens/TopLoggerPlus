using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TopLoggerPlus.ApiWrapper;
using TopLoggerPlus.Logic.Model;

namespace TopLoggerPlus.Logic
{
    public class UserService
    {
        private readonly TopLoggerService _topLoggerService;

        public UserService(TopLoggerService topLoggerService)
        {
            _topLoggerService = topLoggerService;
        }

        public async Task<List<User>> GetUsers(int gymId)
        {
            var users = await _topLoggerService.GetUsers(gymId);
            return users.Select(x => new User()
            {
                Id = x.uid.ToString(),
                Name = x.full_name
            }).ToList();
        }
    }
}
