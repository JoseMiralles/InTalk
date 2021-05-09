using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Intalk.Models;
using Intalk.Models.DTOs.Requests;
using Intalk.Models.DTOs.Responses;
using Microsoft.EntityFrameworkCore;
using static Intalk.Models.UserServer;

namespace Intalk.Data
{
    public class ServerRepository : IServerRepository, IDisposable
    {
        private ApiDbContext _context;
        private bool _disposed;

        private ApplicationUser _currentUser;

        public async Task<ApplicationUser> CurrentUser(string id)
        {
            if (_currentUser == null)
            {
                _currentUser = await _context.Users.OfType<ApplicationUser>()
                    .FirstAsync(u => u.Id == id);
            }
            return _currentUser;
        }

        public ServerRepository(ApiDbContext context)
        {
            this._context = context;
        }

        /// <summary>
        /// Creates and saves a new server, and adds the given user to it as an owner.
        /// </summary>
        /// <param name="createServerRequest"></param>
        /// <param name="userIdentifier">The identifier of the owner user</param>
        /// <returns>The Id of the new server</returns>
        public async Task<SingleServerResponseItem> CreateServer
            (CreateServerRequest createServerRequest, string userId)
        {
            // Create server
            var server = new Server
            {
                Title = createServerRequest.Title
            };

            var user = await _context.Users
                .OfType<ApplicationUser>()
                .Include(u => u.Servers)
                .FirstAsync(u => u.Id == userId);

            user.Servers.Add(server);
            await _context.SaveChangesAsync();
            await AddServerOwner(user.Id, server.Id);
            return serverToSingleServerResponseItem(server);
        }

        public async Task<SingleServerResponseItem> GetServerById(long serverId)
        {
            // TODO: Check is user is member of this server.
            Server server = await _context.Server.FindAsync(serverId);
            if (server == null) return null;
            return serverToSingleServerResponseItem(server);
        }

        public async Task<IEnumerable<MultipleUserResponseItem>> GetServerUsers(long serverId)
        {
            var server = await _context.Server
                .Include(s => s.UserServers)
                .ThenInclude(us => us.User)
                .FirstOrDefaultAsync(s => s.Id == serverId);

            IEnumerable<MultipleUserResponseItem> res = ServerToMultipleUserResponseItems(server);
            return res;
        }

        private IEnumerable<MultipleUserResponseItem> ServerToMultipleUserResponseItems(Server server)
        {
            var result = new List<MultipleUserResponseItem>();
            foreach(var userServer in server.UserServers){
                result.Add(new MultipleUserResponseItem(){
                    UserId = userServer.UserId,
                    UserName = userServer.User.UserName,
                    Role = (Roles)userServer.Role
                });
            }
            return result;
        }

        private async Task AddServerOwner(string userId, long serverId)
        {
            var userServer = await _context.UserServers.FirstAsync(
                us => us.UserId == userId && us.ServerId == serverId
            );
            userServer.Role = (int) UserServer.Roles.Owner;
            await _context.SaveChangesAsync();
        }

        public async Task<SingleServerResponseItem> DeleteServer(long serverId)
        {
            var server = await _context.Server.FindAsync(serverId);
            if (server == null) return null;
            var deleted = _context.Server.Remove(server);
            await _context.SaveChangesAsync();
            return serverToSingleServerResponseItem(server);
        }

        public async Task<Server> GetCompleteServerById(long id)
        {
            return await _context.Server.FindAsync(id);
        }

        public async Task SaveChanges()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<MultipleServersResponseItem>> GetUserServers(string userId)
        {
            var User = await _context.Users.OfType<ApplicationUser>()
                .Include(u => u.Servers)
                .FirstOrDefaultAsync(u => u.Id == userId);
            Console.WriteLine(User);
            return await Task.FromResult(
                User.Servers.Select(
                    // Convert to MultipleServersResponseItem to avoid overposting.
                    x => ServerToMultipleServersResponseItem(x)
                ).ToList()
            );
        }

        private static MultipleServersResponseItem ServerToMultipleServersResponseItem
            (Server server)
        {
            return new MultipleServersResponseItem
            {
                Id = server.Id,
                Title = server.Title
            };
        }

        private static SingleServerResponseItem serverToSingleServerResponseItem
            (Server server)
        {
            return new SingleServerResponseItem
            {
                Id = server.Id,
                Title = server.Title
            };
        }

        /// <summary>
        /// Checks to see if the user with the given id, is a owner of the server with the given id.
        /// </summary>
        /// <returns>True if the user is an owner, false otherwise.</returns>
        public async Task<bool> userIsOwner(string userId, long serverId)
        {
            var userServer = await _context.UserServers
                .FirstOrDefaultAsync(us =>
                us.UserId == userId
                && us.ServerId == serverId
                && us.Role == (int) UserServer.Roles.Owner
            );
            return userServer != null ? true : false;
        }

        /// <summary>
        /// Checks to see if the user is either a member or a owner of the server.
        /// </summary>
        /// <returns>True if the user is part of the server.</returns>
        public async Task<bool> userIsMember(string userId, long serverId)
        {
            var userServer = await _context.UserServers
                .FirstOrDefaultAsync(us =>
                us.UserId == userId
                && us.ServerId == serverId
            );
            return userServer != null ? true : false;
        }

        public void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
            }
            this._disposed = true;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}