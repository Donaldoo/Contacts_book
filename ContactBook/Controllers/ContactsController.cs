using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using ContactBook.Api;
using ContactBook.Data;
using ContactBook.Models;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContactBook.Controllers
{
    public class ContactsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ContactsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin, User")]
        // GET: Contacts
        public async Task<IActionResult> Index()
        {
            var contacts = await _context.Contacts.Include(c => c.Email).ToListAsync();
            return View(contacts);
        }

        // Global variable
        private static List<Contacts> _contacts;

        //Search functionality
        [Authorize(Roles = "Admin, User")]
        [HttpGet]
        public async Task<IActionResult> Index(string ContactSearch)
        {
            ViewData["GetContactDetails"] = ContactSearch;
            var contactQuery = from x in _context.Contacts select x;

            if (!String.IsNullOrEmpty(ContactSearch))
            {
                contactQuery = contactQuery.Where(x => x.Name.Contains(ContactSearch) || x.Lastname.Contains(ContactSearch) ||
                                                  x.Address.Contains(ContactSearch) || /*x.Email.Contains(ContactSearch) ||*/ x.Phone.Contains(ContactSearch));
            }

            _contacts = await contactQuery.Include(c => c.Email).AsNoTracking().ToListAsync();
            return View(_contacts);
        }



        // import from csv
        [HttpPost]
        public async Task<IActionResult> ImportCsv(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("The file must not be null");
            }
            // Read the CSV file
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                stream.Position = 0;

                file = null;

                using (var reader = new StreamReader(stream))
                {
                    var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        BadDataFound = null,
                        HasHeaderRecord = true,
                        IgnoreBlankLines = true,
                        MissingFieldFound = null,
                        HeaderValidated = null,
                    };

                    using (var csv = new CsvReader(reader, configuration))
                    {
                        var contacts = csv.GetRecords<CsvContactImport>().ToList();

                        foreach (var contact in contacts)
                        {
                            var newContact = new Contacts
                            {
                                Name = contact.Name,
                                Lastname = contact.Lastname,
                                Phone = contact.Phone,
                                Address = contact.Address,
                                Email = new List<Email>()
                            };

                            var emailAddresses = contact.Emails.Split(';');

                            foreach (var emailAddress in emailAddresses)
                            {
                                if (!string.IsNullOrEmpty(emailAddress))
                                {
                                    var email = new Email
                                    {
                                        ContactId = newContact.Id,
                                        EmailAddress = emailAddress.Trim()
                                    };
                                    newContact.Email.Add(email);
                                    await _context.Emails.AddAsync(email);
                                }
                            }

                            await _context.Contacts.AddAsync(newContact);
                        }

                        await _context.SaveChangesAsync();
                    }
                }
            }

            return RedirectToAction("Index");
        }






        //Download to csv
        public IActionResult DownloadSearchResults()
        {

            if (_contacts != null)
            {

                var csv = new StringBuilder();
                csv.AppendLine("ID, Name, Lastname, Phone, Address, Emails");
                foreach (var contact in _contacts)
                {
                    var emails = string.Join("; ", contact.Email.Select(e => e.EmailAddress));
                    csv.AppendLine($"{contact.Id},{contact.Name},{contact.Lastname},{contact.Phone},{contact.Address}, {emails}");
                }

                var csvBytes = Encoding.UTF8.GetBytes(csv.ToString());
                var csvFilename = "search_results.csv";
                var csvMimeType = "text/csv";
                return File(csvBytes, csvMimeType, csvFilename);
            }
            else
            {
                return RedirectToAction("Index");
            }
        }


        //Download to excel
        public IActionResult DownloadToExel()
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("_contacts");
                var currentRow = 1;
                worksheet.Cell(currentRow, 1).Value = "Id";
                worksheet.Cell(currentRow, 2).Value = "First Name";
                worksheet.Cell(currentRow, 3).Value = "Last Name";
                worksheet.Cell(currentRow, 4).Value = "Phone Number";
               // worksheet.Cell(currentRow, 5).Value = "Email";
                worksheet.Cell(currentRow, 6).Value = "Address";
                foreach (var contact in _contacts)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = contact.Id;
                    worksheet.Cell(currentRow, 2).Value = contact.Name;
                    worksheet.Cell(currentRow, 3).Value = contact.Lastname;
                    worksheet.Cell(currentRow, 4).Value = contact.Phone;
                //    worksheet.Cell(currentRow, 5).Value = contact.Email;
                    worksheet.Cell(currentRow, 6).Value = contact.Address;
                }
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "search_results.xlsx");
                }
            }
        }


        [Authorize(Roles = "Admin, User")]
        // GET: Contacts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Contacts == null)
            {
                return NotFound();
            }

            var contacts = await _context.Contacts.Include(e => e.Email)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contacts == null)
            {
                return NotFound();
            }

            return View(contacts);
        }


        [Authorize(Roles = "Admin")]
        // GET: Contacts/Create
        public IActionResult Create()
        {
            return View();
        }


        [Authorize(Roles = "Admin")]
        // POST: Contacts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Contacts contacts, List<string> emails)
        {
            if (ModelState.IsValid)
            {
                _context.Add(contacts);
                await _context.SaveChangesAsync();

                

                foreach (var emailAddress in emails)
                {
                    if (!string.IsNullOrEmpty(emailAddress))
                    {
                        var email = new Email
                        {
                            EmailAddress = emailAddress,
                            ContactId = contacts.Id
                        };
                        contacts.Email.Add(email);
                        _context.Add(email);
                    }
                }

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            return View(contacts);
        }


        [Authorize(Roles = "Admin")]
        // GET: Contacts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Contacts == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts.AsNoTracking()
                .Where(c => c.Id == id)
                .Include(c => c.Email)
                .SingleOrDefaultAsync();

            return contact == null ? NotFound() : View(contact);
        }

        [Authorize(Roles = "Admin")]
        // POST: Contacts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditContactsRequest request)
        {
            if (id != request.Id)
            {
                return NotFound();
            }

            foreach (var email in request.Emails)
            {
                email.Contacts = null;
            }


            var contacts = await _context.Contacts.AsNoTracking().Include(c => c.Email).FirstOrDefaultAsync(c => c.Id == id);


            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(contacts);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ContactsExists(contacts.Id))
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
            return View(contacts);
        }


        [Authorize(Roles = "Admin")]
        // GET: Contacts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Contacts == null)
            {
                return NotFound();
            }

            var contacts = await _context.Contacts.Include(e => e.Email)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contacts == null)
            {
                return NotFound();
            }

            return View(contacts);
        }


        [Authorize(Roles = "Admin")]
        // POST: Contacts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Contacts == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Contacts'  is null.");
            }
            var contacts = await _context.Contacts.Include(e => e.Email).FirstOrDefaultAsync(c => c.Id == id);
            if (contacts != null)
            {
                _context.Contacts.Remove(contacts);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ContactsExists(int id)
        {
          return _context.Contacts.Any(e => e.Id == id);
        }
    }
}
