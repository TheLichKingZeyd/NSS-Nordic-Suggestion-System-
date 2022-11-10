using NordicDoorSuggestionSystem.Entities;
using NordicDoorSuggestionSystem.Models.Employees;
using NordicDoorSuggestionSystem.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NordicDoorSuggestionSystem.Models.Account;

namespace NordicDoorSuggestionSystem.Controllers
{
    // [Authorize]
    public class AdministrateEmployeeAccountController : Controller
    {
        private readonly IEmployeeRepository employeeRepository;

        public AdministrateEmployeeAccountController(IEmployeeRepository employeeRepository)
        {
            this.employeeRepository = employeeRepository;
        }
        [HttpGet]
        public IActionResult Index(int? employeeNumber)
        {
            var model = new EmployeeAccountViewModel();
            model.Employee = employeeRepository.GetEmployees();
            if (employeeNumber != null)
            {
                var currentUser = model.Employee.FirstOrDefault(x => x.EmployeeNumber == employeeNumber);
                if (currentUser != null)
                {
                    model.EmployeeNumber = currentUser.EmployeeNumber;
                    model.FirstName = currentUser.FirstName;
                    model.LastName = currentUser.LastName;
                    model.Role = currentUser.Role;
                }
            }
            return View(model);
        }

        [HttpPost]
        public IActionResult Save(EmployeeAccountViewModel model)
        {
            Employee newEmployeeAccount = new Employee
            {
                EmployeeNumber = model.EmployeeNumber,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Role = model.Role,
            };
            var roles = new List<string>();
            if (model.Role == "Administrator")
            {
                roles.Add("Administrator");
            }
            else if (model.Role == "Team Leader")
            {
                //roles.Add();
            }
                
            employeeRepository.Add(newEmployeeAccount);
            //AccountController.Register(model, "");
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete(int EmployeeNumber)
        {
            employeeRepository.Delete(EmployeeNumber);
            return RedirectToAction("Index");
        }
    }
}
