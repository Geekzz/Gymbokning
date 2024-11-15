using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Gymbokning.Data;
using Gymbokning.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace Gymbokning.Controllers
{
    public class GymClassesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public GymClassesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize]
        public async Task<IActionResult> BookingToggle(int? id)
        {
            // Fetch the gym class with user bookings
            var gymClass = await _context.GymClasses
                .Include(g => g.UserGymClasses)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (gymClass == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            // Check if the user is already booked for this gym class
            var userBooking = gymClass.UserGymClasses
                .SingleOrDefault(ug => ug.ApplicationUserId == userId);

            // If the user is already booked, unbook
            if (userBooking != null)
            {
                //return RedirectToAction(nameof(Index)); 
                _context.Remove(userBooking);
            }
            else
            {
                // Add a new booking for the user
                _context.Add(new ApplicationUserGymClass
                {
                    ApplicationUserId = userId,
                    GymClassId = gymClass.Id
                });
            }

            // Save changes to the database
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index)); // Redirect to the index page after booking
        }




        // GET: GymClasses
        public async Task<IActionResult> Index(bool showPastClasses = false)
        {
            var currentDate = DateTime.Now;

            // Filtrera gympassen direkt innan vi inkluderar relaterad data
            IQueryable<GymClass> gymClassesQuery;

            if (!showPastClasses)
            {
                gymClassesQuery = _context.GymClasses
                    .Where(g => g.StartTime > currentDate); // Filtrera på StartTime först
            }
            else
            {
                gymClassesQuery = _context.GymClasses; // Om vi vill visa alla, inga filter
            }

            // Inkludera relaterade användare (UserGymClasses och ApplicationUser) efter filtrering
            gymClassesQuery = gymClassesQuery
                .Include(g => g.UserGymClasses)
                .ThenInclude(ug => ug.ApplicationUser); // Inkludera användare

            // Hämta gympassen och returnera till vyn
            var gymClasses = await gymClassesQuery.ToListAsync();
            return View(gymClasses);
        }

        // Action to display booked classes
        public async Task<IActionResult> BookedClasses()
        {
            var userId = User.Identity?.Name;  // Get the logged-in user's ID
            if (userId == null)
            {
                return RedirectToAction("Index", "Home");  // Redirect if user is not authenticated
            }

            // Filter gym classes that the user has booked, no date filter
            var bookedClassesQuery = _context.GymClasses
                .Where(g => g.UserGymClasses.Any(ug => ug.ApplicationUser.UserName == userId)) // Classes the user has booked
                .Include(g => g.UserGymClasses)
                .ThenInclude(ug => ug.ApplicationUser); // Include related user data

            var bookedClasses = await bookedClassesQuery.ToListAsync();

            return View(bookedClasses);  // Pass the booked classes to the view
        }


        // Action to display history (past classes)
        public async Task<IActionResult> History()
        {
            var userId = User.Identity?.Name;  // Get the logged-in user's ID
            if (userId == null)
            {
                return RedirectToAction("Index", "Home");  // Redirect if user is not authenticated
            }

            var currentDate = DateTime.Now;

            // Filter gym classes that the user has attended (past classes only)
            var historyQuery = _context.GymClasses
                .Where(g => g.UserGymClasses.Any(ug => ug.ApplicationUser.UserName == userId) && g.StartTime < currentDate) // Classes the user has attended
                .Include(g => g.UserGymClasses)
                .ThenInclude(ug => ug.ApplicationUser); // Include related user data

            var historyClasses = await historyQuery.ToListAsync();

            return View(historyClasses);  // Pass the history classes to the view
        }


        // GET: GymClasses/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gymClass = await _context.GymClasses
                .Include(g => g.UserGymClasses)  // Include the bookings (UserGymClasses)
                .ThenInclude(ug => ug.ApplicationUser)  // Include the ApplicationUser (User) for each booking
                .FirstOrDefaultAsync(g => g.Id == id);

            if (gymClass == null)
            {
                return NotFound();
            }

            return View(gymClass);
        }

        // GET: GymClasses/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: GymClasses/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Id,Name,StartTime,Duration,Description")] GymClass gymClass)
        {
            if (ModelState.IsValid)
            {
                _context.Add(gymClass);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(gymClass);
        }

        // GET: GymClasses/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gymClass = await _context.GymClasses.FindAsync(id);
            if (gymClass == null)
            {
                return NotFound();
            }
            return View(gymClass);
        }

        // POST: GymClasses/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,StartTime,Duration,Description")] GymClass gymClass)
        {
            if (id != gymClass.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(gymClass);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GymClassExists(gymClass.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(gymClass);
        }

        // GET: GymClasses/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gymClass = await _context.GymClasses
                .FirstOrDefaultAsync(m => m.Id == id);
            if (gymClass == null)
            {
                return NotFound();
            }

            return View(gymClass);
        }

        // POST: GymClasses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var gymClass = await _context.GymClasses.FindAsync(id);
            if (gymClass != null)
            {
                _context.GymClasses.Remove(gymClass);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GymClassExists(int id)
        {
            return _context.GymClasses.Any(e => e.Id == id);
        }
    }
}
