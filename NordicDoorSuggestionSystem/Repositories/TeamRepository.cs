﻿using NordicDoorSuggestionSystem.DataAccess;
using NordicDoorSuggestionSystem.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace NordicDoorSuggestionSystem.Repositories
{
    public class TeamRepository : ITeamRepository
    {
        private readonly DataContext _context;
        private readonly UserManager<User> _userManager;

        public TeamRepository(DataContext dataContext, UserManager<User>userManager)
        {
            this._context = dataContext;
            this._userManager = userManager;
        }

        public async Task<Team> GetTeam(int? TeamID)
        {
            if (TeamID == null) 
                throw new NullReferenceException("TeamID can not be null");

            var team = await _context.Team.FindAsync(TeamID);
            
            if (team == null) 
                return null;

            return team;
        }

        public Task<List<Team>> GetTeams()
        {
            return _context.Team.ToListAsync();
        }

        public async Task DeleteTeam(Team team)
        {
            _context.Team.Remove(team);
        }
        public async Task SaveChanges(){
            await _context.SaveChangesAsync();
        }

        public async Task Update(Team team) 
        {
            _context.Team.Update(team);
        }
        /* public async Task<List<Team>> QueryTeam(int employeeNumber)
        {
            return await _context.Employees.Where(s => s.TeamID!.Equals()).ToListAsync();
        }*/
    }
}
