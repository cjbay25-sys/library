using Npgsql;
using library.Data;
using library.Models;
using Microsoft.AspNetCore.Mvc;

namespace library.Controllers
{
    public class MembersController : Controller
    {
        private static readonly string[] MemberTypes = { "Regular", "Student", "Faculty" };

        private readonly MemberRepository _members;
        private readonly LoanRepository _loans;

        // ASP.NET Core hands in the repositories, because Program.cs registered them.
        public MembersController(MemberRepository members, LoanRepository loans)
        {
            _members = members;
            _loans = loans;
        }

        // GET: /Members
        public async Task<IActionResult> Index()
        {
            var members = await _members.GetAllAsync();
            return View(members);
        }

        // GET: /Members/Create
        public IActionResult Create()
        {
            return View(new Member());
        }

        // POST: /Members/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Member member)
        {
            CheckMemberType(member);

            if (!ModelState.IsValid)
            {
                return View(member);
            }

            try
            {
                await _members.AddAsync(member);
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                ModelState.AddModelError(nameof(Member.Email), "Another member already uses this email.");
                return View(member);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Members/Edit/5
        public async Task<IActionResult> Edit(long id)
        {
            var member = await _members.GetByIdAsync(id);

            if (member == null)
            {
                return NotFound();
            }

            return View(member);
        }

        // POST: /Members/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Member member)
        {
            CheckMemberType(member);

            if (!ModelState.IsValid)
            {
                return View(member);
            }

            try
            {
                await _members.UpdateAsync(member);
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                ModelState.AddModelError(nameof(Member.Email), "Another member already uses this email.");
                return View(member);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Members/Delete/5
        public async Task<IActionResult> Delete(long id)
        {
            var member = await _members.GetByIdAsync(id);

            if (member == null)
            {
                return NotFound();
            }

            return View(member);
        }

        // POST: /Members/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id);
                // GET /Members/Details/5 : one member and their loans.
    public async Task<IActionResult> Details(long id)
        {
            // 1. Load the member.
            if (/* 2. there is no such member */)
            {
                // 3. Answer "not found".
            }

            // 4. Put the member and their loans together for the page.
            // 5. Show the page.
        }
        {
            try
            {
                await _members.DeleteAsync(id);
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
            {
                var member = await _members.GetByIdAsync(id);

                if (member == null)
                {
                    return NotFound();
                }

                ViewBag.Error = "This member has loans on record, so the database will not delete them.";
                return View("Delete", member);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Members/Details/5 : one member and their loans.
        public async Task<IActionResult> Details(long id)
        {
            var member = await _members.GetByIdAsync(id);

            if (member == null)
            {
                return NotFound();
            }

            var details = new MemberDetails
            {
                Member = member,
                Loans = await _loans.GetByMemberAsync(id)
            };

            return View(details);
        }

        // Adds a visible error when the member type is not one the database allows.
        private void CheckMemberType(Member member)
        {
            if (!MemberTypes.Contains(member.MemberType))
            {
                ModelState.AddModelError(nameof(Member.MemberType), "Choose Regular, Student or Faculty.");
            }
        }
    }
}