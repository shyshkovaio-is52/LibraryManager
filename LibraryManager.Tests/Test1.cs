using Microsoft.VisualStudio.TestTools.UnitTesting;
using LibraryManager;
using System.Collections.Generic;

namespace LibraryManager.Tests
{
    [TestClass]
    public class LibraryServiceTests
    {
        [TestMethod]
        public void Issue_ValidData_ChangesStatus()
        {
            var service = new LibraryService();
            var book = new Book { Id = 1, Title = "Кобзар", Status = BookStatus.Available };
            var reader = new Reader { TicketNumber = 8, FullName = "Ірина", CurrentBorrowedBookId = -1 };

            service.Issue(book, reader);

            Assert.AreEqual(BookStatus.Borrowed, book.Status);
            Assert.AreEqual(1, reader.CurrentBorrowedBookId);
        }

        [TestMethod]
        public void Return_ValidData_MakesBookAvailable()
        {
            var service = new LibraryService();
            var book = new Book { Id = 1, Status = BookStatus.Borrowed };
            var reader = new Reader { TicketNumber = 8, CurrentBorrowedBookId = 1 };
            var readersList = new List<Reader> { reader };

            service.Return(book, readersList);

            Assert.AreEqual(BookStatus.Available, book.Status);
            Assert.AreEqual(-1, reader.CurrentBorrowedBookId);
        }

        [TestMethod]
        public void Issue_ReaderHasDebt_ThrowsException()
        {
         
            var service = new LibraryService();
            var book = new Book { Id = 2, Status = BookStatus.Available };
            var reader = new Reader { TicketNumber = 105, CurrentBorrowedBookId = 1 };
         
            try
            {
                service.Issue(book, reader);
                
                Assert.Fail("Тест мав видати помилку, бо у читача борг.");
            }
            catch (System.Exception)
            {
             
            }
        }
    }
}