using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Intalk.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using static Intalk.Models.UserServer;

namespace Intalk.RealTime
{
    [Authorize]
    public class InTalkHub : Hub
    {

        private IServerRepository _serverRepo;

        public InTalkHub (IServerRepository serverRepo)
        {
            _serverRepo = serverRepo;
        }

        public override async Task OnConnectedAsync()
        {
            string userId = this.Context.UserIdentifier;
            var servers = await _serverRepo.GetUserServers(userId);
            string _tempServerID;
            foreach (var server in servers)
            {
                _tempServerID = server.Id.ToString();
                UserManager.userGroups.AddUserToGroup(userId, _tempServerID);
                await this.Clients.Groups(_tempServerID)
                    .SendAsync("ReceiveUserStatus", userId, true);
            }
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Sender gets a list of all of the users that are online on newServer, and also
        /// subscribes to listen for user status changes for this one server.
        /// It unsubscribes from the oldServer.
        /// </summary>
        /// <param name="newServer"></param>
        /// <param name="oldServer"></param>
        public async Task RejoinServer(string newServer, string oldServer = null)
        {
            if (! await this.JoinServer(newServer, oldServer)){
                return; // Failed to join server.
            }

            var onlineUsers = UserManager.GetOnlineUsersFromServers(newServer);
            if (onlineUsers != null) {
                await this.Clients.Caller.SendAsync("ReceiveAllOnlineUsers", onlineUsers);
            } else {
                await this.Clients.Caller.SendAsync( "ReceiveAllOnlineUsers", Array.Empty<string>());
            }
        }

        public async Task<bool> JoinServer(string newServer, string oldServer = null)
        {
            string userId = this.Context.UserIdentifier;
            if (! await _serverRepo.userIsMember(userId, long.Parse(newServer))){
                return false; // Leave if the user is not a member of this server.
            }

            if ( oldServer != null ){
                await this.Groups.RemoveFromGroupAsync(
                    this.Context.ConnectionId, oldServer);
            }
            await this.Groups.AddToGroupAsync(this.Context.ConnectionId, newServer);
            await this.Clients.Caller.SendAsync("ServerJoined", newServer);

            return true;
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            // Remove user from servers, and notify them that the user went offline.
            string userId = this.Context.UserIdentifier;
            var serverIds = UserManager.RemoveUserFromAllGroupsAndGetServers(userId);
            NotifyServerMembers(serverIds, isOnline: false);

            return base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Notifies all the members of all the servers in the list,
        /// that a user went online or offline.
        /// This method should be calles from the AuthController,
        /// when the user is authenticated.
        /// </summary>
        /// <param name="serverIds">Ids of servers that the user is a member of.</param>
        /// <param name="isOnline">Wether the user just went online or offline.</param>
        private void NotifyServerMembers(List<string> serverIds, bool isOnline)
        {
            string userId = this.Context.UserIdentifier;
            foreach (string group in serverIds){
                UserManager.userGroups.AddUserToGroup(userId, group);
                Clients.Group(group).SendAsync("ReceiveUserStatus", userId, isOnline);
            }
        } 

        /// <summary>
        /// Notifies server memebers that a user has changed roles.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="serverId"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        public Task ChangeRole(string serverId, Roles role)
        {
            string userId = this.Context.UserIdentifier;
            return Clients.Group(serverId).SendAsync("Role", serverId, userId, role);
        }
    }
}