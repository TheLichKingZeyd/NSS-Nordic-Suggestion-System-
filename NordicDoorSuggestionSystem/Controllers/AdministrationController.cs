﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NordicDoorSuggestionSystem.Entities;
using NordicDoorSuggestionSystem.Repositories;
using System.Data;
using NordicDoorSuggestionSystem.DataAccess;
using NordicDoorSuggestionSystem.Models;
using NordicDoorSuggestionSystem.Models.Administrate;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using NordicDoorSuggestionSystem.Models.Account;
using System.Linq.Expressions;

namespace bacit_dotnet.MVC.Controllers
{

    public class AdministrationController : Controller
    {

        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmployeeRepository employeeRepository;
        private readonly ITeamRepository _teamRepository;
        private readonly IDepartmentRepository _departmentRepository;

        private readonly DataContext _context;

        public AdministrationController(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            IEmployeeRepository employeeRepository,
            DataContext context,
            ITeamRepository teamRepository,
            IDepartmentRepository departmentRepository)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            this.employeeRepository = employeeRepository;
            _context = context;
            _teamRepository = teamRepository;
            this._departmentRepository = departmentRepository;
        }

        [Authorize(Roles = "Administrator,Team Leder")]
        public IActionResult Index()
        {
            return View();
        }

        [Authorize(Policy = "Administrator")]
        [HttpGet]
        public IActionResult Users()
        {
            var users = _userManager.Users;
            return View(users);
        }

        [Authorize(Policy = "Administrator")]
        [HttpGet]        
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            List<SelectListItem> listItems = new List<SelectListItem>();
            listItems.Add(new SelectListItem()
            {
                Value = "Standard Bruker",
                Text = "Standard Bruker"
            });
            listItems.Add(new SelectListItem()
            {
                Value = "Team Leder",
                Text = "Team Leder"
            });
            listItems.Add(new SelectListItem()
            {
                Value = "Administrator",
                Text = "Administrator"
            });
            var userRoles = await _userManager.GetRolesAsync(user);
            var toEdit = new UserEditViewModel
            {
                Id = user.Id,
                EmployeeNumber = user.EmployeeNumber.ToString(),
                FirstName = user.FirstName,
                LastName = user.LastName,
                RoleList = listItems,
                PreviousRole = userRoles.ElementAt(0),
            };
            return View(toEdit);
        }

        [Authorize(Policy = "Administrator")]
        [HttpPost]        
        public async Task<IActionResult> EditUser(UserEditViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Role = model.RoleSelected;
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                var employee = employeeRepository.GetEmployeeByNumber(user.EmployeeNumber);
                Employee updatedEmployee = new Employee
                {
                    EmployeeNumber = user.EmployeeNumber,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = model.RoleSelected,
                    ProfilePicture = employee.ProfilePicture,
                    CreatedSuggestions = employee.CreatedSuggestions,
                    CompletedSuggestions = employee.CompletedSuggestions
                };
                employeeRepository.Update(updatedEmployee);
                if (model.PreviousRole != model.RoleSelected)
                {
                    await _userManager.AddToRoleAsync(user, model.RoleSelected);
                    await _userManager.RemoveFromRoleAsync(user, model.PreviousRole);
                }
                return RedirectToAction("Users");
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View(model);
        }

        [Authorize(Policy = "Administrator")]
        [HttpGet]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            var employee = employeeRepository.GetEmployeeByNumber(Int32.Parse(id));
            if (user == null)
            {
                return View("Denne brukeren eksisterer ikke");
            }
            else
            {
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    employeeRepository.Delete(employee);
                    return RedirectToAction("Users");
                }
                else
                {
                    return RedirectToAction("Users");
                }
            }
        }

        [Authorize(Roles = "Administrator,Team Leder")]
        [HttpGet]
        public IActionResult Teams()
        {
            var teams = _context.Team.ToList();
            List<TeamsViewModel> teamsToShow = new List<TeamsViewModel>();
            for (var i = 0; i < teams.Count(); i++)
            {
                var leader = employeeRepository.GetEmployeeByNumber(teams[i].TeamLeader.Value);
                var department = _departmentRepository.GetDepartmentByID(teams[i].DepartmentID.Value);
                var team = new TeamsViewModel
                {
                    TeamID = teams[i].TeamID,
                    TeamName = teams[i].TeamName,
                    TeamLeader = leader.LastName + ", " + leader.FirstName,
                    TeamSgstnCount = teams[i].TeamSgstnCount.Value,
                    DepartmentName = department.DepartmentName
                };
                teamsToShow.Add(team);
            }
            return View(teamsToShow);
        }

        [Authorize(Roles = "Administrator,Team Leder")]
        [HttpGet]
        public IActionResult CreateTeam()
        {
            var departments = _context.Departments.ToList();
            List<SelectListItem> departmentsItems = new List<SelectListItem>();
            for (var i = 0; i < departments.Count(); i++)
            {
                
                departmentsItems.Add(new SelectListItem()
                {
                    Value = departments[i].DepartmentID.ToString(),
                    Text = departments[i].DepartmentName
                });
            }
            var leaders = _context.Employees.Where(x => x.Role.Equals("Team Leder")).ToList();
            var moreLeaders = _context.Employees.Where(x => x.Role.Equals("Administrator")).ToList();
            for (var i = 0; i < moreLeaders.Count(); i++)
            {
                leaders.Add(moreLeaders[i]);
            }
            for (var i = leaders.Count()-1; i >= 0; i--)
            {
                if (leaders[i].TeamID != null)
                {
                    leaders.Remove(leaders[i]);                    
                }
            }
            List<SelectListItem> leadersItems = new List<SelectListItem>();
            for (var i = 0; i < leaders.Count(); i++)
            {

                leadersItems.Add(new SelectListItem()
                {
                    Value = leaders[i].EmployeeNumber.ToString(),
                    Text = leaders[i].LastName + ", " + leaders[i].FirstName
                });
            }
            var createTeam = new CreateTeamViewModel
            {
                DepartmentList = departmentsItems,
                LeaderList = leadersItems
            };
            return View(createTeam);
        }

        [Authorize(Roles = "Administrator,Team Leder")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTeam(CreateTeamViewModel createTeamViewModel)
        {
            var leader = employeeRepository.GetEmployeeByNumber(Int32.Parse(createTeamViewModel.LeaderSelected));
            var department = _departmentRepository.GetDepartmentByID(Int32.Parse(createTeamViewModel.DepartmentSelected));
            if (ModelState.IsValid && leader.TeamID == null)
            {
                var newTeam = new Team {
                    TeamName = createTeamViewModel.TeamName,
                    TeamLeader = leader.EmployeeNumber,
                    DepartmentID = department.DepartmentID,
                    TeamSgstnCount = 0
                };
                department.TeamCount++;
                await _teamRepository.AddTeam(newTeam);
                await _departmentRepository.Update(department);
                leader.TeamID = newTeam.TeamID;
                employeeRepository.Update(leader);
                return RedirectToAction(nameof(Teams));
            }
            return View();
        }

        [Authorize(Roles = "Administrator,Team Leder")]
        [HttpGet]
        public async Task<IActionResult> DeleteTeam(int? id)
        {
            if (id == null || await _teamRepository.GetTeams() == null)
            {
                return NotFound();
            }
            var team = _teamRepository.GetTeam(id);
            if (team == null)
            {
                return NotFound();
            }
            else
            {
                var employees = employeeRepository.GetEmployees();
                for (int i = 0; i < employees.Count; i++)
                {
                    if (employees[i].TeamID == team.TeamID)
                    {
                        employees[i].TeamID = null;
                        employeeRepository.Update(employees[i]);
                    }
                }
                var department = _departmentRepository.GetDepartmentByID(team.DepartmentID.Value);
                department.TeamCount--;
                if (department.TeamCount < 0){
                    department.TeamCount = 0;
                }
                var leader = employeeRepository.GetEmployeeByNumber(team.TeamLeader.Value);
                leader.TeamID = null;                
                employeeRepository.Update(leader);
                await _departmentRepository.Update(department);
                await _teamRepository.DeleteTeam(team);
                await _teamRepository.SaveChanges();
            }
            return RedirectToAction(nameof(Teams));
        }

        [Authorize(Roles = "Administrator,Team Leder")]
        [HttpGet]
        public async Task<IActionResult> EditTeam(int? id)
        {
            if (id == null || await _teamRepository.GetTeams() == null)
            {
                return NotFound();
            }
            var team = _teamRepository.GetTeam(id);
            if (team == null)
            {
                return NotFound();
            }
            var leaders = _context.Employees.Where(x => x.Role.Equals("Team Leder")).ToList();
            var moreLeaders = _context.Employees.Where(x => x.Role.Equals("Administrator")).ToList();
            var teams = _context.Team.ToList();
            for (var i = 0; i < moreLeaders.Count(); i++)
            {
                leaders.Add(moreLeaders[i]);
            }
            for (var i = leaders.Count()-1; i >= 0; i--)
            {
                if (leaders[i].TeamID != null)
                {
                    leaders.Remove(leaders[i]);                    
                }
            }
            List<SelectListItem> leadersItems = new List<SelectListItem>();
            for (var i = 0; i < leaders.Count(); i++)
            {

                leadersItems.Add(new SelectListItem()
                {
                    Value = leaders[i].EmployeeNumber.ToString(),
                    Text = leaders[i].LastName + ", " + leaders[i].FirstName
                });
            }
            var editTeam = new EditTeamViewModel
            {
                TeamID = team.TeamID,
                TeamName = team.TeamName,                
                LeaderList = leadersItems
            };
            return View(editTeam);
        }

        [Authorize(Roles = "Administrator,Team Leder")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTeam(EditTeamViewModel editTeamViewModel)
        {
            var team = _teamRepository.GetTeam(editTeamViewModel.TeamID);
            if ( team == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    team.TeamName = editTeamViewModel.TeamName;
                    var leader = employeeRepository.GetEmployeeByNumber(Int32.Parse(editTeamViewModel.LeaderSelected));
                    team.TeamLeader = leader.EmployeeNumber;

                    await _teamRepository.UpdateTeam(team);
                    await _teamRepository.SaveChanges();
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw;
                }
                return RedirectToAction(nameof(Teams));
            }
            return View();
        }


        [Authorize(Roles = "Administrator,Team Leder")]
        [HttpGet]
        public IActionResult EditTeamMembers()
        {
            EditTeamMemberViewModel vm = new EditTeamMemberViewModel();
            var teams = _context.Team.ToList();
            var employees = _context.Employees.ToList();
            vm.EmployeeList = employees;
            vm.TeamList = teams;
            return View(vm);
        }

        [Authorize(Roles = "Administrator,Team Leder")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTeamMembers(DetailMemberViewModel detailMemberViewModel)
        {
            var employees = _context.Employees.ToList();
            foreach (Employee employee in employees) 
            { 
                foreach (Employee updated in detailMemberViewModel.EmployeeList)
                {
                    if (employee.EmployeeNumber == updated.EmployeeNumber && employee.TeamID != updated.TeamID)
                    {
                        employeeRepository.Update(updated);
                    }
                }
            }
            return RedirectToAction("EditTeamMembers");
        }

        [Authorize(Roles = "Administrator,Team Leder")]
        [HttpGet]
        public async Task<IActionResult> DetailsMembers(int id)
        {
            var team = _teamRepository.GetTeam(id);
            DetailMemberViewModel vm = new DetailMemberViewModel();
            if (team == null)
            {
                return NotFound();
            }

            vm.TeamName = team.TeamName;
            vm.TeamID = team.TeamID;

            var employees = _context.Employees.Where(d => d.TeamID.Equals(team.TeamID)).ToList();
            vm.EmployeeList = employees;

            return View(vm);
        }

        [Authorize(Roles = "Administrator,Team Leder")]
        public async Task<IActionResult> MittTeam()
        {
            var currentUser = await _userManager.GetUserAsync(HttpContext.User);
            MittTeamViewModel vm = new MittTeamViewModel();
            if (currentUser == null)
            {
                return NotFound();
            }
            vm.EmployeeNumber = currentUser.EmployeeNumber;
            var mittTeam = _context.Employees.Where(e => e.EmployeeNumber.Equals(vm.EmployeeNumber)).Select(e => e.TeamID).FirstOrDefault();
            vm.TeamID = mittTeam;
            var employees = _context.Employees.Where(e => e.TeamID.Equals(vm.TeamID)).ToList();
            var teamname = _context.Team.Where(e => e.TeamID.Equals(vm.TeamID)).Select(e => e.TeamName).FirstOrDefault();
            vm.TeamName = teamname;
            vm.EmployeeTeamList = employees;
            return View(vm);
        }

        [Authorize(Policy = "Administrator")]
        [HttpGet]
        public IActionResult Departments()
        {
            var departments = _context.Departments;
            return View(departments);
        }

        [Authorize(Policy = "Administrator")]
        [HttpGet]
        public IActionResult CreateDepartment()
        {
            return View();
        }

        [Authorize(Policy = "Administrator")]
        [HttpPost]
        public async Task<IActionResult> CreateDepartment(CreateDepartmentViewModel createDepViewModel)
        {

            if (ModelState.IsValid)
            {
                var newDepartment = new Department
                {
                    DepartmentName = createDepViewModel.DepartmentName,
                    TeamCount = 0
                };

                _departmentRepository.Add(newDepartment);
                return RedirectToAction(nameof(Departments));
            }
            return View();
        }

        [Authorize(Policy = "Administrator")]
        [HttpGet]
        public IActionResult EditDepartment(int id)
        {
            var department = _departmentRepository.GetDepartmentByID(id);
            var departmentToChange = new EditDepartmentViewModel
            {
                DepartmentID = department.DepartmentID,
                DepartmentName = department.DepartmentName,
                TeamCount = department.TeamCount.Value
            };
            return View(departmentToChange);
        }

        [Authorize(Policy = "Administrator")]
        [HttpPost]
        public async Task<IActionResult> EditDepartment(EditDepartmentViewModel editedDepartment)
        {
            var changedDepartment = new Department
            {
                DepartmentID = editedDepartment.DepartmentID,
                DepartmentName = editedDepartment.DepartmentName,
                TeamCount = editedDepartment.TeamCount
            };
            await _departmentRepository.Update(changedDepartment);
            await _departmentRepository.SaveChanges();
            return RedirectToAction("Departments");
        }

        [Authorize(Policy = "Administrator")]
        [HttpGet]
        public IActionResult DetailDepartment(int id)
        {
            var department = _departmentRepository.GetDepartmentByID(id);
            var teams = _context.Team.Where(e => e.DepartmentID.Equals(id)).ToList();
            //var teams = _teamRepository.GetTeams();
            var departmentToShow = new DetailDepartmentViewModel
            {
                DepartmentName = department.DepartmentName,
                TeamList = teams
            };
            return View(departmentToShow);
        }

        [Authorize(Policy = "Administrator")]
        [HttpGet]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            var department = _departmentRepository.GetDepartmentByID(id);
            if (department == null)
            {
                return View("Denne avdelingen eksisterer ikke");
            }
            else
            {
                await _departmentRepository.Delete(department);
                await _departmentRepository.SaveChanges();
                return RedirectToAction("Departments");
            }
        }
    }
}
