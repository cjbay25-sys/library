
using library.Data;
using library.Models;
using Microsoft.AspNetCore.Mvc;

namespace library.Controllers
{
    public class BooksController : Controller
    {
        private readonly BookRepository _repository;

        public BooksController(BookRepository repository)
        {
            _repository = repository;
        }

        // Display all books
        public async Task<IActionResult> Index()
        {
            var books = await _repository.GetAllAsync();
            return View(books);
        }

        // Display the Add Book form
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Book());
        }

        // Save the new book to the database
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Book book)
        {
            // Check that the price is positive.
            if (book.Price.HasValue &&
                (book.Price.Value < 0.01m || book.Price.Value > 99999.99m))
            {
                ModelState.AddModelError(
                    nameof(Book.Price),
                    "The price must be from 0.01 to 99,999.99."
                );
            }

            // Do not save if validation fails.
            if (!ModelState.IsValid)
            {
                return View(book);
            }

            await _repository.AddAsync(book);

            return RedirectToAction(nameof(Index));
        }

        // Display the Delete confirmation page
        [HttpGet]
        public async Task<IActionResult> Delete(long id)
        {
            var book = await _repository.GetByIdAsync(id);

            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // Delete the selected book
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var book = await _repository.GetByIdAsync(id);

            if (book == null)
            {
                return NotFound();
            }

            await _repository.DeleteAsync(id);

            return RedirectToAction(nameof(Index));
        }
    }
}
