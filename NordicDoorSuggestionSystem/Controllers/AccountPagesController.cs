﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;


namespace NordicDoorSuggestionSystem.Controllers
{
    public class AccountPagesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult MyTeam()
        {
            return View();
        }
    }
}
